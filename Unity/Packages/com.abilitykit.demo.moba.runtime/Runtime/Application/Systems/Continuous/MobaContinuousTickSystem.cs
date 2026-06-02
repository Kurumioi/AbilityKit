using AbilityKit.Ability.World;
using AbilityKit.Ability.World.DI;
using AbilityKit.Ability.World.Services;
using AbilityKit.Demo.Moba.Services;

namespace AbilityKit.Demo.Moba.Systems
{
    [WorldSystem(order: MobaSystemOrder.ContinuousTick, Phase = WorldSystemPhase.Execute)]
    public sealed class MobaContinuousTickSystem : WorldSystemBase
    {
        private IWorldClock _clock;
        private MobaContinuousManager _continuous;

        public MobaContinuousTickSystem(global::Entitas.IContexts contexts, IWorldResolver services)
            : base(contexts, services)
        {
        }

        protected override void OnInit()
        {
            Services.TryResolve(out _clock);
            Services.TryResolve(out _continuous);
        }

        protected override void OnExecute()
        {
            if (_clock == null || _continuous == null) return;

            var dt = _clock.DeltaTime;
            if (dt <= 0f) return;

            _continuous.Tick(dt);
        }
    }
}
