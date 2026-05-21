using System;
using AbilityKit.Demo.Moba.Console.Platform;
using AbilityKit.Demo.Moba.Console.Battle.Flow;
using AbilityKit.Demo.Moba.Console.View;
using AbilityKit.Demo.Moba.Console.Battle.Context;
using BattleConfig = AbilityKit.Demo.Moba.Console.Battle.Config;

namespace AbilityKit.Demo.Moba.Console
{
    public interface IBattleBootstrapper : IDisposable
    {
        IConsoleBattleView BattleView { get; }
        IBattleFlow Flow { get; }
        bool IsRunning { get; }

        BattleConfig.BattleStartPlan Build();

        void Initialize();
        void Start();
        void Stop();
        void Tick(float deltaTime = 0.033f);
        void TransitionTo(string phaseName);
        void SetupBattle();
        void ShowHud();
        void PrintWorldStatus();
    }

    /// <summary>
    /// 战斗启动配置提供者接口
    /// </summary>
    public interface IBattleStartConfigProvider
    {
        /// <summary>
        /// 获取战斗启动配置
        /// </summary>
        BattleConfig.BattleStartConfig Config { get; }

        /// <summary>
        /// 构建战斗启动计划
        /// </summary>
        BattleConfig.BattleStartPlan BuildPlan();
    }
}
