using AbilityKit.Ability.Host;
using AbilityKit.Ability.Triggering;
using AbilityKit.Game.Battle.Entity;
using AbilityKit.Game.Battle.Vfx;
using AbilityKit.Protocol.Moba.StateSync;
using UnityEngine;
using EC = AbilityKit.World.ECS;

namespace AbilityKit.Game.Flow.Battle.ViewEvents
{
    internal sealed class BattleProjectileViewEventHandler
    {
        private readonly BattleContext _ctx;
        private readonly IBattleEntityQuery _query;
        private readonly BattleVfxManager _vfx;
        private readonly EC.IEntity _vfxNode;
        private readonly BattleProjectileFollowTargetResolver _followTargets;

        public BattleProjectileViewEventHandler(
            BattleContext ctx,
            IBattleEntityQuery query,
            BattleVfxManager vfx,
            in EC.IEntity vfxNode)
        {
            _ctx = ctx;
            _query = query;
            _vfx = vfx;
            _vfxNode = vfxNode;
            _followTargets = new BattleProjectileFollowTargetResolver(query);
        }

        public void HandleTriggerHit(in TriggerEvent evt)
        {
            if (!CanSpawnVfx()) return;
            if (!BattleProjectileVfxResolver.TryResolveTriggerHit(evt, out var vfxId, out var pos)) return;

            _vfx.TryCreateVfxEntity(_ctx.EntityWorld, _vfxNode, vfxId, default, in pos, out _);
        }

        public void HandleSnapshot(MobaProjectileEventSnapshotEntry[] entries)
        {
            if (entries == null || entries.Length == 0) return;
            if (!CanSpawnVfx()) return;
            if (_query == null) return;

            for (int i = 0; i < entries.Length; i++)
            {
                HandleSnapshotEntry(entries[i]);
            }
        }

        private void HandleSnapshotEntry(MobaProjectileEventSnapshotEntry entry)
        {
            var vfxId = BattleProjectileVfxResolver.ResolveSnapshotVfxId(entry.TemplateId, entry.Kind);
            if (vfxId <= 0) return;

            var pos = new Vector3(entry.X, entry.Y, entry.Z);
            var followId = _followTargets.Resolve(entry.ProjectileActorId);
            _vfx.TryCreateVfxEntity(_ctx.EntityWorld, _vfxNode, vfxId, followId, in pos, out _);
        }

        private bool CanSpawnVfx()
        {
            if (_ctx?.EntityWorld == null) return false;
            if (_vfx == null) return false;
            if (!_vfxNode.IsValid) return false;
            return true;
        }
    }
}
