using System;
using AbilityKit.Core.Common.Log;
using AbilityKit.Core.Generic;
using AbilityKit.Effect;
using AbilityKit.Ability.World.DI;
using AbilityKit.Ability.World.Services;
using AbilityKit.Ability.World.Services.Attributes;
using AbilityKit.Pipeline;

namespace AbilityKit.Demo.Moba.Services
{
    using AbilityKit.Demo.Moba;
    using AbilityKit.Ability;
    [WorldService(typeof(MobaEffectInvokerService))]
    public sealed class MobaEffectInvokerService : IService
    {
        [WorldInject] private MobaEffectExecutionService _effects;
        [WorldInject] private IWorldResolver _services;

        public void Execute(int effectId, int sourceActorId, int targetActorId, int contextKind, long sourceContextId, IWorldResolver worldServices = null, Action<MobaEffectPipelineContext> configure = null)
        {
            if (effectId <= 0) return;
            if (_effects == null)
            {
                Log.Warning($"[MobaEffectInvokerService] Skip effect: MobaEffectExecutionService not injected. effectId={effectId}, source={sourceActorId}, target={targetActorId}");
                return;
            }
            var ctx = new MobaEffectPipelineContext();
            ctx.Initialize(
                abilityInstance: null,
                sourceActorId: sourceActorId,
                targetActorId: targetActorId,
                contextKind: contextKind,
                sourceContextId: sourceContextId,
                worldServices: worldServices ?? _services,
                eventBus: null);

            configure?.Invoke(ctx);
            _effects.Execute(effectId, ctx, EffectExecuteMode.InternalOnly);
        }

        public void Execute(int effectId, IAbilityPipelineContext context)
        {
            if (effectId <= 0) return;
            if (context == null) return;
            if (_effects == null)
            {
                Log.Warning($"[MobaEffectInvokerService] Skip effect: MobaEffectExecutionService not injected. effectId={effectId}, context={context.GetType().Name}");
                return;
            }
            _effects.Execute(effectId, context, EffectExecuteMode.InternalOnly);
        }

        public void Dispose()
        {
        }
    }
}
