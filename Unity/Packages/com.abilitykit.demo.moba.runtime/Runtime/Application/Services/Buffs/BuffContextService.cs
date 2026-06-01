using System;
using AbilityKit.Demo.Moba.Components;
using AbilityKit.Core.Common.Log;
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
            if (rt.SourceContextId != 0) return;
            if (_trace == null) return;

            var parentContextId = origin.ParentContextId;
            if (parentContextId != 0)
            {
                rt.SourceContextId = _trace.CreateChildContext(
                    parentContextId,
                    MobaTraceKind.BuffApply,
                    buffId,
                    sourceActorId,
                    targetActorId,
                    origin.ToOriginSourceEndpoint(),
                    origin.ToOriginTargetEndpoint());
            }
            else
            {
                rt.SourceContextId = _trace.CreateRootContext(
                    MobaTraceKind.BuffApply,
                    buffId,
                    sourceActorId,
                    targetActorId,
                    origin.ToOriginSourceEndpoint(),
                    origin.ToOriginTargetEndpoint());
            }
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

            rt.SourceContextId = 0;
        }

        public void EndByRuntime(BuffRuntime rt, TraceLifecycleReason reason)
        {
            if (rt == null) return;

            EndByRuntimeNoClear(rt, reason);
            rt.SourceContextId = 0;
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

        private int GetFrame()
        {
            try
            {
                return _frameTime != null ? _frameTime.Frame.Value : 0;
            }
            catch
            {
                return 0;
            }
        }
    }
}
