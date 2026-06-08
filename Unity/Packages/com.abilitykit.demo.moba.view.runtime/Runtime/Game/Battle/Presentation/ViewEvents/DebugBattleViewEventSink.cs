using System;
using AbilityKit.Ability.Host;
using AbilityKit.Ability.Triggering;
using AbilityKit.Protocol.Moba;
using AbilityKit.Protocol.Moba.StateSync;

namespace AbilityKit.Game.Flow.Battle.ViewEvents
{
    internal sealed class DebugBattleViewEventSink : IBattleViewEventSink
    {
        private readonly string[] _lines;
        private int _next;
        private int _count;

        public int Total { get; private set; }

        public DebugBattleViewEventSink(int maxLines)
        {
            if (maxLines <= 0) maxLines = 16;
            _lines = new string[maxLines];
        }

        public string[] GetRecentLines()
        {
            if (_count <= 0) return Array.Empty<string>();

            var n = Math.Min(_count, _lines.Length);
            var arr = new string[n];
            var start = (_next - n + _lines.Length) % _lines.Length;
            for (int i = 0; i < n; i++)
            {
                arr[i] = _lines[(start + i) % _lines.Length];
            }
            return arr;
        }

        private void Push(string line)
        {
            if (string.IsNullOrWhiteSpace(line)) return;
            _lines[_next] = line;
            _next = (_next + 1) % _lines.Length;
            if (_count < _lines.Length) _count++;
            Total++;
        }

        public void OnTriggerEvent(in TriggerEvent evt)
        {
            var id = evt.Id != null ? evt.Id.ToString() : "<null>";
            Push($"Trigger:{id}");
        }

        public void OnEnterGameSnapshot(ISnapshotEnvelope packet, EnterMobaGameRes res)
        {
            Push($"EnterGame: tickRate={res.TickRate}");
        }

        public void OnActorTransformSnapshot(ISnapshotEnvelope packet, MobaActorTransformSnapshotEntry[] entries)
        {
            if (entries == null) return;
            Push($"Transform: n={entries.Length}");
        }

        public void OnProjectileEventSnapshot(ISnapshotEnvelope packet, MobaProjectileEventSnapshotEntry[] entries)
        {
            if (entries == null) return;
            Push($"Projectile: n={entries.Length}");
        }

        public void OnAreaEventSnapshot(ISnapshotEnvelope packet, MobaAreaEventSnapshotEntry[] entries)
        {
            if (entries == null) return;
            Push($"Area: n={entries.Length}");
        }

        public void OnDamageEventSnapshot(ISnapshotEnvelope packet, MobaDamageEventSnapshotEntry[] entries)
        {
            if (entries == null) return;
            Push($"Damage: n={entries.Length}");
        }
    }
}
