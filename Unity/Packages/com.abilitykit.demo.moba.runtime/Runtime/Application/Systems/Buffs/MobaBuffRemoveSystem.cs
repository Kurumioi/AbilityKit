using AbilityKit.Ability.World;
using AbilityKit.Ability.World.DI;
using AbilityKit.Core.Logging;
using AbilityKit.Demo.Moba.Services;

namespace AbilityKit.Demo.Moba.Systems.Buffs
{
    [WorldSystem(order: MobaSystemOrder.BuffsRemove, Phase = WorldSystemPhase.Execute)]
    public sealed class MobaBuffRemoveSystem : WorldSystemBase
    {
        private BuffLifecycleExecutor _lifecycle;
        private global::Entitas.IGroup<global::ActorEntity> _group;

        public MobaBuffRemoveSystem(global::Entitas.IContexts contexts, IWorldResolver services)
            : base(contexts, services)
        {
        }

        protected override void OnInit()
        {
            _lifecycle = BuffLifecycleExecutorFactory.Create(Services);
            _group = Contexts.Actor().GetGroup(ActorMatcher.AllOf(ActorComponentsLookup.ActorId, ActorComponentsLookup.RemoveBuffRequest));
        }

        protected override void OnExecute()
        {
            var entities = _group.GetEntities();
            if (entities == null || entities.Length == 0) return;

            for (int i = 0; i < entities.Length; i++)
            {
                var e = entities[i];
                if (e == null || !e.hasActorId || !e.hasRemoveBuffRequest) continue;

                var req = e.removeBuffRequest;
                e.RemoveRemoveBuffRequest();
                var request = new BuffRemoveRequest
                {
                    TargetActorId = e.actorId.Value,
                    BuffId = req.BuffId,
                    SourceActorId = req.SourceId,
                    Reason = req.Reason,
                };

                if (_lifecycle != null && !_lifecycle.Remove(request))
                {
                    var reject = _lifecycle.LastReject;
                    Log.Warning($"[MobaBuffRemoveSystem] Remove buff rejected. target={request.TargetActorId} buffId={request.BuffId} source={request.SourceActorId} reason={request.Reason} rejectCode={FormatRejectCode(reject.Code)} lifecycleReason={FormatRejectReason(reject.Message)}");
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
