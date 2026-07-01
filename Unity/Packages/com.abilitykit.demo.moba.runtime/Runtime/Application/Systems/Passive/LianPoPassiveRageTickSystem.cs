using AbilityKit.Ability.World;
using AbilityKit.Ability.World.DI;
using AbilityKit.Ability.World.Services;
using AbilityKit.Demo.Moba.Services;
using AbilityKit.Demo.Moba.Services.Passive;

namespace AbilityKit.Demo.Moba.Systems.Passive
{
    [WorldSystem(order: MobaSystemOrder.GameplayTick - 1, Phase = WorldSystemPhase.Execute)]
    public sealed class LianPoPassiveRageTickSystem : WorldSystemBase
    {
        private LianPoPassiveRageService _rage;
        private MobaActorRegistry _actors;
        private IWorldClock _clock;

        public LianPoPassiveRageTickSystem(global::Entitas.IContexts contexts, IWorldResolver services)
            : base(contexts, services)
        {
        }

        protected override void OnInit()
        {
            Services.TryResolve(out _rage);
            Services.TryResolve(out _actors);
            Services.TryResolve(out _clock);
        }

        protected override void OnExecute()
        {
            if (_rage == null || _actors == null || _clock == null) return;

            var deltaSeconds = _clock.DeltaTime;
            if (deltaSeconds <= 0f) return;

            var nowSeconds = _clock.Time;
            foreach (var entry in _actors.Entries)
            {
                _rage.TickActor(entry.Key, entry.Value, nowSeconds, deltaSeconds);
            }
        }
    }
}
