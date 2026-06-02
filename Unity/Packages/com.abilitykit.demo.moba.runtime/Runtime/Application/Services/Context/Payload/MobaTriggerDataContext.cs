using System.Collections.Generic;

namespace AbilityKit.Demo.Moba.Services
{
    public interface IMobaTriggerDataContext
    {
        Dictionary<string, object> SharedData { get; }
        T GetData<T>(string key, T defaultValue = default);
        void SetData<T>(string key, T value);
        bool TryGetData<T>(string key, out T value);
        bool RemoveData(string key);
        void ClearData();
    }

    public sealed class MobaTriggerDataBag : IMobaTriggerDataContext
    {
        private readonly Dictionary<string, object> _sharedData = new Dictionary<string, object>();

        public Dictionary<string, object> SharedData => _sharedData;

        public T GetData<T>(string key, T defaultValue = default)
        {
            if (key == null) return defaultValue;
            if (!_sharedData.TryGetValue(key, out var value) || value == null) return defaultValue;
            if (value is T typed) return typed;
            return defaultValue;
        }

        public void SetData<T>(string key, T value)
        {
            if (key == null) return;
            _sharedData[key] = value;
        }

        public bool TryGetData<T>(string key, out T value)
        {
            value = default;
            if (key == null) return false;
            if (!_sharedData.TryGetValue(key, out var raw) || raw == null) return false;
            if (raw is T typed)
            {
                value = typed;
                return true;
            }

            return false;
        }

        public bool RemoveData(string key)
        {
            return key != null && _sharedData.Remove(key);
        }

        public void ClearData()
        {
            _sharedData.Clear();
        }
    }

    public static class MobaTriggerContextDataExtensions
    {
        public static void SyncInvocationData(this IMobaTriggerDataContext dataContext, IMobaTriggerInvocationContext invocation)
        {
            if (dataContext == null || invocation == null) return;
            dataContext.SetData(AbilityContextKeys.ContextKind.ToKeyString(), (int)invocation.Kind);
            dataContext.SetData(AbilityContextKeys.TriggerId.ToKeyString(), invocation.TriggerId);
            dataContext.SetData(AbilityContextKeys.SourceActorId.ToKeyString(), invocation.SourceActorId);
            dataContext.SetData(AbilityContextKeys.TargetActorId.ToKeyString(), invocation.TargetActorId);
            dataContext.SetData(AbilityContextKeys.SourceContextId.ToKeyString(), invocation.SourceContextId);
        }

        public static void SyncSkillRuntimeData(this IMobaTriggerDataContext dataContext, in MobaSkillCastRuntimeHandle handle)
        {
            if (dataContext == null || !handle.IsValid) return;
            dataContext.SetData(AbilityContextKeys.SkillRuntimeHandle.ToKeyString(), handle);
            dataContext.SetData(AbilityContextKeys.SkillRuntimeId.ToKeyString(), handle.RuntimeId);
        }

        public static void SyncTraceData(this IMobaTriggerDataContext dataContext, in MobaTriggerTraceContext traceContext)
        {
            if (dataContext == null) return;
            dataContext.SetData(AbilityContextKeys.TraceKind.ToKeyString(), (int)traceContext.TraceKind);
            dataContext.SetData(AbilityContextKeys.OwnerKey.ToKeyString(), traceContext.OwnerKey);
            dataContext.SetData(AbilityContextKeys.SourceConfigId.ToKeyString(), traceContext.SourceConfigId);
        }
    }
}
