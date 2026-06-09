using AbilityKit.Ability.Share.Effect;
using AbilityKit.Ability.Triggering;
using AbilityKit.Core.Math;
using AbilityKit.Demo.Moba.Config.BattleDemo.MO;
using AbilityKit.Protocol.Moba.StateSync;
using UnityEngine;

namespace AbilityKit.Game.Flow.Battle.ViewEvents
{
    internal sealed class BattleProjectileVfxResolver
    {
        private readonly BattleViewResourceProvider _resources;
        private readonly BattleProjectileTriggerHitInputResolver _triggerInputs;
        private readonly BattleProjectileSnapshotVfxIdResolver _snapshotVfxIds;

        public BattleProjectileVfxResolver(
            BattleViewResourceProvider resources = null,
            BattleProjectileTriggerHitInputResolver triggerInputs = null,
            BattleProjectileSnapshotVfxIdResolver snapshotVfxIds = null)
        {
            _resources = BattleViewResourceProvider.OrDefault(resources);
            _triggerInputs = triggerInputs ?? new BattleProjectileTriggerHitInputResolver();
            _snapshotVfxIds = snapshotVfxIds ?? new BattleProjectileSnapshotVfxIdResolver();
        }

        public bool TryResolveTriggerHit(in TriggerEvent evt, out int vfxId, out Vector3 position)
        {
            vfxId = 0;
            position = default;

            if (!_triggerInputs.TryResolve(in evt, out var input)) return false;

            var projectile = _resources.TryGetProjectile(input.TemplateId);
            if (projectile == null || projectile.OnHitVfxId <= 0) return false;

            vfxId = projectile.OnHitVfxId;
            position = input.HitPoint;
            return true;
        }

        public int ResolveSnapshotVfxId(int templateId, int kind)
        {
            if (templateId <= 0) return 0;

            var projectile = _resources.TryGetProjectile(templateId);
            return _snapshotVfxIds.Resolve(projectile, kind);
        }
    }

    internal readonly struct BattleProjectileTriggerHitInput
    {
        public BattleProjectileTriggerHitInput(int templateId, in Vector3 hitPoint)
        {
            TemplateId = templateId;
            HitPoint = hitPoint;
        }

        public int TemplateId { get; }

        public Vector3 HitPoint { get; }
    }

    internal sealed class BattleProjectileTriggerHitInputResolver
    {
        public bool TryResolve(in TriggerEvent evt, out BattleProjectileTriggerHitInput input)
        {
            input = default;
            if (evt.Args == null) return false;

            if (!evt.Args.TryGetValue(ProjectileTriggering.Args.TemplateId, out var templateObj)
                || templateObj is not int templateId
                || templateId <= 0)
            {
                return false;
            }

            if (!evt.Args.TryGetValue(ProjectileTriggering.Args.HitPoint, out var hitPointObj)
                || hitPointObj is not Vec3 hitPoint)
            {
                return false;
            }

            var position = new Vector3(hitPoint.X, hitPoint.Y, hitPoint.Z);
            input = new BattleProjectileTriggerHitInput(templateId, in position);
            return true;
        }
    }

    internal sealed class BattleProjectileSnapshotVfxIdResolver
    {
        public int Resolve(ProjectileMO projectile, int kind)
        {
            if (projectile == null) return 0;

            if (kind == (int)ProjectileEventKind.Spawn) return projectile.OnSpawnVfxId;
            if (kind == (int)ProjectileEventKind.Hit) return projectile.OnHitVfxId;
            if (kind == (int)ProjectileEventKind.Exit) return projectile.OnExpireVfxId;
            return 0;
        }
    }
}
