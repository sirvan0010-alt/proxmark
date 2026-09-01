using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PM5Control.Core.Connections
{
    /// <summary>
    /// Klient pro MIFARE Classic. NEOVĚŘENO proti aktuálnímu firmwaru — viz README v této složce.
    /// </summary>
    public class MifareClassicClient
    {
        private readonly IProxmarkTransport _transport;

        public MifareClassicClient(IProxmarkTransport transport)
        {
            _transport = transport ?? throw new ArgumentNullException(nameof(transport));
        }

        public async Task<string> ReadBlockAsync(int block, byte keyType = 0x60, byte[]? key = null, CancellationToken ct = default)
        {
            string keyHex = key != null ? BitConverter.ToString(key).Replace("-", "").ToLower() : "FFFFFFFFFFFF";
            string command = $"hf mf rdbl {block} {(keyType == 0x60 ? "A" : "B")} {keyHex}\n";
            await _transport.WriteAsync(Encoding.ASCII.GetBytes(command), ct);
            var response = await _transport.ReadFrameAsync(TimeSpan.FromSeconds(5), ct);
            return response != null ? Encoding.ASCII.GetString(response.Payload) : "Timeout";
        }

        public async Task<string> WriteBlockAsync(int block, byte[] data, byte keyType = 0x60, byte[]? key = null, CancellationToken ct = default)
        {
            if (data.Length != 16) throw new ArgumentException("Data musí mít 16 bajtů");
            string keyHex = key != null ? BitConverter.ToString(key).Replace("-", "").ToLower() : "FFFFFFFFFFFF";
            string dataHex = BitConverter.ToString(data).Replace("-", "").ToLower();
            string command = $"hf mf wrbl {block} {(keyType == 0x60 ? "A" : "B")} {keyHex} {dataHex}\n";
            await _transport.WriteAsync(Encoding.ASCII.GetBytes(command), ct);
            var response = await _transport.ReadFrameAsync(TimeSpan.FromSeconds(5), ct);
            return response != null ? Encoding.ASCII.GetString(response.Payload) : "Timeout";
        }

        public async Task<bool> AuthenticateAsync(int block, byte keyType = 0x60, byte[]? key = null, CancellationToken ct = default)
        {
            string keyHex = key != null ? BitConverter.ToString(key).Replace("-", "").ToLower() : "FFFFFFFFFFFF";
            string command = $"hf mf auth {block} {(keyType == 0x60 ? "A" : "B")} {keyHex}\n";
            await _transport.WriteAsync(Encoding.ASCII.GetBytes(command), ct);
            var response = await _transport.ReadFrameAsync(TimeSpan.FromSeconds(3), ct);
            if (response == null) return false;
            string text = Encoding.ASCII.GetString(response.Payload);
            return text.Contains("Auth ok") || text.Contains("Authenticated");
        }

        public async Task<string> RunNestedAttackAsync(int block = 0, byte keyType = 0x60, byte[]? knownKey = null, CancellationToken ct = default)
        {
            if (knownKey == null || knownKey.Length != 6)
                throw new ArgumentException("Klíč musí mít 6 bajtů");
            string keyHex = BitConverter.ToString(knownKey).Replace("-", "").ToLower();
            string command = $"hf mf nested {block} {(keyType == 0x60 ? "A" : "B")} {keyHex}\n";
            await _transport.WriteAsync(Encoding.ASCII.GetBytes(command), ct);
            var response = await _transport.ReadFrameAsync(TimeSpan.FromMinutes(2), ct);
            return response != null ? Encoding.ASCII.GetString(response.Payload) : "Timeout";
        }

        public async Task<string> RunHardNestedAttackAsync(int block = 0, byte keyType = 0x60, byte[]? knownKey = null, int maxAttempts = 1000000, CancellationToken ct = default)
        {
            string keyHex = knownKey != null ? BitConverter.ToString(knownKey).Replace("-", "").ToLower() : "FFFFFFFFFFFF";
            string command = $"hf mf hardnested {block} {(keyType == 0x60 ? "A" : "B")} {keyHex} {maxAttempts}\n";
            await _transport.WriteAsync(Encoding.ASCII.GetBytes(command), ct);
            var response = await _transport.ReadFrameAsync(TimeSpan.FromHours(4), ct);
            return response != null ? Encoding.ASCII.GetString(response.Payload) : "Timeout";
        }

        public async Task<string> RunDarksideAttackAsync(CancellationToken ct = default)
        {
            string command = "hf mf darkside\n";
            await _transport.WriteAsync(Encoding.ASCII.GetBytes(command), ct);
            var response = await _transport.ReadFrameAsync(TimeSpan.FromMinutes(10), ct);
            return response != null ? Encoding.ASCII.GetString(response.Payload) : "Timeout";
        }

        public async Task<string> RunAutopwnAsync(CancellationToken ct = default)
        {
            string command = "hf mf autopwn\n";
            await _transport.WriteAsync(Encoding.ASCII.GetBytes(command), ct);
            var response = await _transport.ReadFrameAsync(TimeSpan.FromMinutes(30), ct);
            return response != null ? Encoding.ASCII.GetString(response.Payload) : "Timeout";
        }

        public async Task<string> CheckKeysAsync(int sector = 0, byte keyType = 0x60, string keyFile = "default_keys.dic", CancellationToken ct = default)
        {
            string command = $"hf mf chk {sector} {(keyType == 0x60 ? "A" : "B")} {keyFile}\n";
            await _transport.WriteAsync(Encoding.ASCII.GetBytes(command), ct);
            var response = await _transport.ReadFrameAsync(TimeSpan.FromSeconds(30), ct);
            return response != null ? Encoding.ASCII.GetString(response.Payload) : "Timeout";
        }

        public async Task<string> DumpCardAsync(string fileName = "dump", CancellationToken ct = default)
        {
            string command = $"hf mf dump {fileName}\n";
            await _transport.WriteAsync(Encoding.ASCII.GetBytes(command), ct);
            var response = await _transport.ReadFrameAsync(TimeSpan.FromMinutes(5), ct);
            return response != null ? Encoding.ASCII.GetString(response.Payload) : "Timeout";
        }

        public async Task<string> RestoreCardAsync(bool confirmedOverwrite, string fileName = "dump", CancellationToken ct = default)
        {
            if (!confirmedOverwrite)
                throw new ArgumentException(
                    "RestoreCardAsync přepíše aktuální obsah karty. Potvrďte confirmedOverwrite: true.",
                    nameof(confirmedOverwrite));
            string command = $"hf mf restore {fileName}\n";
            await _transport.WriteAsync(Encoding.ASCII.GetBytes(command), ct);
            var response = await _transport.ReadFrameAsync(TimeSpan.FromMinutes(5), ct);
            return response != null ? Encoding.ASCII.GetString(response.Payload) : "Timeout";
        }

        public async Task<string> SetGen3UidAsync(bool confirmedOverwrite, byte[] uid, CancellationToken ct = default)
        {
            if (!confirmedOverwrite)
                throw new ArgumentException(
                    "SetGen3UidAsync přepíše UID karty. Potvrďte confirmedOverwrite: true.",
                    nameof(confirmedOverwrite));
            string uidHex = BitConverter.ToString(uid).Replace("-", "").ToLower();
            string command = $"hf mf gen3uid {uidHex}\n";
            await _transport.WriteAsync(Encoding.ASCII.GetBytes(command), ct);
            var response = await _transport.ReadFrameAsync(TimeSpan.FromSeconds(5), ct);
            return response != null ? Encoding.ASCII.GetString(response.Payload) : "Timeout";
        }

        public async Task<string> WriteGen3BlockAsync(int block, byte[] data, CancellationToken ct = default)
        {
            if (data.Length != 16) throw new ArgumentException("Data musí mít 16 bajtů");
            string dataHex = BitConverter.ToString(data).Replace("-", "").ToLower();
            string command = $"hf mf gen3blk {block} {dataHex}\n";
            await _transport.WriteAsync(Encoding.ASCII.GetBytes(command), ct);
            var response = await _transport.ReadFrameAsync(TimeSpan.FromSeconds(5), ct);
            return response != null ? Encoding.ASCII.GetString(response.Payload) : "Timeout";
        }

        public async Task<string> FreezeGen3Async(bool confirmedIrreversible, CancellationToken ct = default)
        {
            if (!confirmedIrreversible)
                throw new ArgumentException(
                    "FreezeGen3Async je nevratná operace. Potvrďte confirmedIrreversible: true.",
                    nameof(confirmedIrreversible));
            string command = "hf mf gen3freeze\n";
            await _transport.WriteAsync(Encoding.ASCII.GetBytes(command), ct);
            var response = await _transport.ReadFrameAsync(TimeSpan.FromSeconds(5), ct);
            return response != null ? Encoding.ASCII.GetString(response.Payload) : "Timeout";
        }

        public async Task<string> FormatNdefAsync(CancellationToken ct = default)
        {
            string command = "hf mf ndefformat\n";
            await _transport.WriteAsync(Encoding.ASCII.GetBytes(command), ct);
            var response = await _transport.ReadFrameAsync(TimeSpan.FromSeconds(10), ct);
            return response != null ? Encoding.ASCII.GetString(response.Payload) : "Timeout";
        }

        public async Task<string> ReadNdefAsync(CancellationToken ct = default)
        {
            string command = "hf mf ndefread\n";
            await _transport.WriteAsync(Encoding.ASCII.GetBytes(command), ct);
            var response = await _transport.ReadFrameAsync(TimeSpan.FromSeconds(5), ct);
            return response != null ? Encoding.ASCII.GetString(response.Payload) : "Timeout";
        }

        public async Task<string> WriteNdefAsync(string filePath, CancellationToken ct = default)
        {
            string command = $"hf mf ndefwrite {filePath}\n";
            await _transport.WriteAsync(Encoding.ASCII.GetBytes(command), ct);
            var response = await _transport.ReadFrameAsync(TimeSpan.FromSeconds(5), ct);
            return response != null ? Encoding.ASCII.GetString(response.Payload) : "Timeout";
        }
    }
}
