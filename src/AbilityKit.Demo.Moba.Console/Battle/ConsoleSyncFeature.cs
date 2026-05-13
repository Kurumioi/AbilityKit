using System;
using AbilityKit.Demo.Moba.Console.Battle;
using AbilityKit.Demo.Moba.Console.Flow;

namespace AbilityKit.Demo.Moba.Console.Battle
{
    /// <summary>
    /// 同步模块
    /// </summary>
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

            // 每30帧输出一次状态更新（1秒@30FPS）
            if (_syncTickCounter % 30 == 0)
            {
                Platform.Log.Sync($"[Sync] Frame: {_ctx.LastFrame}, State: {_ctx.State}, Actors: {_ctx.EcsWorld?.AliveCount ?? 0}");
            }

            // 处理移动同步
            if (_ctx.HudHasMove && Math.Abs(_ctx.HudMoveDx) > 0.01f || Math.Abs(_ctx.HudMoveDz) > 0.01f)
            {
                _lastMoveUpdateTime += deltaTime;
                if (_lastMoveUpdateTime >= 0.1f) // 每100ms更新一次位置
                {
                    Platform.Log.Sync($"[Sync] Move update - Local:{_ctx.LocalActorId} dx:{_ctx.HudMoveDx:F2} dz:{_ctx.HudMoveDz:F2}");
                    _lastMoveUpdateTime = 0f;
                }
            }
        }
    }
}
