using System;
using AbilityKit.Demo.Moba.Console.Core.Battle.Context;
using AbilityKit.Demo.Moba.Console.Flow;
using AbilityKit.Demo.Moba.Console.Platform;
using AbilityKit.Demo.Moba.Console.View;
using AbilityKit.Demo.Moba.Console.Events;

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
        private IConsoleBattleView _battleView;
        private int _tickCount;
        private DateTime _lastRender;

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

        public void OnAttach(ConsoleBattleContext context)
        {
            _ctx = context ?? throw new ArgumentNullException(nameof(context));
            _tickCount = 0;
            _lastRender = DateTime.Now;
            _lastKnownFrame = context.LastFrame;
            _lastKnownActorCount = context.EcsWorld?.AliveCount ?? 0;

            SubscribeToEvents();
            Log.System("[HUD] Attached (Event-based)");
        }

        public void OnDetach(ConsoleBattleContext context)
        {
            UnsubscribeFromEvents();
            _ctx = null;
            _battleView = null;
            Log.System("[HUD] Detached");
        }

        private void SubscribeToEvents()
        {
            BattleEventBus.Subscribe<FrameSyncEvent>(OnFrameSync);
            BattleEventBus.Subscribe<MoveInputProcessedEvent>(OnMoveInput);
            BattleEventBus.Subscribe<SkillExecutedEvent>(OnSkillExecuted);
            BattleEventBus.Subscribe<EntityCreatedEvent>(OnEntityCreated);
            BattleEventBus.Subscribe<EntityUpdatedEvent>(OnEntityUpdated);
        }

        private void UnsubscribeFromEvents()
        {
            BattleEventBus.Unsubscribe<FrameSyncEvent>(OnFrameSync);
            BattleEventBus.Unsubscribe<MoveInputProcessedEvent>(OnMoveInput);
            BattleEventBus.Unsubscribe<SkillExecutedEvent>(OnSkillExecuted);
            BattleEventBus.Unsubscribe<EntityCreatedEvent>(OnEntityCreated);
            BattleEventBus.Unsubscribe<EntityUpdatedEvent>(OnEntityUpdated);
        }

        private void OnFrameSync(FrameSyncEvent evt)
        {
            _lastKnownFrame = evt.Frame;
            _lastKnownActorCount = evt.ActorCount;
        }

        private void OnMoveInput(MoveInputProcessedEvent evt)
        {
            if (evt.ActorId == _ctx?.LocalActorId)
            {
                Log.Input($"[HUD] Local move: dx={evt.Dx:F2}, dz={evt.Dz:F2}");
            }
        }

        private void OnSkillExecuted(SkillExecutedEvent evt)
        {
            if (evt.ActorId == _ctx?.LocalActorId)
            {
                if (evt.Success)
                {
                    Log.Skill($"[HUD] Local skill{evt.Slot} executed");
                }
                else
                {
                    Log.Skill($"[HUD] Local skill{evt.Slot} failed: {evt.FailReason}");
                }
            }
        }

        private void OnEntityCreated(EntityCreatedEvent evt)
        {
            Log.Entity($"[HUD] Entity created: #{evt.ActorId} {evt.Name}");
        }

        private void OnEntityUpdated(EntityUpdatedEvent evt)
        {
            _lastKnownActorCount = _ctx?.EcsWorld?.AliveCount ?? _lastKnownActorCount;
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
            Log.System($"           BATTLE HUD - Frame {_lastKnownFrame}");
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
