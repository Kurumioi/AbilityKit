using System;
using System.Collections.Generic;
using AbilityKit.Core.Continuous;
using AbilityKit.Modifiers;
using AbilityKit.Samples.Abstractions;

namespace AbilityKit.Samples.Logic.Samples.Continuous
{
    /// <summary>
    /// ContinuousBasics - 持续行为基础示例
    /// 演示 IContinuous 接口、生命周期管理和 IContinuousManager 的使用
    /// </summary>
    [Sample]
    public sealed class ContinuousBasics : SampleBase
    {
        public override string Title => "持续行为系统 (Continuous)";
        public override string Description => "演示 IContinuous 接口和生命周期管理";
        public override SampleCategory Category => SampleCategory.Continuous;

        private SampleContinuousManager? _manager;

        protected override void OnRun()
        {
            Log("=== 持续行为系统基础 ===");
            Divider();

            _manager = new SampleContinuousManager();

            // 1. 核心概念
            Log("【1】核心概念");
            Bullet("IContinuous - 所有持续行为的外壳接口");
            Bullet("IContinuousConfig - 持续行为的配置接口");
            Bullet("IContinuousManager - 持续行为管理器");
            Bullet("ContinuousState - 生命周期状态枚举");
            Bullet("ContinuousEndReason - 结束原因枚举");
            Log("");

            // 2. IContinuous 状态机
            Log("【2】IContinuous 状态机");
            Log("  Inactive → Activate() → Active → Pause() → Paused");
            Log("                          ↓         ↑");
            Log("                       Resume()    Resume()");
            Log("                          ↓         ↑");
            Log("                        Abort() → Aborted (终态)");
            Log("                          ↓");
            Log("                     Expire() → Expired (终态)");
            Log("");

            // 3. 创建和激活持续行为
            Log("【3】创建和激活持续行为");
            Log("");

            var burnConfig = new BuffConfig
            {
                Id = "burn_001",
                OwnerId = 1001,
                DurationSeconds = 5f,
                Tags = new[] { "damage", "debuff" }
            };

            var burnEffect = new BuffEffect("燃烧", 10f, burnConfig);
            burnEffect.OnEnded += (_, reason) => Log($"  [事件] 燃烧结束: {reason}");

            Log($"  创建燃烧效果: 伤害 10/秒, 持续 5 秒");
            Log($"  尝试激活...");
            
            if (_manager.TryActivate(burnEffect))
            {
                Log("  ✓ 激活成功!");
            }
            else
            {
                Log("  ✗ 激活失败");
            }
            Log("");

            // 4. 模拟时间流逝
            Log("【4】模拟时间流逝 (Tick 0.5秒/次)");
            Log("");

            for (int i = 0; i < 10; i++)
            {
                _manager.Tick(0.5f);

                var state = burnEffect.State;
                var elapsed = burnEffect.ElapsedSeconds;
                Log($"  [Tick {i + 1}] State={state}, Elapsed={elapsed:F1}s");
            }

            Log("");

            // 5. 暂停和恢复
            Log("【5】暂停和恢复");
            Log("");

            var stunConfig = new BuffConfig
            {
                Id = "stun_001",
                OwnerId = 1001,
                DurationSeconds = 3f,
                Tags = new[] { "control", "stun" }
            };

            var stunEffect = new BuffEffect("眩晕", 0f, stunConfig);
            _manager.TryActivate(stunEffect);

            Log($"  激活眩晕效果, 持续 3 秒");
            Log($"  State: {stunEffect.State}, Elapsed: {stunEffect.ElapsedSeconds:F1}s");
            Log("");

            // 暂停
            Log("  调用 Pause()...");
            _manager.Pause(stunEffect);
            Log($"  State: {stunEffect.State}, Elapsed: {stunEffect.ElapsedSeconds:F1}s");
            Log("");

            // Tick 不会推进时间
            _manager.Tick(1f);
            Log("  Tick(1f) 之后:");
            Log($"  State: {stunEffect.State}, Elapsed: {stunEffect.ElapsedSeconds:F1}s");
            Log("");

            // 恢复
            Log("  调用 Resume()...");
            _manager.Resume(stunEffect);
            Log($"  State: {stunEffect.State}, Elapsed: {stunEffect.ElapsedSeconds:F1}s");
            Log("");

            // 6. 中断
            Log("【6】中断 (Abort)");
            Log("");

            var poisonConfig = new BuffConfig
            {
                Id = "poison_001",
                OwnerId = 1001,
                DurationSeconds = 10f,
                Tags = new[] { "damage", "debuff" }
            };

            var poisonEffect = new BuffEffect("中毒", 5f, poisonConfig);
            poisonEffect.OnEnded += (_, reason) => Log($"  [事件] 中毒结束: {reason}");
            _manager.TryActivate(poisonEffect);

            Log($"  激活中毒效果, 持续 10 秒");
            Log($"  State: {poisonEffect.State}");
            Log("");

            // 假设被净化技能中断
            Log("  触发净化技能, 调用 Abort()...");
            _manager.Abort(poisonEffect, "cleansed");
            Log($"  State: {poisonEffect.State}");
            Log("");

            // 7. 所有者死亡
            Log("【7】所有者死亡中断所有效果");
            Log("");

            var healthRegenConfig = new BuffConfig
            {
                Id = "health_regen_001",
                OwnerId = 1001,
                DurationSeconds = 5f,
                Tags = new[] { "heal" }
            };

            var healthRegen = new BuffEffect("生命回复", 20f, healthRegenConfig);
            healthRegen.OnEnded += (_, reason) => Log($"  [事件] 生命回复结束: {reason}");
            _manager.TryActivate(healthRegen);

            Log($"  激活生命回复效果");
            Log($"  调用 InterruptAll(1001, \"owner_dead\")...");
            _manager.InterruptAll(1001, "owner_dead");
            Log($"  剩余活跃持续行为: {_manager.ActiveCount}");
            Log("");

            // 8. 管理器统计
            Log("【8】IContinuousManager 关键方法");
            Bullet("Register() - 注册持续体");
            Bullet("Unregister() - 注销持续体");
            Bullet("TryActivate() - 尝试激活");
            Bullet("Pause() / Resume() - 暂停/恢复");
            Bullet("Abort() - 中断");
            Bullet("InterruptAll() - 中断所有者所有效果");
            Bullet("PauseByTag() / ResumeByTag() - 按标签操作");
            Bullet("GetOwnerContinuous() - 获取所有者的所有持续体");
            Log("");

            // 9. 结束原因
            Log("【9】ContinuousEndReason 枚举值");
            Bullet("Completed - 正常完成（到期）");
            Bullet("Interrupted - 被中断（Abort）");
            Bullet("Replaced - 被替换（互斥）");
            Bullet("OwnerDead - 所属实体死亡");
            Bullet("CleanedUp - 被清理");
            Log("");

            Divider();
            Log("【总结】IContinuous 提供了统一的持续行为抽象，");
            Log("       IContinuousManager 提供统一的生命周期管理。");
            Log("       Behavior 包和 Triggering 包都实现了此接口。");
        }
    }

    /// <summary>
    /// 示例 Buff 配置
    /// </summary>
    public sealed class BuffConfig
    {
        public string Id { get; set; } = string.Empty;
        public long OwnerId { get; set; }
        public float DurationSeconds { get; set; }
        public string[] Tags { get; set; } = Array.Empty<string>();
        public bool CanBeInterrupted => true;
    }

    /// <summary>
    /// 示例 Buff 效果实现
    /// </summary>
    public sealed class BuffEffect : IContinuous
    {
        private readonly string _name;
        private readonly float _damagePerSecond;
        private readonly BuffConfig _config;
        private ContinuousState _state = ContinuousState.Inactive;
        private float _elapsed;

        public event Action<IContinuous, ContinuousEndReason>? OnEnded;

        public IContinuousConfig Config => new BuffConfigAdapter(_config);
        public ContinuousState State => _state;
        public bool IsActive => _state == ContinuousState.Active;
        public bool IsTerminated => _state == ContinuousState.Expired || _state == ContinuousState.Aborted;
        public bool IsPaused => _state == ContinuousState.Paused;
        public float ElapsedSeconds => _elapsed;

        public BuffEffect(string name, float damagePerSecond, BuffConfig config)
        {
            _name = name;
            _damagePerSecond = damagePerSecond;
            _config = config;
        }

        public void Activate()
        {
            _state = ContinuousState.Active;
            _elapsed = 0f;
        }

        public void Pause() => _state = ContinuousState.Paused;
        public void Resume() => _state = ContinuousState.Active;

        public void Abort(string reason)
        {
            _state = ContinuousState.Aborted;
            OnEnded?.Invoke(this, ContinuousEndReason.Interrupted);
        }

        public void InternalTick(float deltaTime)
        {
            if (_state != ContinuousState.Active) return;

            _elapsed += deltaTime;

            // 每秒造成伤害
            if (_damagePerSecond > 0)
            {
                var damage = _damagePerSecond * deltaTime;
                // Log($"  [{_name}] 造成 {damage:F2} 伤害");
            }

            // 检查时长
            if (_elapsed >= _config.DurationSeconds)
            {
                _state = ContinuousState.Expired;
                OnEnded?.Invoke(this, ContinuousEndReason.Completed);
            }
        }

        private sealed class BuffConfigAdapter : IContinuousConfig
        {
            private readonly BuffConfig _inner;
            public BuffConfigAdapter(BuffConfig inner) => _inner = inner;
            public string Id => _inner.Id;
            public long OwnerId => _inner.OwnerId;
            public bool CanBeInterrupted => _inner.CanBeInterrupted;
        }
    }

    /// <summary>
    /// 示例持续行为管理器
    /// </summary>
    public sealed class SampleContinuousManager
    {
        private readonly List<IContinuous> _continuous = new();

        public int ActiveCount => _continuous.Count(c => c.IsActive);

        public bool TryActivate(IContinuous continuous)
        {
            if (continuous.State != ContinuousState.Inactive)
                return false;

            continuous.Activate();
            _continuous.Add(continuous);
            return true;
        }

        public void Pause(IContinuous continuous)
        {
            if (continuous.IsActive)
                continuous.Pause();
        }

        public void Resume(IContinuous continuous)
        {
            if (continuous.IsPaused)
                continuous.Resume();
        }

        public void Abort(IContinuous continuous, string reason)
        {
            if (!continuous.IsTerminated)
            {
                continuous.Abort(reason);
                _continuous.Remove(continuous);
            }
        }

        public void InterruptAll(long ownerId, string reason)
        {
            var toRemove = new List<IContinuous>();
            foreach (var c in _continuous)
            {
                if (c.Config.OwnerId == ownerId)
                {
                    c.Abort(reason);
                    toRemove.Add(c);
                }
            }
            foreach (var c in toRemove)
            {
                _continuous.Remove(c);
            }
        }

        public void Tick(float deltaTime)
        {
            var toRemove = new List<IContinuous>();

            foreach (var c in _continuous)
            {
                // Tick 所有需要更新的 IContinuous
                if (c is BuffEffect buff)
                {
                    buff.InternalTick(deltaTime);
                }

                // 检查是否已终止
                if (c.IsTerminated)
                {
                    toRemove.Add(c);
                }
            }

            // 清理已终止的
            foreach (var c in toRemove)
            {
                _continuous.Remove(c);
            }
        }
    }
}
