using System;
using System.Collections.Generic;
using AbilityKit.Ability.FrameSync;
using AbilityKit.Demo.Moba.Config.Core;
using AbilityKit.Demo.Moba;
using AbilityKit.Demo.Moba.Components;
using AbilityKit.Core.Common.Log;
using AbilityKit.Effect;
using AbilityKit.Core.Math;
using AbilityKit.Ability.Triggering;
using AbilityKit.Ability.Triggering.Runtime;
using AbilityKit.Pipeline;
using AbilityKit.Trace;

namespace AbilityKit.Demo.Moba.Services
{
    using AbilityKit.Ability;
    public sealed class SkillPipelineRunner
    {
        public readonly struct RunningSnapshot
        {
            public readonly long InstanceId;
            public readonly int OwnerActorId;
            public readonly int SkillId;
            public readonly int SkillSlot;
            public readonly int SkillLevel;
            public readonly int StartFrame;
            public readonly int Sequence;
            public readonly int TargetActorId;
            public readonly Vec3 AimPos;
            public readonly Vec3 AimDir;
            public readonly SkillCastStage Stage;
            public readonly int ElapsedMs;
            public readonly int NextEventIndex;

            public RunningSnapshot(
                long instanceId,
                int ownerActorId,
                int skillId,
                int skillSlot,
                int skillLevel,
                int startFrame,
                int sequence,
                int targetActorId,
                Vec3 aimPos,
                Vec3 aimDir,
                SkillCastStage stage,
                int elapsedMs,
                int nextEventIndex)
            {
                InstanceId = instanceId;
                OwnerActorId = ownerActorId;
                SkillId = skillId;
                SkillSlot = skillSlot;
                SkillLevel = skillLevel;
                StartFrame = startFrame;
                Sequence = sequence;
                TargetActorId = targetActorId;
                AimPos = aimPos;
                AimDir = aimDir;
                Stage = stage;
                ElapsedMs = elapsedMs;
                NextEventIndex = nextEventIndex;
            }
        }

        private readonly int _actorId;
        private readonly IMobaBattleDiagnosticsService _diagnostics;
        private readonly IMobaBattleExceptionPolicy _exceptions;
        private readonly List<Entry> _running = new List<Entry>(4);
        private readonly List<RunningSnapshot> _ended = new List<RunningSnapshot>(2);

        public string LastFailReason { get; private set; }

        public SkillPipelineRunner(int actorId, IMobaBattleDiagnosticsService diagnostics = null, IMobaBattleExceptionPolicy exceptions = null)
        {
            _actorId = actorId;
            _diagnostics = diagnostics;
            _exceptions = exceptions;
        }

        public int ActorId => _actorId;

        public bool HasRunning => _running.Count > 0;

        public bool TryGetLatestRunningBySlot(int slot, out RunningSnapshot snapshot)
        {
            snapshot = default;
            if (slot <= 0) return false;

            for (int i = _running.Count - 1; i >= 0; i--)
            {
                var e = _running[i];
                if (e.Context == null) continue;
                if (e.Context.SkillSlot != slot) continue;

                snapshot = CreateSnapshot(in e, ToSkillCastStage(e.Stage));
                return true;
            }

            return false;
        }

        public bool TryGetRunningByInstanceId(long instanceId, out RunningSnapshot snapshot)
        {
            snapshot = default;
            if (instanceId == 0L) return false;

            for (int i = _running.Count - 1; i >= 0; i--)
            {
                var e = _running[i];
                if (e.Context == null) continue;
                if (GetInstanceId(in e) != instanceId) continue;

                snapshot = CreateSnapshot(in e, ToSkillCastStage(e.Stage));
                return true;
            }

            return false;
        }

        public bool UpdateInputBySlot(int slot, in Vec3 aimPos, in Vec3 aimDir, int targetActorId)
        {
            if (slot <= 0) return false;

            for (int i = _running.Count - 1; i >= 0; i--)
            {
                var e = _running[i];
                if (e.Context == null) continue;
                if (e.Context.SkillSlot != slot) continue;

                e.Context.UpdateInput(in aimPos, in aimDir, targetActorId);
                if (e.TriggerContext != null)
                {
                    e.TriggerContext.AimPos = e.Context.AimPos;
                    e.TriggerContext.AimDir = e.Context.AimDir;
                    e.TriggerContext.TargetActorId = e.Context.TargetActorId;
                }

                _running[i] = e;
                return true;
            }

            return false;
        }

        public bool MarkReleaseBySlot(int slot)
        {
            if (slot <= 0) return false;

            for (int i = _running.Count - 1; i >= 0; i--)
            {
                var e = _running[i];
                if (e.Context == null) continue;
                if (e.Context.SkillSlot != slot) continue;

                e.Context.MarkInputReleased();
                _running[i] = e;
                return true;
            }

            return false;
        }

        public void FillRunningSnapshots(List<RunningSnapshot> buffer)
        {
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));

            buffer.Clear();
            if (_running.Count == 0) return;

            for (int i = 0; i < _running.Count; i++)
            {
                var e = _running[i];
                if (e.Context == null) continue;

                buffer.Add(CreateSnapshot(in e, ToSkillCastStage(e.Stage)));
            }
        }

        public void FillEndedSnapshots(List<RunningSnapshot> buffer)
        {
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));

            buffer.Clear();
            if (_ended.Count == 0) return;

            buffer.AddRange(_ended);
            _ended.Clear();
        }

        public bool Start(
            IAbilityPipelineConfig preCastConfig,
            IReadOnlyList<IAbilityPipelinePhase<SkillPipelineContext>> preCastPhases,
            IAbilityPipelineConfig castConfig,
            IReadOnlyList<IAbilityPipelinePhase<SkillPipelineContext>> castPhases,
            object abilityInstance,
            in SkillCastRequest request)
        {
            return Start(preCastConfig, preCastPhases, castConfig, castPhases, abilityInstance, in request, triggerContext: null, out _);
        }

        public bool Start(
            IAbilityPipelineConfig preCastConfig,
            IReadOnlyList<IAbilityPipelinePhase<SkillPipelineContext>> preCastPhases,
            IAbilityPipelineConfig castConfig,
            IReadOnlyList<IAbilityPipelinePhase<SkillPipelineContext>> castPhases,
            object abilityInstance,
            in SkillCastRequest request,
            SkillCastContext triggerContext)
        {
            return Start(preCastConfig, preCastPhases, castConfig, castPhases, abilityInstance, in request, triggerContext, out _);
        }

        public bool Start(
            IAbilityPipelineConfig preCastConfig,
            IReadOnlyList<IAbilityPipelinePhase<SkillPipelineContext>> preCastPhases,
            IAbilityPipelineConfig castConfig,
            IReadOnlyList<IAbilityPipelinePhase<SkillPipelineContext>> castPhases,
            object abilityInstance,
            in SkillCastRequest request,
            out string failReason,
            bool allowParallel = false,
            bool interruptRunning = false)
        {
            return Start(preCastConfig, preCastPhases, castConfig, castPhases, abilityInstance, in request, triggerContext: null, out failReason, allowParallel: allowParallel, interruptRunning: interruptRunning);
        }

        public bool Start(
            IAbilityPipelineConfig preCastConfig,
            IReadOnlyList<IAbilityPipelinePhase<SkillPipelineContext>> preCastPhases,
            IAbilityPipelineConfig castConfig,
            IReadOnlyList<IAbilityPipelinePhase<SkillPipelineContext>> castPhases,
            object abilityInstance,
            in SkillCastRequest request,
            SkillCastContext triggerContext,
            out string failReason,
            bool allowParallel = false,
            bool interruptRunning = false)
        {
            failReason = null;
            LastFailReason = null;

            if (!allowParallel && _running.Count > 0)
            {
                if (interruptRunning)
                {
                    SkillLogger.Instance.LogSkillCancel(request.CasterActorId, request.SkillId, triggerContext?.SourceContextId ?? 0, "InterruptRunning");
                    CancelAll();
                }
                else
                {
                    failReason = "Skill is already running.";
                    LastFailReason = failReason;
                    SkillLogger.Instance.LogSkillFail(request.CasterActorId, request.SkillId, triggerContext?.SourceContextId ?? 0, failReason);
                    return false;
                }
            }

            if (triggerContext == null)
            {
                failReason = "Skill cast context is required.";
                LastFailReason = failReason;
                SkillLogger.Instance.LogSkillFail(request.CasterActorId, request.SkillId, 0L, failReason);
                return false;
            }

            if (castConfig == null)
            {
                failReason = "Skill cast pipeline config is missing.";
                LastFailReason = failReason;
                SkillLogger.Instance.LogSkillFail(request.CasterActorId, request.SkillId, triggerContext.SourceContextId, failReason);
                return false;
            }
            if (castPhases == null || castPhases.Count == 0)
            {
                failReason = "Skill cast pipeline phases are missing.";
                LastFailReason = failReason;
                SkillLogger.Instance.LogSkillFail(request.CasterActorId, request.SkillId, triggerContext.SourceContextId, failReason);
                return false;
            }

            SkillLogger.Instance.LogSkillStart(
                request.CasterActorId,
                request.SkillId,
                request.SkillSlot,
                triggerContext.SkillLevel,
                request.TargetActorId,
                request.AimPos,
                request.AimDir,
                triggerContext.SourceContextId);

            var entry = new Entry(
                preCastConfig,
                preCastPhases,
                castConfig,
                castPhases,
                abilityInstance,
                request,
                triggerContext);

            try
            {
                var ft = request.WorldServices != null ? request.WorldServices.Resolve<IFrameTime>() : null;
                if (ft == null)
                {
                    failReason = "IFrameTime is required to start skill pipeline.";
                    LastFailReason = failReason;
                    SkillLogger.Instance.LogSkillFail(request.CasterActorId, request.SkillId, triggerContext.SourceContextId, failReason);
                    return false;
                }

                entry.StartFrame = ft.Frame.Value;
            }
            catch (Exception ex)
            {
                failReason = "Failed to resolve skill pipeline start frame.";
                LastFailReason = failReason;
                Log.Exception(ex, $"[SkillPipelineRunner] {failReason} actor={request.CasterActorId} skillId={request.SkillId}");
                SkillLogger.Instance.LogSkillFail(request.CasterActorId, request.SkillId, triggerContext.SourceContextId, failReason);
                return false;
            }

            // If PreCast is missing, go straight to Cast.
            if (preCastConfig == null || preCastPhases == null || preCastPhases.Count == 0)
            {
                var ok = StartCast(ref entry);
                if (ok && entry.Run != null && entry.Run.State == EAbilityPipelineState.Executing)
                {
                    _running.Add(entry);
                }
                failReason = entry.FailReason;
                LastFailReason = entry.FailReason;
                return ok;
            }

            var started = StartPreCast(ref entry);
            if (started) _running.Add(entry);
            failReason = entry.FailReason;
            LastFailReason = entry.FailReason;
            return started;
        }

        private static bool StartPreCast(ref Entry entry)
        {
            entry.Stage = EntryStage.PreCast;
            entry.Pipeline = new SkillCastPipeline();
            for (int i = 0; i < entry.PreCastPhases.Count; i++)
            {
                entry.Pipeline.AddPhase(entry.PreCastPhases[i]);
            }

            entry.Context = new SkillPipelineContext();
            entry.Context.Initialize(entry.AbilityInstance, in entry.Request, entry.TriggerContext);
            entry.Context.SetFrame(entry.StartFrame);
            entry.Run = entry.Pipeline.Start(entry.PreCastConfig, entry.Context);

            var instanceId = entry.TriggerContext != null ? entry.TriggerContext.SourceContextId : 0L;
            SkillLogger.Instance.LogSkillStage(entry.TriggerContext.CasterActorId, entry.TriggerContext.SkillId, instanceId, "PreCast", "Starting");

            MobaSkillTriggering.Publish(MobaSkillTriggering.Events.PreCastStart, entry.TriggerContext);

            entry.Run.Tick(0f);
            var state = entry.Run.State;
            if (state == EAbilityPipelineState.Executing) return true;
            if (state == EAbilityPipelineState.Completed)
            {
                SkillLogger.Instance.LogSkillStage(entry.TriggerContext.CasterActorId, entry.TriggerContext.SkillId, instanceId, "PreCast", "Completed");
                MobaSkillTriggering.Publish(MobaSkillTriggering.Events.PreCastComplete, entry.TriggerContext);
                RunCleanups(in entry, "precast.complete.immediate");
                // Immediately chain to Cast.
                return StartCast(ref entry);
            }

            entry.FailReason = TryGetFailReason(entry);
            SkillLogger.Instance.LogSkillFail(entry.TriggerContext.CasterActorId, entry.TriggerContext.SkillId, instanceId, $"PreCastFailed: {entry.FailReason}");
            MobaSkillTriggering.Publish(MobaSkillTriggering.Events.PreCastFail, entry.TriggerContext, entry.FailReason);

            TryEndTraceContext(entry, TraceLifecycleReason.Cancelled);
            RunCleanups(in entry, "precast.failed.immediate");
            return false;
        }

        private static bool StartCast(ref Entry entry)
        {
            entry.Stage = EntryStage.Cast;
            entry.Pipeline = new SkillCastPipeline();
            for (int i = 0; i < entry.CastPhases.Count; i++)
            {
                entry.Pipeline.AddPhase(entry.CastPhases[i]);
            }

            entry.Context = new SkillPipelineContext();
            entry.Context.Initialize(entry.AbilityInstance, in entry.Request, entry.TriggerContext);
            entry.Context.SetFrame(ResolveCurrentFrame(in entry, entry.StartFrame));
            entry.Run = entry.Pipeline.Start(entry.CastConfig, entry.Context);

            var instanceId = entry.TriggerContext != null ? entry.TriggerContext.SourceContextId : 0L;
            SkillLogger.Instance.LogSkillStage(entry.TriggerContext.CasterActorId, entry.TriggerContext.SkillId, instanceId, "Cast", "Starting");

            MobaSkillTriggering.Publish(MobaSkillTriggering.Events.CastStart, entry.TriggerContext);

            entry.Run.Tick(0f);
            var state = entry.Run.State;
            if (state == EAbilityPipelineState.Executing)
            {
                entry.Stage = EntryStage.Cast;
                return true;
            }

            if (state != EAbilityPipelineState.Completed)
            {
                entry.FailReason = TryGetFailReason(entry);
                SkillLogger.Instance.LogSkillFail(entry.TriggerContext.CasterActorId, entry.TriggerContext.SkillId, instanceId, $"CastFailed: {entry.FailReason}");
                MobaSkillTriggering.Publish(MobaSkillTriggering.Events.CastFail, entry.TriggerContext, entry.FailReason);

                TryEndTraceContext(entry, TraceLifecycleReason.Cancelled);
            }
            else
            {
                SkillLogger.Instance.LogSkillComplete(entry.TriggerContext.CasterActorId, entry.TriggerContext.SkillId, instanceId, (int)(entry.Context.ElapsedTime * 1000f));
                MobaSkillTriggering.Publish(MobaSkillTriggering.Events.CastComplete, entry.TriggerContext);

                TryEndTraceContext(entry, TraceLifecycleReason.Completed);
            }

            RunCleanups(in entry, state == EAbilityPipelineState.Completed ? "cast.complete.immediate" : "cast.failed.immediate");

            return state == EAbilityPipelineState.Completed;
        }

        private static int ReadNextEventIndex(SkillPipelineContext context)
        {
            if (context == null) return -1;
            try
            {
                return context.TimelineNextEventIndex;
            }
            catch (Exception ex)
            {
                Log.Exception(ex, $"[SkillPipelineRunner] read TimelineNextEventIndex failed (actor={context.CasterActorId}, skill={context.SkillId}, runtime={context.RuntimeId})");
                return -1;
            }
        }

        private static void TryEndSkillRuntime(in Entry entry, MobaSkillRuntimeEndReason reason)
        {
            var handle = entry.TriggerContext != null ? entry.TriggerContext.RuntimeHandle : default;
            if (!handle.IsValid) return;

            MobaSkillCastRuntimeService runtimes = null;
            try
            {
                runtimes = entry.Request.WorldServices != null ? entry.Request.WorldServices.Resolve<MobaSkillCastRuntimeService>() : null;
            }
            catch (Exception ex)
            {
                Log.Exception(ex, $"[SkillPipelineRunner] resolve MobaSkillCastRuntimeService failed (actor={entry.Request.CasterActorId}, skill={entry.Request.SkillId}, handle={handle.RuntimeId}:{handle.Generation}, reason={reason})");
                runtimes = null;
            }

            if (runtimes == null) return;
            runtimes.MarkPipelineEnded(in handle, reason);
        }

        private static void TryCancelSkillRuntime(in Entry entry, MobaSkillRuntimeEndReason reason = MobaSkillRuntimeEndReason.Cancelled)
        {
            var handle = entry.TriggerContext != null ? entry.TriggerContext.RuntimeHandle : default;
            if (!handle.IsValid) return;

            MobaSkillCastRuntimeService runtimes = null;
            try
            {
                runtimes = entry.Request.WorldServices != null ? entry.Request.WorldServices.Resolve<MobaSkillCastRuntimeService>() : null;
            }
            catch (Exception ex)
            {
                Log.Exception(ex, $"[SkillPipelineRunner] resolve MobaSkillCastRuntimeService failed (actor={entry.Request.CasterActorId}, skill={entry.Request.SkillId}, handle={handle.RuntimeId}:{handle.Generation}, reason={reason})");
                runtimes = null;
            }

            if (runtimes == null) return;
            runtimes.Cancel(in handle, reason);
        }

        private static void TryEndTraceContext(in Entry entry, TraceLifecycleReason reason)
        {
            if (entry.TriggerContext != null && entry.TriggerContext.RuntimeHandle.IsValid)
            {
                TryEndSkillRuntime(in entry, ToRuntimeEndReason(reason));
                return;
            }

            var rootId = 0L;
            try
            {
                rootId = entry.TriggerContext != null ? entry.TriggerContext.SourceContextId : 0L;
            }
            catch (Exception ex)
            {
                Log.Exception(ex, "[SkillPipelineRunner] read TriggerContext.SourceContextId failed");
                rootId = 0L;
            }

            if (rootId == 0) return;

            MobaTraceRegistry trace = null;
            try
            {
                trace = entry.Request.WorldServices != null ? entry.Request.WorldServices.Resolve<MobaTraceRegistry>() : null;
            }
            catch (Exception ex)
            {
                Log.Exception(ex, "[SkillPipelineRunner] resolve MobaTraceRegistry failed");
                trace = null;
            }

            if (trace == null) return;

            try
            {
                try
                {
                    var origin = new TraceOrigin(
                        kind: (int)MobaTraceKind.SkillCast,
                        sourceActorId: entry.Request.CasterActorId,
                        targetActorId: entry.Request.TargetActorId,
                        originSource: TraceEndpoint.Actor(entry.Request.CasterActorId),
                        originTarget: TraceEndpoint.Actor(entry.Request.TargetActorId),
                        configId: entry.Request.SkillId);
                    trace.EnsureRoot(rootId, in origin);
                }
                catch (Exception ex)
                {
                    Log.Exception(ex, $"[SkillPipelineRunner] Trace.EnsureRoot failed (rootId={rootId})");
                }

                trace.EndContext(rootId, reason);
            }
            catch (Exception ex)
            {
                Log.Exception(ex, $"[SkillPipelineRunner] Trace.EndContext failed (rootId={rootId}, reason={reason})");
            }
        }

        private static MobaSkillRuntimeEndReason ToRuntimeEndReason(TraceLifecycleReason reason)
        {
            switch (reason)
            {
                case TraceLifecycleReason.Completed:
                    return MobaSkillRuntimeEndReason.PipelineCompleted;
                case TraceLifecycleReason.Failed:
                    return MobaSkillRuntimeEndReason.Failed;
                case TraceLifecycleReason.Dead:
                    return MobaSkillRuntimeEndReason.OwnerRemoved;
                case TraceLifecycleReason.Cancelled:
                case TraceLifecycleReason.Interrupted:
                    return MobaSkillRuntimeEndReason.Cancelled;
                default:
                    return MobaSkillRuntimeEndReason.Cancelled;
            }
        }

        private static string TryGetFailReason(in Entry entry)
        {
            if (entry.Context == null) return null;
            return entry.Context.FailReason;
        }

        private static int ResolveCurrentFrame(in Entry entry, int defaultFrame)
        {
            try
            {
                var time = entry.Request.WorldServices != null ? entry.Request.WorldServices.Resolve<IFrameTime>() : null;
                return time != null ? time.Frame.Value : defaultFrame;
            }
            catch (Exception ex)
            {
                Log.Exception(ex, $"[SkillPipelineRunner] resolve current frame failed (actor={entry.Request.CasterActorId}, skill={entry.Request.SkillId}, defaultFrame={defaultFrame})");
                return defaultFrame;
            }
        }

        public void CancelAll()
        {
            if (_running.Count == 0) return;

            SkillLogger.Instance.LogInfo($"CancelAll: ActorId={_actorId} Count={_running.Count}");

            for (int i = 0; i < _running.Count; i++)
            {
                var e = _running[i];
                var p = e.Run;

                var instanceId = e.TriggerContext != null ? e.TriggerContext.SourceContextId : 0L;
                var stageStr = e.Stage == EntryStage.PreCast ? "PreCast" : "Cast";
                SkillLogger.Instance.LogSkillInterrupt(e.TriggerContext.CasterActorId, e.TriggerContext.SkillId, instanceId, stageStr);

                if (e.Stage == EntryStage.PreCast)
                {
                    MobaSkillTriggering.Publish(MobaSkillTriggering.Events.PreCastInterrupt, e.TriggerContext);
                }
                else
                {
                    MobaSkillTriggering.Publish(MobaSkillTriggering.Events.CastInterrupt, e.TriggerContext);
                }

                p?.Interrupt();

                TryEndTraceContext(e, TraceLifecycleReason.Cancelled);

                RunCleanups(e.Context, "cancelAll");

                TryAddEndedSnapshot(in e, SkillCastStage.Cancelled);
            }
            _running.Clear();
        }

        public bool CancelBySlot(int slot)
        {
            if (slot <= 0) return false;
            if (_running.Count == 0) return false;

            var cancelled = false;
            for (int i = _running.Count - 1; i >= 0; i--)
            {
                var e = _running[i];
                if (e.Context == null) continue;
                if (e.Context.SkillSlot != slot) continue;

                CancelAt(i, in e, "CancelBySlot");
                cancelled = true;
            }

            return cancelled;
        }

        public void CancelBySkillId(int skillId)
        {
            if (skillId <= 0) return;
            if (_running.Count == 0) return;

            for (int i = _running.Count - 1; i >= 0; i--)
            {
                var e = _running[i];
                if (e.Context == null) continue;
                if (e.Context.SkillId != skillId) continue;

                CancelAt(i, in e, "CancelBySkillId");
            }
        }

        private void CancelAt(int index, in Entry e, string reason)
        {
            var p = e.Run;

            var instanceId = GetInstanceId(in e);
            var stageStr = e.Stage == EntryStage.PreCast ? "PreCast" : "Cast";
            SkillLogger.Instance.LogSkillCancel(e.Context.CasterActorId, e.Context.SkillId, instanceId, $"{reason}_{stageStr}");

            if (e.Stage == EntryStage.PreCast)
            {
                MobaSkillTriggering.Publish(MobaSkillTriggering.Events.PreCastInterrupt, e.TriggerContext);
            }
            else
            {
                MobaSkillTriggering.Publish(MobaSkillTriggering.Events.CastInterrupt, e.TriggerContext);
            }

            p?.Interrupt();
            TryCancelSkillRuntime(in e, MobaSkillRuntimeEndReason.Cancelled);
            TryEndTraceContext(e, TraceLifecycleReason.Cancelled);

            RunCleanups(e.Context, reason);
            TryAddEndedSnapshot(in e, SkillCastStage.Cancelled);

            _running.RemoveAt(index);
        }

        public void Step(float deltaTime)
        {
            if (_running.Count == 0) return;

            var diagnostics = _diagnostics;
            var start = diagnostics != null ? diagnostics.GetTimestamp() : 0L;
            var runningAtStart = _running.Count;
            var ticked = 0;
            var ended = 0;

            for (int i = _running.Count - 1; i >= 0; i--)
            {
                var entry = _running[i];
                var p = entry.Run;
                if (p == null || entry.Context == null)
                {
                    _running.RemoveAt(i);
                    continue;
                }

                if (p.State == EAbilityPipelineState.Executing)
                {
                    ticked++;
                    entry.Context.SetFrame(ResolveCurrentFrame(in entry, entry.StartFrame));
                    entry.Context.AdvanceTime(deltaTime);
                    p.Tick(deltaTime);

                    // var instanceId = entry.TriggerContext != null ? entry.TriggerContext.SourceContextId : 0L;
                    // var stageStr = entry.Stage == EntryStage.PreCast ? "PreCast" : "Cast";
                    // SkillLogger.Instance.LogSkillTick(
                    //     entry.Context.CasterActorId,
                    //     entry.Context.SkillId,
                    //     instanceId,
                    //     deltaTime,
                    //     entry.Context.ElapsedTime,
                    //     $"{stageStr}_{p.State}");
                }

                if (p.State != EAbilityPipelineState.Executing)
                {
                    var instanceId = entry.TriggerContext != null ? entry.TriggerContext.SourceContextId : 0L;

                    if (p.State == EAbilityPipelineState.Completed && entry.Stage == EntryStage.PreCast)
                    {
                        SkillLogger.Instance.LogSkillStage(entry.Context.CasterActorId, entry.Context.SkillId, instanceId, "PreCast", "Completed_ChainingToCast");
                        MobaSkillTriggering.Publish(MobaSkillTriggering.Events.PreCastComplete, entry.TriggerContext);
                        RunCleanups(entry.Context, "precast.complete");
                        // Chain to Cast.
                        if (StartCast(ref entry))
                        {
                            _running[i] = entry;
                            continue;
                        }
                    }

                    if (p.State == EAbilityPipelineState.Completed && entry.Stage == EntryStage.Cast)
                    {
                        SkillLogger.Instance.LogSkillComplete(entry.Context.CasterActorId, entry.Context.SkillId, instanceId, (int)(entry.Context.ElapsedTime * 1000f));
                        MobaSkillTriggering.Publish(MobaSkillTriggering.Events.CastComplete, entry.TriggerContext);

                        TryEndTraceContext(entry, TraceLifecycleReason.Completed);

                        RunCleanups(entry.Context, "cast.complete");

                        TryAddEndedSnapshot(in entry, SkillCastStage.Completed);
                    }

                    if (p.State != EAbilityPipelineState.Completed)
                    {
                        entry.FailReason = entry.FailReason ?? TryGetFailReason(entry);
                        LastFailReason = entry.FailReason;

                        var stageStr = entry.Stage == EntryStage.PreCast ? "PreCast" : "Cast";
                        SkillLogger.Instance.LogSkillFail(entry.Context.CasterActorId, entry.Context.SkillId, instanceId, $"{stageStr}Failed: {entry.FailReason}");

                        if (entry.Stage == EntryStage.PreCast)
                        {
                            MobaSkillTriggering.Publish(MobaSkillTriggering.Events.PreCastFail, entry.TriggerContext, entry.FailReason);
                        }
                        else
                        {
                            MobaSkillTriggering.Publish(MobaSkillTriggering.Events.CastFail, entry.TriggerContext, entry.FailReason);
                        }

                        TryEndTraceContext(entry, TraceLifecycleReason.Cancelled);

                        RunCleanups(entry.Context, "pipeline.failed");

                        var terminal = entry.Context != null && entry.Context.IsAborted ? SkillCastStage.Cancelled : SkillCastStage.Failed;
                        TryAddEndedSnapshot(in entry, terminal);
                    }

                    _running.RemoveAt(i);
                    ended++;
                }
            }

            if (diagnostics != null)
            {
                diagnostics.Sample("moba.skill.runner.running", runningAtStart);
                diagnostics.Sample("moba.skill.runner.ticked", ticked);
                diagnostics.Sample("moba.skill.runner.ended", ended);
                diagnostics.RecordDuration(
                    MobaBattleDiagnosticMetric.SkillRunnerStep,
                    start,
                    MobaBattleDiagnosticsDefaults.SkillRunnerStepWarnMs,
                    $"actor={_actorId} running={runningAtStart} ticked={ticked} ended={ended} remaining={_running.Count}");
            }
        }

        private void RunCleanups(SkillPipelineContext context, string reason)
        {
            try
            {
                context?.RunAndClearCleanups();
            }
            catch (Exception ex)
            {
                ReportCleanupException(_exceptions, _diagnostics, ex, _actorId, context != null ? context.SkillId : 0, context != null ? context.RuntimeId : 0L, reason);
            }
        }

        private static void RunCleanups(in Entry entry, string reason)
        {
            try
            {
                entry.Context?.RunAndClearCleanups();
            }
            catch (Exception ex)
            {
                var exceptions = ResolveExceptions(in entry);
                var diagnostics = exceptions == null ? ResolveDiagnostics(in entry) : null;
                var actorId = entry.Context != null ? entry.Context.CasterActorId : entry.Request.CasterActorId;
                var skillId = entry.Context != null ? entry.Context.SkillId : entry.Request.SkillId;
                var runtimeId = entry.Context != null ? entry.Context.RuntimeId : 0L;
                ReportCleanupException(exceptions, diagnostics, ex, actorId, skillId, runtimeId, reason);
            }
        }

        private static IMobaBattleExceptionPolicy ResolveExceptions(in Entry entry)
        {
            try
            {
                return entry.Request.WorldServices != null
                    ? entry.Request.WorldServices.Resolve<IMobaBattleExceptionPolicy>()
                    : null;
            }
            catch (Exception ex)
            {
                Log.Exception(ex, $"[SkillPipelineRunner] resolve IMobaBattleExceptionPolicy failed (actor={entry.Request.CasterActorId}, skill={entry.Request.SkillId})");
                return null;
            }
        }

        private static IMobaBattleDiagnosticsService ResolveDiagnostics(in Entry entry)
        {
            try
            {
                return entry.Request.WorldServices != null
                    ? entry.Request.WorldServices.Resolve<IMobaBattleDiagnosticsService>()
                    : null;
            }
            catch (Exception ex)
            {
                Log.Exception(ex, $"[SkillPipelineRunner] resolve IMobaBattleDiagnosticsService failed (actor={entry.Request.CasterActorId}, skill={entry.Request.SkillId})");
                return null;
            }
        }

        private static void ReportCleanupException(IMobaBattleDiagnosticsService diagnostics, Exception ex, int actorId, string reason)
        {
            ReportCleanupException(null, diagnostics, ex, actorId, 0, 0L, reason);
        }

        private static void ReportCleanupException(IMobaBattleExceptionPolicy exceptions, IMobaBattleDiagnosticsService diagnostics, Exception ex, int actorId, int skillId, long runtimeId, string reason)
        {
            if (exceptions != null)
            {
                exceptions.Handle(
                    ex,
                    new MobaBattleExceptionContext(
                        MobaBattleExceptionDomain.Cleanup,
                        "skill.runner.cleanup",
                        actorId: actorId,
                        skillId: skillId,
                        runtimeId: runtimeId,
                        detail: "reason=" + reason),
                    MobaBattleExceptionSeverity.Recoverable);
            }
            else if (diagnostics != null)
            {
                diagnostics.Exception(
                    "skill.runner.cleanup",
                    ex,
                    $"Skill cleanup failed. actor={actorId} reason={reason}");
                diagnostics.Counter("moba.skill.runner.cleanupExceptions");
            }
            else
            {
                Log.Exception(ex, $"[SkillPipelineRunner] Skill cleanup failed. actor={actorId} reason={reason}");
            }
        }

        private void TryAddEndedSnapshot(in Entry e, SkillCastStage terminalStage)
        {
            if (e.Context == null) return;
            if (GetInstanceId(in e) == 0L) return;

            _ended.Add(CreateSnapshot(in e, terminalStage));
        }

        private static SkillCastStage ToSkillCastStage(EntryStage stage)
        {
            return stage == EntryStage.PreCast ? SkillCastStage.PreCast : SkillCastStage.Cast;
        }

        private static long GetInstanceId(in Entry e)
        {
            return e.TriggerContext != null ? e.TriggerContext.SourceContextId : 0L;
        }

        private static RunningSnapshot CreateSnapshot(in Entry e, SkillCastStage stage)
        {
            var elapsedMs = e.Context != null ? (int)(e.Context.ElapsedTime * 1000f) : 0;
            var nextEventIndex = e.Context != null ? e.Context.TimelineNextEventIndex : 0;

            return new RunningSnapshot(
                instanceId: GetInstanceId(in e),
                ownerActorId: e.Context != null ? e.Context.CasterActorId : 0,
                skillId: e.Context != null ? e.Context.SkillId : 0,
                skillSlot: e.Context != null ? e.Context.SkillSlot : 0,
                skillLevel: e.TriggerContext != null ? e.TriggerContext.SkillLevel : 0,
                startFrame: e.StartFrame,
                sequence: e.TriggerContext != null ? e.TriggerContext.Sequence : 0,
                targetActorId: e.Context != null ? e.Context.TargetActorId : 0,
                aimPos: e.Context != null ? e.Context.AimPos : Vec3.Zero,
                aimDir: e.Context != null ? e.Context.AimDir : Vec3.Forward,
                stage: stage,
                elapsedMs: elapsedMs,
                nextEventIndex: nextEventIndex);
        }

        private enum EntryStage
        {
            PreCast = 0,
            Cast = 1,
        }

        private struct Entry
        {
            public EntryStage Stage;
            public SkillCastPipeline Pipeline;
            public IAbilityPipelineRun<SkillPipelineContext> Run;
            public SkillPipelineContext Context;
            public string FailReason;

            public int StartFrame;

            public readonly IAbilityPipelineConfig PreCastConfig;
            public readonly IReadOnlyList<IAbilityPipelinePhase<SkillPipelineContext>> PreCastPhases;
            public readonly IAbilityPipelineConfig CastConfig;
            public readonly IReadOnlyList<IAbilityPipelinePhase<SkillPipelineContext>> CastPhases;
            public readonly object AbilityInstance;
            public readonly SkillCastRequest Request;
            public readonly SkillCastContext TriggerContext;

            public Entry(
                IAbilityPipelineConfig preCastConfig,
                IReadOnlyList<IAbilityPipelinePhase<SkillPipelineContext>> preCastPhases,
                IAbilityPipelineConfig castConfig,
                IReadOnlyList<IAbilityPipelinePhase<SkillPipelineContext>> castPhases,
                object abilityInstance,
                SkillCastRequest request,
                SkillCastContext triggerContext)
            {
                Stage = EntryStage.PreCast;
                Pipeline = null;
                Run = null;
                Context = null;
                FailReason = null;

                StartFrame = 0;
                PreCastConfig = preCastConfig;
                PreCastPhases = preCastPhases;
                CastConfig = castConfig;
                CastPhases = castPhases;
                AbilityInstance = abilityInstance;
                Request = request;
                TriggerContext = triggerContext;
            }
        }
    }
}
