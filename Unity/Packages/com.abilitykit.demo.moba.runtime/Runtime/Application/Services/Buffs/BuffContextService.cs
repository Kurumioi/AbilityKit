using System;
using AbilityKit.Demo.Moba.Components;
using AbilityKit.Core.Logging;
using AbilityKit.Effect;
using AbilityKit.Ability.FrameSync;
using AbilityKit.Ability.Triggering.Runtime;
using AbilityKit.Trace;

namespace AbilityKit.Demo.Moba.Services
{
    internal sealed class BuffContextService
    {
        private readonly MobaTraceRegistry _trace;
        private readonly ITriggerActionRunner _actionRunner;
        private readonly IFrameTime _frameTime;

        public BuffContextService(MobaTraceRegistry trace, ITriggerActionRunner actionRunner, IFrameTime frameTime)
        {
            _trace = trace;
            _actionRunner = actionRunner;
            _frameTime = frameTime;
        }

        public void EnsureBuffContext(BuffRuntime rt, int buffId, int sourceActorId, int targetActorId, in BuffOriginContext origin)
        {
            if (rt == null) return;

            var sourceOrigin = ResolveSourceOrigin(sourceActorId, targetActorId, buffId, in origin);
            var parentContextId = sourceOrigin.EffectiveParentContextId;
            var buffContextId = rt.SourceContextId;

            if (buffContextId == 0 && _trace != null)
            {
                buffContextId = parentContextId != 0
                    ? _trace.CreateChildContext(
                        parentContextId,
                        MobaTraceKind.BuffApply,
                        buffId,
                        sourceActorId,
                        targetActorId,
                        origin.ToOriginSourceEndpoint(),
                        origin.ToOriginTargetEndpoint())
                    : _trace.CreateRootContext(
                        MobaTraceKind.BuffApply,
                        buffId,
                        sourceActorId,
                        targetActorId,
                        origin.ToOriginSourceEndpoint(),
                        origin.ToOriginTargetEndpoint());
            }

            var buffOrigin = MobaGameplayOriginBuilder.Create()
                .FromOrigin(in sourceOrigin)
                .WithActors(sourceActorId, targetActorId)
                .WithImmediate(MobaTraceKind.BuffApply, buffId, buffContextId)
                .WithRootContext(sourceOrigin.EffectiveRootContextId != 0L ? sourceOrigin.EffectiveRootContextId : buffContextId)
                .WithOwnerContext(sourceOrigin.OwnerContextId != 0L ? sourceOrigin.OwnerContextId : buffContextId)
                .WithSkillRuntimeIfMissing(origin.SkillRuntimeHandle)
                .Build();

            rt.SourceContextId = buffContextId;
            rt.Origin = buffOrigin;
            rt.ContextSource = MobaContextSourceView.FromOrigin(
                in buffOrigin,
                MobaContextSourceResolveKind.DirectProvider,
                MobaContextSourceBoundary.Snapshot,
                false,
                "Buff",
                buffId);
        }

        public void CancelAndEnd(BuffRuntime rt)
        {
            if (rt == null) return;

            if (rt.SourceContextId == 0) return;

            try
            {
                _actionRunner?.CancelByOwnerKey(rt.SourceContextId);
            }
            catch (Exception ex)
            {
                Log.Exception(ex, $"[BuffContextService] CancelByOwnerKey exception (sourceContextId={rt.SourceContextId})");
            }

            try
            {
                _trace?.EndContext(rt.SourceContextId, TraceLifecycleReason.Replaced);
            }
            catch (Exception ex)
            {
                Log.Exception(ex, $"[BuffContextService] Trace.End exception (sourceContextId={rt.SourceContextId})");
            }

            ClearSourceSnapshot(rt);
        }

        public void EndByRuntime(BuffRuntime rt, TraceLifecycleReason reason)
        {
            if (rt == null) return;

            EndByRuntimeNoClear(rt, reason);
            ClearSourceSnapshot(rt);
        }

        public void EndByRuntimeNoClear(BuffRuntime rt, TraceLifecycleReason reason)
        {
            if (rt == null) return;

            if (rt.SourceContextId == 0) return;

            try
            {
                _actionRunner?.CancelByOwnerKey(rt.SourceContextId);
            }
            catch (Exception ex)
            {
                Log.Exception(ex, $"[BuffContextService] CancelByOwnerKey exception (sourceContextId={rt.SourceContextId})");
            }

            try
            {
                _trace?.EndContext(rt.SourceContextId, reason);
            }
            catch (Exception ex)
            {
                Log.Exception(ex, $"[BuffContextService] Trace.End exception (sourceContextId={rt.SourceContextId}, reason={reason})");
            }
        }

        private static MobaGameplayOrigin ResolveSourceOrigin(int sourceActorId, int targetActorId, int buffId, in BuffOriginContext origin)
        {
            var hasOrigin = origin.TryGetOrigin(out var sourceOrigin);
            return hasOrigin
                ? sourceOrigin.WithActors(sourceActorId, targetActorId)
                : MobaGameplayOrigin.FromLegacy(sourceActorId, targetActorId, MobaTraceKind.BuffApply, buffId, 0, in origin.SkillRuntimeHandle);
        }

        private static void ClearSourceSnapshot(BuffRuntime rt)
        {
            if (rt == null) return;
            rt.SourceContextId = 0;
            rt.Origin = default;
            rt.ContextSource = default;
        }

        private int GetFrame()
        {
            if (_frameTime != null) return _frameTime.Frame.Value;
            throw new InvalidOperationException("BuffContextService requires IFrameTime for buff context frames.");
        }
    }
}
