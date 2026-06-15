using System;
using AbilityKit.Ability.Host;
using AbilityKit.Ability.Host.Extensions.Moba.Room;
using PlayerId = AbilityKit.Ability.Host.PlayerId;

namespace ET.Logic
{
    /// <summary>
    /// ET 房间组件只负责持有正式 MobaRoomOrchestrator，并把房间变化接入 ET 生命周期。
    /// </summary>
    [ComponentOf(typeof(ETBattleComponent))]
    public class ETMobaRoomComponent : Entity, IAwake, IDestroy
    {
        public MobaRoomState? RoomState => RoomOrchestrator?.State;

        public MobaRoomOrchestrator RoomOrchestrator { get; private set; } = null!;

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
        public event Action OnAllPlayersReady = delegate { };

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

        public void InitializeRoom(string matchId, int mapId, int maxPlayers, int tickRate, int localPlayerId, int randomSeed, int inputDelayFrames = 2, int minPlayers = 1)
        {
            var roomState = new MobaRoomState(matchId, mapId, randomSeed, tickRate, inputDelayFrames);
            roomState.Configure(minPlayers: minPlayers, maxPlayers: maxPlayers);

            RoomOrchestrator = new MobaRoomOrchestrator(roomState);
            RoomOrchestrator.AddChanged(OnRoomChanged);

            LocalPlayerId = new PlayerId(localPlayerId.ToString());

            Log.Info($"[ETMobaRoom] Initialized room: MatchId={matchId}, MapId={mapId}, MaxPlayers={maxPlayers}, LocalPlayer={LocalPlayerId.Value}");
        }

        private void OnRoomChanged(MobaRoomChangedArgs args)
        {
            Log.Info($"[ETMobaRoom] Room changed: Kind={args.Kind}, PlayerId={args.PlayerId.Value}, Revision={args.Revision}");
            CheckAndTriggerBattleStart();
        }

        public void CheckAndTriggerBattleStart()
        {
            if (HasTriggeredBattleStart)
                return;

            if (!CanStartBattle)
            {
                Log.Info($"[ETMobaRoom] Cannot start battle yet: CanStart={CanStartBattle}");
                return;
            }

            HasTriggeredBattleStart = true;
            Log.Info($"[ETMobaRoom] All players ready! Triggering battle start...");

            OnAllPlayersReady?.Invoke();
        }

        public int PlayerCount => RoomOrchestrator?.State.Players.Count ?? 0;

        public int MaxPlayerCount => RoomOrchestrator?.State.MaxPlayers ?? 0;

        public bool CanStartBattle => RoomOrchestrator?.State.CanStart() ?? false;

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
        public bool PickHero(int playerId, int heroId, int attributeTemplateId, int level, int basicAttackSkillId, int[] skillIds)
        {
            var pid = new PlayerId(playerId.ToString());
            var result = RoomOrchestrator.TryPickHero(pid, heroId, attributeTemplateId, level, basicAttackSkillId, skillIds);
            Log.Info($"[ETMobaRoom] PickHero: PlayerId={playerId}, HeroId={heroId}, Result={result}");
            return result;
        }

        public MobaRoomPlayerSnapshot[] GetPlayers()
        {
            return RoomOrchestrator?.Snapshot.Players ?? Array.Empty<MobaRoomPlayerSnapshot>();
        }

        public MobaRoomSnapshot GetSnapshot()
        {
            return RoomOrchestrator?.Snapshot ?? default;
        }
    }
}
