using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace PM5Control.Core.Discovery;

/// <summary>
/// A DNS-SD instance advertised by a PM5 BWM mDNS responder.
/// Addresses may be empty when the responder did not include A/AAAA records;
/// callers can then resolve HostName or allow the user to enter an IP address.
/// </summary>
public sealed record Pm5MdnsService(
    string InstanceName,
    string HostName,
    int Port,
    IReadOnlyList<IPAddress> Addresses,
    IReadOnlyDictionary<string, string> TextRecords);

/// <summary>
/// Minimal DNS-SD discovery for the upstream BWM service _proxmark5._tcp.
/// Discovery is optional: older BWM images, builds with mDNS disabled, and
/// networks that block multicast may return no results.
/// </summary>
public static class Pm5MdnsDiscovery
{
    public const string ServiceType = "_proxmark5._tcp.local";
    public const int MdnsPort = 5353;
    private static readonly IPAddress MulticastAddress = IPAddress.Parse("224.0.0.251");

    public static byte[] BuildQuery()
    {
        var labels = ServiceType.Split('.');
        var nameLength = labels.Sum(label => 1 + Encoding.ASCII.GetByteCount(label)) + 1;
        var packet = new byte[12 + nameLength + 4];
        BinaryPrimitives.WriteUInt16BigEndian(packet.AsSpan(4, 2), 1); // QDCOUNT

        var offset = 12;
        foreach (var label in labels)
        {
            var bytes = Encoding.ASCII.GetBytes(label);
            if (bytes.Length is 0 or > 63)
                throw new InvalidOperationException("Invalid DNS-SD service label.");
            packet[offset++] = (byte)bytes.Length;
            bytes.CopyTo(packet.AsSpan(offset));
            offset += bytes.Length;
        }
        packet[offset++] = 0;
        BinaryPrimitives.WriteUInt16BigEndian(packet.AsSpan(offset, 2), 12); // PTR
        BinaryPrimitives.WriteUInt16BigEndian(packet.AsSpan(offset + 2, 2), 1); // IN
        return packet;
    }

    public static async Task<IReadOnlyList<Pm5MdnsService>> DiscoverAsync(
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        if (timeout <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(timeout));

        using var client = new UdpClient(AddressFamily.InterNetwork);
        client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        client.Client.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastTimeToLive, 255);
        client.Client.Bind(new IPEndPoint(IPAddress.Any, MdnsPort));
        client.JoinMulticastGroup(MulticastAddress);
        client.MulticastLoopback = false;

        await client.SendAsync(
            BuildQuery(),
            new IPEndPoint(MulticastAddress, MdnsPort),
            cancellationToken).ConfigureAwait(false);

        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(timeout);
        var found = new Dictionary<string, Pm5MdnsService>(StringComparer.OrdinalIgnoreCase);

        while (true)
        {
            UdpReceiveResult received;
            try
            {
                received = await client.ReceiveAsync(timeoutSource.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                break;
            }

            foreach (var service in ParseResponse(received.Buffer))
                found[service.InstanceName] = service;
        }

        return found.Values.ToArray();
    }

    public static IReadOnlyList<Pm5MdnsService> ParseResponse(ReadOnlySpan<byte> message)
    {
        if (message.Length < 12)
            return Array.Empty<Pm5MdnsService>();

        var flags = BinaryPrimitives.ReadUInt16BigEndian(message.Slice(2, 2));
        if ((flags & 0x8000) == 0) // QR must indicate a response
            return Array.Empty<Pm5MdnsService>();

        var questionCount = BinaryPrimitives.ReadUInt16BigEndian(message.Slice(4, 2));
        var answerCount = BinaryPrimitives.ReadUInt16BigEndian(message.Slice(6, 2));
        var authorityCount = BinaryPrimitives.ReadUInt16BigEndian(message.Slice(8, 2));
        var additionalCount = BinaryPrimitives.ReadUInt16BigEndian(message.Slice(10, 2));
        var offset = 12;

        for (var i = 0; i < questionCount; i++)
        {
            if (!TryReadName(message, ref offset, out _ ) || offset + 4 > message.Length)
                return Array.Empty<Pm5MdnsService>();
            offset += 4;
        }

        var records = new List<DnsRecord>();
        var recordCount = (int)answerCount + authorityCount + additionalCount;
        for (var i = 0; i < recordCount; i++)
        {
            if (!TryReadName(message, ref offset, out var owner) || offset + 10 > message.Length)
                return Array.Empty<Pm5MdnsService>();

            var type = BinaryPrimitives.ReadUInt16BigEndian(message.Slice(offset, 2));
            offset += 2;
            offset += 2; // class
            offset += 4; // TTL
            var dataLength = BinaryPrimitives.ReadUInt16BigEndian(message.Slice(offset, 2));
            offset += 2;
            if (offset + dataLength > message.Length)
                return Array.Empty<Pm5MdnsService>();

            records.Add(new DnsRecord(owner, type, offset, dataLength));
            offset += dataLength;
        }

        var instances = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var srvRecords = new Dictionary<string, (string Host, int Port)>(StringComparer.OrdinalIgnoreCase);
        var txtRecords = new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
        var addresses = new Dictionary<string, List<IPAddress>>(StringComparer.OrdinalIgnoreCase);

        foreach (var record in records)
        {
            if (record.Type == 12 && record.Name.Equals(ServiceType, StringComparison.OrdinalIgnoreCase))
            {
                var nameOffset = record.DataOffset;
                if (TryReadName(message, ref nameOffset, out var instance))
                    instances.Add(instance);
            }
            else if (record.Type == 33 && record.DataLength >= 7) // SRV
            {
                var port = BinaryPrimitives.ReadUInt16BigEndian(message.Slice(record.DataOffset + 4, 2));
                var nameOffset = record.DataOffset + 6;
                if (TryReadName(message, ref nameOffset, out var host))
                    srvRecords[record.Name] = (host, port);
            }
            else if (record.Type == 16) // TXT
            {
                txtRecords[record.Name] = ParseTxt(message.Slice(record.DataOffset, record.DataLength));
            }
            else if (record.Type == 1 && record.DataLength == 4) // A
            {
                AddAddress(addresses, record.Name, new IPAddress(message.Slice(record.DataOffset, 4)));
            }
            else if (record.Type == 28 && record.DataLength == 16) // AAAA
            {
                AddAddress(addresses, record.Name, new IPAddress(message.Slice(record.DataOffset, 16)));
            }
        }

        var result = new List<Pm5MdnsService>();
        foreach (var instance in instances)
        {
            if (!srvRecords.TryGetValue(instance, out var srv) || srv.Port is < 1 or > 65535)
                continue;

            var resolvedAddresses = addresses.TryGetValue(srv.Host, out var list)
                ? list.ToArray()
                : Array.Empty<IPAddress>();
            var txt = txtRecords.TryGetValue(instance, out var values)
                ? values
                : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            result.Add(new Pm5MdnsService(instance, srv.Host, srv.Port, resolvedAddresses, txt));
        }

        return result;
    }

    private static void AddAddress(Dictionary<string, List<IPAddress>> addresses, string host, IPAddress address)
    {
        if (!addresses.TryGetValue(host, out var list))
        {
            list = new List<IPAddress>();
            addresses.Add(host, list);
        }
        if (!list.Contains(address))
            list.Add(address);
    }

    private static IReadOnlyDictionary<string, string> ParseTxt(ReadOnlySpan<byte> data)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var offset = 0;
        while (offset < data.Length)
        {
            var length = data[offset++];
            if (offset + length > data.Length)
                break;

            var item = Encoding.UTF8.GetString(data.Slice(offset, length));
            offset += length;
            var separator = item.IndexOf('=');
            var key = separator < 0 ? item : item[..separator];
            var value = separator < 0 ? string.Empty : item[(separator + 1)..];
            if (key.Length > 0)
                result[key] = value;
        }
        return result;
    }

    private static bool TryReadName(ReadOnlySpan<byte> message, ref int offset, out string name)
    {
        name = string.Empty;
        var labels = new List<string>();
        var cursor = offset;
        var returnOffset = -1;
        var jumps = 0;

        while (true)
        {
            if (cursor >= message.Length)
                return false;

            var length = message[cursor++];
            if ((length & 0xC0) == 0xC0)
            {
                if (cursor >= message.Length || ++jumps > 32)
                    return false;
                var pointer = ((length & 0x3F) << 8) | message[cursor++];
                if (pointer >= message.Length)
                    return false;
                if (returnOffset < 0)
                    returnOffset = cursor;
                cursor = pointer;
                continue;
            }

            if ((length & 0xC0) != 0 || length > 63)
                return false;
            if (length == 0)
            {
                offset = returnOffset >= 0 ? returnOffset : cursor;
                name = string.Join('.', labels);
                return true;
            }

            if (cursor + length > message.Length)
                return false;
            labels.Add(Encoding.ASCII.GetString(message.Slice(cursor, length)));
            cursor += length;
        }
    }

    private sealed record DnsRecord(string Name, ushort Type, int DataOffset, int DataLength);
}
