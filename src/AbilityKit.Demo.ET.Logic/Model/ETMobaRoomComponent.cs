using System;
using System.Collections.Generic;
using AbilityKit.Ability.Host;
using AbilityKit.Ability.Host.Extensions.Moba.Room;
using AbilityKit.Ability.Share.Impl.Moba.Struct;
using PlayerId = AbilityKit.Ability.Host.PlayerId;

namespace ET.Logic
{
    /// <summary>
    /// ET 房间管理器 Component
    /// 封装 host.extension 的 MobaRoomOrchestrator，提供 ET 风格的生命周期管理
    /// 只定义数据，不包含业务逻辑
    /// </summary>
    [ComponentOf(typeof(ETBattleComponent))]
    public class ETMobaRoomComponent : Entity, IAwake, IDestroy
    {
        /// <summary>
        /// 房间状态
        /// </summary>
        public MobaRoomState RoomState { get; set; }

        /// <summary>
        /// 房间编排器（使用 host.extension 的实现）
        /// </summary>
        public MobaRoomOrchestrator RoomOrchestrator { get; set; }

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

        /// <summary>
        /// 调用 OnAllPlayersReady 事件（供 System 调用）
        /// </summary>
        public void InvokeOnAllPlayersReady()
        {
            OnAllPlayersReady?.Invoke();
        }

        public void Awake()
        {
            HasTriggeredBattleStart = false;
        }

        public void Destroy()
        {
            Log.Info("[ETMobaRoom] ETMobaRoomComponent destroyed");
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
    }
}
