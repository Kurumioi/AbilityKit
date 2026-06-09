using AbilityKit.Demo.Moba.Config.Core;
using AbilityKit.Game.Battle.Vfx;

namespace AbilityKit.Game.Flow
{
    internal interface IBattlePresentationSessionFactory
    {
        BattlePresentationSessionContext Create();
    }

    internal sealed class BattlePresentationSessionFactory : IBattlePresentationSessionFactory
    {
        public BattlePresentationSessionContext Create()
        {
            return BattlePresentationSessionContext.CreateFromDefaultResources();
        }
    }

    public sealed class BattlePresentationSessionContext
    {
        private int _retainCount;

        public BattlePresentationSessionContext(BattleViewResourceProvider resources)
        {
            Resources = resources ?? new BattleViewResourceProvider();
        }

        public BattleViewResourceProvider Resources { get; }

        internal void Retain()
        {
            _retainCount++;
        }

        internal bool Release()
        {
            if (_retainCount > 0)
            {
                _retainCount--;
            }

            return _retainCount == 0;
        }

        public static BattlePresentationSessionContext CreateDefault(
            MobaConfigDatabase configs = null,
            VfxDatabase vfxDb = null)
        {
            return new BattlePresentationSessionContext(new BattleViewResourceProvider(configs, vfxDb));
        }

        internal static BattlePresentationSessionContext CreateFromDefaultResources()
        {
            return CreateDefault(
                BattleViewResourceProvider.Default.Configs,
                BattleViewResourceProvider.Default.VfxDb);
        }
    }
}
