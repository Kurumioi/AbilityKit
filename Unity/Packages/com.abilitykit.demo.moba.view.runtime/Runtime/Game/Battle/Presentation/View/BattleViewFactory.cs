using AbilityKit.Demo.Moba.Config.BattleDemo;
using AbilityKit.Demo.Moba.Config.BattleDemo.MO;
using AbilityKit.Demo.Moba.Config.Core;
using AbilityKit.Game.Battle.Entity;
using AbilityKit.Game.Battle.Moba.Config;
using AbilityKit.Game.Battle.Vfx;
using UnityEngine;

namespace AbilityKit.Game.Flow
{
    public static class BattleViewFactory
    {
        public static MobaConfigDatabase Configs;
        public static VfxDatabase VfxDb;

        public static MobaConfigDatabase GetOrLoadConfigs()
        {
            if (Configs == null) Configs = MobaConfigLoader.LoadDefault();
            return Configs;
        }

        public static GameObject CreateShellGameObject(int actorId, int modelId)
        {
            return BattleViewModelFactory.CreateActorShell(GetOrLoadConfigs(), actorId, modelId);
        }

        public static GameObject CreateModelGo(int modelId)
        {
            return BattleViewModelFactory.CreateAoeModel(GetOrLoadConfigs(), modelId);
        }

        public static GameObject CreateVfxGo(int vfxId)
        {
            return BattleViewVfxFactory.CreateAoeVfx(GetOrLoadVfxDb(), vfxId);
        }

        public static int ResolveModelId(BattleEntityMetaComponent meta)
        {
            return BattleViewConfigLookup.ResolveModelId(GetOrLoadConfigs(), meta);
        }

        public static int ResolveProjectileVfxId(BattleEntityMetaComponent meta)
        {
            return BattleViewConfigLookup.ResolveProjectileVfxId(GetOrLoadConfigs(), meta);
        }

        public static ProjectileMO TryGetProjectile(int templateId)
        {
            return BattleViewConfigLookup.TryGetProjectile(GetOrLoadConfigs(), templateId);
        }

        public static AoeMO TryGetAoe(int templateId)
        {
            return BattleViewConfigLookup.TryGetAoe(GetOrLoadConfigs(), templateId);
        }

        private static VfxDatabase GetOrLoadVfxDb()
        {
            if (VfxDb == null) VfxDb = VfxDatabase.LoadFromResources("vfx/vfx");
            return VfxDb;
        }
    }
}
