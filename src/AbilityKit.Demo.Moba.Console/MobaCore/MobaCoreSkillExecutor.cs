using System;
using System.Collections.Generic;
using AbilityKit.Ability.World.DI;
using AbilityKit.Ability.World.Services;
using AbilityKit.Demo.Moba.Services;
using AbilityKit.Core.Math;
using AbilityKit.Ability.Triggering.Runtime;
using AbilityKit.Pipeline;
using AbilityKit.Demo.Moba.Console.Platform;

namespace AbilityKit.Demo.Moba.Console.MobaCore
{
    using AbilityKit.Ability;

    /// <summary>
    /// Console 平台适配层：桥接 Console 输入到 moba.core 技能系统
    /// </summary>
    public sealed class MobaCoreSkillExecutor : IDisposable
    {
        private readonly IWorldResolver _worldServices;
        private readonly MobaActorRegistry _actorRegistry;
        private readonly Dictionary<int, SkillPipelineRunner> _runners = new();

        public event Action<int, int, int, int> OnSkillCast;
        public event Action<int, int, float, int> OnDamageDealt;

        public MobaCoreSkillExecutor(IWorldResolver worldServices, MobaActorRegistry actorRegistry)
        {
            _worldServices = worldServices ?? throw new ArgumentNullException(nameof(worldServices));
            _actorRegistry = actorRegistry ?? throw new ArgumentNullException(nameof(actorRegistry));
        }

        /// <summary>
        /// 执行技能
        /// </summary>
        public bool ExecuteSkill(int actorId, int skillSlot, int targetActorId = 0, Vec3? aimPos = null, Vec3? aimDir = null)
        {
            var skillId = GetSkillIdBySlot(skillSlot);
            Log.Skill($"[MobaCoreSkillExecutor] Execute: Actor#{actorId} Skill#{skillId} (Slot{skillSlot}) Target#{targetActorId}");

            var runner = GetOrCreateRunner(actorId);

            var pos = aimPos ?? Vec3.Zero;
            var dir = aimDir ?? Vec3.Forward;

            var request = new SkillCastRequest(
                skillId: skillId,
                skillSlot: skillSlot,
                casterActorId: actorId,
                targetActorId: targetActorId,
                aimPos: in pos,
                aimDir: in dir,
                worldServices: _worldServices,
                eventBus: _worldServices.Resolve<AbilityKit.Triggering.Eventing.IEventBus>(),
                casterUnit: null,
                targetUnit: null);

            var lib = _worldServices.Resolve<IMobaSkillPipelineLibrary>();
            if (lib == null)
            {
                Log.Warn("[MobaCoreSkillExecutor] IMobaSkillPipelineLibrary not found, using fallback");
                return ExecuteFallback(actorId, skillId, skillSlot, targetActorId);
            }

            if (!lib.TryGet(skillId, out var preCastConfig, out var preCastPhases, out var castConfig, out var castPhases))
            {
                Log.Warn($"[MobaCoreSkillExecutor] No skill pipeline for {skillId}, using fallback");
                return ExecuteFallback(actorId, skillId, skillSlot, targetActorId);
            }

            if (castConfig == null || castPhases == null || castPhases.Count == 0)
            {
                Log.Warn($"[MobaCoreSkillExecutor] Skill pipeline empty for {skillId}, using fallback");
                return ExecuteFallback(actorId, skillId, skillSlot, targetActorId);
            }

            var success = runner.Start(
                preCastConfig,
                preCastPhases,
                castConfig,
                castPhases,
                abilityInstance: null,
                in request,
                out var failReason);

            if (!success)
            {
                Log.Skill($"[MobaCoreSkillExecutor] Skill failed: {failReason ?? "unknown"}");
                return false;
            }

            OnSkillCast?.Invoke(actorId, skillId, skillSlot, targetActorId);
            return true;
        }

        /// <summary>
        /// 帧同步推进
        /// </summary>
        public void Step(int actorId, float deltaTime)
        {
            if (_runners.TryGetValue(actorId, out var runner))
            {
                runner.Step(deltaTime);
            }
        }

        /// <summary>
        /// 取消所有技能
        /// </summary>
        public void CancelAll(int actorId)
        {
            if (_runners.TryGetValue(actorId, out var runner))
            {
                runner.CancelAll();
            }
        }

        private SkillPipelineRunner GetOrCreateRunner(int actorId)
        {
            if (!_runners.TryGetValue(actorId, out var runner))
            {
                runner = new SkillPipelineRunner(actorId);
                _runners[actorId] = runner;
            }
            return runner;
        }

        private bool ExecuteFallback(int actorId, int skillId, int skillSlot, int targetActorId)
        {
            Log.Skill($"[MobaCoreSkillExecutor] Fallback: Actor#{actorId} Skill#{skillId}");
            // Fallback 模式下使用默认伤害（仅在无法获取技能配置时使用）
            // 后续应该通过 BattleServices 进行完整的伤害计算
            float defaultDamage = 50f;
            if (targetActorId > 0)
            {
                // TODO: 应该通过 BattleServices.ApplyDamage 进行完整伤害计算
                // 这里仅作为临时 fallback
                OnDamageDealt?.Invoke(actorId, targetActorId, defaultDamage, skillId);
            }
            OnSkillCast?.Invoke(actorId, skillId, skillSlot, targetActorId);
            return true;
        }

        private static int GetSkillIdBySlot(int slot)
        {
            return slot switch
            {
                1 => 101,
                2 => 102,
                3 => 103,
                _ => 100 + slot
            };
        }

        public void Dispose()
        {
            _runners.Clear();
        }
    }
}
