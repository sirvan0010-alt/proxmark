using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PM5Control.Core.Connections
{
    /// <summary>
    /// Klient pro ISO14443-A (HF 14a) technologie.
    ///
    /// POZOR: Syntaxe příkazů níže odpovídá obecné Iceman dokumentaci, ne nutně
    /// přesně vaší verzi firmwaru — Proxmark3/PM5 CLI mezi verzemi mění flagy
    /// (viz `<command> -h` na vašem zařízení). `hf 14a sim`/`hf 14a raw` níže mají
    /// oporu v aktuální dokumentaci, zbytek doporučuju ověřit před prvním použitím.
    /// </summary>
    public class Hf14aClient
    {
        private readonly IProxmarkTransport _transport;

        public Hf14aClient(IProxmarkTransport transport)
        {
            _transport = transport ?? throw new ArgumentNullException(nameof(transport));
        }

        /// <summary>
        /// Přečte UID karty.
        /// Příkaz: hf 14a read
        /// </summary>
        public async Task<string> ReadUidAsync(CancellationToken ct = default)
        {
            string command = "hf 14a read\n";
            await _transport.WriteAsync(Encoding.ASCII.GetBytes(command), ct);
            var response = await _transport.ReadFrameAsync(TimeSpan.FromSeconds(5), ct);
            return response != null ? Encoding.ASCII.GetString(response.Payload) : "Timeout";
        }

        /// <summary>
        /// Simulace karty s daným UID.
        /// Příkaz: hf 14a sim -t <type> -u <uid>
        /// </summary>
        public async Task<string> SimulateAsync(byte[] uid, int type = 7, CancellationToken ct = default)
        {
            string uidHex = BitConverter.ToString(uid).Replace("-", "").ToLower();
            string command = $"hf 14a sim -t {type} -u {uidHex}\n";
            await _transport.WriteAsync(Encoding.ASCII.GetBytes(command), ct);
            var response = await _transport.ReadFrameAsync(TimeSpan.FromSeconds(5), ct);
            return response != null ? Encoding.ASCII.GetString(response.Payload) : "Timeout";
        }

        /// <summary>
        /// Sniffing komunikace mezi čtečkou a kartou.
        /// Příkaz: hf 14a sniff
        /// </summary>
        public async Task<string> SniffAsync(CancellationToken ct = default)
        {
            string command = "hf 14a sniff\n";
            await _transport.WriteAsync(Encoding.ASCII.GetBytes(command), ct);
            var response = await _transport.ReadFrameAsync(TimeSpan.FromSeconds(30), ct);
            return response != null ? Encoding.ASCII.GetString(response.Payload) : "Timeout";
        }

        /// <summary>
        /// Odeslání surového příkazu. `data` musí být validní PM3 raw rámec —
        /// tahle metoda ho jen pošle, žádnou validitu nekontroluje.
        /// Příkaz: hf 14a raw <data>
        /// </summary>
        public async Task<string> SendRawCommandAsync(byte[] data, CancellationToken ct = default)
        {
            string dataHex = BitConverter.ToString(data).Replace("-", "").ToLower();
            string command = $"hf 14a raw {dataHex}\n";
            await _transport.WriteAsync(Encoding.ASCII.GetBytes(command), ct);
            var response = await _transport.ReadFrameAsync(TimeSpan.FromSeconds(5), ct);
            return response != null ? Encoding.ASCII.GetString(response.Payload) : "Timeout";
        }

        /// <summary>
        /// Konfigurace HF rozhraní.
        /// Příkaz: hf 14a config <options>
        /// NEOVĚŘENO — ověřte syntaxi přes `hf 14a config -h` na vašem firmwaru.
        /// </summary>
        public async Task<string> ConfigureAsync(string options, CancellationToken ct = default)
        {
            string command = $"hf 14a config {options}\n";
            await _transport.WriteAsync(Encoding.ASCII.GetBytes(command), ct);
            var response = await _transport.ReadFrameAsync(TimeSpan.FromSeconds(5), ct);
            return response != null ? Encoding.ASCII.GetString(response.Payload) : "Timeout";
        }

        /// <summary>
        /// Výpis zachycených rámců.
        /// Příkaz: hf 14a list
        /// </summary>
        public async Task<string> ListFramesAsync(CancellationToken ct = default)
        {
            string command = "hf 14a list\n";
            await _transport.WriteAsync(Encoding.ASCII.GetBytes(command), ct);
            var response = await _transport.ReadFrameAsync(TimeSpan.FromSeconds(5), ct);
            return response != null ? Encoding.ASCII.GetString(response.Payload) : "Timeout";
        }

        /// <summary>
        /// Detekce "magic" karty vyhodnocením textu odpovědi na `hf 14a read`.
        /// Křehké (string matching na výstup firmwaru).
        /// </summary>
        public async Task<bool> DetectMagicCardAsync(CancellationToken ct = default)
        {
            string response = await ReadUidAsync(ct);
            return response.Contains("Magic") || response.Contains("Gen1") || response.Contains("Gen2");
        }

        /// <summary>
        /// ODSTRANĚNO: původní implementace posílala nesprávnou unbrick sekvenci.
        /// Viz RfidResearchGroup/proxmark3 doc/magic_cards_notes.md — postupy jsou
        /// vícekrokové a závislé na generaci karty.
        /// </summary>
        public Task<string> UnbrickMagicCardAsync(CancellationToken ct = default) =>
            throw new NotSupportedException(
                "UnbrickMagicCardAsync zatím není implementovaný — původní sekvence byla nesprávná. " +
                "Doplňte podle generace karty (viz XML komentář u této metody) před použitím.");
    }
}
