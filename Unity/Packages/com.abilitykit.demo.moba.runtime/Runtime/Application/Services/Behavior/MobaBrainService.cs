using AbilityKit.Ability.Behavior;
using AbilityKit.Ability.World.DI;
using AbilityKit.Ability.World.Services;
using AbilityKit.Ability.World.Services.Attributes;

namespace AbilityKit.Demo.Moba.Services
{
    [WorldService(typeof(MobaBrainService), WorldLifetime.Scoped)]
    public sealed class MobaBrainService : IService
    {
        private const string DefaultBehaviorKind = "moba.actor.brain.idle";

        private readonly BehaviorManager _behaviors = new BehaviorManager();

        public BehaviorRuntime EnsureBehavior(global::ActorEntity actor)
        {
            if (actor == null || !actor.hasActorId || !actor.hasActorBrain) return null;

            var brain = actor.actorBrain;
            if (brain.BrainId <= 0) return null;

            var existing = brain.BehaviorInstanceId > 0 ? _behaviors.GetBehavior(brain.BehaviorInstanceId) : null;
            if (existing != null && existing.Phase == BehaviorPhase.Running) return existing;

            var runtime = _behaviors.CreateBehavior(new BehaviorCreateConfig
            {
                BehaviorKind = DefaultBehaviorKind,
                SourceContextId = brain.BrainId,
                OwnerId = new BehaviorEntityId(actor.actorId.Value),
                Decision = new IdleActorBrainDecision(),
                Executor = new DefaultExecutor(),
                World = new DefaultWorldQuery()
            });

            actor.ReplaceActorBrain(
                brain.BrainId,
                brain.OwnerActorId,
                brain.SourceKind,
                brain.SourceId,
                runtime.InstanceId);

            return runtime;
        }

        public void Tick(float deltaTimeSeconds, long frame)
        {
            if (deltaTimeSeconds <= 0f) return;
            _behaviors.Tick(deltaTimeSeconds, frame);
        }

        public void Dispose()
        {
        }

        private sealed class IdleActorBrainDecision : IBehaviorDecision
        {
            public string DecisionType => "MobaActorBrainIdle";
            public string CurrentState => "Idle";

            public DecisionResult Decide(IBehaviorContext context, IWorldQuery world)
            {
                return DecisionResult.Continue(CurrentState);
            }
        }
    }
}
