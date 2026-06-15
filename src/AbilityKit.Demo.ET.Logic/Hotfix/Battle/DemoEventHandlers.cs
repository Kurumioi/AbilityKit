using ET.AbilityKit.Demo.ET.Share;

namespace ET.Logic
{
    /// <summary>
    /// Demo 登录完成事件处理
    /// </summary>
    [Event(SceneType.DemoLogin)]
    public class DemoLoginFinish_EventHandler: AEvent<Scene, DemoLoginFinish>
    {
        protected override async ETTask Run(Scene scene, DemoLoginFinish args)
        {
            Log.Info($"[DemoLoginHandler] Login finished for {args.PlayerName}");

            var root = scene.Root();
            var processComponent = root?.GetComponent<DemoProcessComponent>();

            if (processComponent == null)
            {
                Log.Warning($"[DemoLoginHandler] DemoProcessComponent not found!");
            }
            else if (processComponent.LaunchOptions?.AutoEnterBattle == true)
            {
                Log.Info($"[DemoLoginHandler] Auto enter battle enabled for {args.PlayerName}");
                await processComponent.ChangeToBattleScene(args.PlayerId, args.PlayerName);
            }
            else
            {
                Log.Info($"[DemoLoginHandler] Login complete; waiting for explicit enter-battle request");
            }

            await ETTask.CompletedTask;
        }
    }

    /// <summary>
    /// Demo 请求进入战斗事件处理
    /// </summary>
    [Event(SceneType.DemoLogin)]
    public class DemoRequestEnterBattle_EventHandler: AEvent<Scene, DemoRequestEnterBattle>
    {
        protected override async ETTask Run(Scene scene, DemoRequestEnterBattle args)
        {
            Log.Info($"[DemoLoginHandler] Requesting to enter battle for {args.PlayerName}...");

            // 获取根场景的 DemoProcessComponent
            var root = scene.Root();
            var processComponent = root?.GetComponent<DemoProcessComponent>();

            if (processComponent != null)
            {
                await processComponent.ChangeToBattleScene(args.PlayerId, args.PlayerName);
            }
            else
            {
                Log.Warning($"[DemoLoginHandler] DemoProcessComponent not found!");
            }

            await ETTask.CompletedTask;
        }
    }

    /// <summary>
    /// Demo 战斗结束事件处理
    /// </summary>
    [Event(SceneType.DemoBattle)]
    public class DemoBattleEnd_EventHandler: AEvent<Scene, BattleEndEvent>
    {
        protected override async ETTask Run(Scene scene, BattleEndEvent args)
        {
            Log.Info($"[DemoBattleHandler] Battle {args.BattleId} ended with {(args.IsVictory ? "VICTORY" : "DEFEAT")}");

            // 获取根场景的 DemoProcessComponent
            var root = scene.Root();
            var processComponent = root?.GetComponent<DemoProcessComponent>();

            if (processComponent != null)
            {
                Log.Info($"[DemoBattleHandler] Returning to login scene...");
                await processComponent.ChangeToLoginScene();
            }

            await ETTask.CompletedTask;
        }
    }
}
