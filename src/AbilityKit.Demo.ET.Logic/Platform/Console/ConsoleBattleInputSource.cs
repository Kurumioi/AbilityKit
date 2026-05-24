using System;
using System.Threading.Tasks;
using ET.AbilityKit.Demo.ET.Share;

namespace ET.Logic
{
    /// <summary>
    /// Console Platform 层 - 输入源
    /// 封装所有 Console 特定的输入处理
    /// </summary>
    public static class ConsoleBattleInputSource
    {
        private static bool _isRunning;

        /// <summary>
        /// 运行 Console 输入循环
        /// </summary>
        public static async Task RunInputLoopAsync(
            Scene scene,
            Func<Scene, bool> shouldContinue)
        {
            _isRunning = true;

            var battleComponent = scene.GetComponent<ETBattleComponent>();
            var unitComponent = scene.GetComponent<ETUnitComponent>();
            var inputComponent = scene.GetComponent<ETInputComponent>();

            while (_isRunning && shouldContinue(scene))
            {
                if (Console.KeyAvailable)
                {
                    var key = Console.ReadKey(true);
                    ProcessInput(scene, key, battleComponent, unitComponent, inputComponent);
                }

                await Task.Delay(16); // ~60 FPS
            }
        }

        /// <summary>
        /// 停止输入循环
        /// </summary>
        public static void Stop()
        {
            _isRunning = false;
        }

        /// <summary>
        /// 处理键盘输入
        /// </summary>
        private static void ProcessInput(
            Scene scene,
            ConsoleKeyInfo key,
            ETBattleComponent battleComponent,
            ETUnitComponent unitComponent,
            ETInputComponent inputComponent)
        {
            if (battleComponent == null || unitComponent == null || inputComponent == null)
                return;

            var playerUnit = unitComponent.GetLocalPlayerUnit();
            if (playerUnit == null)
                return;

            float moveStep = 2f;

            switch (key.Key)
            {
                case ConsoleKey.W:
                case ConsoleKey.UpArrow:
                    playerUnit.AddTargetOffset(0, moveStep);
                    inputComponent.SubmitMoveInput(
                        battleComponent.CurrentFrame,
                        playerUnit.GetEntityCode(),
                        playerUnit.GetTargetX(),
                        playerUnit.GetTargetY());
                    break;

                case ConsoleKey.S:
                case ConsoleKey.DownArrow:
                    playerUnit.AddTargetOffset(0, -moveStep);
                    inputComponent.SubmitMoveInput(
                        battleComponent.CurrentFrame,
                        playerUnit.GetEntityCode(),
                        playerUnit.GetTargetX(),
                        playerUnit.GetTargetY());
                    break;

                case ConsoleKey.A:
                case ConsoleKey.LeftArrow:
                    playerUnit.AddTargetOffset(-moveStep, 0);
                    inputComponent.SubmitMoveInput(
                        battleComponent.CurrentFrame,
                        playerUnit.GetEntityCode(),
                        playerUnit.GetTargetX(),
                        playerUnit.GetTargetY());
                    break;

                case ConsoleKey.D:
                case ConsoleKey.RightArrow:
                    playerUnit.AddTargetOffset(moveStep, 0);
                    inputComponent.SubmitMoveInput(
                        battleComponent.CurrentFrame,
                        playerUnit.GetEntityCode(),
                        playerUnit.GetTargetX(),
                        playerUnit.GetTargetY());
                    break;

                case ConsoleKey.D1:
                case ConsoleKey.D2:
                case ConsoleKey.D3:
                case ConsoleKey.D4:
                    int skillSlot = key.Key - ConsoleKey.D1;
                    inputComponent.SubmitSkillInput(
                        battleComponent.CurrentFrame,
                        playerUnit.GetEntityCode(),
                        skillSlot,
                        playerUnit.GetX() + 5f,
                        playerUnit.GetY());
                    break;

                case ConsoleKey.Spacebar:
                    inputComponent.SubmitStopInput(battleComponent.CurrentFrame, playerUnit.GetEntityCode());
                    playerUnit.StopMove();
                    break;

                case ConsoleKey.Q:
                    _isRunning = false;
                    Log.Info("[ConsoleInput] Quit requested");
                    break;
            }
        }
    }
}
