using System;
using System.Collections.Generic;
using AbilityKit.Demo.Moba.Share;
using AbilityKit.Demo.Moba.Console.Platform;

namespace AbilityKit.Demo.Moba.Console.View
{
    /// <summary>
    /// Console 版本的战斗视图事件接收器
    /// 实现 Share 层的 BaseBattleViewEventSink 接口
    /// </summary>
    public sealed class ConsoleBattleViewEventSink : BaseBattleViewEventSink
    {
        private readonly IConsoleBattleView _battleView;
        private readonly Dictionary<int, float> _maxHpCache = new();
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
            Log.View($"[ViewEventSink] EnterGame: LocalPlayer#{data.LocalPlayerId}, {playerCount} players");
        }

        protected override void OnActorSpawn(in ActorSpawnData data)
        {
            _maxHpCache[data.ActorId] = data.MaxHp;
            _battleView.RegisterEntity(
                data.ActorId, data.Name, "Character",
                data.Hp, data.MaxHp,
                data.PositionX, data.PositionY, data.PositionZ);

            Log.View($"[ViewEventSink] ActorSpawn: #{data.ActorId} ({data.Name}), HP:{data.Hp:F0}/{data.MaxHp:F0}");
        }

        protected override void OnActorTransform(int actorId, float x, float y, float z, float rotationY, float scale)
        {
            _battleView.UpdateActorPosition(actorId, x, y, z);
        }

        protected override void OnProjectileEvent(
            int projectileId, int ownerId, ProjectileEventKind kind,
            int targetId, float x, float y, float z,
            float startX, float startY, float startZ)
        {
            switch (kind)
            {
                case ProjectileEventKind.Spawn:
                    _battleView.ShowProjectileSpawn(projectileId, 0, x, y, z);
                    break;
                case ProjectileEventKind.Hit:
                    _battleView.ShowProjectileHit(0, x, y, z);
                    break;
                case ProjectileEventKind.Destroy:
                    _battleView.ShowProjectileExpire(projectileId);
                    break;
            }
        }

        protected override void OnAreaEvent(int areaId, AreaEventKind kind, float x, float y, float z, float radius)
        {
            switch (kind)
            {
                case AreaEventKind.Appear:
                    _battleView.ShowAreaEffectStart(areaId, 0, x, z, radius);
                    break;
                case AreaEventKind.Disappear:
                    _battleView.ShowAreaEffectEnd(areaId);
                    break;
            }
        }

        protected override void OnDamageEvent(
            int attackerId, int targetId, int sourceId,
            int damageType, int damageValue, int targetHpAfter, bool isKill)
        {
            float maxHp = _maxHpCache.TryGetValue(targetId, out var cached) ? cached : 5000f;

            _battleView.ShowFloatingText(targetId, $"-{damageValue}", false);
            _battleView.UpdateEntityHp(targetId, targetHpAfter, maxHp);

            if (isKill)
            {
                _battleView.ShowFloatingText(targetId, "DIED!", false);
            }

            Log.Damage($"[ViewEventSink] Damage: #{targetId} took {damageValue} from #{attackerId}, HP: {targetHpAfter}/{maxHp}");
        }

        protected override void OnStateHash(int frameIndex, uint stateHash)
        {
            Log.Trace($"[ViewEventSink] StateHash: Frame#{frameIndex} = {stateHash}");
        }

        #endregion

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _maxHpCache.Clear();
        }
    }
}
