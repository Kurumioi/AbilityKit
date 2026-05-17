using System;
using System.Collections.Concurrent;
using AbilityKit.Demo.Moba.Console.Platform;

namespace AbilityKit.Demo.Moba.Console.Services
{
    /// <summary>
    /// 技能施法请求
    /// </summary>
    public readonly struct ConsoleSkillCastRequest
    {
        public int SkillId { get; }
        public int SkillSlot { get; }
        public int CasterActorId { get; }
        public int TargetActorId { get; }
        public float AimX { get; }
        public float AimZ { get; }

        public ConsoleSkillCastRequest(int skillId, int skillSlot, int casterActorId, int targetActorId = 0, float aimX = 0, float aimZ = 0)
        {
            SkillId = skillId;
            SkillSlot = skillSlot;
            CasterActorId = casterActorId;
            TargetActorId = targetActorId;
            AimX = aimX;
            AimZ = aimZ;
        }
    }

    /// <summary>
    /// 技能执行结果
    /// </summary>
    public readonly struct SkillCastResult
    {
        public bool Success { get; }
        public string FailReason { get; }
        public int SkillId { get; }
        public int CasterActorId { get; }

        private SkillCastResult(bool success, int skillId, int casterActorId, string failReason)
        {
            Success = success;
            SkillId = skillId;
            CasterActorId = casterActorId;
            FailReason = failReason ?? "";
        }

        public static SkillCastResult Succeeded(int skillId, int casterActorId) =>
            new SkillCastResult(true, skillId, casterActorId, null);

        public static SkillCastResult Failed(int skillId, int casterActorId, string reason) =>
            new SkillCastResult(false, skillId, casterActorId, reason);
    }

    /// <summary>
    /// 技能执行器（表现层）
    ///
    /// 职责边界：
    /// - ✅ 技能输入处理
    /// - ✅ 冷却管理
    /// - ✅ 播放技能特效
    /// - ❌ 伤害计算（应在 MobaCoreSkillExecutor）
    /// - ❌ 技能流程执行（应在 MobaCoreSkillExecutor）
    ///
    /// 注意：这是纯表现层的简化实现。
    /// 完整逻辑应在 AbilityKit.Demo.Moba.Core 项目中通过 MobaCoreSkillExecutor 执行。
    /// </summary>
    public sealed class ConsoleSkillExecutor
    {
        private readonly BattleServices _battleServices;
        private readonly ConcurrentDictionary<int, int> _cooldownBySlot = new();
        private int _globalCooldownFrames;

        public ConsoleSkillExecutor(BattleServices battleServices)
        {
            _battleServices = battleServices ?? throw new ArgumentNullException(nameof(battleServices));
        }

        /// <summary>
        /// 处理技能输入事件
        /// </summary>
        public SkillCastResult HandleInput(int actorId, in SkillInputEvent evt)
        {
            switch (evt.Phase)
            {
                case SkillInputPhase.Press:
                    return HandlePress(actorId, evt.Slot);
                case SkillInputPhase.Release:
                    return HandleRelease(actorId, evt);
                case SkillInputPhase.Cancel:
                    return HandleCancel(actorId, evt.Slot);
                default:
                    return SkillCastResult.Failed(evt.Slot, actorId, $"Unknown phase: {evt.Phase}");
            }
        }

        /// <summary>
        /// 按槽位施放技能
        /// </summary>
        public SkillCastResult CastBySlot(int actorId, int slot)
        {
            var skillId = GetSkillIdBySlot(actorId, slot);
            return ExecuteSkill(actorId, skillId, slot, 0, 0, 0);
        }

        /// <summary>
        /// 带瞄准的技能施放
        /// </summary>
        public SkillCastResult CastBySlot(int actorId, int slot, float aimX, float aimZ)
        {
            var skillId = GetSkillIdBySlot(actorId, slot);
            return ExecuteSkill(actorId, skillId, slot, 0, aimX, aimZ);
        }

        /// <summary>
        /// 取消所有技能
        /// </summary>
        public void CancelAll(int actorId)
        {
            _cooldownBySlot.Clear();
        }

        /// <summary>
        /// 帧同步推进
        /// </summary>
        public void Step(int actorId)
        {
            if (_globalCooldownFrames > 0)
            {
                Interlocked.Decrement(ref _globalCooldownFrames);
            }

            foreach (var kvp in _cooldownBySlot)
            {
                var newValue = kvp.Value - 1;
                if (newValue <= 0)
                {
                    _cooldownBySlot.TryRemove(kvp.Key, out _);
                }
                else
                {
                    _cooldownBySlot.TryUpdate(kvp.Key, newValue, kvp.Value);
                }
            }
        }

        private SkillCastResult HandlePress(int actorId, int slot)
        {
            if (_globalCooldownFrames > 0)
            {
                return SkillCastResult.Failed(GetSkillIdBySlot(actorId, slot), actorId, "Global cooldown active");
            }

            if (_cooldownBySlot.TryGetValue(slot, out var cd) && cd > 0)
            {
                return SkillCastResult.Failed(GetSkillIdBySlot(actorId, slot), actorId, $"Slot {slot} on cooldown");
            }

            var skillId = GetSkillIdBySlot(actorId, slot);
            return ExecuteSkill(actorId, skillId, slot, 0, 0, 0);
        }

        private SkillCastResult HandleRelease(int actorId, in SkillInputEvent evt)
        {
            var skillId = GetSkillIdBySlot(actorId, evt.Slot);
            return ExecuteSkill(actorId, skillId, evt.Slot, evt.TargetActorId, evt.AimX, evt.AimZ);
        }

        private SkillCastResult HandleCancel(int actorId, int slot)
        {
            var skillId = GetSkillIdBySlot(actorId, slot);
            return SkillCastResult.Failed(skillId, actorId, "Cancelled");
        }

        private SkillCastResult ExecuteSkill(int actorId, int skillId, int slot, int targetActorId, float aimX, float aimZ)
        {
            // 设置冷却（简化：默认 1 秒 = 30 帧）
            _cooldownBySlot[slot] = 30;
            Interlocked.Exchange(ref _globalCooldownFrames, 3);

            Log.Skill($"[Skill] Actor#{actorId} cast skill#{skillId} (slot {slot})");

            // 播放技能特效（表现层）
            // 注意：伤害计算和技能流程应在 MobaCoreSkillExecutor 中执行

            return SkillCastResult.Succeeded(skillId, actorId);
        }

        private int GetSkillIdBySlot(int actorId, int slot)
        {
            // 简化：使用槽位映射
            return slot switch
            {
                1 => 101,
                2 => 102,
                3 => 103,
                _ => 100 + slot
            };
        }

        public bool IsOnCooldown(int slot) =>
            _cooldownBySlot.TryGetValue(slot, out var cd) && cd > 0;

        public int GetCooldownRemaining(int slot) =>
            _cooldownBySlot.TryGetValue(slot, out var cd) ? cd : 0;
    }
}
