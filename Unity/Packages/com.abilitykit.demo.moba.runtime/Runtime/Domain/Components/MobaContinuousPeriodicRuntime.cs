using System.Collections.Generic;
using AbilityKit.Ability.Triggering.Runtime;
using AbilityKit.Demo.Moba.Services;
using Entitas;
using Entitas.CodeGeneration.Attributes;

namespace AbilityKit.Demo.Moba.Components
{
    public static class MobaContinuousPeriodicActionKind
    {
        public const int None = 0;
        public const int EffectList = 1;
        public const int TriggerPlan = 2;
        public const int Custom = 255;
    }

    public static class MobaContinuousPeriodicPhase
    {
        public const string Start = "start";
        public const string Tick = "tick";
        public const string Stop = "stop";
    }

    public sealed class MobaContinuousPeriodicSpec
    {
        public int ActionKind;
        public int ActionConfigId;
        public int SourceActorId;
        public int TargetActorId;
        public long OwnerKey;
        public long SourceContextId;
        public MobaSkillCastRuntimeHandle SkillRuntimeHandle;
        public int DurationMs;
        public int PeriodMs;
        public int OnStartTriggerId;
        public int OnTickTriggerId;
        public int OnStopTriggerId;
        public IReadOnlyList<int> OnStartTriggerIds;
        public IReadOnlyList<int> OnTickTriggerIds;
        public IReadOnlyList<int> OnStopTriggerIds;
        public int OnStartEffectId;
        public IReadOnlyList<int> OnTickEffectIds;
        public int OnStopEffectId;
    }

    [Actor]
    public sealed class MobaContinuousPeriodicComponent : IComponent
    {
        public List<MobaContinuousPeriodicRuntime> Active;
    }

    public class MobaContinuousPeriodicRuntime
    {
        public long InstanceId;
        public int ActionKind;
        public int ActionConfigId;
        public int SourceActorId;
        public int TargetActorId;

        public int RemainingMs;
        public int NextTickMs;
        public int BasePeriodMs;
        public int EffectivePeriodMs;
        public float PeriodScale = 1f;

        public int OnStartTriggerId;
        public int OnTickTriggerId;
        public int OnStopTriggerId;
        public IReadOnlyList<int> OnStartTriggerIds;
        public IReadOnlyList<int> OnTickTriggerIds;
        public IReadOnlyList<int> OnStopTriggerIds;
        public int OnStartEffectId;
        public IReadOnlyList<int> OnTickEffectIds;
        public int OnStopEffectId;

        public long OwnerKey;
        public long SourceContextId;
        public MobaSkillCastRuntimeHandle SkillRuntimeHandle;
        public int ElapsedMs;
        public int TickIndex;

        public bool Started;
        public bool IsStopped;
    }

    public sealed class MobaPeriodicTriggerContext : IMobaActorContextProvider, IMobaTriggerInvocationContext, IMobaTriggerTraceContextProvider, IMobaTriggerRuntimeContext<MobaContinuousPeriodicRuntime>, IMobaTriggerDataContext, IMobaOriginContextProvider, IMobaTriggerSkillRuntimeContext
    {
        private readonly MobaTriggerDataBag _data = new MobaTriggerDataBag();

        public int TriggerId { get; set; }
        public EffectContextKind Kind => EffectContextKind.ContinuousPeriodic;
        public int SourceActorId { get; set; }
        public int TargetActorId { get; set; }
        public long SourceContextId { get; set; }
        public long OwnerKey { get; set; }
        public string Phase { get; set; }
        public long InstanceId { get; set; }
        public int ActionKind { get; set; }
        public int ActionConfigId { get; set; }
        public int ElapsedMsSnapshot { get; set; }
        public int RemainingMsSnapshot { get; set; }
        public int PeriodMsSnapshot { get; set; }
        public int TickIndexSnapshot { get; set; }
        public int ElapsedMs
        {
            get => ElapsedMsSnapshot;
            set => ElapsedMsSnapshot = value;
        }
        public int RemainingMs
        {
            get => RemainingMsSnapshot;
            set => RemainingMsSnapshot = value;
        }
        public int PeriodMs
        {
            get => PeriodMsSnapshot;
            set => PeriodMsSnapshot = value;
        }
        public int TickIndex
        {
            get => TickIndexSnapshot;
            set => TickIndexSnapshot = value;
        }
        public float ElapsedTime => ElapsedMsSnapshot / 1000f;
        public MobaContinuousPeriodicRuntime Runtime { get; set; }
        public MobaTriggerDataBag Data => _data;
        public System.Collections.Generic.Dictionary<string, object> SharedData => _data.SharedData;

        public bool TryGetSourceActorId(out int actorId)
        {
            actorId = SourceActorId;
            return actorId > 0;
        }

        public bool TryGetTargetActorId(out int actorId)
        {
            actorId = TargetActorId;
            return actorId > 0;
        }

        public bool TryGetTraceContext(out MobaTriggerTraceContext traceContext)
        {
            traceContext = new MobaTriggerTraceContext(Kind, Phase == MobaContinuousPeriodicPhase.Tick ? MobaTraceKind.BuffTick : MobaTraceKind.EffectExecution, SourceActorId, TargetActorId, SourceContextId, SourceContextId, OwnerKey, ActionConfigId);
            return true;
        }

        public bool TryGetOrigin(out MobaGameplayOrigin origin)
        {
            if (TryGetTraceContext(out var traceContext))
            {
                var handle = Runtime != null ? Runtime.SkillRuntimeHandle : default;
                origin = MobaGameplayOrigin.FromTraceContext(in traceContext, in handle);
                return origin.IsValid;
            }

            origin = default;
            return false;
        }

        public bool TryGetSkillRuntimeHandle(out MobaSkillCastRuntimeHandle handle)
        {
            handle = Runtime != null ? Runtime.SkillRuntimeHandle : default;
            return handle.IsValid;
        }

        public bool TryGetRuntime(out MobaContinuousPeriodicRuntime runtime)
        {
            runtime = Runtime;
            return runtime != null && !runtime.IsStopped;
        }

        public bool TryGetPeriodicRuntime(out MobaContinuousPeriodicRuntime runtime) => TryGetRuntime(out runtime);

        public T GetData<T>(string key, T defaultValue = default) => _data.GetData(key, defaultValue);
        public void SetData<T>(string key, T value) => _data.SetData(key, value);
        public bool TryGetData<T>(string key, out T value) => _data.TryGetData(key, out value);
        public bool RemoveData(string key) => _data.RemoveData(key);
        public void ClearData() => _data.ClearData();
    }

    public interface IMobaContinuousPeriodicHandle : IRunningAction
    {
        long InstanceId { get; }
        long OwnerKey { get; }
        int TargetActorId { get; }
        bool SetPeriodScale(float periodScale);
        bool SetPeriodMs(int periodMs);
    }
}
