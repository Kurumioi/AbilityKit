using System;
using AbilityKit.Demo.Moba.Share;
using AbilityKit.Demo.Moba.Console.Platform;

namespace AbilityKit.Demo.Moba.Console.View
{
    /// <summary>
    /// Console 版本的战斗视图事件接收器
    /// 实现 Share 层的 BaseBattleViewEventSink 接口
    ///
    /// 职责：
    /// - ✅ 只通过 Share 层事件更新视图
    /// - ✅ 订阅 FrameSnapshotDispatcher 分发的事件
    /// - ✅ 更新 ConsoleBattleView 进行渲染
    /// - ❌ 不直接订阅 BattleEventBus
    ///
    /// 事件流程：
    /// 1. Simulation 层发布 DamageEvent
    /// 2. ConsoleFrameSnapshotDispatcher 转换为 Share 类型
    /// 3. Share FrameSnapshotDispatcher 分发
    /// 4. 此类通过 Share 层订阅收到事件 → 更新视图
    /// </summary>
    public sealed class ConsoleBattleViewEventSink : BaseBattleViewEventSink
    {
        private readonly IConsoleBattleView _battleView;
        private bool _disposed;

        public ConsoleBattleViewEventSink(IConsoleBattleView battleView, string playerId = "player_1")
        {
            _battleView = battleView ?? throw new ArgumentNullException(nameof(battleView));
        }

        #region BaseBattleViewEventSink 抽象方法实现

        protected override void OnEnterGame(in EnterGameData data)
        {
            int playerCount = data.PlayerIds?.Count ?? 0;
            _battleView.OnGameStart(playerCount);
            Log.View($"[ConsoleViewEventSink] EnterGame: LocalPlayer#{data.LocalPlayerId}, {playerCount} players");
        }

        protected override void OnActorTransform(int actorId, float x, float y, float z, float rotationY, float scale)
        {
            _battleView.UpdateActorPosition(actorId, x, y, z);
            Log.Trace($"[ConsoleViewEventSink] ActorTransform: #{actorId} -> ({x:F1}, {y:F1}, {z:F1})");
        }

        protected override void OnProjectileEvent(
            int projectileId, 
            int ownerId, 
            ProjectileEventKind kind, 
            int targetId,
            float x, float y, float z,
            float startX, float startY, float startZ)
        {
            switch (kind)
            {
                case ProjectileEventKind.Spawn:
                    _battleView.ShowProjectileSpawn(projectileId, 0, x, y, z);
                    Log.Projectile($"[ConsoleViewEventSink] Spawn: #{projectileId} at ({x:F1}, {y:F1}, {z:F1})");
                    break;

                case ProjectileEventKind.Hit:
                    _battleView.ShowProjectileHit(0, x, y, z);
                    Log.Projectile($"[ConsoleViewEventSink] Hit: #{projectileId} at ({x:F1}, {y:F1}, {z:F1})");
                    break;

                case ProjectileEventKind.Destroy:
                    _battleView.ShowProjectileExpire(projectileId);
                    Log.Projectile($"[ConsoleViewEventSink] Destroy: #{projectileId}");
                    break;
            }
        }

        protected override void OnAreaEvent(int areaId, AreaEventKind kind, float x, float y, float z, float radius)
        {
            switch (kind)
            {
                case AreaEventKind.Appear:
                    _battleView.ShowAreaEffectStart(areaId, 0, x, z, radius);
                    Log.Area($"[ConsoleViewEventSink] AreaStart: #{areaId} Center=({x:F1}, {z:F1}) Radius={radius:F1}");
                    break;

                case AreaEventKind.Disappear:
                    _battleView.ShowAreaEffectEnd(areaId);
                    Log.Area($"[ConsoleViewEventSink] AreaEnd: #{areaId}");
                    break;
            }
        }

        protected override void OnDamageEvent(
            int attackerId,
            int targetId,
            int sourceId,
            int damageType,
            int damageValue,
            int targetHpAfter,
            bool isKill)
        {
            _battleView.ShowFloatingText(targetId, $"-{damageValue}", false);

            // 从 DamageEventData 中获取 targetHpAfter（伤害后的 HP）更新视图
            // 注意：Console 层简化处理，假设所有实体的最大 HP 为 5000
            const float defaultMaxHp = 5000f;
            _battleView.UpdateEntityHp(targetId, targetHpAfter, defaultMaxHp);

            if (isKill)
            {
                _battleView.ShowFloatingText(targetId, "DIED!", false);
            }

            Log.Damage($"[ConsoleViewEventSink] Damage: #{targetId} took {damageValue} from #{attackerId}, HP now: {targetHpAfter}");
        }

        protected override void OnStateHash(int frameIndex, uint stateHash)
        {
            Log.Trace($"[ConsoleViewEventSink] StateHash: Frame#{frameIndex} = {stateHash}");
        }

        #endregion

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            Log.View("[ConsoleBattleViewEventSink] Disposed");
        }
    }
}
