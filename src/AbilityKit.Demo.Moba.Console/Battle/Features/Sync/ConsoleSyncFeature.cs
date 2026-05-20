using System;
using AbilityKit.Demo.Moba.Console.Battle.Context;
using AbilityKit.Demo.Moba.Share;

namespace AbilityKit.Demo.Moba.Console.Battle.Features
{
    /// <summary>
    /// Console 帧同步特性
    /// 实现 IFrameSyncController 接口和 IConsoleSubFeature 接口
    /// </summary>
    public sealed class ConsoleSyncFeature : ConsoleSubFeatureBase, IFrameSyncController
    {
        public override string Id => "console_sync_feature";
        public override string[] Dependencies => new[] { "console_view_feature" };

        private int _syncTickCounter;
        private float _lastMoveUpdateTime;

        public override void OnAttach(ConsoleBattleContext ctx)
        {
            base.OnAttach(ctx);
            Platform.Log.Sync($"[Sync] Attached - SyncMode: {ctx.Plan.SyncMode}");
        }

        public override void Tick(ConsoleBattleContext ctx, float deltaTime)
        {
            if (Context == null) return;

            _syncTickCounter++;

            if (_syncTickCounter % 300 == 0)
            {
                Platform.Log.Sync($"[Sync] Frame: {ctx.LastFrame}, State: {ctx.State}, Actors: {ctx.EcsWorld?.AliveCount ?? 0}");
            }

            bool hasMoveInput = Math.Abs(ctx.HudMoveDx) > 0.01f || Math.Abs(ctx.HudMoveDz) > 0.01f;
            if (hasMoveInput)
            {
                _lastMoveUpdateTime += deltaTime;
                if (_lastMoveUpdateTime >= 1.0f)
                {
                    Platform.Log.Sync($"[Sync] Move - Local:{ctx.LocalActorId} dx:{ctx.HudMoveDx:F2} dz:{ctx.HudMoveDz:F2}");
                    _lastMoveUpdateTime = 0f;
                }
            }
            else
            {
                _lastMoveUpdateTime = 0f;
            }
        }

        #region IFrameSyncController 实现

        bool IFrameSyncController.IsPaused => false;
        int IFrameSyncController.TargetFrame => Context?.LastFrame ?? 0;
        bool IFrameSyncController.IsCatchingUp => false;
        int IFrameSyncController.FrameDelay => 0;

        void IFrameSyncController.Pause() { }
        void IFrameSyncController.Resume() { }

        void IFrameSyncController.AdvanceToFrame(int targetFrame)
        {
            if (Context != null)
            {
                Context.LastFrame = targetFrame;
                Platform.Log.Sync($"[Sync] Advanced to frame {targetFrame}");
            }
        }

        void IFrameSyncController.SetFrameRate(int framesPerSecond)
        {
            Platform.Log.Sync($"[Sync] Frame rate set to {framesPerSecond} FPS (read-only for Console)");
        }

        #endregion
    }
}
