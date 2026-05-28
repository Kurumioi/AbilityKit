using System;
using System.Linq;
using AbilityKit.Ability.Share.Effect;
using AbilityKit.Ability.Triggering.Json;
using AbilityKit.Ability.World.DI;
using AbilityKit.Core.Common.Log;
using AbilityKit.Core.Common.Projectile;
using AbilityKit.Demo.Moba.Services;
using AbilityKit.Demo.Moba.Events.Buff;
using AbilityKit.Demo.Moba.Events.Unit;
using AbilityKit.Demo.Moba.Events.Summon;
using AbilityKit.Effect;

namespace AbilityKit.Demo.Moba.Systems.Bootstrap.Flow.Stages
{
    /// <summary>
    /// TargetingAndSkills Stage
    /// 注册事件订阅、触发器索引、技能条件等服务
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
                reg.RegisterPrefix<SkillCastContext>("skill.");
                reg.RegisterPrefix<BuffEventArgs>("buff.");
                reg.RegisterPrefix<AbilityKit.Demo.Moba.Services.Projectile.AreaEventArgs>("area.");
                reg.RegisterPrefix<AbilityKit.Demo.Moba.Triggering.PresentationEventArgs>("presentation.");

                // damage.* uses multiple payload types, so we register exact mappings.
                reg.RegisterExact<AttackInfo>(DamagePipelineEvents.AttackCreated);
                reg.RegisterExact<AttackInfo>(DamagePipelineEvents.BeforeCalc);

                reg.RegisterExact<AttackCalcInfo>(DamagePipelineEvents.CalcBegin);
                reg.RegisterExact<AttackCalcInfo>(DamagePipelineEvents.AfterBase);
                reg.RegisterExact<AttackCalcInfo>(DamagePipelineEvents.AfterMitigate);
                reg.RegisterExact<AttackCalcInfo>(DamagePipelineEvents.AfterShield);
                reg.RegisterExact<AttackCalcInfo>(DamagePipelineEvents.CalcFinal);
                reg.RegisterExact<AttackCalcInfo>(DamagePipelineEvents.BeforeApply);

                reg.RegisterExact<DamageResult>(DamagePipelineEvents.AfterApply);

                // projectile.*
                reg.RegisterExact<ProjectileHitEvent>(ProjectileTriggering.Events.Hit);
                reg.RegisterExact<ProjectileSpawnEvent>(ProjectileTriggering.Events.Spawn);
                reg.RegisterExact<ProjectileTickEvent>(ProjectileTriggering.Events.Tick);
                reg.RegisterExact<ProjectileExitEvent>(ProjectileTriggering.Events.Exit);

                // summon.*
                reg.RegisterPrefix<SummonEventPayload>("summon.");

                // unit.*
                reg.RegisterPrefix<UnitEventPayload>("unit.");
                reg.RegisterExact<UnitDieEventPayload>(MobaUnitTriggering.Events.Die);
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

            builder.TryRegister<SkillConditionRegistry>(WorldLifetime.Singleton, _ =>
            {
                var reg = new SkillConditionRegistry();
                reg.DiscoverAndRegister();
                Log.Info($"[TargetingAndSkillsStage] SkillConditionRegistry initialized with {reg.GetAllConditionIds().Count()} conditions");
                return reg;
            });
        }
    }
}
