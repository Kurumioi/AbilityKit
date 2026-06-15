using AbilityKit.Ability.World;
using AbilityKit.Ability.World.DI;
using AbilityKit.Core.Logging;
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

                var request = new BuffApplyRequest
                {
                    TargetActorId = e.actorId.Value,
                    BuffId = req.BuffId,
                    SourceActorId = req.SourceId,
                    DurationOverrideMs = req.DurationOverrideMs,
                    Origin = BuffOriginContext.FromActors(req.ParentContextId, req.OriginSourceActorId, req.OriginTargetActorId),
                };

                if (_lifecycle != null && !_lifecycle.Apply(request))
                {
                    var reject = _lifecycle.LastReject;
                    Log.Warning($"[MobaBuffApplySystem] Apply buff rejected. target={request.TargetActorId} buffId={request.BuffId} source={request.SourceActorId} rejectCode={FormatRejectCode(reject.Code)} reason={FormatRejectReason(reject.Message)}");
                }
            }
        }

        private static string FormatRejectReason(string reason)
        {
            return string.IsNullOrEmpty(reason) ? "unknown" : reason;
        }

        private static string FormatRejectCode(string code)
        {
            return string.IsNullOrEmpty(code) ? "buff.lifecycle.rejected" : code;
        }
    }
}
