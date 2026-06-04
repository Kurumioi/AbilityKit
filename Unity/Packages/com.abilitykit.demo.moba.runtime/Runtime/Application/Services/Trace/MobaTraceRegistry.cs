using System;
using System.Collections.Generic;
using AbilityKit.Core.Common.Log;
using AbilityKit.Ability.World.Services;
using AbilityKit.Ability.World.Services.Attributes;
using AbilityKit.Trace;

using MobaTraceRegistryExtensions = AbilityKit.Trace.TraceTreeRegistryExtensions;

namespace AbilityKit.Demo.Moba.Services
{
    /// <summary>
    /// MOBA 战斗溯源注册表
    /// 管理技能/效果/Buff 等执行的链路追踪
    /// </summary>
    [WorldService(typeof(MobaTraceRegistry))]
    public sealed class MobaTraceRegistry : TraceTreeRegistry<MobaTraceMetadata>, IService
    {
        private readonly long _instanceId;

        /// <summary>
        /// 创建溯源注册表
        /// </summary>
        public MobaTraceRegistry()
            : base(new DictionaryTraceMetadataStore<MobaTraceMetadata>())
        {
            _instanceId = GetHashCode();
        }

        /// <summary>
        /// 实例ID
        /// </summary>
        public long InstanceId => _instanceId;

        public long CreateNode(in TraceOrigin origin)
        {
            return origin.ParentContextId != 0 ? CreateChild(origin) : CreateRoot(origin);
        }

        public long CreateRootContext(
            MobaTraceKind kind,
            int configId,
            int sourceActorId,
            int targetActorId,
            TraceEndpoint originSource = default,
            TraceEndpoint originTarget = default)
        {
            var origin = new TraceOrigin(
                kind: (int)kind,
                sourceActorId: sourceActorId,
                targetActorId: targetActorId,
                originSource: originSource.IsValid ? originSource : ToOriginSource(kind, configId),
                originTarget: originTarget.IsValid ? originTarget : TraceEndpoint.Actor(targetActorId),
                configId: configId);
            return CreateRoot(origin);
        }

        public long CreateChildContext(
            long parentContextId,
            MobaTraceKind kind,
            int configId,
            int sourceActorId,
            int targetActorId,
            TraceEndpoint originSource = default,
            TraceEndpoint originTarget = default)
        {
            var origin = new TraceOrigin(
                kind: (int)kind,
                sourceActorId: sourceActorId,
                targetActorId: targetActorId,
                originSource: originSource.IsValid ? originSource : ToOriginSource(kind, configId),
                originTarget: originTarget.IsValid ? originTarget : TraceEndpoint.Actor(targetActorId),
                configId: configId,
                parentContextId: parentContextId);
            return CreateChild(origin);
        }

        public bool EndContext(long contextId, TraceLifecycleReason reason)
        {
            return End(contextId, (int)reason);
        }

        public bool EndContext(long contextId, MobaTraceEndReason reason)
        {
            return End(contextId, (int)reason);
        }

        private static TraceEndpoint ToOriginSource(MobaTraceKind kind, int configId)
        {
            switch (kind)
            {
                case MobaTraceKind.SkillCast:
                case MobaTraceKind.SkillEffect:
                case MobaTraceKind.SkillPhase:
                    return TraceEndpoint.Config("Skill", configId);
                case MobaTraceKind.EffectExecution:
                    return TraceEndpoint.Config("Effect", configId);
                case MobaTraceKind.EffectAction:
                    return TraceEndpoint.Config("Action", configId);
                case MobaTraceKind.BuffApply:
                case MobaTraceKind.BuffTick:
                case MobaTraceKind.BuffRemove:
                    return TraceEndpoint.Config("Buff", configId);
                case MobaTraceKind.ProjectileLaunch:
                case MobaTraceKind.ProjectileHit:
                    return TraceEndpoint.Config("Projectile", configId);
                case MobaTraceKind.AreaSpawn:
                case MobaTraceKind.AreaEnter:
                case MobaTraceKind.AreaExit:
                case MobaTraceKind.AreaExpire:
                    return TraceEndpoint.Config("Area", configId);
                case MobaTraceKind.SummonSpawn:
                case MobaTraceKind.SummonDeath:
                    return TraceEndpoint.Config("Summon", configId);
                case MobaTraceKind.PresentationPlay:
                case MobaTraceKind.PresentationStop:
                    return TraceEndpoint.Config("Presentation", configId);
                default:
                    return default;
            }
        }

        /// <summary>
        /// 创建技能效果溯源根节点
        /// </summary>
        public TraceRootScope CreateSkillEffectRoot(
            int skillConfigId,
            long skillInstanceId,
            int sourceActorId,
            int targetActorId,
            EffectContextKind contextKind = EffectContextKind.Skill)
        {
            return MobaTraceRegistryExtensions.CreateRootScope(
                this,
                kind: (int)MobaTraceKind.SkillEffect,
                sourceActorId: sourceActorId,
                targetActorId: targetActorId,
                originSource: $"Skill:{skillConfigId}",
                originTarget: targetActorId > 0 ? $"Actor:{targetActorId}" : null,
                configId: skillConfigId);
        }

        /// <summary>
        /// 创建效果执行溯源根节点
        /// </summary>
        public TraceRootScope CreateEffectRoot(
            int effectConfigId,
            int triggerPlanId,
            int sourceActorId,
            int targetActorId,
            EffectContextKind contextKind = EffectContextKind.ContinuousPeriodic)
        {
            return MobaTraceRegistryExtensions.CreateRootScope(
                this,
                kind: (int)MobaTraceKind.EffectExecution,
                sourceActorId: sourceActorId,
                targetActorId: targetActorId,
                originSource: $"Effect:{effectConfigId}",
                originTarget: targetActorId > 0 ? $"Actor:{targetActorId}" : null,
                configId: effectConfigId);
        }

        /// <summary>
        /// 创建Buff溯源根节点
        /// </summary>
        public TraceRootScope CreateBuffRoot(
            int buffConfigId,
            int sourceActorId,
            int targetActorId)
        {
            return MobaTraceRegistryExtensions.CreateRootScope(
                this,
                kind: (int)MobaTraceKind.BuffApply,
                sourceActorId: sourceActorId,
                targetActorId: targetActorId,
                originSource: $"Buff:{buffConfigId}",
                originTarget: targetActorId > 0 ? $"Actor:{targetActorId}" : null,
                configId: buffConfigId);
        }

        /// <summary>
        /// 创建弹道溯源根节点
        /// </summary>
        public TraceRootScope CreateProjectileRoot(
            int projectileConfigId,
            int sourceActorId,
            int targetActorId)
        {
            return MobaTraceRegistryExtensions.CreateRootScope(
                this,
                kind: (int)MobaTraceKind.ProjectileLaunch,
                sourceActorId: sourceActorId,
                targetActorId: targetActorId,
                originSource: $"Projectile:{projectileConfigId}",
                originTarget: targetActorId > 0 ? $"Actor:{targetActorId}" : null,
                configId: projectileConfigId);
        }

        /// <summary>
        /// 创建区域溯源根节点
        /// </summary>
        public TraceRootScope CreateAreaRoot(
            int areaConfigId,
            int sourceActorId)
        {
            return MobaTraceRegistryExtensions.CreateRootScope(
                this,
                kind: (int)MobaTraceKind.AreaSpawn,
                sourceActorId: sourceActorId,
                targetActorId: 0,
                originSource: $"Area:{areaConfigId}",
                originTarget: null,
                configId: areaConfigId);
        }

        /// <summary>
        /// 创建动作执行子节点
        /// </summary>
        public TraceTreeScope CreateActionChild(
            long parentRootId,
            int actionId,
            int sourceActorId,
            int targetActorId,
            string actionName = null)
        {
            return MobaTraceRegistryExtensions.CreateChildScope(
                this,
                parentContextId: parentRootId,
                kind: (int)MobaTraceKind.EffectAction,
                sourceActorId: sourceActorId,
                targetActorId: targetActorId,
                originSource: $"Action:{actionId}",
                originTarget: targetActorId > 0 ? $"Actor:{targetActorId}" : null,
                configId: actionId);
        }

        /// <summary>
        /// 创建Buff触发子节点
        /// </summary>
        public TraceTreeScope CreateBuffTickChild(
            long parentRootId,
            int buffConfigId,
            int sourceActorId,
            int targetActorId)
        {
            return MobaTraceRegistryExtensions.CreateChildScope(
                this,
                parentContextId: parentRootId,
                kind: (int)MobaTraceKind.BuffTick,
                sourceActorId: sourceActorId,
                targetActorId: targetActorId,
                originSource: $"Buff:{buffConfigId}",
                originTarget: targetActorId > 0 ? $"Actor:{targetActorId}" : null,
                configId: buffConfigId);
        }

        /// <summary>
        /// 创建弹道命中子节点
        /// </summary>
        public TraceTreeScope CreateProjectileHitChild(
            long parentRootId,
            int targetActorId)
        {
            return MobaTraceRegistryExtensions.CreateChildScope(
                this,
                parentContextId: parentRootId,
                kind: (int)MobaTraceKind.ProjectileHit,
                sourceActorId: 0,
                targetActorId: targetActorId,
                originSource: "ProjectileHit",
                originTarget: $"Actor:{targetActorId}",
                configId: 0);
        }

        /// <summary>
        /// 创建区域进入子节点
        /// </summary>
        public TraceTreeScope CreateAreaEnterChild(
            long parentRootId,
            int targetActorId)
        {
            return MobaTraceRegistryExtensions.CreateChildScope(
                this,
                parentContextId: parentRootId,
                kind: (int)MobaTraceKind.AreaEnter,
                sourceActorId: 0,
                targetActorId: targetActorId,
                originSource: "AreaEnter",
                originTarget: $"Actor:{targetActorId}",
                configId: 0);
        }

        /// <summary>
        /// 获取指定根节点下的完整链路
        /// </summary>
        public List<TraceSnapshot<MobaTraceMetadata>> GetChain(long rootId)
        {
            var chain = new List<TraceSnapshot<MobaTraceMetadata>>();
            TryBuildChain(rootId, chain);
            return chain;
        }

        /// <summary>
        /// 获取所有活跃的技能效果溯源
        /// </summary>
        public IEnumerable<TraceSnapshot<MobaTraceMetadata>> GetActiveSkillEffects()
        {
            return GetNodesByKind((int)MobaTraceKind.SkillEffect);
        }

        /// <summary>
        /// 获取所有活跃的Buff溯源
        /// </summary>
        public IEnumerable<TraceSnapshot<MobaTraceMetadata>> GetActiveBuffs()
        {
            return GetNodesByKind((int)MobaTraceKind.BuffApply);
        }

        /// <summary>
        /// 验证链路完整性（调试用）
        /// </summary>
        public void ValidateChain(long rootId)
        {
            if (!Contains(rootId))
            {
                Log.Warning($"[MobaTraceRegistry] Root node {rootId} not found.");
                return;
            }

            var chain = GetChain(rootId);
            Log.Info($"[MobaTraceRegistry] Chain validation for root {rootId}:");
            Log.Info($"  Total nodes: {chain.Count}");

            for (int i = 0; i < chain.Count; i++)
            {
                var node = chain[i];
                var metadata = node.Metadata;
                Log.Info($"  [{i}] Kind={node.Kind}, ContextId={node.ContextId}, Parent={node.ParentId}");
                if (metadata != null)
                {
                    Log.Info($"       Metadata: {metadata.ToDisplayString()}");
                }
            }
        }

        #region TraceMetadata Factory

        protected override MobaTraceMetadata CreateMetadata(
            long rootId,
            int kind,
            long sourceActorId,
            long targetActorId,
            long originId,
            string originDisplay,
            long targetId,
            string targetDisplay,
            int configId)
        {
            var metadata = new MobaTraceMetadata
            {
                SourceActorId = (int)sourceActorId,
                TargetActorId = (int)targetActorId,
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };

            // 根据溯源种类填充元数据
            var traceKind = (MobaTraceKind)kind;
            switch (traceKind)
            {
                case MobaTraceKind.SkillEffect:
                case MobaTraceKind.SkillCast:
                    metadata.SkillConfigId = configId;
                    metadata.Description = $"SkillEffect: {originDisplay}";
                    break;

                case MobaTraceKind.EffectExecution:
                    metadata.EffectConfigId = configId;
                    metadata.Description = $"EffectExecution: {originDisplay}";
                    break;

                case MobaTraceKind.EffectAction:
                    metadata.ActionId = configId;
                    metadata.Description = $"Action: {originDisplay}";
                    break;

                case MobaTraceKind.BuffApply:
                case MobaTraceKind.BuffTick:
                    metadata.SkillConfigId = configId; // BuffConfigId
                    metadata.Description = $"Buff: {originDisplay}";
                    break;

                case MobaTraceKind.ProjectileLaunch:
                case MobaTraceKind.ProjectileHit:
                    metadata.Description = $"Projectile: {originDisplay}";
                    break;

                case MobaTraceKind.AreaSpawn:
                case MobaTraceKind.AreaEnter:
                case MobaTraceKind.AreaExit:
                case MobaTraceKind.AreaExpire:
                    metadata.Description = $"Area: {originDisplay}";
                    break;

                case MobaTraceKind.PresentationPlay:
                case MobaTraceKind.PresentationStop:
                    metadata.Description = $"Presentation: {originDisplay}";
                    break;
            }

            // 解析 originSource
            if (!string.IsNullOrEmpty(originDisplay))
            {
                metadata.OriginSource = originDisplay;
                if (originDisplay.StartsWith("Skill:"))
                {
                    if (int.TryParse(originDisplay.Substring(6), out var skillId))
                        metadata.SkillConfigId = skillId;
                }
                else if (originDisplay.StartsWith("Effect:"))
                {
                    if (int.TryParse(originDisplay.Substring(7), out var effectId))
                        metadata.EffectConfigId = effectId;
                }
                else if (originDisplay.StartsWith("Buff:"))
                {
                    if (int.TryParse(originDisplay.Substring(5), out var buffId))
                        metadata.SkillConfigId = buffId; // 复用 SkillConfigId 字段存储 BuffId
                }
            }

            return metadata;
        }

        protected override long GetSourceActorId(MobaTraceMetadata metadata) => metadata.SourceActorId;
        protected override long GetTargetActorId(MobaTraceMetadata metadata) => metadata.TargetActorId;
        protected override long GetOriginSourceId(MobaTraceMetadata metadata) => metadata.SkillConfigId > 0 ? metadata.SkillConfigId : metadata.EffectConfigId;
        protected override string GetOriginSourceDisplay(MobaTraceMetadata metadata) => metadata.OriginSource;
        protected override long GetOriginTargetId(MobaTraceMetadata metadata) => metadata.TargetActorId;
        protected override string GetOriginTargetDisplay(MobaTraceMetadata metadata) => metadata.TargetActorId > 0 ? $"Actor:{metadata.TargetActorId}" : null;

        #endregion
    }
}
