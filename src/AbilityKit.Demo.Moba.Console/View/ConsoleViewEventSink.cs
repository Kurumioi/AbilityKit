using System;
using AbilityKit.Demo.Moba.Console.Events;
using AbilityKit.Demo.Moba.Console.Platform;
using AbilityKit.Demo.Moba.Console.Services;

namespace AbilityKit.Demo.Moba.Console.View
{
    /// <summary>
    /// Console 版本的 View 事件接收器
    /// 负责订阅框架事件并调用 View 接口进行表现
    /// </summary>
    public sealed class ConsoleViewEventSink : IDisposable
    {
        private readonly IConsoleBattleView _battleView;
        private readonly ConsoleEntityDisplayService _entityDisplay;
        private bool _disposed;

        public ConsoleViewEventSink(IConsoleBattleView battleView)
        {
            _battleView = battleView ?? throw new ArgumentNullException(nameof(battleView));
            _entityDisplay = battleView.EntityDisplay;

            SubscribeEvents();
        }

        private void SubscribeEvents()
        {
            BattleEventBus.Subscribe<DamageEvent>(OnDamage);
            BattleEventBus.Subscribe<HealEvent>(OnHeal);
            BattleEventBus.Subscribe<BuffAppliedEvent>(OnBuffApplied);
            BattleEventBus.Subscribe<EntityDestroyedEvent>(OnEntityDestroyed);
            BattleEventBus.Subscribe<EntityCreatedEvent>(OnEntityCreated);
            BattleEventBus.Subscribe<SkillExecutedEvent>(OnSkillExecuted);
            BattleEventBus.Subscribe<ProjectileHitEvent>(OnProjectileHit);

            Log.Trace("[ConsoleViewEventSink] Subscribed to framework events");
        }

        private void UnsubscribeEvents()
        {
            BattleEventBus.Unsubscribe<DamageEvent>(OnDamage);
            BattleEventBus.Unsubscribe<HealEvent>(OnHeal);
            BattleEventBus.Unsubscribe<BuffAppliedEvent>(OnBuffApplied);
            BattleEventBus.Unsubscribe<EntityDestroyedEvent>(OnEntityDestroyed);
            BattleEventBus.Unsubscribe<EntityCreatedEvent>(OnEntityCreated);
            BattleEventBus.Unsubscribe<SkillExecutedEvent>(OnSkillExecuted);
            BattleEventBus.Unsubscribe<ProjectileHitEvent>(OnProjectileHit);

            Log.Trace("[ConsoleViewEventSink] Unsubscribed from framework events");
        }

        #region Event Handlers

        private void OnDamage(DamageEvent evt)
        {
            if (evt.TargetId <= 0) return;
            if (evt.Damage == 0) return;

            if (_entityDisplay.TryGet(evt.TargetId, out var entity))
            {
                _battleView.ShowFloatingText(evt.TargetId, $"-{evt.Damage:F0}", false);
                _battleView.UpdateEntityHp(evt.TargetId, entity.Hp - evt.Damage, entity.MaxHp);
                Log.Trace($"[View] Damage: Actor#{evt.TargetId} took {evt.Damage:F0} damage from Actor#{evt.SourceId}");
            }
        }

        private void OnHeal(HealEvent evt)
        {
            if (evt.TargetId <= 0) return;
            if (evt.Amount == 0) return;

            if (_entityDisplay.TryGet(evt.TargetId, out var entity))
            {
                var newHp = Math.Min(entity.Hp + evt.Amount, entity.MaxHp);
                _battleView.ShowFloatingText(evt.TargetId, $"+{evt.Amount:F0}", true);
                _battleView.UpdateEntityHp(evt.TargetId, newHp, entity.MaxHp);
                Log.Trace($"[View] Heal: Actor#{evt.TargetId} healed for {evt.Amount:F0}");
            }
        }

        private void OnBuffApplied(BuffAppliedEvent evt)
        {
            if (evt.TargetId <= 0) return;
            _battleView.ShowBuffApply(evt.TargetId, evt.BuffId, evt.CasterId);
            Log.Trace($"[View] Buff: Actor#{evt.TargetId} gained Buff#{evt.BuffId} from Actor#{evt.CasterId}");
        }

        private void OnEntityDestroyed(EntityDestroyedEvent evt)
        {
            if (evt.ActorId <= 0) return;

            if (_entityDisplay.TryGet(evt.ActorId, out var entity))
            {
                _battleView.ShowFloatingText(evt.ActorId, "DIED!", false);
                _battleView.UpdateEntityHp(evt.ActorId, 0, entity.MaxHp);
                Log.Trace($"[View] Death: Actor#{evt.ActorId} ({entity.Name}) died");
            }
        }

        private void OnEntityCreated(EntityCreatedEvent evt)
        {
            if (evt.ActorId <= 0) return;

            _battleView.RegisterEntity(
                evt.ActorId,
                evt.Name ?? $"Actor#{evt.ActorId}",
                "Character",
                evt.HP,
                evt.MaxHp,
                evt.X,
                0,
                evt.Z);

            Log.Trace($"[View] Entity: Actor#{evt.ActorId} ({evt.Name}) created at ({evt.X:F1}, {evt.Z:F1})");
        }

        private void OnSkillExecuted(SkillExecutedEvent evt)
        {
            if (evt.Success)
            {
                Log.Trace($"[View] Skill: Actor#{evt.ActorId} executed skill in slot {evt.Slot}");
            }
            else
            {
                Log.Trace($"[View] Skill: Actor#{evt.ActorId} failed to execute skill in slot {evt.Slot}: {evt.FailReason}");
            }
        }

        private void OnProjectileHit(ProjectileHitEvent evt)
        {
            Log.Trace($"[View] Projectile: Hit projectile {evt.ProjectileId} on Actor#{evt.TargetId}");
        }

        #endregion

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            UnsubscribeEvents();
        }
    }
}
