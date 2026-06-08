using AbilityKit.Core.Common.Log;
using AbilityKit.Demo.Moba.Config.BattleDemo.MO;
using AbilityKit.Demo.Moba.Config.Core;
using AbilityKit.Game.Battle.Component;
using AbilityKit.Game.Battle.Entity;

namespace AbilityKit.Game.Flow
{
    internal static class BattleViewConfigLookup
    {
        public static int ResolveModelId(MobaConfigDatabase configs, BattleEntityMetaComponent meta)
        {
            if (meta == null) return 0;
            if (configs == null) return 0;

            try
            {
                if (meta.Kind != BattleEntityKind.Character) return 0;

                var character = configs.GetCharacter(meta.EntityCode);
                return character != null ? character.ModelId : 0;
            }
            catch (System.Exception ex)
            {
                Log.Exception(ex);
                return 0;
            }
        }

        public static int ResolveProjectileVfxId(MobaConfigDatabase configs, BattleEntityMetaComponent meta)
        {
            if (meta == null) return 0;
            if (meta.Kind != BattleEntityKind.Projectile) return 0;
            if (configs == null) return 0;

            try
            {
                var projectile = configs.GetProjectile(meta.EntityCode);
                return projectile != null ? projectile.VfxId : 0;
            }
            catch (System.Exception ex)
            {
                Log.Exception(ex);
                return 0;
            }
        }

        public static ProjectileMO TryGetProjectile(MobaConfigDatabase configs, int templateId)
        {
            if (templateId <= 0) return null;
            if (configs == null) return null;

            try
            {
                return configs.GetProjectile(templateId);
            }
            catch (System.Exception ex)
            {
                Log.Exception(ex);
                return null;
            }
        }

        public static AoeMO TryGetAoe(MobaConfigDatabase configs, int templateId)
        {
            if (templateId <= 0) return null;
            if (configs == null) return null;

            try
            {
                return configs.GetAoe(templateId);
            }
            catch (System.Exception ex)
            {
                Log.Exception(ex);
                return null;
            }
        }
    }
}
