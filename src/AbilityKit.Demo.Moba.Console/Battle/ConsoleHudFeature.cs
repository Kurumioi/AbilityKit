using System;
using System.Text;
using AbilityKit.Demo.Moba.Console.Battle;
using AbilityKit.Demo.Moba.Console.Flow;
using AbilityKit.Demo.Moba.Console.Platform;
using AbilityKit.Demo.Moba.Console.View;

namespace AbilityKit.Demo.Moba.Console.Battle
{
    public sealed class BattleHudConfig
    {
        public bool ShowEntityList { get; set; } = true;
        public bool ShowMinimap { get; set; } = true;
        public bool ShowDamageNumbers { get; set; } = true;
        public bool ShowSkillCooldowns { get; set; } = true;
        public int UpdateIntervalMs { get; set; } = 100;
        public static BattleHudConfig Default => new();
    }

    public sealed class ConsoleHudFeature : IGameModule<ConsoleBattleContext>, IGameModuleTick<ConsoleBattleContext>
    {
        private ConsoleBattleContext _ctx;
        private BattleHudConfig _config;
        private ConsoleInputFeature _inputFeature;
        private ConsoleSyncFeature _syncFeature;
        private IConsoleBattleView _battleView;
        private int _tickCount;
        private DateTime _lastRender;

        public ConsoleHudFeature()
        {
            _config = BattleHudConfig.Default;
        }

        public void SetConfig(BattleHudConfig config)
        {
            _config = config ?? BattleHudConfig.Default;
        }

        public void SetInputFeature(ConsoleInputFeature inputFeature)
        {
            _inputFeature = inputFeature;
        }

        public void SetSyncFeature(ConsoleSyncFeature syncFeature)
        {
            _syncFeature = syncFeature;
        }

        public void SetBattleView(IConsoleBattleView battleView)
        {
            _battleView = battleView;
        }

        public void OnAttach(ConsoleBattleContext context)
        {
            _ctx = context ?? throw new ArgumentNullException(nameof(context));
            _tickCount = 0;
            _lastRender = DateTime.Now;
            Log.System("[HUD] Attached");
        }

        public void OnDetach(ConsoleBattleContext context)
        {
            _ctx = null;
            _inputFeature = null;
            _syncFeature = null;
            _battleView = null;
            Log.System("[HUD] Detached");
        }

        public void Tick(ConsoleBattleContext context, float deltaTime)
        {
            if (_ctx == null || _ctx.State != BattleState.InMatch) return;
            _tickCount++;
        }

        public void RenderHud()
        {
            if (_ctx == null) return;

            var view = _battleView as ConsoleBattleView;
            if (view == null) return;

            Log.System("");
            Log.System("========================================");
            Log.System($"           BATTLE HUD - Frame {_ctx.LastFrame}");
            Log.System("========================================");
            Log.System($"Phase: {_ctx.State} | LocalActorId: {_ctx.LocalActorId}");
            Log.System("----------------------------------------");
            Log.System("Commands: WASD/Arrows=Move  J=Skill1  K=Skill2  L=Skill3  SPACE=Stop");
            Log.System("----------------------------------------");
            Log.System($"{"ID",-8} {"Name",-15} {"Type",-10} {"HP",-15} {"Position",-20}");
            Log.System("----------------------------------------");

            foreach (var entity in view.EntityDisplay.GetAll())
            {
                var hpText = entity.MaxHp > 0 ? $"{entity.Hp:F0}/{entity.MaxHp:F0}" : "N/A";
                var posText = $"({entity.X:F1}, {entity.Z:F1})";
                var isLocal = entity.ActorId == _ctx.LocalActorId ? "*" : " ";
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
