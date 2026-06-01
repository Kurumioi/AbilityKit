using System;
using System.Collections.Generic;
using AbilityKit.Demo.Moba.Components;
using AbilityKit.Core.Common.Log;
using AbilityKit.Ability.World.DI;
using AbilityKit.Ability.World.Services;
using AbilityKit.Ability.World.Services.Attributes;

namespace AbilityKit.Demo.Moba.Services
{
    [WorldService(typeof(MobaPeriodicEffectService))]
    public class MobaPeriodicEffectService : IWorldDeinitializable
    {
        [WorldInject] private MobaEffectInvokerService _invoker;
        [WorldInject] private MobaEffectExecutionService _effects;
        [WorldInject] private MobaActorLookupService _actors;

        private readonly Dictionary<long, MobaContinuousPeriodicRuntime> _instances = new Dictionary<long, MobaContinuousPeriodicRuntime>();
        private readonly Dictionary<long, MobaPeriodicEffectHandle> _handles = new Dictionary<long, MobaPeriodicEffectHandle>();

        private static long _nextInstanceId;

        public IMobaContinuousPeriodicHandle Start(MobaContinuousPeriodicSpec spec)
        {
            if (spec == null) return null;
            if (spec.TargetActorId <= 0) return null;
            if (_actors == null) return null;
            if (!_actors.TryGetActorEntity(spec.TargetActorId, out var e) || e == null) return null;

            if (spec.OwnerKey != 0)
            {
                StopByOwnerKey(spec.TargetActorId, spec.OwnerKey);
            }

            if (!e.hasMobaContinuousPeriodic)
            {
                e.AddMobaContinuousPeriodic(new List<MobaContinuousPeriodicRuntime>());
            }

            var list = e.mobaContinuousPeriodic.Active;
            if (list == null)
            {
                list = new List<MobaContinuousPeriodicRuntime>();
                e.ReplaceMobaContinuousPeriodic(list);
            }

            var instanceId = ++_nextInstanceId;
            var periodMs = spec.PeriodMs > 0 ? spec.PeriodMs : 0;
            var rt = new MobaContinuousPeriodicRuntime
            {
                InstanceId = instanceId,
                ActionKind = spec.ActionKind,
                ActionConfigId = spec.ActionConfigId,
                SourceActorId = spec.SourceActorId,
                TargetActorId = spec.TargetActorId,
                RemainingMs = spec.DurationMs,
                NextTickMs = periodMs,
                BasePeriodMs = periodMs,
                EffectivePeriodMs = periodMs,
                PeriodScale = 1f,
                OnStartTriggerId = spec.OnStartTriggerId,
                OnTickTriggerId = spec.OnTickTriggerId,
                OnStopTriggerId = spec.OnStopTriggerId,
                OnStartTriggerIds = spec.OnStartTriggerIds ?? Array.Empty<int>(),
                OnTickTriggerIds = spec.OnTickTriggerIds ?? Array.Empty<int>(),
                OnStopTriggerIds = spec.OnStopTriggerIds ?? Array.Empty<int>(),
                OnStartEffectId = spec.OnStartEffectId,
                OnTickEffectIds = spec.OnTickEffectIds ?? Array.Empty<int>(),
                OnStopEffectId = spec.OnStopEffectId,
                OwnerKey = spec.OwnerKey,
                SourceContextId = spec.SourceContextId,
                SkillRuntimeHandle = spec.SkillRuntimeHandle,
                ElapsedMs = 0,
                TickIndex = 0,
                Started = false,
                IsStopped = false,
            };
            list.Add(rt);
            RegisterRuntime(rt);

            ExecuteRuntimePhase(rt, MobaContinuousPeriodicPhase.Start);
            rt.Started = true;
            return RegisterHandle(rt);
        }

        public bool TryGetRuntime(long instanceId, out MobaContinuousPeriodicRuntime runtime)
        {
            if (instanceId == 0)
            {
                runtime = null;
                return false;
            }

            return _instances.TryGetValue(instanceId, out runtime) && runtime != null && !runtime.IsStopped;
        }

        public bool SetPeriodScale(long instanceId, float periodScale)
        {
            if (!TryGetRuntime(instanceId, out var runtime)) return false;
            if (periodScale <= 0f) return false;

            runtime.PeriodScale = periodScale;
            runtime.EffectivePeriodMs = CalculateEffectivePeriodMs(runtime.BasePeriodMs, runtime.PeriodScale);
            if (runtime.EffectivePeriodMs <= 0)
            {
                runtime.NextTickMs = 0;
            }
            else if (runtime.NextTickMs <= 0 || runtime.NextTickMs > runtime.EffectivePeriodMs)
            {
                runtime.NextTickMs = runtime.EffectivePeriodMs;
            }

            return true;
        }

        public bool SetPeriodMs(long instanceId, int periodMs)
        {
            if (!TryGetRuntime(instanceId, out var runtime)) return false;
            if (periodMs < 0) return false;

            runtime.BasePeriodMs = periodMs;
            runtime.EffectivePeriodMs = CalculateEffectivePeriodMs(runtime.BasePeriodMs, runtime.PeriodScale);
            if (runtime.EffectivePeriodMs <= 0)
            {
                runtime.NextTickMs = 0;
            }
            else if (runtime.NextTickMs <= 0 || runtime.NextTickMs > runtime.EffectivePeriodMs)
            {
                runtime.NextTickMs = runtime.EffectivePeriodMs;
            }

            return true;
        }

        public bool Stop(long instanceId, bool executeRemoveEffect = true)
        {
            if (!TryGetRuntime(instanceId, out var runtime)) return false;
            return StopRuntime(runtime, executeRemoveEffect);
        }

        public void NotifyRuntimeRemoved(MobaContinuousPeriodicRuntime runtime)
        {
            if (runtime == null) return;
            UnregisterRuntime(runtime);
        }

        public int StopByOwnerKey(int targetActorId, long ownerKey)
        {
            if (targetActorId <= 0) return 0;
            if (ownerKey == 0) return 0;
            if (_actors == null) return 0;
            if (!_actors.TryGetActorEntity(targetActorId, out var e) || e == null) return 0;
            if (!e.hasMobaContinuousPeriodic) return 0;

            var list = e.mobaContinuousPeriodic.Active;
            if (list == null || list.Count == 0) return 0;

            var removed = 0;

            for (int i = list.Count - 1; i >= 0; i--)
            {
                var rt = list[i];
                if (rt == null)
                {
                    list.RemoveAt(i);
                    continue;
                }

                if (rt.OwnerKey != ownerKey) continue;

                if (StopRuntime(rt, executeRemoveEffect: true))
                {
                    removed++;
                }
            }

            return removed;
        }

        private void RegisterRuntime(MobaContinuousPeriodicRuntime runtime)
        {
            if (runtime == null) return;
            if (runtime.InstanceId == 0) return;
            _instances[runtime.InstanceId] = runtime;
        }

        private MobaPeriodicEffectHandle RegisterHandle(MobaContinuousPeriodicRuntime runtime)
        {
            if (runtime == null) return null;
            if (runtime.InstanceId == 0) return null;

            var handle = new MobaPeriodicEffectHandle(this, runtime.InstanceId, runtime.OwnerKey, runtime.TargetActorId);
            _handles[runtime.InstanceId] = handle;
            return handle;
        }

        private void UnregisterRuntime(MobaContinuousPeriodicRuntime runtime)
        {
            if (runtime == null) return;
            if (runtime.InstanceId == 0) return;

            runtime.IsStopped = true;
            _instances.Remove(runtime.InstanceId);
            if (_handles.TryGetValue(runtime.InstanceId, out var handle) && handle != null)
            {
                handle.MarkDone();
            }

            _handles.Remove(runtime.InstanceId);
        }

        private bool StopRuntime(MobaContinuousPeriodicRuntime runtime, bool executeRemoveEffect)
        {
            if (runtime == null) return false;
            if (runtime.IsStopped) return false;

            var targetActorId = runtime.TargetActorId;
            if (targetActorId <= 0) return false;
            if (_actors == null) return false;
            if (!_actors.TryGetActorEntity(targetActorId, out var e) || e == null) return false;
            if (!e.hasMobaContinuousPeriodic) return false;

            var list = e.mobaContinuousPeriodic.Active;
            if (list == null || list.Count == 0) return false;

            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (!ReferenceEquals(list[i], runtime)) continue;

                if (executeRemoveEffect)
                {
                    ExecuteRemoveEffect(runtime, targetActorId);
                }

                list.RemoveAt(i);
                UnregisterRuntime(runtime);
                return true;
            }

            UnregisterRuntime(runtime);
            return false;
        }

        private void ExecuteRemoveEffect(MobaContinuousPeriodicRuntime runtime, int targetActorId)
        {
            if (runtime == null) return;
            runtime.TargetActorId = targetActorId;
            ExecuteRuntimePhase(runtime, MobaContinuousPeriodicPhase.Stop);
        }

        internal void ExecuteRuntimePhase(MobaContinuousPeriodicRuntime runtime, string phase)
        {
            if (runtime == null) return;
            if (phase == MobaContinuousPeriodicPhase.Tick)
            {
                runtime.TickIndex++;
            }

            var executedTrigger = false;
            var triggerId = GetTriggerId(runtime, phase);
            if (triggerId > 0)
            {
                ExecuteTrigger(runtime, triggerId, phase);
                executedTrigger = true;
            }

            var triggerIds = GetTriggerIds(runtime, phase);
            if (triggerIds != null && triggerIds.Count > 0)
            {
                for (int i = 0; i < triggerIds.Count; i++)
                {
                    var id = triggerIds[i];
                    if (id <= 0) continue;
                    ExecuteTrigger(runtime, id, phase);
                    executedTrigger = true;
                }
            }

            if (executedTrigger) return;

            ExecuteCompatibleEffects(runtime, phase);
        }

        internal void ExecuteRuntimeEffect(MobaContinuousPeriodicRuntime runtime, int effectId, string stage)
        {
            if (runtime == null) return;
            if (effectId <= 0) return;
            ExecuteEffect(effectId, runtime.SourceActorId, runtime.TargetActorId, runtime.ActionKind, runtime.ActionConfigId, runtime.SourceContextId, stage);
        }

        private void ExecuteTrigger(MobaContinuousPeriodicRuntime runtime, int triggerId, string phase)
        {
            if (runtime == null) return;
            if (triggerId <= 0) return;
            if (_effects == null) return;

            try
            {
                _effects.ExecuteTriggerId(triggerId, CreatePayload(runtime, triggerId, phase));
            }
            catch (Exception ex)
            {
                Log.Warning($"[ContinuousPeriodic] Execute trigger {phase} failed. actionKind={runtime.ActionKind} actionConfigId={runtime.ActionConfigId} triggerId={triggerId} ex={ex.Message}");
            }
        }

        private void ExecuteCompatibleEffects(MobaContinuousPeriodicRuntime runtime, string phase)
        {
            if (runtime == null) return;

            if (phase == MobaContinuousPeriodicPhase.Start)
            {
                ExecuteRuntimeEffect(runtime, runtime.OnStartEffectId, phase);
                return;
            }

            if (phase == MobaContinuousPeriodicPhase.Stop)
            {
                ExecuteRuntimeEffect(runtime, runtime.OnStopEffectId, phase);
                return;
            }

            if (phase != MobaContinuousPeriodicPhase.Tick) return;
            if (runtime.OnTickEffectIds == null || runtime.OnTickEffectIds.Count == 0) return;

            for (int i = 0; i < runtime.OnTickEffectIds.Count; i++)
            {
                ExecuteRuntimeEffect(runtime, runtime.OnTickEffectIds[i], phase);
            }
        }

        private static int GetTriggerId(MobaContinuousPeriodicRuntime runtime, string phase)
        {
            if (runtime == null) return 0;
            if (phase == MobaContinuousPeriodicPhase.Start) return runtime.OnStartTriggerId;
            if (phase == MobaContinuousPeriodicPhase.Stop) return runtime.OnStopTriggerId;
            if (phase == MobaContinuousPeriodicPhase.Tick) return runtime.OnTickTriggerId;
            return 0;
        }

        private static IReadOnlyList<int> GetTriggerIds(MobaContinuousPeriodicRuntime runtime, string phase)
        {
            if (runtime == null) return null;
            if (phase == MobaContinuousPeriodicPhase.Start) return runtime.OnStartTriggerIds;
            if (phase == MobaContinuousPeriodicPhase.Stop) return runtime.OnStopTriggerIds;
            if (phase == MobaContinuousPeriodicPhase.Tick) return runtime.OnTickTriggerIds;
            return null;
        }

        private static MobaPeriodicTriggerContext CreatePayload(MobaContinuousPeriodicRuntime runtime, int triggerId, string phase)
        {
            var payload = new MobaPeriodicTriggerContext
            {
                TriggerId = triggerId,
                SourceActorId = runtime.SourceActorId,
                TargetActorId = runtime.TargetActorId,
                SourceContextId = runtime.SourceContextId,
                OwnerKey = runtime.OwnerKey,
                InstanceId = runtime.InstanceId,
                ActionKind = runtime.ActionKind,
                ActionConfigId = runtime.ActionConfigId,
                Phase = phase,
                ElapsedMsSnapshot = runtime.ElapsedMs,
                RemainingMsSnapshot = runtime.RemainingMs,
                PeriodMsSnapshot = runtime.EffectivePeriodMs,
                TickIndexSnapshot = runtime.TickIndex,
                Runtime = runtime,
            };

            payload.Data.SyncInvocationData(payload);
            if (payload.TryGetTraceContext(out var traceContext)) payload.Data.SyncTraceData(traceContext);
            if (payload.TryGetSkillRuntimeHandle(out var skillRuntimeHandle)) payload.Data.SyncSkillRuntimeData(in skillRuntimeHandle);
            payload.Data.SetData(AbilityContextKeys.BuffId.ToKeyString(), runtime.ActionKind == MobaContinuousPeriodicActionKind.EffectList ? runtime.ActionConfigId : 0);
            payload.Data.SetData(AbilityContextKeys.Phase.ToKeyString(), phase);
            payload.Data.SetData("continuous.periodic.instanceId", runtime.InstanceId);
            payload.Data.SetData("continuous.periodic.ownerKey", runtime.OwnerKey);
            payload.Data.SetData("continuous.periodic.actionKind", runtime.ActionKind);
            payload.Data.SetData("continuous.periodic.actionConfigId", runtime.ActionConfigId);
            payload.Data.SetData("continuous.periodic.phase", phase);
            payload.Data.SetData("continuous.periodic.tickIndex", payload.TickIndexSnapshot);
            payload.Data.SetData("continuous.periodic.elapsedMs", payload.ElapsedMsSnapshot);
            payload.Data.SetData("continuous.periodic.remainingMs", payload.RemainingMsSnapshot);
            payload.Data.SetData("continuous.periodic.periodMs", payload.PeriodMsSnapshot);
            payload.Data.SetData("continuous.periodic.tickIndexSnapshot", payload.TickIndexSnapshot);
            payload.Data.SetData("continuous.periodic.elapsedMsSnapshot", payload.ElapsedMsSnapshot);
            payload.Data.SetData("continuous.periodic.remainingMsSnapshot", payload.RemainingMsSnapshot);
            payload.Data.SetData("continuous.periodic.periodMsSnapshot", payload.PeriodMsSnapshot);
            return payload;
        }

        private void ExecuteEffect(int effectId, int sourceActorId, int targetActorId, int actionKind, int actionConfigId, long sourceContextId, string stage)
        {
            if (effectId <= 0) return;
            if (_invoker == null) return;

            try
            {
                _invoker.Execute(
                    effectId: effectId,
                    sourceActorId: sourceActorId,
                    targetActorId: targetActorId,
                    contextKind: (int)EffectContextKind.ContinuousPeriodic,
                    sourceContextId: sourceContextId);
            }
            catch (Exception ex)
            {
                Log.Warning($"[ContinuousPeriodic] Execute {stage} failed. actionKind={actionKind} actionConfigId={actionConfigId} effectId={effectId} ex={ex.Message}");
            }
        }

        private static int CalculateEffectivePeriodMs(int basePeriodMs, float periodScale)
        {
            if (basePeriodMs <= 0) return 0;
            if (periodScale <= 0f) return basePeriodMs;

            var period = (int)Math.Round(basePeriodMs / periodScale);
            return period > 0 ? period : 1;
        }

        public void OnDeinit(IWorldResolver services)
        {
            var runtimes = new List<MobaContinuousPeriodicRuntime>(_instances.Values);
            for (int i = 0; i < runtimes.Count; i++)
            {
                StopRuntime(runtimes[i], executeRemoveEffect: true);
            }
        }

        public void Dispose()
        {
            foreach (var handle in _handles.Values)
            {
                handle?.MarkDone();
            }

            _handles.Clear();
            _instances.Clear();
        }

        public sealed class MobaPeriodicEffectHandle : IMobaContinuousPeriodicHandle
        {
            private readonly MobaPeriodicEffectService _service;
            private bool _disposed;
            private bool _done;

            internal MobaPeriodicEffectHandle(MobaPeriodicEffectService service, long instanceId, long ownerKey, int targetActorId)
            {
                _service = service;
                InstanceId = instanceId;
                OwnerKey = ownerKey;
                TargetActorId = targetActorId;
            }

            public long InstanceId { get; }
            public long OwnerKey { get; }
            public int TargetActorId { get; }
            public bool IsDone => _done || _disposed || _service == null || !_service.TryGetRuntime(InstanceId, out _);

            public bool SetPeriodScale(float periodScale)
            {
                if (IsDone) return false;
                return _service.SetPeriodScale(InstanceId, periodScale);
            }

            public bool SetPeriodMs(int periodMs)
            {
                if (IsDone) return false;
                return _service.SetPeriodMs(InstanceId, periodMs);
            }

            public void Tick(float deltaTime)
            {
            }

            public void Cancel()
            {
                if (_done) return;
                _done = true;
                _service?.Stop(InstanceId);
            }

            public void Dispose()
            {
                if (_disposed) return;
                _disposed = true;
                Cancel();
            }

            internal void MarkDone()
            {
                _done = true;
            }
        }
    }
}
