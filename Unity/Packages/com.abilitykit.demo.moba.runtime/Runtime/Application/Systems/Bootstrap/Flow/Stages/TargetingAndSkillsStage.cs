using AbilityKit.Ability.Triggering.Json;
using AbilityKit.Ability.World.DI;
using AbilityKit.Core.Common.Log;
using AbilityKit.Demo.Moba.Services;

namespace AbilityKit.Demo.Moba.Systems.Bootstrap.Flow.Stages
{
    /// <summary>
    /// TargetingAndSkills Stage
    /// 注册事件订阅与触发器索引服务
    /// </summary>
    [MobaBootstrapStage]
    public sealed class TargetingAndSkillsStage : MobaBootstrapStageBase
    {
        public override string Name => "TargetingAndSkills";

        protected internal override void Configure(WorldContainerBuilder builder)
        {
            builder.TryRegister<MobaEventSubscriptionRegistry>(WorldLifetime.Singleton, _ =>
            {
                var reg = new MobaEventSubscriptionRegistry();
                reg.DiscoverAndRegister();
                return reg;
            });

            builder.TryRegister<MobaTriggerIndexService>(WorldLifetime.Singleton, _ =>
            {
                var loader = _.Resolve<ITextLoader>();
                var s = new MobaTriggerIndexService(loader);
                Log.Info("[TargetingAndSkillsStage] MobaTriggerIndexService.LoadFromResources begin");
                s.LoadFromResources();
                Log.Info("[TargetingAndSkillsStage] MobaTriggerIndexService.LoadFromResources end");
                return s;
            });

        }
    }
}
