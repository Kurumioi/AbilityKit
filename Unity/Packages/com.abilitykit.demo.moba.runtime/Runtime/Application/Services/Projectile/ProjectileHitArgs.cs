using System.Collections.Generic;
using AbilityKit.Core.Common.Projectile;
using AbilityKit.Core.Math;

namespace AbilityKit.Demo.Moba.Services.Projectile
{
    public sealed class ProjectileHitArgs : IMobaActorContextProvider, IMobaTriggerInvocationContext, IMobaTriggerLineageContextProvider, IMobaTriggerTraceContextProvider, IMobaTriggerDataContext, IMobaOriginContextProvider, IMobaTriggerSkillRuntimeContext
    {
        private readonly MobaTriggerDataBag _data = new MobaTriggerDataBag();

        public int TriggerId { get; set; }
        public EffectContextKind Kind => EffectContextKind.Projectile;
        public int SourceActorId { get; set; }
        public int TargetActorId { get; set; }
        public long SourceContextId { get; set; }
        public int SourceConfigId { get; set; }
        public int Frame { get; set; }
        public object Raw { get; set; }
        public ProjectileSourceContext SourceContext { get; set; }

        public int CasterActorId;
        public int ProjectileTemplateId;
        public ProjectileId ProjectileId;
        public Vec3 Point;
        public Vec3 Normal;
        public ColliderId HitCollider;
        public MobaTriggerDataBag Data => _data;
        public Dictionary<string, object> SharedData => _data.SharedData;

        public bool TryGetSourceActorId(out int actorId)
        {
            actorId = SourceActorId > 0 ? SourceActorId : CasterActorId;
            return actorId > 0;
        }

        public bool TryGetTargetActorId(out int actorId)
        {
            actorId = TargetActorId;
            return actorId > 0;
        }

        public bool TryGetLineageContext(out MobaTriggerLineageContext lineageContext)
        {
            if (SourceContext.IsValid)
            {
                lineageContext = SourceContext.ToHitLineageContext(ProjectileId, TargetActorId);
                return true;
            }

            lineageContext = new MobaTriggerLineageContext(Kind, MobaTraceKind.ProjectileHit, SourceActorId, TargetActorId, SourceContextId, SourceContextId, ProjectileId.Value, SourceConfigId);
            return true;
        }

        public bool TryGetTraceContext(out MobaTriggerTraceContext traceContext)
        {
            if (TryGetLineageContext(out var lineageContext))
            {
                traceContext = lineageContext.ToTraceContext();
                return true;
            }

            traceContext = default;
            return false;
        }

        public bool TryGetOrigin(out MobaGameplayOrigin origin)
        {
            if (SourceContext.TryGetOrigin(out var sourceOrigin))
            {
                origin = sourceOrigin.WithActors(SourceActorId > 0 ? SourceActorId : CasterActorId, TargetActorId);
                return origin.IsValid;
            }

            if (TryGetLineageContext(out var lineageContext))
            {
                origin = MobaGameplayOrigin.FromLineageContext(in lineageContext);
                return origin.IsValid;
            }

            origin = default;
            return false;
        }

        public bool TryGetSkillRuntimeHandle(out MobaSkillCastRuntimeHandle handle)
        {
            handle = SourceContext.SkillRuntimeHandle;
            return handle.IsValid;
        }

        public T GetData<T>(string key, T defaultValue = default) => _data.GetData(key, defaultValue);
        public void SetData<T>(string key, T value) => _data.SetData(key, value);
        public bool TryGetData<T>(string key, out T value) => _data.TryGetData(key, out value);
        public bool RemoveData(string key) => _data.RemoveData(key);
        public void ClearData() => _data.ClearData();
    }
}
