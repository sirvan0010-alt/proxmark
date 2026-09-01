using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PM5Control.Core.Connections
{
    /// <summary>
    /// Klient pro iClass technologie.
    /// POZOR: ověřte syntaxi přes `-h` na vašem klientovi.
    /// </summary>
    public class HfIclassClient
    {
        private readonly IProxmarkTransport _transport;

        public HfIclassClient(IProxmarkTransport transport)
        {
            _transport = transport ?? throw new ArgumentNullException(nameof(transport));
        }

        public async Task<string> GetInfoAsync(CancellationToken ct = default)
        {
            string command = "hf iclass info\n";
            await _transport.WriteAsync(Encoding.ASCII.GetBytes(command), ct);
            var response = await _transport.ReadFrameAsync(TimeSpan.FromSeconds(5), ct);
            return response != null ? Encoding.ASCII.GetString(response.Payload) : "Timeout";
        }

        /// <summary>
        /// Příkaz: hf iclass dump -f &lt;file_name&gt;
        /// OPRAVENO — dump bere jméno souboru přes -f flag.
        /// </summary>
        public async Task<string> DumpCardAsync(string fileName = "iclass_dump", CancellationToken ct = default)
        {
            string command = $"hf iclass dump -f {fileName}\n";
            await _transport.WriteAsync(Encoding.ASCII.GetBytes(command), ct);
            var response = await _transport.ReadFrameAsync(TimeSpan.FromMinutes(5), ct);
            return response != null ? Encoding.ASCII.GetString(response.Payload) : "Timeout";
        }

        /// <summary>NEOVĚŘENO — ověřte hf iclass rdbl -h</summary>
        public async Task<string> ReadBlockAsync(int block, CancellationToken ct = default)
        {
            string command = $"hf iclass rdbl {block}\n";
            await _transport.WriteAsync(Encoding.ASCII.GetBytes(command), ct);
            var response = await _transport.ReadFrameAsync(TimeSpan.FromSeconds(5), ct);
            return response != null ? Encoding.ASCII.GetString(response.Payload) : "Timeout";
        }

        /// <summary>NEOVĚŘENO — zápisová operace; neověřovat na produkční kartě.</summary>
        public async Task<string> WriteBlockAsync(int block, byte[] data, CancellationToken ct = default)
        {
            string dataHex = BitConverter.ToString(data).Replace("-", "").ToLower();
            string command = $"hf iclass wrbl {block} {dataHex}\n";
            await _transport.WriteAsync(Encoding.ASCII.GetBytes(command), ct);
            var response = await _transport.ReadFrameAsync(TimeSpan.FromSeconds(5), ct);
            return response != null ? Encoding.ASCII.GetString(response.Payload) : "Timeout";
        }

        /// <summary>NEOVĚŘENO</summary>
        public async Task<string> SimulateAsync(byte[] uid, CancellationToken ct = default)
        {
            string uidHex = BitConverter.ToString(uid).Replace("-", "").ToLower();
            string command = $"hf iclass sim {uidHex}\n";
            await _transport.WriteAsync(Encoding.ASCII.GetBytes(command), ct);
            var response = await _transport.ReadFrameAsync(TimeSpan.FromSeconds(5), ct);
            return response != null ? Encoding.ASCII.GetString(response.Payload) : "Timeout";
        }

        public async Task<string> SniffAsync(CancellationToken ct = default)
        {
            string command = "hf iclass sniff\n";
            await _transport.WriteAsync(Encoding.ASCII.GetBytes(command), ct);
            var response = await _transport.ReadFrameAsync(TimeSpan.FromSeconds(30), ct);
            return response != null ? Encoding.ASCII.GetString(response.Payload) : "Timeout";
        }
    }
}
