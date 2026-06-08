using AbilityKit.Ability.Share.Effect;
using AbilityKit.Ability.Triggering;
using AbilityKit.Core.Math;
using AbilityKit.Demo.Moba.Config.BattleDemo.MO;
using AbilityKit.Protocol.Moba.StateSync;
using UnityEngine;

namespace AbilityKit.Game.Flow.Battle.ViewEvents
{
    internal static class BattleProjectileVfxResolver
    {
        public static bool TryResolveTriggerHit(in TriggerEvent evt, out int vfxId, out Vector3 position)
        {
            vfxId = 0;
            position = default;

            if (evt.Args == null) return false;
            if (!evt.Args.TryGetValue(ProjectileTriggering.Args.TemplateId, out var templateObj) || templateObj is not int templateId || templateId <= 0)
            {
                return false;
            }

            var projectile = BattleViewFactory.TryGetProjectile(templateId);
            if (projectile == null || projectile.OnHitVfxId <= 0) return false;

            if (!evt.Args.TryGetValue(ProjectileTriggering.Args.HitPoint, out var hitPointObj) || hitPointObj is not Vec3 hitPoint)
            {
                return false;
            }

            vfxId = projectile.OnHitVfxId;
            position = new Vector3(hitPoint.X, hitPoint.Y, hitPoint.Z);
            return true;
        }

        public static int ResolveSnapshotVfxId(int templateId, int kind)
        {
            if (templateId <= 0) return 0;

            var projectile = BattleViewFactory.TryGetProjectile(templateId);
            return ResolveSnapshotVfxId(projectile, kind);
        }

        private static int ResolveSnapshotVfxId(ProjectileMO projectile, int kind)
        {
            if (projectile == null) return 0;

            if (kind == (int)ProjectileEventKind.Spawn) return projectile.OnSpawnVfxId;
            if (kind == (int)ProjectileEventKind.Hit) return projectile.OnHitVfxId;
            if (kind == (int)ProjectileEventKind.Exit) return projectile.OnExpireVfxId;
            return 0;
        }
    }
}
