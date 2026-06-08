using AbilityKit.Demo.Moba;
using AbilityKit.Game.Battle.Entity;
using AbilityKit.Game.Flow.Battle.View;
using AbilityKit.Protocol.Moba.StateSync;
using UnityEngine;
using EC = AbilityKit.World.ECS;

namespace AbilityKit.Game.Flow.Battle.ViewEvents
{
    internal sealed class BattleDamageViewEventHandler
    {
        private readonly BattleContext _ctx;
        private readonly IBattleEntityQuery _query;
        private readonly EC.IEntity _vfxNode;
        private readonly BattleFloatingTextSystem _floatingTexts;

        public BattleDamageViewEventHandler(
            BattleContext ctx,
            IBattleEntityQuery query,
            in EC.IEntity vfxNode,
            BattleFloatingTextSystem floatingTexts)
        {
            _ctx = ctx;
            _query = query;
            _vfxNode = vfxNode;
            _floatingTexts = floatingTexts;
        }

        public void HandleDamageResult(DamageResult result)
        {
            if (result == null) return;
            SpawnFloatingText(result.TargetActorId, result.Value, result.Value < 0f);
        }

        public void HandleSnapshot(MobaDamageEventSnapshotEntry[] entries)
        {
            if (entries == null || entries.Length == 0) return;
            if (!CanSpawnText()) return;

            for (int i = 0; i < entries.Length; i++)
            {
                var entry = entries[i];
                SpawnFloatingText(entry.TargetActorId, entry.Value, entry.Kind == (int)DamageEventKind.Heal);
            }
        }

        private void SpawnFloatingText(int targetActorId, float value, bool isHeal)
        {
            if (!CanSpawnText()) return;
            if (targetActorId <= 0) return;
            if (!BattleDamageFloatingTextFormatter.TryFormat(value, isHeal, out var spec)) return;

            var pos = ResolveTextPosition(targetActorId);
            _floatingTexts?.Spawn(_vfxNode, spec.Text, in pos, spec.Color);
        }

        private Vector3 ResolveTextPosition(int targetActorId)
        {
            var pos = Vector3.zero;
            if (_query.TryGetTransform(new BattleNetId(targetActorId), out var transform) && transform != null)
            {
                pos = transform.Position;
            }

            return pos + Vector3.up * 2f;
        }

        private bool CanSpawnText()
        {
            if (_ctx?.EntityWorld == null) return false;
            if (_query == null) return false;
            if (!_vfxNode.IsValid) return false;
            return true;
        }
    }
}
