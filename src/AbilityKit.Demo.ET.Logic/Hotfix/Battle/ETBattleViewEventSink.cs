using System;
using AbilityKit.Demo.Moba.Share;
using ET.AbilityKit.Demo.ET.Share;

namespace ET.Logic
{
    /// <summary>
    /// ET Battle View Event Sink
    /// Bridges IBattleViewEventSink events to ET event system
    /// Also updates the entity cache component for ET.View queries
    ///
    /// Design:
    /// - Uses virtual methods to allow extension in subclasses
    /// - Empty stubs indicate reserved functionality for future use
    /// </summary>
    public class ETBattleViewEventSink : IBattleViewEventSink
    {
        private readonly ETBattleComponent _battleComponent;
        private ETBattleEntityCacheComponent _cacheComponent;
        private ETViewSnapshotProvider _snapshotProvider;

        public ETBattleViewEventSink(ETBattleComponent battleComponent)
        {
            _battleComponent = battleComponent ?? throw new ArgumentNullException(nameof(battleComponent));
        }

        #region Cache Setup

        /// <summary>
        /// 初始化缓存组件
        /// 由 ETBattleComponentSystem 在初始化时调用
        /// </summary>
        public void InitializeCache(ETBattleEntityCacheComponent cacheComponent)
        {
            _cacheComponent = cacheComponent;
            _snapshotProvider = new ETViewSnapshotProvider(cacheComponent);
        }

        /// <summary>
        /// 获取快照提供器
        /// </summary>
        public IETViewSnapshotProvider GetSnapshotProvider() => _snapshotProvider;

        #endregion

        #region Unit Events

        public void OnEnterGameSnapshot(in FrameSnapshotData snapshot)
        {
            var scene = _battleComponent.Scene();
            if (scene == null)
                return;

            // 更新缓存
            if (_cacheComponent != null)
            {
                _cacheComponent.UpdateCache(snapshot.FrameIndex, snapshot);
            }

            // 初始化自动测试组件
            InitializeAutoTestComponent(scene, snapshot);
        }

        /// <summary>
        /// 初始化自动测试组件
        /// </summary>
        private void InitializeAutoTestComponent(Scene scene, in FrameSnapshotData snapshot)
        {
            var autoTest = scene.GetComponent<ETBattleAutoTestComponent>();
            var skillTest = scene.GetComponent<ETBattleSkillTestComponent>();
            if (autoTest == null && skillTest == null)
                return;

            int actorIdToUse = 0;
            int entityCodeToUse = 0;
            string playerIdToUse = null;
            float startX = 0f;
            float startY = 0f;

            // 优先使用 ActorSpawns 中的数据
            if (snapshot.ActorSpawns != null && snapshot.ActorSpawns.Count > 0)
            {
                var firstSpawn = snapshot.ActorSpawns[0];
                actorIdToUse = firstSpawn.ActorId;
                entityCodeToUse = firstSpawn.EntityCode;
                playerIdToUse = firstSpawn.PlayerId;
                startX = firstSpawn.PositionX;
                startY = firstSpawn.PositionY;
            }
            else if (_battleComponent.PlayerActorId > 0)
            {
                actorIdToUse = (int)_battleComponent.PlayerActorId;
                playerIdToUse = actorIdToUse.ToString();
            }

            if (autoTest != null && playerIdToUse != null)
            {
                autoTest.Initialize(actorIdToUse, playerIdToUse, startX, startY);
            }

            if (skillTest != null && playerIdToUse != null)
            {
                skillTest.Initialize(actorIdToUse, playerIdToUse, 0);
            }
        }

        public void OnActorTransformSnapshot(in FrameSnapshotData snapshot)
        {
            var scene = _battleComponent.Scene();
            if (scene == null)
                return;

            // 更新缓存
            if (_cacheComponent != null)
            {
                _cacheComponent.UpdateCache(snapshot.FrameIndex, snapshot);
            }

            // 发布 ActorMoveEvent 事件
            if (snapshot.ActorTransforms != null)
            {
                foreach (var transform in snapshot.ActorTransforms)
                {
                    EventSystem.Instance.Publish<Scene, ActorMoveEvent>(
                        scene,
                        new ActorMoveEvent
                        {
                            ActorId = transform.ActorId,
                            X = transform.PositionX,
                            Y = transform.PositionY,
                            Z = transform.PositionZ,
                            Rotation = transform.RotationY
                        });
                }
            }
        }

        public void OnDamageEventSnapshot(in FrameSnapshotData snapshot)
        {
            var scene = _battleComponent.Scene();
            if (scene == null)
                return;

            // 更新缓存
            if (_cacheComponent != null)
            {
                _cacheComponent.UpdateCache(snapshot.FrameIndex, snapshot);
            }

            // 发布伤害和死亡事件
            foreach (var damage in snapshot.DamageEvents)
            {
                EventSystem.Instance.Publish<Scene, ActorDamageEvent>(
                    scene,
                    new ActorDamageEvent
                    {
                        ActorId = damage.TargetId,
                        SourceActorId = damage.AttackerId,
                        Damage = damage.DamageValue,
                        CurrentHp = damage.TargetHpAfter,
                        MaxHp = 100f
                    });

                if (damage.IsKill)
                {
                    EventSystem.Instance.Publish<Scene, ActorDeadEvent>(
                        scene,
                        new ActorDeadEvent
                        {
                            ActorId = damage.TargetId,
                            KillerId = damage.AttackerId
                        });
                }

                Log.Debug($"[ETBattleViewEventSink] Damage: {damage.AttackerId} -> {damage.TargetId}, dmg={damage.DamageValue}, kill={damage.IsKill}");
            }
        }

        #endregion

        #region Battle Events

        public void OnBattleStart(int frameIndex)
        {
            _battleComponent.ViewSink?.OnBattleStart(new BattleStartEvent()
            {
                BattleId = _battleComponent.BattleId,
                PlayerId = _battleComponent.PlayerId
            });
        }

        public void OnBattleEnd(int frameIndex, int winTeamId)
        {
            bool isVictory = winTeamId == 1;
            _battleComponent.ViewSink?.OnBattleEnd(new BattleEndEvent()
            {
                BattleId = _battleComponent.BattleId,
                IsVictory = isVictory
            });
        }

        public void OnFrameSyncComplete(int frameIndex)
        {
            // 此方法已被 OnActorTransformSnapshot 替代
        }

        #endregion

        #region Extended Events (Reserved)

        /// <summary>
        /// 投射物事件快照（预留，暂未实现）
        /// </summary>
        public virtual void OnProjectileEventSnapshot(in FrameSnapshotData snapshot)
        {
            // Reserved for future projectile rendering support
        }

        /// <summary>
        /// 范围事件快照（预留，暂未实现）
        /// </summary>
        public virtual void OnAreaEventSnapshot(in FrameSnapshotData snapshot)
        {
            // Reserved for future area effect rendering support
        }

        /// <summary>
        /// 状态哈希快照（预留，暂未实现）
        /// </summary>
        public virtual void OnStateHashSnapshot(in FrameSnapshotData snapshot)
        {
            // Reserved for future state hash verification support
        }

        public virtual void OnTriggerEvent(in TriggerEventData evt)
        {
        }

        #endregion
    }
}
