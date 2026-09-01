using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PM5Control.Core.Protocols.Pm3
{
    /// <summary>
    /// Correlates a request with the first non-debug response frame.
    /// Requires a transport implementing IPm3FrameTransport.
    /// </summary>
    public sealed class Pm3ResponseCorrelator
    {
        private readonly IPm3FrameTransport _transport;

        public Pm3ResponseCorrelator(IPm3FrameTransport transport)
        {
            _transport = transport ?? throw new ArgumentNullException(nameof(transport));
        }

        public async Task<Pm3TransactionResult> QueryCorrelatedAsync(
            ushort expectedCmd,
            byte[] request,
            TimeSpan timeout,
            CancellationToken ct)
        {
            var debugFrames = new List<Pm3RawFrame>();
            var deadline = DateTime.UtcNow + timeout;

            await _transport.WriteAsync(request, ct).ConfigureAwait(false);

            while (DateTime.UtcNow < deadline)
            {
                var remaining = deadline - DateTime.UtcNow;
                if (remaining <= TimeSpan.Zero) break;

                var frame = await _transport.ReadFrameAsync(remaining, ct).ConfigureAwait(false);
                if (frame is null) break;

                if (frame.Command == Pm3CommandCodec.CmdDebugPrintString)
                {
                    debugFrames.Add(frame);
                    continue;
                }

                if (frame.Command == expectedCmd)
                    return Pm3TransactionResult.Matched(frame, debugFrames);

                debugFrames.Add(frame);
            }

            return Pm3TransactionResult.TimedOut(debugFrames);
        }
    }

    /// <summary>
    /// Frame-level transport for correlator. Separate from Connections.IProxmarkTransport.
    /// </summary>
    public interface IPm3FrameTransport
    {
        Task WriteAsync(byte[] data, CancellationToken ct);
        Task<Pm3RawFrame?> ReadFrameAsync(TimeSpan timeout, CancellationToken ct);
    }

    public sealed class Pm3RawFrame
    {
        public ushort Command { get; init; }
        public byte[] Payload { get; init; } = Array.Empty<byte>();
        public byte? Sequence { get; init; }
    }

    public sealed class Pm3TransactionResult
    {
        public bool IsMatched { get; init; }
        public bool IsTimedOut { get; init; }
        public Pm3RawFrame? Frame { get; init; }
        public IReadOnlyList<Pm3RawFrame> DebugFrames { get; init; } = Array.Empty<Pm3RawFrame>();

        public static Pm3TransactionResult Matched(Pm3RawFrame frame, List<Pm3RawFrame> debugFrames)
            => new() { IsMatched = true, Frame = frame, DebugFrames = debugFrames };

        public static Pm3TransactionResult TimedOut(List<Pm3RawFrame> debugFrames)
            => new() { IsTimedOut = true, DebugFrames = debugFrames };
    }
}
