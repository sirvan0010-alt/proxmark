using System;
using System.Linq;
using System.Text;

namespace PM5Control.Core.Protocols.Pm3
{
    /// <summary>
    /// Minimal PM3 command helpers for future transport wiring.
    /// Full NG framing belongs in the existing Protocols/Pm3 layer when integrated.
    /// </summary>
    public static class Pm3CommandCodec
    {
        public const uint MagicCommand = 0x504D3361;  // "PM3a"
        public const uint MagicResponse = 0x504D3362; // "PM3b"
        public const ushort CmdDebugPrintString = 0x0100;

        public static byte[] EncodeAsciiLine(string commandLine)
        {
            if (string.IsNullOrWhiteSpace(commandLine))
                throw new ArgumentException("Command line is required.", nameof(commandLine));

            var line = commandLine.EndsWith('\n') ? commandLine : commandLine + "\n";
            return Encoding.ASCII.GetBytes(line);
        }

        public static (ushort? Command, byte[]? Payload) TryDecodeResponse(byte[] raw)
        {
            if (raw is null || raw.Length < 2) return (null, null);
            return (BitConverter.ToUInt16(raw, 0), raw.Skip(2).ToArray());
        }
    }
}
