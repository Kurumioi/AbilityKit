using System;
using AbilityKit.Ability.Host;
using AbilityKit.Network.Abstractions;
using AbilityKit.Network.Runtime;

namespace AbilityKit.Game.Flow
{
    internal sealed class RemoteDrivenInputRuntime : IDisposable
    {
        private FrameJitterBuffer<PlayerInputCommand[]> _buffer;

        private RemoteDrivenInputRuntime(FrameJitterBuffer<PlayerInputCommand[]> buffer)
        {
            _buffer = buffer;
        }

        public IRemoteFrameSource<PlayerInputCommand[]> Source => _buffer;

        public IConsumableRemoteFrameSource<PlayerInputCommand[]> Consumable => _buffer;

        public IRemoteFrameSink<PlayerInputCommand[]> Sink => _buffer;

        public static RemoteDrivenInputRuntime Create(int delayFrames)
        {
            var buffer = new FrameJitterBuffer<PlayerInputCommand[]>(
                delayFrames: delayFrames,
                missingMode: MissingFrameMode.FillDefault,
                missingFrameFactory: Array.Empty<PlayerInputCommand>,
                initialCapacity: 256);

            return new RemoteDrivenInputRuntime(buffer);
        }

        public void PublishDebugStats()
        {
            if (_buffer == null) return;

            BattleFlowDebugProvider.JitterBufferStats = new JitterBufferStatsSnapshot
            {
                DelayFrames = _buffer.DelayFrames,
                MissingMode = _buffer.MissingMode.ToString(),
                TargetFrame = _buffer.TargetFrame,
                MaxReceivedFrame = _buffer.MaxReceivedFrame,
                LastConsumedFrame = _buffer.LastConsumedFrame,
                BufferedCount = _buffer.Count,
                MinBufferedFrame = _buffer.MinBufferedFrame,

                AddedCount = _buffer.AddedCount,
                DuplicateCount = _buffer.DuplicateCount,
                LateCount = _buffer.LateCount,
                ConsumedCount = _buffer.ConsumedCount,
                FilledDefaultCount = _buffer.FilledDefaultCount,
            };
        }

        public void Dispose()
        {
            _buffer?.Dispose();
            _buffer = null;
        }
    }
}
