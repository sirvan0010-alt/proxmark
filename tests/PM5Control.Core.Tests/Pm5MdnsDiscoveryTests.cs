using System.Buffers.Binary;
using System.Net;
using System.Text;
using PM5Control.Core.Discovery;

namespace PM5Control.Core.Tests;

public sealed class Pm5MdnsDiscoveryTests
{
    [Fact]
    public void BuildQueryRequestsPm5TcpDnsSdService()
    {
        var query = Pm5MdnsDiscovery.BuildQuery();

        Assert.Equal(12, query.Length - Encoding.ASCII.GetByteCount(Pm5MdnsDiscovery.ServiceType) - 5);
        Assert.Equal((ushort)1, BinaryPrimitives.ReadUInt16BigEndian(query.AsSpan(4, 2)));
        Assert.Equal((ushort)12, BinaryPrimitives.ReadUInt16BigEndian(query.AsSpan(query.Length - 4, 2)));
        Assert.Equal((ushort)1, BinaryPrimitives.ReadUInt16BigEndian(query.AsSpan(query.Length - 2, 2)));
    }

    [Fact]
    public void ParseResponseReadsCompressedPtrSrvAndAddressRecords()
    {
        var response = BuildCompressedServiceResponse();

        var services = Pm5MdnsDiscovery.ParseResponse(response);

        var service = Assert.Single(services);
        Assert.Equal("PM5._proxmark5._tcp.local", service.InstanceName);
        Assert.Equal("pm5.local", service.HostName);
        Assert.Equal(7777, service.Port);
        Assert.Contains(IPAddress.Parse("192.168.1.77"), service.Addresses);
    }

    [Fact]
    public void ParseResponseIgnoresTruncatedAndNonResponsePackets()
    {
        Assert.Empty(Pm5MdnsDiscovery.ParseResponse(new byte[8]));

        var query = Pm5MdnsDiscovery.BuildQuery();
        Assert.Empty(Pm5MdnsDiscovery.ParseResponse(query));
    }

    private static byte[] BuildCompressedServiceResponse()
    {
        var packet = new List<byte>(new byte[12]);
        packet[2] = 0x84; // response + authoritative answer
        packet[6] = 0;
        packet[7] = 1; // ANCOUNT = 1 PTR
        packet[10] = 0;
        packet[11] = 2; // ARCOUNT = 2 SRV + A

        AddName(packet, Pm5MdnsDiscovery.ServiceType);
        AddUInt16(packet, 12); // PTR
        AddUInt16(packet, 1);  // IN
        AddUInt32(packet, 120);
        var instanceData = EncodeName("PM5._proxmark5._tcp.local");
        AddUInt16(packet, (ushort)instanceData.Length);
        var instanceOffset = packet.Count;
        packet.AddRange(instanceData);

        // SRV owner is a compressed pointer to the PTR target instance name.
        AddPointer(packet, instanceOffset);
        AddUInt16(packet, 33); // SRV
        AddUInt16(packet, 1);
        AddUInt32(packet, 120);
        AddUInt16(packet, 0); // RDATA length placeholder
        var srvLengthOffset = packet.Count - 2;
        var srvDataStart = packet.Count;
        AddUInt16(packet, 0); // priority
        AddUInt16(packet, 0); // weight
        AddUInt16(packet, 7777);
        var hostOffset = packet.Count;
        AddName(packet, "pm5.local");
        BinaryPrimitives.WriteUInt16BigEndian(CollectionsMarshal.AsSpan(packet).Slice(srvLengthOffset, 2),
            (ushort)(packet.Count - srvDataStart));

        // A owner is compressed to the host name inside the SRV RDATA.
        AddPointer(packet, hostOffset);
        AddUInt16(packet, 1); // A
        AddUInt16(packet, 1);
        AddUInt32(packet, 120);
        AddUInt16(packet, 4);
        packet.AddRange(new byte[] { 192, 168, 1, 77 });

        return packet.ToArray();
    }

    private static byte[] EncodeName(string name)
    {
        var packet = new List<byte>();
        AddName(packet, name);
        return packet.ToArray();
    }

    private static void AddName(List<byte> packet, string name)
    {
        foreach (var label in name.Split('.'))
        {
            var bytes = Encoding.ASCII.GetBytes(label);
            packet.Add((byte)bytes.Length);
            packet.AddRange(bytes);
        }
        packet.Add(0);
    }

    private static void AddPointer(List<byte> packet, int offset)
    {
        packet.Add((byte)(0xC0 | (offset >> 8)));
        packet.Add((byte)(offset & 0xFF));
    }

    private static void AddUInt16(List<byte> packet, ushort value)
    {
        packet.Add((byte)(value >> 8));
        packet.Add((byte)value);
    }

    private static void AddUInt32(List<byte> packet, uint value)
    {
        packet.Add((byte)(value >> 24));
        packet.Add((byte)(value >> 16));
        packet.Add((byte)(value >> 8));
        packet.Add((byte)value);
    }
}
