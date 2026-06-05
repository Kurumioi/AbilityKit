using System;
using System.Collections.Generic;
using AbilityKit.Ability.Host;
using AbilityKit.Ability.Host.Extensions.Moba.Room;
using PlayerId = AbilityKit.Ability.Host.PlayerId;

namespace ET.Logic
{
    /// <summary>
    /// ET 房间管理器 Component
    /// 封装 host.extension 的 MobaRoomOrchestrator，提供 ET 风格的生命周期管理
    ///
    /// 职责：
    /// 1. 管理房间状态（MobaRoomState）
    /// 2. 处理房间命令（Join, Leave, SetReady, PickHero）
    /// 3. 监听房间状态变化事件
    /// 4. 当所有玩家准备完成后，触发战斗初始化
    ///
    /// 设计原则：
    /// - Component 只存数据和引用，不包含业务逻辑
    /// - 业务逻辑由 ETMobaRoomSystem 处理
    /// </summary>
    [ComponentOf(typeof(ETBattleComponent))]
    public class ETMobaRoomComponent : Entity, IAwake, IDestroy
    {
        /// <summary>
        /// 房间状态
        /// </summary>
        public MobaRoomState RoomState { get; private set; }

        /// <summary>
        /// 房间编排器（使用 host.extension 的实现）
        /// </summary>
        public MobaRoomOrchestrator RoomOrchestrator { get; private set; }

        /// <summary>
        /// 当前玩家ID
        /// </summary>
        public PlayerId LocalPlayerId { get; set; }

        /// <summary>
        /// 是否已触发战斗开始
        /// </summary>
        public bool HasTriggeredBattleStart { get; set; }

        /// <summary>
        /// 当所有玩家准备好时触发
        /// </summary>
        public event Action OnAllPlayersReady;

        public void Awake()
        {
            HasTriggeredBattleStart = false;
        }

        public void Destroy()
        {
            if (RoomOrchestrator != null)
            {
                RoomOrchestrator.RemoveChanged(OnRoomChanged);
            }
            Log.Info("[ETMobaRoom] ETMobaRoomComponent destroyed");
        }

        /// <summary>
        /// 初始化房间
        /// </summary>
        public void InitializeRoom(string matchId, int mapId, int maxPlayers, int tickRate, int localPlayerId)
        {
            var randomSeed = Environment.TickCount;
            RoomState = new MobaRoomState(matchId, mapId, randomSeed, tickRate, inputDelayFrames: 2);
            RoomState.Configure(minPlayers: 1, maxPlayers: maxPlayers);

            RoomOrchestrator = new MobaRoomOrchestrator(RoomState);
            RoomOrchestrator.AddChanged(OnRoomChanged);

            LocalPlayerId = new PlayerId(localPlayerId.ToString());

            Log.Info($"[ETMobaRoom] Initialized room: MatchId={matchId}, MapId={mapId}, MaxPlayers={maxPlayers}, LocalPlayer={LocalPlayerId.Value}");
        }

        /// <summary>
        /// 处理房间状态变化
        /// </summary>
        private void OnRoomChanged(MobaRoomChangedArgs args)
        {
            Log.Info($"[ETMobaRoom] Room changed: Kind={args.Kind}, PlayerId={args.PlayerId.Value}, Revision={args.Revision}");

            // 检查是否可以开始战斗
            CheckAndTriggerBattleStart();
        }

        /// <summary>
        /// 检查并触发战斗开始
        /// </summary>
        public void CheckAndTriggerBattleStart()
        {
            if (HasTriggeredBattleStart)
                return;

            if (RoomState == null || !RoomState.CanStart())
            {
                Log.Info($"[ETMobaRoom] Cannot start battle yet: CanStart={RoomState?.CanStart()}");
                return;
            }

            HasTriggeredBattleStart = true;
            Log.Info($"[ETMobaRoom] All players ready! Triggering battle start...");

            // 触发事件
            OnAllPlayersReady?.Invoke();
        }

        /// <summary>
        /// 调用 OnAllPlayersReady 事件（供 System 调用）
        /// </summary>
        public void InvokeOnAllPlayersReady()
        {
            OnAllPlayersReady?.Invoke();
        }

        /// <summary>
        /// Auto setup for local test
        /// </summary>
        public void AutoSetupForLocalTest(int heroId, int attributeTemplateId)
        {
            if (RoomOrchestrator == null)
                return;

            var localPlayerId = int.Parse(LocalPlayerId.Value);
            var enemyPlayerId = localPlayerId == 2 ? 3 : 2;

            // Join local player
            JoinRoom(localPlayerId, teamId: 1);

            // Pick local hero
            PickHero(localPlayerId, heroId, attributeTemplateId, basicAttackSkillId: ETPlayerSpawnData.DefaultBasicAttackSkillId, skillIds: ETPlayerSpawnData.CloneDefaultSkillIds());

            // Add a deterministic opponent so smoke can verify formal target-driven event snapshots.
            JoinRoom(enemyPlayerId, teamId: 2);
            PickHero(enemyPlayerId, heroId, attributeTemplateId, basicAttackSkillId: ETPlayerSpawnData.DefaultBasicAttackSkillId, skillIds: ETPlayerSpawnData.CloneDefaultSkillIds());
            SetPlayerReady(enemyPlayerId, ready: true);

            // Set local ready last. The room can start as soon as min player rules are satisfied.
            SetPlayerReady(localPlayerId, ready: true);

            Log.Info($"[ETMobaRoom] AutoSetupForLocalTest: HeroId={heroId}, AttrTemplateId={attributeTemplateId}, LocalPlayer={localPlayerId}, EnemyPlayer={enemyPlayerId}");
        }

        /// <summary>
        /// 获取当前玩家数
        /// </summary>
        public int PlayerCount => RoomState?.Players.Count ?? 0;

        /// <summary>
        /// 获取最大玩家数
        /// </summary>
        public int MaxPlayerCount => RoomState?.MaxPlayers ?? 0;

        /// <summary>
        /// 是否可以开始战斗
        /// </summary>
        public bool CanStartBattle => RoomState?.CanStart() ?? false;

        #region 房间命令（由 System 调用）

        /// <summary>
        /// 玩家加入房间
        /// </summary>
        public bool JoinRoom(int playerId, int teamId = 0)
        {
            var pid = new PlayerId(playerId.ToString());
            var result = RoomOrchestrator.TryJoin(pid, teamId);
            Log.Info($"[ETMobaRoom] JoinRoom: PlayerId={playerId}, TeamId={teamId}, Result={result}");
            return result;
        }

        /// <summary>
        /// 玩家离开房间
        /// </summary>
        public bool LeaveRoom(int playerId)
        {
            var pid = new PlayerId(playerId.ToString());
            var result = RoomOrchestrator.TryLeave(pid);
            Log.Info($"[ETMobaRoom] LeaveRoom: PlayerId={playerId}, Result={result}");
            return result;
        }

        /// <summary>
        /// 设置玩家准备状态
        /// </summary>
        public bool SetPlayerReady(int playerId, bool ready)
        {
            var pid = new PlayerId(playerId.ToString());
            var result = RoomOrchestrator.TrySetReady(pid, ready);
            Log.Info($"[ETMobaRoom] SetPlayerReady: PlayerId={playerId}, Ready={ready}, Result={result}");
            return result;
        }

        /// <summary>
        /// 玩家选择英雄
        /// </summary>
        public bool PickHero(int playerId, int heroId, int attributeTemplateId = 0, int level = 1, int basicAttackSkillId = 0, int[] skillIds = null)
        {
            var pid = new PlayerId(playerId.ToString());
            var result = RoomOrchestrator.TryPickHero(pid, heroId, attributeTemplateId, level, basicAttackSkillId, skillIds);
            Log.Info($"[ETMobaRoom] PickHero: PlayerId={playerId}, HeroId={heroId}, Result={result}");
            return result;
        }

        /// <summary>
        /// 获取房间中的玩家列表
        /// </summary>
        public MobaRoomPlayerSnapshot[] GetPlayers()
        {
            var snapshot = RoomOrchestrator.Snapshot;
            return snapshot.Players;
        }

        /// <summary>
        /// 获取房间快照
        /// </summary>
        public MobaRoomSnapshot GetSnapshot()
        {
            return RoomOrchestrator.Snapshot;
        }

        #endregion
    }
}
