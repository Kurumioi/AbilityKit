using System;
using AbilityKit.Demo.Moba.Console.Core.Battle.Context;
using AbilityKit.Demo.Moba.Console.Core.Battle.Features;
using AbilityKit.Demo.Moba.Console.Flow;
using AbilityKit.Demo.Moba.Console.Platform;
using AbilityKit.Demo.Moba.Console.View;

namespace AbilityKit.Demo.Moba.Console.Battle
{
    /// <summary>
    /// HUD 显示配置
    /// </summary>
    public sealed class BattleHudConfig
    {
        public bool ShowEntityList { get; set; } = true;
        public bool ShowMinimap { get; set; } = true;
        public bool ShowDamageNumbers { get; set; } = true;
        public bool ShowSkillCooldowns { get; set; } = true;
        public int UpdateIntervalMs { get; set; } = 100;

        public static BattleHudConfig Default => new();
    }

    /// <summary>
    /// Console HUD 特性模块
    /// 负责渲染战斗 HUD 信息
    /// </summary>
    public sealed class ConsoleHudFeature : ConsoleSubFeatureBase
    {
        public override string Id => "console_hud_feature";
        public override string[] Dependencies => new[] { "console_view_feature" };

        private IConsoleBattleView _battleView;
        private BattleHudConfig _config;

        private int _lastKnownFrame;
        private int _lastKnownActorCount;

        public ConsoleHudFeature()
        {
            _config = BattleHudConfig.Default;
        }

        public void SetConfig(BattleHudConfig config)
        {
            _config = config ?? BattleHudConfig.Default;
        }

        public void SetBattleView(IConsoleBattleView battleView)
        {
            _battleView = battleView;
        }

        public override void OnAttach(ConsoleBattleContext ctx)
        {
            base.OnAttach(ctx);
            _lastKnownFrame = ctx.LastFrame;
            _lastKnownActorCount = ctx.EcsWorld?.AliveCount ?? 0;
            Log.System("[HUD] Attached");
        }

        public override void OnDetach(ConsoleBattleContext ctx)
        {
            _battleView = null;
            Log.System("[HUD] Detached");
        }

        public override void Tick(ConsoleBattleContext ctx, float deltaTime)
        {
            if (Context == null || ctx.State != BattleState.InMatch) return;

            _lastKnownFrame = ctx.LastFrame;
            _lastKnownActorCount = ctx.EcsWorld?.AliveCount ?? _lastKnownActorCount;
        }

        public void RenderHud()
        {
            if (Context == null) return;

            var view = _battleView as ConsoleBattleView;
            if (view == null) return;

            Log.System("");
            Log.System("========================================");
            Log.System($"           BATTLE HUD - Frame {_lastKnownFrame}");
            Log.System("========================================");
            Log.System($"Phase: {Context.State} | LocalActorId: {Context.LocalActorId}");
            Log.System("----------------------------------------");
            Log.System("Commands: WASD/Arrows=Move  J=Skill1  K=Skill2  L=Skill3  SPACE=Stop");
            Log.System("----------------------------------------");
            Log.System($"{"ID",-8} {"Name",-15} {"Type",-10} {"HP",-15} {"Position",-20}");
            Log.System("----------------------------------------");

            foreach (var entity in view.EntityDisplay.GetAll())
            {
                var hpText = entity.MaxHp > 0 ? $"{entity.Hp:F0}/{entity.MaxHp:F0}" : "N/A";
                var posText = $"({entity.X:F1}, {entity.Z:F1})";
                var isLocal = entity.ActorId == Context.LocalActorId ? "*" : " ";
                Log.System($"{isLocal}{entity.ActorId,-7} {entity.Name,-15} {entity.Type,-10} {hpText,-15} {posText,-20}");
            }

            Log.System("========================================");
            Log.System("");
        }

        public void ShowStatus(string message)
        {
            Log.System($"[STATUS] {message}");
        }
    }
}
