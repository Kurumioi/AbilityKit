using AbilityKit.Demo.Moba.Config.Core;
using AbilityKit.Game.Battle.Moba.Config;
using AbilityKit.Game.Battle.Vfx;

namespace AbilityKit.Game.Flow
{
    internal sealed class BattleViewResourceCache
    {
        private const string DefaultVfxResourcePath = "vfx/vfx";

        public MobaConfigDatabase GetOrLoadConfigs(ref MobaConfigDatabase configs)
        {
            if (configs == null)
            {
                configs = MobaConfigLoader.LoadDefault();
            }

            return configs;
        }

        public VfxDatabase GetOrLoadVfxDb(ref VfxDatabase vfxDb)
        {
            if (vfxDb == null)
            {
                vfxDb = VfxDatabase.LoadFromResources(DefaultVfxResourcePath);
            }

            return vfxDb;
        }
    }
}
