using AbilityKit.Demo.Moba.Config.BattleDemo.MO;
using AbilityKit.Demo.Moba.Config.Core;
using AbilityKit.Game.Battle.Entity;
using AbilityKit.Game.Battle.Vfx;
using UnityEngine;

namespace AbilityKit.Game.Flow
{
    public sealed class BattleViewResourceProvider
    {
        internal static readonly BattleViewResourceProvider Default = new BattleViewResourceProvider();

        private MobaConfigDatabase _configs;
        private VfxDatabase _vfxDb;
        private readonly BattleViewResourceCache _cache;
        private readonly BattleViewConfigLookup _configLookup;
        private readonly BattleViewModelFactory _models;
        private readonly BattleViewVfxFactory _vfx;

        internal static BattleViewResourceProvider OrDefault(BattleViewResourceProvider resources)
        {
            return resources ?? Default;
        }

        public BattleViewResourceProvider()
            : this(null, null)
        {
        }

        public BattleViewResourceProvider(MobaConfigDatabase configs, VfxDatabase vfxDb)
            : this(configs, vfxDb, null, null, null, null)
        {
        }

        internal BattleViewResourceProvider(
            MobaConfigDatabase configs,
            VfxDatabase vfxDb,
            BattleViewResourceCache cache,
            BattleViewConfigLookup configLookup,
            BattleViewModelFactory models,
            BattleViewVfxFactory vfx,
            BattleViewResourceProviderComponentFactory components = null)
        {
            components ??= new BattleViewResourceProviderComponentFactory();

            _configs = configs;
            _vfxDb = vfxDb;
            _cache = cache ?? components.CreateCache();
            _configLookup = configLookup ?? components.CreateConfigLookup();

            var primitives = components.CreatePrimitives();
            _models = models ?? components.CreateModels(primitives);
            _vfx = vfx ?? components.CreateVfx(primitives);
        }

        public MobaConfigDatabase Configs
        {
            get => _configs;
            set => _configs = value;
        }

        public VfxDatabase VfxDb
        {
            get => _vfxDb;
            set => _vfxDb = value;
        }

        public MobaConfigDatabase GetOrLoadConfigs()
        {
            return _cache.GetOrLoadConfigs(ref _configs);
        }

        public VfxDatabase GetOrLoadVfxDb()
        {
            return _cache.GetOrLoadVfxDb(ref _vfxDb);
        }

        public GameObject CreateShellGameObject(int actorId, int modelId)
        {
            return _models.CreateActorShell(GetOrLoadConfigs(), actorId, modelId);
        }

        public GameObject CreateModelGo(int modelId)
        {
            return _models.CreateAoeModel(GetOrLoadConfigs(), modelId);
        }

        public GameObject CreateVfxGo(int vfxId)
        {
            return _vfx.CreateAoeVfx(GetOrLoadVfxDb(), vfxId);
        }

        public int ResolveModelId(BattleEntityMetaComponent meta)
        {
            return _configLookup.ResolveModelId(GetOrLoadConfigs(), meta);
        }

        public int ResolveProjectileVfxId(BattleEntityMetaComponent meta)
        {
            return _configLookup.ResolveProjectileVfxId(GetOrLoadConfigs(), meta);
        }

        public ProjectileMO TryGetProjectile(int templateId)
        {
            return _configLookup.TryGetProjectile(GetOrLoadConfigs(), templateId);
        }

        public AoeMO TryGetAoe(int templateId)
        {
            return _configLookup.TryGetAoe(GetOrLoadConfigs(), templateId);
        }
    }

    internal sealed class BattleViewResourceProviderComponentFactory
    {
        public BattleViewResourceCache CreateCache()
        {
            return new BattleViewResourceCache();
        }

        public BattleViewConfigLookup CreateConfigLookup()
        {
            return new BattleViewConfigLookup();
        }

        public BattleViewPrimitiveFactory CreatePrimitives()
        {
            return new BattleViewPrimitiveFactory();
        }

        public BattleViewModelFactory CreateModels(BattleViewPrimitiveFactory primitives)
        {
            return new BattleViewModelFactory(primitives);
        }

        public BattleViewVfxFactory CreateVfx(BattleViewPrimitiveFactory primitives)
        {
            return new BattleViewVfxFactory(primitives);
        }
    }
}
