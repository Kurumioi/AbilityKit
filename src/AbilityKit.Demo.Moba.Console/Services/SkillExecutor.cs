using System;
using System.Collections.Generic;
using AbilityKit.Demo.Moba.Console.Bootstrap;
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
        public float DirX { get; }
        public float DirZ { get; }

        public ConsoleSkillCastRequest(
            int skillId,
            int skillSlot,
            int casterActorId,
            int targetActorId = 0,
            float aimX = 0,
            float aimZ = 0,
            float dirX = 0,
            float dirZ = 0)
        {
            SkillId = skillId;
            SkillSlot = skillSlot;
            CasterActorId = casterActorId;
            TargetActorId = targetActorId;
            AimX = aimX;
            AimZ = aimZ;
            DirX = dirX;
            DirZ = dirZ;
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
        public int TargetActorId { get; }
        public float Damage { get; }

        private SkillCastResult(bool success, int skillId, int casterActorId, int targetActorId, float damage, string failReason)
        {
            Success = success;
            SkillId = skillId;
            CasterActorId = casterActorId;
            TargetActorId = targetActorId;
            Damage = damage;
            FailReason = failReason ?? "";
        }

        public static SkillCastResult Succeeded(int skillId, int casterActorId, int targetActorId, float damage = 0)
        {
            return new SkillCastResult(true, skillId, casterActorId, targetActorId, damage, null);
        }

        public static SkillCastResult Failed(int skillId, int casterActorId, string reason)
        {
            return new SkillCastResult(false, skillId, casterActorId, 0, 0, reason);
        }
    }

    /// <summary>
    /// 技能执行器
    /// 处理技能输入并执行技能
    /// </summary>
    public sealed class ConsoleSkillExecutor
    {
        private readonly MobaConfigDatabase _config;
        private readonly BattleServices _battleServices;
        private readonly Dictionary<int, int> _cooldownBySlot = new();
        private int _globalCooldownFrames;

        public bool AllowParallel { get; set; }
        public bool InterruptRunning { get; set; }

        public ConsoleSkillExecutor(MobaConfigDatabase config, BattleServices battleServices)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _battleServices = battleServices ?? throw new ArgumentNullException(nameof(battleServices));
            AllowParallel = false;
            InterruptRunning = false;
            Log.Trace("[TRACE] ConsoleSkillExecutor created");
        }

        /// <summary>
        /// 处理技能输入事件
        /// </summary>
        public SkillCastResult HandleInput(int actorId, in SkillInputEvent evt)
        {
            Log.Trace($"[TRACE] ConsoleSkillExecutor.HandleInput - Actor:{actorId}, Slot:{evt.Slot}, Phase:{evt.Phase}");
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
            Log.Trace($"[TRACE] ConsoleSkillExecutor.CastBySlot - Actor:{actorId}, Slot:{slot}");

            // 获取角色的技能列表
            var caster = _battleServices.GetActor(actorId);
            if (caster == null)
            {
                Log.Trace($"[TRACE] CastBySlot - Caster not found: {actorId}");
                return SkillCastResult.Failed(slot, actorId, "Caster not found");
            }

            // 从角色配置获取技能列表
            int skillIndex = slot - 1; // slot 1 = index 0
            if (_config.TryGetCharacter(caster.CharacterId, out var charConfig) && charConfig.SkillIds != null)
            {
                if (skillIndex >= 0 && skillIndex < charConfig.SkillIds.Length)
                {
                    var skillId = charConfig.SkillIds[skillIndex];
                    Log.Trace($"[TRACE] CastBySlot - Using skillId:{skillId} from character config");
                    var request = new ConsoleSkillCastRequest(
                        skillId: skillId,
                        skillSlot: slot,
                        casterActorId: actorId);
                    return ExecuteSkill(request);
                }
            }

            // Fallback: 使用配置的槽位映射（如果角色没有配置技能）
            return HandlePress(actorId, slot);
        }

        /// <summary>
        /// 带瞄准的技能施放
        /// </summary>
        public SkillCastResult CastBySlot(int actorId, int slot, float aimX, float aimZ, float dirX = 0, float dirZ = 0)
        {
            Log.Trace($"[TRACE] ConsoleSkillExecutor.CastBySlot(aimed) - Actor:{actorId}, Slot:{slot}, Aim:({aimX:F1},{aimZ:F1})");
            var request = new ConsoleSkillCastRequest(
                skillId: GetSkillIdBySlot(slot),
                skillSlot: slot,
                casterActorId: actorId,
                aimX: aimX,
                aimZ: aimZ,
                dirX: dirX,
                dirZ: dirZ);

            return ExecuteSkill(request);
        }

        /// <summary>
        /// 取消所有技能
        /// </summary>
        public void CancelAll(int actorId)
        {
            _cooldownBySlot.Clear();
            Log.Skill($"[SkillExecutor] CancelAll for actor {actorId}");
        }

        /// <summary>
        /// 按技能ID取消
        /// </summary>
        public void CancelBySkillId(int actorId, int skillId)
        {
            Log.Skill($"[SkillExecutor] CancelBySkillId {skillId} for actor {actorId}");
        }

        /// <summary>
        /// 帧同步推进
        /// </summary>
        public void Step(int actorId)
        {
            if (_globalCooldownFrames > 0)
            {
                _globalCooldownFrames--;
            }

            // 减少各槽位冷却
            var toRemove = new List<int>();
            foreach (var kvp in _cooldownBySlot)
            {
                if (kvp.Value > 0)
                {
                    _cooldownBySlot[kvp.Key] = kvp.Value - 1;
                    if (_cooldownBySlot[kvp.Key] <= 0)
                    {
                        toRemove.Add(kvp.Key);
                    }
                }
            }

            foreach (var slot in toRemove)
            {
                _cooldownBySlot.Remove(slot);
                Log.Skill($"[SkillExecutor] Skill slot {slot} cooldown ready");
            }
        }

        private SkillCastResult HandlePress(int actorId, int slot)
        {
            Log.Trace($"[TRACE] ConsoleSkillExecutor.HandlePress - Actor:{actorId}, Slot:{slot}");
            if (_globalCooldownFrames > 0)
            {
                Log.Trace($"[TRACE] HandlePress - Global cooldown active ({_globalCooldownFrames} frames)");
                return SkillCastResult.Failed(GetSkillIdBySlot(slot), actorId, "Global cooldown not ready");
            }

            if (_cooldownBySlot.TryGetValue(slot, out var cd) && cd > 0)
            {
                Log.Trace($"[TRACE] HandlePress - Slot {slot} on cooldown ({cd} frames)");
                return SkillCastResult.Failed(GetSkillIdBySlot(slot), actorId, $"Skill slot {slot} on cooldown: {cd} frames");
            }

            var request = new ConsoleSkillCastRequest(
                skillId: GetSkillIdBySlot(slot),
                skillSlot: slot,
                casterActorId: actorId);

            Log.Trace($"[TRACE] HandlePress - Executing skill, SkillId:{request.SkillId}");
            return ExecuteSkill(request);
        }

        private SkillCastResult HandleRelease(int actorId, in SkillInputEvent evt)
        {
            Log.Trace($"[TRACE] ConsoleSkillExecutor.HandleRelease - Actor:{actorId}, Slot:{evt.Slot}");
            var request = new ConsoleSkillCastRequest(
                skillId: GetSkillIdBySlot(evt.Slot),
                skillSlot: evt.Slot,
                casterActorId: actorId,
                targetActorId: evt.TargetActorId,
                aimX: evt.AimX,
                aimZ: evt.AimZ,
                dirX: evt.DirX,
                dirZ: evt.DirZ);

            return ExecuteSkill(request);
        }

        private SkillCastResult HandleCancel(int actorId, int slot)
        {
            Log.Trace($"[TRACE] ConsoleSkillExecutor.HandleCancel - Actor:{actorId}, Slot:{slot}");
            CancelBySkillId(actorId, GetSkillIdBySlot(slot));
            return SkillCastResult.Failed(GetSkillIdBySlot(slot), actorId, "Cancelled");
        }

        private SkillCastResult ExecuteSkill(in ConsoleSkillCastRequest request)
        {
            Log.Trace($"[TRACE] ConsoleSkillExecutor.ExecuteSkill - Entry - SkillId:{request.SkillId}, Caster:{request.CasterActorId}");
            var skillId = request.SkillId;

            // 查找技能配置
            if (!_config.TryGetSkill(skillId, out var skillConfig))
            {
                Log.Trace($"[TRACE] ExecuteSkill - Skill config not found: {skillId}, using default");
                Log.Warn($"[SkillExecutor] Skill config not found: {skillId}");
                return ExecuteDefaultSkill(request);
            }

            Log.Trace($"[TRACE] ExecuteSkill - Found skill config: {skillConfig.Name}");
            Log.Skill($"[SkillExecutor] Executing skill {skillConfig.Name} (ID:{skillId}) from slot {request.SkillSlot}");

            // 设置冷却（CooldownMs -> frames，假设 30 FPS）
            var cooldownFrames = skillConfig.CooldownMs / (1000 / 30);
            if (cooldownFrames > 0)
            {
                _cooldownBySlot[request.SkillSlot] = cooldownFrames;
                _globalCooldownFrames = 3; // 0.1秒全局冷却
                Log.Trace($"[TRACE] ExecuteSkill - Set cooldown: Slot{request.SkillSlot}={cooldownFrames} frames, GCD=3 frames");
            }

            // 获取施法者和属性
            var caster = _battleServices.GetActor(request.CasterActorId);
            if (caster == null)
            {
                Log.Trace($"[TRACE] ExecuteSkill - Caster not found: Actor#{request.CasterActorId}");
                return SkillCastResult.Failed(skillId, request.CasterActorId, "Caster not found");
            }

            Log.Trace($"[TRACE] ExecuteSkill - Found caster: {caster.Name}, ATK:{caster.PhysicsAttack}");

            // 从角色配置获取属性模板
            var attributes = _config.GetCharacterAttributes(new CharacterConfig { AttributeTemplateId = caster.AttributeTemplateId });

            // 使用角色攻击力作为伤害基础
            float totalAttack = caster.PhysicsAttack + (attributes?.PhysicsAttack ?? 0);
            Log.Trace($"[TRACE] ExecuteSkill - TotalAttack: {totalAttack:F1} (base:{caster.PhysicsAttack} + attr:{attributes?.PhysicsAttack ?? 0})");

            if (request.TargetActorId > 0)
            {
                var target = _battleServices.GetActor(request.TargetActorId);
                if (target != null)
                {
                    Log.Trace($"[TRACE] ExecuteSkill - Target found: {target.Name}, DEF:{target.PhysicsDefense}");
                    // 伤害 = 攻击力 * (1 - 防御减免)
                    var defenseReduction = target.PhysicsDefense / (target.PhysicsDefense + 100f);
                    float damage = totalAttack * (1f - defenseReduction);

                    Log.Trace($"[TRACE] ExecuteSkill - Applying damage: {damage:F1} to #{request.TargetActorId}");
                    _battleServices.ApplyDamage(request.TargetActorId, damage, request.CasterActorId, skillId);
                    Log.Skill($"[SkillExecutor] {skillConfig.Name} dealt {damage:F1} damage to #{request.TargetActorId}");
                    Log.Trace($"[TRACE] ExecuteSkill - Exit (target damage) - Success");
                    return SkillCastResult.Succeeded(skillId, request.CasterActorId, request.TargetActorId, damage);
                }
            }

            // 区域伤害检测
            if (request.AimX != 0 || request.AimZ != 0)
            {
                Log.Trace($"[TRACE] ExecuteSkill - Area detection at ({request.AimX:F1}, {request.AimZ:F1})");
                var hitActorId = _battleServices.FindActorAtPosition(request.AimX, request.AimZ);
                if (hitActorId > 0)
                {
                    var target = _battleServices.GetActor(hitActorId);
                    if (target != null)
                    {
                        Log.Trace($"[TRACE] ExecuteSkill - Area hit: #{hitActorId} ({target.Name})");
                        var defenseReduction = target.PhysicsDefense / (target.PhysicsDefense + 100f);
                        float damage = totalAttack * (1f - defenseReduction);
                        _battleServices.ApplyDamage(hitActorId, damage, request.CasterActorId, skillId);
                        Log.Skill($"[SkillExecutor] {skillConfig.Name} dealt {damage:F1} damage to #{hitActorId}");
                        Log.Trace($"[TRACE] ExecuteSkill - Exit (area damage) - Success");
                        return SkillCastResult.Succeeded(skillId, request.CasterActorId, hitActorId, damage);
                    }
                }
            }

            // 触发技能事件
            Log.Trace($"[TRACE] ExecuteSkill - Triggering OnSkillCast event");
            _battleServices.OnSkillCast(request.CasterActorId, skillId, request.SkillSlot);
            Log.Trace($"[TRACE] ExecuteSkill - Exit (no target) - Success");

            return SkillCastResult.Succeeded(skillId, request.CasterActorId, request.TargetActorId, 0);
        }

        private SkillCastResult ExecuteDefaultSkill(in ConsoleSkillCastRequest request)
        {
            Log.Trace($"[TRACE] ConsoleSkillExecutor.ExecuteDefaultSkill - SkillId:{request.SkillId}, Caster:{request.CasterActorId}");
            Log.Skill($"[SkillExecutor] Executing default skill slot {request.SkillSlot} for actor {request.CasterActorId}");

            // 默认技能效果
            float damage = 50f; // 默认伤害

            if (request.AimX != 0 || request.AimZ != 0)
            {
                var hitActorId = _battleServices.FindActorAtPosition(request.AimX, request.AimZ);
                if (hitActorId > 0)
                {
                    _battleServices.ApplyDamage(hitActorId, damage, request.CasterActorId, request.SkillId);
                }
            }

            _battleServices.OnSkillCast(request.CasterActorId, request.SkillId, request.SkillSlot);
            Log.Trace($"[TRACE] ExecuteDefaultSkill - Exit - Success");

            return SkillCastResult.Succeeded(request.SkillId, request.CasterActorId, request.TargetActorId, damage);
        }

        private int GetSkillIdBySlot(int slot)
        {
            // 槽位到技能ID的映射
            return slot switch
            {
                1 => 101, // 普通攻击 -> 101
                2 => 102, // 技能1 -> 102
                3 => 103, // 技能2 -> 103
                _ => 100 + slot
            };
        }

        /// <summary>
        /// 检查冷却状态
        /// </summary>
        public bool IsOnCooldown(int slot)
        {
            return _cooldownBySlot.TryGetValue(slot, out var cd) && cd > 0;
        }

        /// <summary>
        /// 获取冷却剩余帧数
        /// </summary>
        public int GetCooldownRemaining(int slot)
        {
            return _cooldownBySlot.TryGetValue(slot, out var cd) ? cd : 0;
        }
    }
}
