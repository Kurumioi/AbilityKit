using System;
using System.Collections.Generic;
using AbilityKit.Ability.World.DI;
using AbilityKit.Ability.World.Services;
using AbilityKit.Ability.World.Services.Attributes;
using AbilityKit.Core.Common.Log;
using AbilityKit.Trace;

namespace AbilityKit.Demo.Moba.Services
{
    [WorldService(typeof(MobaBuffService))]
    public sealed class MobaBuffService : IWorldInitializable
    {
        private enum BuffCommandKind
        {
            Apply = 0,
            Remove = 1,
        }

        private sealed class BuffCommand
        {
            public long Seq;
            public BuffCommandKind Kind;
            public BuffApplyRequest ApplyRequest;
            public BuffRemoveRequest RemoveRequest;

            public int BuffId => ApplyRequest != null ? ApplyRequest.BuffId : RemoveRequest != null ? RemoveRequest.BuffId : 0;
        }

        [WorldInject] private MobaActorLookupService _actors;
        [WorldInject(required: false)] private IMobaBattleDiagnosticsService _diagnostics;
        [WorldInject(required: false)] private IMobaBattleExceptionPolicy _exceptions;
        private BuffLifecycleExecutor _lifecycle;
        private long _nextCommandSeq;
        private readonly List<BuffCommand> _pending = new List<BuffCommand>(32);
        private int _draining;

        public void OnInit(IWorldResolver services)
        {
            _lifecycle = BuffLifecycleExecutorFactory.Create(services);
        }

        public global::ActorEntity TryGetActorEntity(int actorId)
        {
            if (_actors != null && _actors.TryGetActorEntity(actorId, out var e) && e != null)
            {
                return e;
            }
            return null;
        }

        public bool ApplyBuffImmediate(global::ActorEntity target, int buffId, int sourceActorId, int durationOverrideMs)
        {
            if (target == null || !target.hasActorId) return false;
            return ApplyBuffImmediate(target.actorId.Value, buffId, sourceActorId, durationOverrideMs, default(BuffOriginContext));
        }

        public bool ApplyBuffImmediate(int targetActorId, int buffId, int sourceActorId, int durationOverrideMs)
        {
            return ApplyBuffImmediate(targetActorId, buffId, sourceActorId, durationOverrideMs, default(BuffOriginContext));
        }

        public bool ApplyBuffImmediate(int targetActorId, int buffId, int sourceActorId, int durationOverrideMs, in BuffOriginContext origin)
        {
            if (!EnqueueApply(targetActorId, buffId, sourceActorId, durationOverrideMs, origin))
            {
                return false;
            }

            DrainPending(maxCommands: 256);
            return true;
        }

        public bool RemoveBuffImmediate(global::ActorEntity target, int buffId, int sourceActorId, TraceLifecycleReason reason)
        {
            if (target == null || !target.hasActorId) return false;
            return RemoveBuffImmediate(target.actorId.Value, buffId, sourceActorId, reason);
        }

        public bool RemoveBuffImmediate(int targetActorId, int buffId, int sourceActorId, TraceLifecycleReason reason)
        {
            if (!EnqueueRemove(targetActorId, buffId, sourceActorId, reason))
            {
                return false;
            }

            DrainPending(maxCommands: 256);
            return true;
        }

        public int RemoveBuffsImmediate(int targetActorId, int buffId, int sourceActorId, bool removeAll, TraceLifecycleReason reason)
        {
            if (targetActorId <= 0) return 0;

            var target = TryGetActorEntity(targetActorId);
            if (target == null || !target.hasBuffs || target.buffs.Active == null || target.buffs.Active.Count == 0)
            {
                return 0;
            }

            var active = target.buffs.Active;
            var queued = 0;
            for (var i = active.Count - 1; i >= 0; i--)
            {
                var runtime = active[i];
                if (runtime == null) continue;
                if (buffId > 0 && runtime.BuffId != buffId) continue;
                if (sourceActorId > 0 && runtime.SourceId != sourceActorId) continue;

                var removeSourceId = sourceActorId > 0 ? sourceActorId : runtime.SourceId;
                if (!EnqueueRemove(targetActorId, runtime.BuffId, removeSourceId, reason)) continue;

                queued++;
                if (!removeAll) break;
            }

            if (queued > 0)
            {
                DrainPending(maxCommands: Math.Max(256, queued + 32));
            }

            return queued;
        }

        public void DrainPending(int maxCommands)
        {
            if (maxCommands <= 0) return;

            // Protect against re-entrancy if drain triggers effects that call ApplyBuffImmediate again.
            if (_draining > 0) return;

            var diagnostics = _diagnostics;
            var start = diagnostics != null ? diagnostics.GetTimestamp() : 0L;
            var pendingAtStart = _pending.Count;
            var executed = 0;

            _draining++;
            try
            {
                var cursor = 0;
                while (cursor < _pending.Count)
                {
                    if (executed >= maxCommands)
                    {
                        var message = $"[MobaBuffService] DrainPending exceeded maxCommands={maxCommands}. pending={_pending.Count}.";
                        if (diagnostics != null)
                        {
                            diagnostics.Warning("buff.drain.maxCommands", message);
                            diagnostics.Counter("moba.buff.drain.maxCommandsExceeded");
                        }
                        else
                        {
                            Log.Warning(message);
                        }
                        break;
                    }

                    var cmd = _pending[cursor++];
                    if (cmd == null) continue;

                    try
                    {
                        switch (cmd.Kind)
                        {
                            case BuffCommandKind.Apply:
                                ExecuteApply(cmd.ApplyRequest);
                                break;
                            case BuffCommandKind.Remove:
                                ExecuteRemove(cmd.RemoveRequest);
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        var exceptions = _exceptions;
                        if (exceptions != null)
                        {
                            exceptions.Handle(
                                ex,
                                new MobaBattleExceptionContext(
                                    MobaBattleExceptionDomain.Buff,
                                    "command.execute",
                                    actorId: cmd.ApplyRequest != null ? cmd.ApplyRequest.TargetActorId : cmd.RemoveRequest != null ? cmd.RemoveRequest.TargetActorId : 0,
                                    detail: $"kind={cmd.Kind} buffId={cmd.BuffId}"),
                                MobaBattleExceptionSeverity.Recoverable);
                        }
                        else if (diagnostics != null)
                        {
                            diagnostics.Exception(
                                "buff.command.execute",
                                ex,
                                $"Execute buff command failed. kind={cmd.Kind} buffId={cmd.BuffId}");
                            diagnostics.Counter("moba.buff.command.exceptions");
                        }
                        else
                        {
                            Log.Exception(ex, $"[MobaBuffService] Execute buff command failed. kind={cmd.Kind} buffId={cmd.BuffId}");
                        }
                    }

                    executed++;
                }

                if (cursor > 0)
                {
                    _pending.RemoveRange(0, cursor);
                }
            }
            finally
            {
                _draining--;
                if (diagnostics != null)
                {
                    diagnostics.Sample("moba.buff.drain.pending", pendingAtStart);
                    diagnostics.Sample("moba.buff.drain.executed", executed);
                    diagnostics.Gauge("moba.buff.pending", _pending.Count);
                    diagnostics.RecordDuration(
                        MobaBattleDiagnosticMetric.BuffDrain,
                        start,
                        MobaBattleDiagnosticsDefaults.BuffDrainWarnMs,
                        $"pending={pendingAtStart} executed={executed} remaining={_pending.Count}");
                }
            }
        }

        private bool EnqueueApply(int targetActorId, int buffId, int sourceActorId, int durationOverrideMs, in BuffOriginContext origin)
        {
            if (targetActorId <= 0) return false;
            if (buffId <= 0) return false;

            var request = BuffApplyRequestBuilder.Create()
                .WithTarget(targetActorId)
                .WithBuff(buffId)
                .WithSource(sourceActorId)
                .WithDurationOverride(durationOverrideMs)
                .WithOrigin(in origin)
                .Build();

            _pending.Add(new BuffCommand
            {
                Seq = ++_nextCommandSeq,
                Kind = BuffCommandKind.Apply,
                ApplyRequest = request,
            });
            return true;
        }

        private bool EnqueueRemove(int targetActorId, int buffId, int sourceActorId, TraceLifecycleReason reason)
        {
            if (targetActorId <= 0) return false;
            if (buffId <= 0) return false;

            var request = BuffRemoveRequestBuilder.Create()
                .WithTarget(targetActorId)
                .WithBuff(buffId)
                .WithSource(sourceActorId)
                .WithReason(reason)
                .Build();

            _pending.Add(new BuffCommand
            {
                Seq = ++_nextCommandSeq,
                Kind = BuffCommandKind.Remove,
                RemoveRequest = request,
            });
            return true;
        }

        private bool ExecuteApply(BuffApplyRequest request)
        {
            if (request == null || !request.IsValid) return false;

            var target = TryGetActorEntity(request.TargetActorId);
            if (target != null && target.hasApplyBuffRequest && target.applyBuffRequest != null && target.applyBuffRequest.BuffId == request.BuffId)
            {
                target.RemoveApplyBuffRequest();
            }

            return _lifecycle.Apply(request);
        }

        private bool ExecuteRemove(BuffRemoveRequest request)
        {
            if (request == null || !request.IsValid) return false;

            var target = TryGetActorEntity(request.TargetActorId);
            if (target != null && target.hasApplyBuffRequest && target.applyBuffRequest != null && target.applyBuffRequest.BuffId == request.BuffId)
            {
                target.RemoveApplyBuffRequest();
            }

            return _lifecycle.Remove(request);
        }

        public void Dispose()
        {
            _pending.Clear();
        }
    }
}
