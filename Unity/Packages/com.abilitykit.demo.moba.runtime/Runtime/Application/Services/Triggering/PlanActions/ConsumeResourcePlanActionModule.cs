using System;
using AbilityKit.Ability.Share.ECS; using AbilityKit.ECS; using AbilityKit.Ability.Share.ECS;
using AbilityKit.Ability.Share.ECS.Entitas;
using AbilityKit.Core.Common.Log;
using AbilityKit.Ability.World.DI;
using AbilityKit.Triggering.Registry;
using AbilityKit.Triggering.Runtime;
using AbilityKit.Triggering.Runtime.Plan;
using AbilityKit.Demo.Moba.Services;
using AbilityKit.Demo.Moba.Systems;


namespace AbilityKit.Demo.Moba.Services.Triggering.PlanActions
{
    /// <summary>
    /// 消耗资源的Plan Action模块
    /// 在技能释放前扣除资源（蓝�?能量等）
    /// </summary>
    [PlanActionModule(order: 10)]
    public sealed class ConsumeResourcePlanActionModule : MobaPlanActionModuleBase<ConsumeResourceArgs, ConsumeResourcePlanActionModule>
    {
        protected override IActionSchema<ConsumeResourceArgs, IWorldResolver> Schema => ConsumeResourceSchema.Instance;

        protected override void Execute(object triggerArgs, ConsumeResourceArgs args, ExecCtx<IWorldResolver> ctx)
        {
            if (args.Amount <= 0) return;

            if (!ctx.Context.TryResolve<MobaActorLookupService>(out var actors) || actors == null)
            {
                Log.Warning("[Plan] consume_resource: MobaActorLookupService not found");
                return;
            }

            var input = MobaPlanActionInputResolver.Resolve(triggerArgs, ctx);
            if (!input.HasCasterActor)
            {
                Log.Warning("[Plan] consume_resource: caster actor not found");
                return;
            }

            var casterActorId = input.CasterActorId;

            if (!actors.TryGetActorEntity(casterActorId, out var entity) || entity == null)
            {
                Log.Warning($"[Plan] consume_resource: caster unit not found, actorId={casterActorId}");
                return;
            }

            if (!entity.hasSkillLoadout)
            {
                Log.Warning($"[Plan] consume_resource: caster has no skill loadout, actorId={casterActorId}");
                return;
            }

            // TODO: 实现实际的资源扣除逻辑
            // 目前是占位实现，后续需要：
            // 1. 根据 ResourceType 获取对应属�?
            // 2. 检查当前值是否足�?
            // 3. 扣除资源
            // 4. 如果失败，应该抛出异常或返回失败状�?
            
            Log.Info($"[Plan] consume_resource: actorId={casterActorId}, type={args.ResourceType}, amount={args.Amount}");

            // 示例实现（需要与属性系统对接）�?
            // var attribute = GetAttribute(entity, args.ResourceType);
            // if (attribute.CurrentValue < args.Amount)
            // {
            //     throw new InvalidOperationException(args.FailMessageKey);
            // }
            // attribute.CurrentValue -= args.Amount;
        }
    }
}
