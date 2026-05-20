using System;

namespace AbilityKit.Demo.Moba.Console.Battle.Flow
{
    /// <summary>
    /// 战斗流程接口
    /// </summary>
    public interface IBattleFlow : IModuleContext
    {
        /// <summary>
        /// 启动流程
        /// </summary>
        void Start();

        /// <summary>
        /// 停止流程
        /// </summary>
        void Stop();

        /// <summary>
        /// Tick 流程
        /// </summary>
        void Tick(float deltaTime);

        /// <summary>
        /// 进入战斗
        /// </summary>
        void EnterBattle();

        /// <summary>
        /// 返回大厅
        /// </summary>
        void ReturnToLobby();

        /// <summary>
        /// 切换阶段
        /// </summary>
        void TransitionTo(string phaseName);

        /// <summary>
        /// 获取当前阶段
        /// </summary>
        string CurrentPhase { get; }

        /// <summary>
        /// 是否正在运行
        /// </summary>
        bool IsRunning { get; }

        /// <summary>
        /// 事件
        /// </summary>
        IBattleFlowEvents Events { get; }
    }

    /// <summary>
    /// 战斗流程事件接口
    /// </summary>
    public interface IBattleFlowEvents
    {
        /// <summary>
        /// 阶段进入事件
        /// </summary>
        Action<string> PhaseEntered { get; set; }

        /// <summary>
        /// 阶段退出事�?
        /// </summary>
        Action<string> PhaseExited { get; set; }

        /// <summary>
        /// 战斗开始事�?
        /// </summary>
        Action BattleStarted { get; set; }

        /// <summary>
        /// 战斗结束事件
        /// </summary>
        Action BattleEnded { get; set; }
    }

    /// <summary>
    /// 战斗流程事件
    /// </summary>
    public sealed class BattleFlowEvents : IBattleFlowEvents
    {
        public Action<string> PhaseEntered { get; set; }
        public Action<string> PhaseExited { get; set; }
        public Action BattleStarted { get; set; }
        public Action BattleEnded { get; set; }

        public void OnPhaseEntered(string phaseName) => PhaseEntered?.Invoke(phaseName);
        public void OnPhaseExited(string phaseName) => PhaseExited?.Invoke(phaseName);
        public void OnBattleStarted() => BattleStarted?.Invoke();
        public void OnBattleEnded() => BattleEnded?.Invoke();
    }
}
