using System;
using AbilityKit.Demo.Moba.Console.Platform;
using AbilityKit.Demo.Moba.Console.Flow;
using AbilityKit.Demo.Moba.Console.View;
using AbilityKit.Demo.Moba.Console.Core.Battle.Context;
using AbilityKit.Demo.Moba.Console.Battle;

namespace AbilityKit.Demo.Moba.Console
{
    public interface IBattleBootstrapper : IDisposable
    {
        IConsoleBattleView BattleView { get; }
        IBattleFlow Flow { get; }
        bool IsRunning { get; }

        BattleStartPlan Build();

        void Initialize();
        void Start();
        void Stop();
        void Tick(float deltaTime = 0.033f);
        void TransitionTo(string phaseName);
        void SetupBattle();
        void ShowHud();
        void PrintWorldStatus();

        /// <summary>
        /// 注册演示实体（用于自动测试）
        /// </summary>
        void RegisterDemoEntities();
    }

    public interface IBattleStartConfigProvider
    {
        BattleStartConfig Config { get; }
        BattleStartPlan BuildPlan();
    }
}
