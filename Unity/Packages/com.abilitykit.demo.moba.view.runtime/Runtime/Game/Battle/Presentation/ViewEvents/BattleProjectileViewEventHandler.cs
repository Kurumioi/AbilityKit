using AbilityKit.Ability.Host;
using AbilityKit.Ability.Triggering;
using AbilityKit.Game.Battle.Entity;
using AbilityKit.Game.Battle.Vfx;
using AbilityKit.Protocol.Moba.StateSync;
using EC = AbilityKit.World.ECS;

namespace AbilityKit.Game.Flow.Battle.ViewEvents
{
    internal sealed class BattleProjectileViewEventHandler
    {
        private readonly IBattleEntityQuery _query;
        private readonly BattleProjectileVfxSpawner _spawner;
        private readonly BattleProjectileVfxResolver _vfx;
        private readonly BattleProjectileSnapshotVfxResolver _snapshotVfx;

        public BattleProjectileViewEventHandler(
            BattleContext ctx,
            IBattleEntityQuery query,
            BattleVfxManager vfx,
            in EC.IEntity vfxNode,
            BattleViewResourceProvider resources = null)
            : this(ctx, query, vfx, in vfxNode, resources, null)
        {
        }

        internal BattleProjectileViewEventHandler(
            BattleContext ctx,
            IBattleEntityQuery query,
            BattleVfxManager vfx,
            in EC.IEntity vfxNode,
            BattleViewResourceProvider resources,
            BattleProjectileViewEventHandlerFactory handlers)
        {
            handlers ??= new BattleProjectileViewEventHandlerFactory();

            _query = query;
            _spawner = handlers.CreateSpawner(ctx, vfx, in vfxNode);
            _vfx = handlers.CreateTriggerResolver(resources);
            _snapshotVfx = handlers.CreateSnapshotResolver(query, resources);
        }

        public void HandleTriggerHit(in TriggerEvent evt)
        {
            if (!_spawner.CanSpawn) return;
            if (!_vfx.TryResolveTriggerHit(evt, out var vfxId, out var pos)) return;

            var spec = new BattleProjectileVfxSpawnSpec(vfxId, in pos, default);
            _spawner.TrySpawn(in spec);
        }

        public void HandleSnapshot(MobaProjectileEventSnapshotEntry[] entries)
        {
            if (entries == null || entries.Length == 0) return;
            if (!_spawner.CanSpawn) return;
            if (_query == null) return;

            for (int i = 0; i < entries.Length; i++)
            {
                HandleSnapshotEntry(entries[i]);
            }
        }

        private void HandleSnapshotEntry(MobaProjectileEventSnapshotEntry entry)
        {
            if (!_snapshotVfx.TryResolve(in entry, out var spec)) return;
            _spawner.TrySpawn(in spec);
        }
    }

    internal sealed class BattleProjectileViewEventHandlerFactory
    {
        public BattleProjectileVfxSpawner CreateSpawner(
            BattleContext ctx,
            BattleVfxManager vfx,
            in EC.IEntity vfxNode)
        {
            return new BattleProjectileVfxSpawner(ctx, vfx, in vfxNode);
        }

        public BattleProjectileVfxResolver CreateTriggerResolver(BattleViewResourceProvider resources)
        {
            return new BattleProjectileVfxResolver(resources);
        }

        public BattleProjectileSnapshotVfxResolver CreateSnapshotResolver(
            IBattleEntityQuery query,
            BattleViewResourceProvider resources)
        {
            return new BattleProjectileSnapshotVfxResolver(new BattleProjectileFollowTargetResolver(query), resources);
        }
    }
}
