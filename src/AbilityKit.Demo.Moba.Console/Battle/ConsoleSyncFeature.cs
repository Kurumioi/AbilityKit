using System;
using AbilityKit.Demo.Moba.Console.Core.Battle.Context;
using AbilityKit.Demo.Moba.Console.Flow;
using AbilityKit.Demo.Moba.Console.Events;

namespace AbilityKit.Demo.Moba.Console.Battle
{
    public sealed class ConsoleSyncFeature : IGameModule<ConsoleBattleContext>, IGameModuleTick<ConsoleBattleContext>
    {
        private ConsoleBattleContext _ctx;
        private bool _initialized;
        private int _syncTickCounter;
        private float _lastMoveUpdateTime;

        public void OnAttach(ConsoleBattleContext context)
        {
            _ctx = context ?? throw new ArgumentNullException(nameof(context));
            _initialized = true;
            _syncTickCounter = 0;
            Platform.Log.Sync($"[Sync] Attached - SyncMode: {_ctx.Plan.SyncMode}");
        }

        public void OnDetach(ConsoleBattleContext context)
        {
            _ctx = null;
            _initialized = false;
            Platform.Log.Sync("[Sync] Detached");
        }

        public void Tick(ConsoleBattleContext context, float deltaTime)
        {
            if (!_initialized || _ctx == null) return;

            _syncTickCounter++;

            // 只在每300帧输出一次状态（约10秒@30FPS），避免刷屏
            if (_syncTickCounter % 300 == 0)
            {
                Platform.Log.Sync($"[Sync] Frame: {_ctx.LastFrame}, State: {_ctx.State}, Actors: {_ctx.EcsWorld?.AliveCount ?? 0}");
            }

            // 发布帧同步事件（始终发布，供其他模块使用）
            BattleEventBus.Publish(new FrameSyncEvent
            {
                Frame = _ctx.LastFrame,
                ActorCount = _ctx.EcsWorld?.AliveCount ?? 0,
                LogicTimeSeconds = _ctx.LogicTimeSeconds
            });

            // 处理移动同步（只在有移动输入时记录，且限制频率）
            if (Math.Abs(_ctx.HudMoveDx) > 0.01f || Math.Abs(_ctx.HudMoveDz) > 0.01f)
            {
                _lastMoveUpdateTime += deltaTime;
                if (_lastMoveUpdateTime >= 1.0f) // 降低到每1秒更新一次
                {
                    Platform.Log.Sync($"[Sync] Move - Local:{_ctx.LocalActorId} dx:{_ctx.HudMoveDx:F2} dz:{_ctx.HudMoveDz:F2}");
                    _lastMoveUpdateTime = 0f;
                }
            }
        }
    }
}
