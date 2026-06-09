using AbilityKit.Demo.Moba.Config.BattleDemo.MO;
using AbilityKit.Demo.Moba.Config.Core;
using AbilityKit.Game.Battle.Entity;

namespace AbilityKit.Game.Flow
{
    internal sealed class BattleViewConfigLookup
    {
        private readonly BattleViewCharacterConfigResolver _characters;
        private readonly BattleViewProjectileConfigResolver _projectiles;
        private readonly BattleViewAoeConfigResolver _aoes;

        public BattleViewConfigLookup(
            BattleViewCharacterConfigResolver characters = null,
            BattleViewProjectileConfigResolver projectiles = null,
            BattleViewAoeConfigResolver aoes = null)
        {
            _characters = characters ?? new BattleViewCharacterConfigResolver();
            _projectiles = projectiles ?? new BattleViewProjectileConfigResolver();
            _aoes = aoes ?? new BattleViewAoeConfigResolver();
        }

        public int ResolveModelId(MobaConfigDatabase configs, BattleEntityMetaComponent meta)
        {
            return _characters.ResolveModelId(configs, meta);
        }

        public int ResolveProjectileVfxId(MobaConfigDatabase configs, BattleEntityMetaComponent meta)
        {
            return _projectiles.ResolveAttachedVfxId(configs, meta);
        }

        public ProjectileMO TryGetProjectile(MobaConfigDatabase configs, int templateId)
        {
            return _projectiles.TryGet(configs, templateId);
        }

        public AoeMO TryGetAoe(MobaConfigDatabase configs, int templateId)
        {
            return _aoes.TryGet(configs, templateId);
        }
    }
}
