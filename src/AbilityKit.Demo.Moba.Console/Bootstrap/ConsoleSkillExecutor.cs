using System;
using System.Collections.Generic;
using AbilityKit.Ability.Share.Impl.Moba.Struct;
using AbilityKit.Core.Math;

namespace AbilityKit.Demo.Moba.Console.Bootstrap
{
    /// <summary>
    /// Console 技能执行器实现
    ///
    /// 职责：
    /// - 从配置数据库读取技能配置
    /// - 根据技能等级表计算伤害值
    /// - 管理冷却时间
    /// - 返回执行结果（由 Simulation 层处理分发）
    ///
    /// 数据来源：
    /// - 技能配置从 ConsoleMobaConfigDatabase 读取
    /// - 角色信息从 ConsoleActorRepository 读取
    /// - 目标查找由 ConsoleActorRepository 提供
    /// </summary>
    public sealed class ConsoleSkillExecutor : IConsoleSkillExecutor, IDisposable
    {
        private readonly ConsoleMobaConfigDatabase _configDb;
        private readonly Simulation.ConsoleActorRepository _actorRepository;
        private readonly CooldownManager _cooldownManager;
        private bool _initialized;
        private bool _disposed;

        public ConsoleSkillExecutor(ConsoleMobaConfigDatabase configDb, Simulation.ConsoleActorRepository actorRepository)
        {
            _configDb = configDb ?? throw new ArgumentNullException(nameof(configDb));
            _actorRepository = actorRepository ?? throw new ArgumentNullException(nameof(actorRepository));
            _cooldownManager = new CooldownManager();
        }

        /// <summary>
        /// 初始化执行器
        /// </summary>
        public void Initialize()
        {
            if (_initialized) return;
            _cooldownManager.Initialize();
            _initialized = true;
            Platform.Log.System("[SkillExecutor] Initialized");
        }

        /// <summary>
        /// 按槽位释放技能
        /// </summary>
        public bool CastBySlot(int actorId, int slot)
        {
            return CastBySlot(actorId, slot, Vec3.Zero, Vec3.Forward).Success;
        }

        /// <summary>
        /// 按槽位释放技能（带瞄准信息）
        /// 返回执行结果，由调用者处理事件分发
        /// </summary>
        public SkillCastResult CastBySlot(int actorId, int slot, Vec3 aimPos, Vec3 aimDir)
        {
            if (_disposed) return SkillCastResult.CreateFailure(actorId, slot, 0, "Disposed");
            if (actorId <= 0 || slot <= 0) return SkillCastResult.CreateFailure(actorId, slot, 0, "Invalid actor or slot");

            // 获取技能 ID（通过槽位映射）
            var skillId = GetSkillIdBySlot(actorId, slot);
            if (skillId <= 0)
            {
                Platform.Log.Skill($"[SkillExecutor] No skill found for Actor#{actorId} Slot{slot}");
                return SkillCastResult.CreateFailure(actorId, slot, 0, "No skill found for slot");
            }

            // 检查冷却
            if (_cooldownManager.IsOnCooldown(skillId))
            {
                Platform.Log.Skill($"[SkillExecutor] Skill#{skillId} on cooldown");
                return SkillCastResult.CreateFailure(actorId, slot, skillId, "On cooldown");
            }

            // 获取技能配置
            if (!_configDb.TryGetSkill(skillId, out var skillConfig))
            {
                Platform.Log.Skill($"[SkillExecutor] Skill config not found: {skillId}");
                return SkillCastResult.CreateFailure(actorId, slot, skillId, "Skill config not found");
            }

            // 获取技能等级表
            var levelConfig = GetSkillLevelConfig(skillConfig, actorId);
            if (levelConfig == null)
            {
                Platform.Log.Skill($"[SkillExecutor] Skill level config not found: {skillId}");
                return SkillCastResult.CreateFailure(actorId, slot, skillId, "Skill level config not found");
            }

            // 查找目标
            var targetId = FindTarget(actorId);
            if (targetId <= 0)
            {
                Platform.Log.Skill($"[SkillExecutor] No target found for caster #{actorId}");
                return SkillCastResult.CreateFailure(actorId, slot, skillId, "No target found");
            }

            // 计算伤害值（从等级配置读取 Params[0]）
            var baseDamage = levelConfig.GetParam(0, 50f);

            // 开始冷却
            var cooldownFrames = (int)(levelConfig.CooldownSeconds * 30f); // 假设 30 FPS
            _cooldownManager.StartCooldown(skillId, cooldownFrames);

            // 获取技能等级用于日志
            var actor = _actorRepository.GetActor(actorId);
            var skillLevel = actor?.SkillLevel ?? 1;

            Platform.Log.Skill($"[SkillExecutor] Actor#{actorId} cast Skill#{skillId} (Lv{skillLevel}) " +
                              $"BaseDamage={baseDamage:F0} -> Target#{targetId}");

            return SkillCastResult.CreateSuccess(actorId, slot, skillId, targetId, baseDamage);
        }

        /// <summary>
        /// 计算最终伤害（由 Simulation 层调用）
        /// </summary>
        /// <param name="casterId">释放者 ID</param>
        /// <param name="targetId">目标 ID</param>
        /// <param name="skillId">技能 ID</param>
        /// <param name="baseDamage">基础伤害</param>
        /// <param name="targetCurrentHp">目标当前 HP</param>
        /// <param name="targetMaxHp">目标最大 HP</param>
        /// <returns>伤害执行结果</returns>
        public DamageExecuteResult CalculateDamage(int casterId, int targetId, int skillId,
            float baseDamage, float targetCurrentHp, float targetMaxHp)
        {
            // 简化实现：直接使用配置的基础伤害
            // 完整实现应考虑攻击方属性、防御方属性、伤害类型等
            float damage = baseDamage;
            float newHp = targetCurrentHp - damage;
            bool isDead = newHp <= 0;

            return new DamageExecuteResult(
                casterId, targetId, skillId,
                damage, newHp, targetMaxHp, isDead, false);
        }

        /// <summary>
        /// 帧推进
        /// </summary>
        public void Step(float deltaTime)
        {
            if (_disposed) return;
            _cooldownManager.Step();
        }

        /// <summary>
        /// 根据槽位获取技能 ID
        /// 从角色的 SkillIds 数组中读取（槽位 1=index 0, 槽位 2=index 1, 槽位 3=index 2）
        /// </summary>
        private int GetSkillIdBySlot(int actorId, int slot)
        {
            // 获取角色信息
            var actor = _actorRepository.GetActor(actorId);
            if (actor == null)
            {
                Platform.Log.Warn($"[SkillExecutor] Actor#{actorId} not found in repository");
                return 0;
            }

            // 获取角色配置
            if (!_configDb.TryGetCharacter(actor.CharacterId, out var charConfig))
            {
                Platform.Log.Warn($"[SkillExecutor] Character config not found: {actor.CharacterId}");
                return 0;
            }

            // 槽位转换为索引（槽位 1 -> index 0）
            var index = slot - 1;
            if (index < 0 || index >= charConfig.SkillIds.Length)
            {
                Platform.Log.Warn($"[SkillExecutor] Invalid slot {slot} for Actor#{actorId} (SkillIds length: {charConfig.SkillIds.Length})");
                return 0;
            }

            return charConfig.SkillIds[index];
        }

        /// <summary>
        /// 获取技能等级配置
        /// </summary>
        private SkillLevelConfig GetSkillLevelConfig(SkillConfig skillConfig, int actorId)
        {
            if (skillConfig == null) return null;
            if (skillConfig.LevelTableId <= 0) return null;

            // 从角色信息获取技能等级（默认为 1）
            var actor = _actorRepository.GetActor(actorId);
            var skillLevel = actor?.SkillLevel ?? 1;

            if (!_configDb.TryGetSkillLevelTable(skillConfig.LevelTableId, out var levelTable))
            {
                return null;
            }

            // 索引从 0 开始，技能等级从 1 开始
            var index = skillLevel - 1;
            if (index < 0 || index >= levelTable.Levels.Count)
            {
                index = Math.Min(Math.Max(0, index), levelTable.Levels.Count - 1);
            }

            return levelTable.Levels[index];
        }

        /// <summary>
        /// 查找攻击目标
        /// 通过 ConsoleActorRepository 查找范围内的最近的敌方单位
        /// </summary>
        private int FindTarget(int casterId)
        {
            // 使用 ConsoleActorRepository 查找最近的敌方单位
            const float searchRange = 15f;
            return _actorRepository.FindNearestEnemy(casterId, searchRange);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _cooldownManager.Dispose();
        }

        /// <summary>
        /// 冷却管理器
        /// </summary>
        private sealed class CooldownManager : IDisposable
        {
            private readonly Dictionary<int, int> _cooldowns = new Dictionary<int, int>();
            private bool _disposed;

            public void Initialize()
            {
                _cooldowns.Clear();
            }

            public bool IsOnCooldown(int skillId)
            {
                return _cooldowns.TryGetValue(skillId, out var remaining) && remaining > 0;
            }

            public void StartCooldown(int skillId, int frames)
            {
                _cooldowns[skillId] = frames;
            }

            public void Step()
            {
                var keysToRemove = new List<int>();
                foreach (var kvp in _cooldowns)
                {
                    if (kvp.Value > 0)
                    {
                        _cooldowns[kvp.Key] = kvp.Value - 1;
                        if (_cooldowns[kvp.Key] <= 0)
                        {
                            keysToRemove.Add(kvp.Key);
                        }
                    }
                }
                foreach (var key in keysToRemove)
                {
                    _cooldowns.Remove(key);
                }
            }

            public void Dispose()
            {
                if (_disposed) return;
                _disposed = true;
                _cooldowns.Clear();
            }
        }
    }
}
