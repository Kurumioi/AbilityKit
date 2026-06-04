using AbilityKit.Ability.World.Services;
using AbilityKit.Ability.World.Services.Attributes;

namespace AbilityKit.Demo.Moba.Services
{
    [WorldService(typeof(MobaCombatEffectService))]
    public sealed class MobaCombatEffectService : IService
    {
        private readonly DamagePipelineService _damagePipeline;
        private readonly MobaDamageService _damageApplier;

        public MobaCombatEffectService(DamagePipelineService damagePipeline, MobaDamageService damageApplier)
        {
            _damagePipeline = damagePipeline;
            _damageApplier = damageApplier;
        }

        public DamageResult DealDamage(AttackInfo attack)
        {
            if (attack == null) return null;
            return _damagePipeline != null ? _damagePipeline.Execute(attack) : null;
        }

        public float Heal(int healerActorId, int targetActorId, int healType, float value, int reasonKind = 0, int reasonParam = 0)
        {
            if (_damageApplier == null) return 0f;
            return _damageApplier.ApplyHeal(healerActorId, targetActorId, healType, value, reasonKind, reasonParam);
        }

        public void Dispose()
        {
        }
    }
}
