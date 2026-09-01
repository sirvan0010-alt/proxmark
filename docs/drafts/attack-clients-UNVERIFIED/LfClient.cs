using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PM5Control.Core.Connections
{
    /// <summary>
    /// LF klient. Částečně opraveno (hid sim -r); většina metod NEOVĚŘENO — viz README.
    /// </summary>
    public class LfClient
    {
        private readonly IProxmarkTransport _transport;

        public LfClient(IProxmarkTransport transport)
        {
            _transport = transport ?? throw new ArgumentNullException(nameof(transport));
        }

        public async Task<string> DemodulateEm410xAsync(CancellationToken ct = default)
        {
            string command = "lf em 410x demod\n";
            await _transport.WriteAsync(Encoding.ASCII.GetBytes(command), ct);
            var response = await _transport.ReadFrameAsync(TimeSpan.FromSeconds(5), ct);
            return response != null ? Encoding.ASCII.GetString(response.Payload) : "Timeout";
        }

        /// <summary>NEOVĚŘENO — ověřte lf em -h</summary>
        public async Task<string> SimulateEm410xAsync(string id, CancellationToken ct = default)
        {
            string command = $"lf em 410x sim {id}\n";
            await _transport.WriteAsync(Encoding.ASCII.GetBytes(command), ct);
            var response = await _transport.ReadFrameAsync(TimeSpan.FromSeconds(5), ct);
            return response != null ? Encoding.ASCII.GetString(response.Payload) : "Timeout";
        }

        /// <summary>NEOVĚŘENO</summary>
        public async Task<string> BruteForceEm410xAsync(string start, string end, CancellationToken ct = default)
        {
            string command = $"lf em 410x brute {start} {end}\n";
            await _transport.WriteAsync(Encoding.ASCII.GetBytes(command), ct);
            var response = await _transport.ReadFrameAsync(TimeSpan.FromMinutes(10), ct);
            return response != null ? Encoding.ASCII.GetString(response.Payload) : "Timeout";
        }

        /// <summary>OPRAVENO: lf hid sim -r <id></summary>
        public async Task<string> SimulateHidAsync(string id, CancellationToken ct = default)
        {
            string command = $"lf hid sim -r {id}\n";
            await _transport.WriteAsync(Encoding.ASCII.GetBytes(command), ct);
            var response = await _transport.ReadFrameAsync(TimeSpan.FromSeconds(5), ct);
            return response != null ? Encoding.ASCII.GetString(response.Payload) : "Timeout";
        }

        /// <summary>NEOVĚŘENO — ověřte lf hid clone -h</summary>
        public async Task<string> CloneHidAsync(string id, CancellationToken ct = default)
        {
            string command = $"lf hid clone {id}\n";
            await _transport.WriteAsync(Encoding.ASCII.GetBytes(command), ct);
            var response = await _transport.ReadFrameAsync(TimeSpan.FromSeconds(5), ct);
            return response != null ? Encoding.ASCII.GetString(response.Payload) : "Timeout";
        }

        public async Task<string> DemodulateAwidAsync(CancellationToken ct = default)
        {
            string command = "lf awid demod\n";
            await _transport.WriteAsync(Encoding.ASCII.GetBytes(command), ct);
            var response = await _transport.ReadFrameAsync(TimeSpan.FromSeconds(5), ct);
            return response != null ? Encoding.ASCII.GetString(response.Payload) : "Timeout";
        }

        /// <summary>NEOVĚŘENO</summary>
        public async Task<string> BruteForceAwidAsync(int start, int end, CancellationToken ct = default)
        {
            string command = $"lf awid brute {start} {end}\n";
            await _transport.WriteAsync(Encoding.ASCII.GetBytes(command), ct);
            var response = await _transport.ReadFrameAsync(TimeSpan.FromMinutes(10), ct);
            return response != null ? Encoding.ASCII.GetString(response.Payload) : "Timeout";
        }

        /// <summary>NEOVĚŘENO</summary>
        public async Task<string> CloneAwidAsync(string id, CancellationToken ct = default)
        {
            string command = $"lf awid clone {id}\n";
            await _transport.WriteAsync(Encoding.ASCII.GetBytes(command), ct);
            var response = await _transport.ReadFrameAsync(TimeSpan.FromSeconds(5), ct);
            return response != null ? Encoding.ASCII.GetString(response.Payload) : "Timeout";
        }

        public async Task<string> DemodulateIndalaAsync(CancellationToken ct = default)
        {
            string command = "lf indala demod\n";
            await _transport.WriteAsync(Encoding.ASCII.GetBytes(command), ct);
            var response = await _transport.ReadFrameAsync(TimeSpan.FromSeconds(5), ct);
            return response != null ? Encoding.ASCII.GetString(response.Payload) : "Timeout";
        }

        public async Task<string> SimulateIndalaAsync(string id, CancellationToken ct = default)
        {
            string command = $"lf indala sim {id}\n";
            await _transport.WriteAsync(Encoding.ASCII.GetBytes(command), ct);
            var response = await _transport.ReadFrameAsync(TimeSpan.FromSeconds(5), ct);
            return response != null ? Encoding.ASCII.GetString(response.Payload) : "Timeout";
        }

        /// <summary>NEOVĚŘENO</summary>
        public async Task<string> BruteForceEm4x05Async(CancellationToken ct = default)
        {
            string command = "lf em 4x05 brute\n";
            await _transport.WriteAsync(Encoding.ASCII.GetBytes(command), ct);
            var response = await _transport.ReadFrameAsync(TimeSpan.FromMinutes(10), ct);
            return response != null ? Encoding.ASCII.GetString(response.Payload) : "Timeout";
        }

        /// <summary>NEOVĚŘENO</summary>
        public async Task<string> BruteForceEm4x50Async(CancellationToken ct = default)
        {
            string command = "lf em 4x50 brute\n";
            await _transport.WriteAsync(Encoding.ASCII.GetBytes(command), ct);
            var response = await _transport.ReadFrameAsync(TimeSpan.FromMinutes(10), ct);
            return response != null ? Encoding.ASCII.GetString(response.Payload) : "Timeout";
        }

        /// <summary>NEOVĚŘENO</summary>
        public async Task<string> SimulateLfAsync(string buffer, CancellationToken ct = default)
        {
            string command = $"lf sim {buffer}\n";
            await _transport.WriteAsync(Encoding.ASCII.GetBytes(command), ct);
            var response = await _transport.ReadFrameAsync(TimeSpan.FromSeconds(5), ct);
            return response != null ? Encoding.ASCII.GetString(response.Payload) : "Timeout";
        }

        public async Task<string> SniffLfAsync(CancellationToken ct = default)
        {
            string command = "lf sniff\n";
            await _transport.WriteAsync(Encoding.ASCII.GetBytes(command), ct);
            var response = await _transport.ReadFrameAsync(TimeSpan.FromSeconds(30), ct);
            return response != null ? Encoding.ASCII.GetString(response.Payload) : "Timeout";
        }
    }
}
