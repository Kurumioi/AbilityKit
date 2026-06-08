using AbilityKit.Ability.World;
using AbilityKit.Ability.World.DI;
using AbilityKit.Demo.Moba.Services;

namespace AbilityKit.Demo.Moba.Systems.Buffs
{
    [WorldSystem(order: MobaSystemOrder.BuffsApply, Phase = WorldSystemPhase.Execute)]
    public sealed class MobaBuffApplySystem : WorldSystemBase
    {
        private BuffLifecycleExecutor _lifecycle;
        private global::Entitas.IGroup<global::ActorEntity> _group;

        public MobaBuffApplySystem(global::Entitas.IContexts contexts, IWorldResolver services)
            : base(contexts, services)
        {
        }

        protected override void OnInit()
        {
            _lifecycle = BuffLifecycleExecutorFactory.Create(Services);
            _group = Contexts.Actor().GetGroup(ActorMatcher.AllOf(ActorComponentsLookup.ActorId, ActorComponentsLookup.ApplyBuffRequest));
        }

        protected override void OnExecute()
        {
            var entities = _group.GetEntities();
            if (entities == null || entities.Length == 0) return;

            for (int i = 0; i < entities.Length; i++)
            {
                var e = entities[i];
                if (e == null || !e.hasActorId || !e.hasApplyBuffRequest) continue;

                var req = e.applyBuffRequest;
                e.RemoveApplyBuffRequest();

                _lifecycle?.Apply(new BuffApplyRequest
                {
                    TargetActorId = e.actorId.Value,
                    BuffId = req.BuffId,
                    SourceActorId = req.SourceId,
                    DurationOverrideMs = req.DurationOverrideMs,
                    Origin = BuffOriginContext.FromActors(req.ParentContextId, req.OriginSourceActorId, req.OriginTargetActorId),
                });
            }
        }
    }
}
