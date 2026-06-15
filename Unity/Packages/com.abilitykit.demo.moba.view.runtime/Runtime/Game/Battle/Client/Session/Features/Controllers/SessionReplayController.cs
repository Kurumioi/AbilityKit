using System;
using System.IO;
using AbilityKit.Ability.Host;
using AbilityKit.Core.Logging;
using AbilityKit.Core.Recording.Lockstep;
using AbilityKit.Game.Battle.Component;
using AbilityKit.Game.Flow.Battle.Replay;
using AbilityKit.World.ECS;

namespace AbilityKit.Game.Flow
{
    internal interface ISessionReplayHost
    {
        void StartSession();
        void StopSession();
        void ApplyAutoPlanActions();

        float GetFixedDeltaSeconds();
    }

    internal interface IBattleReplayDriverProvider
    {
        bool TryCreate(in BattleStartPlan plan, out LockstepReplayDriver driver);
    }

    internal sealed partial class SessionReplayController
    {
        private const int StateHashRecordIntervalFrames = 10;
        private const int ReplaySeekChunkFrames = 300;
        private const int RollbackSeekProbeFrames = 120;

        public void PreTick(BattleStartPlan plan, BattleSessionState state, BattleSessionHandles handles, BattleContext ctx, ISessionReplayHost host)
        {
            if (state == null || handles == null || ctx == null || host == null) return;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            HandleReplayDebugInput(plan, state, handles, ctx, host);
#endif
        }

        public void SetupReplayOrRecord(IBattleReplayDriverProvider provider, BattleStartPlan plan, BattleSessionHandles handles, BattleContext ctx)
        {
            if (handles == null) return;

            BattleRecordCodecBootstrap.TryInstallMemoryPack();

            var runMode = plan.RunMode;
            if (runMode == BattleStartConfig.BattleRunMode.Replay)
            {
                SetupReplayDriver(provider, plan, handles);
            }

            if (runMode == BattleStartConfig.BattleRunMode.Record)
            {
                SetupRecordWriter(plan, ctx);
            }
        }

        public void OnFrameReceived(BattleStartPlan plan, BattleSessionState state, BattleSessionHandles handles, BattleContext ctx, FramePacket packet)
        {
            if (state == null || handles == null || ctx == null) return;

            ValidateReplayStateHash(handles, ctx);
            RecordFrameIfNeeded(plan, state, ctx, packet);
        }

        private static void SetupReplayDriver(IBattleReplayDriverProvider provider, BattleStartPlan plan, BattleSessionHandles handles)
        {
            provider ??= new DefaultBattleReplayDriverProvider();
            if (provider.TryCreate(in plan, out var injected) && injected != null)
            {
                handles.Replay.Driver = injected;
                return;
            }

            if (string.IsNullOrEmpty(plan.InputReplayPath))
            {
                Log.Error("[BattleReplay] Replay startup failed: InputReplayPath is empty. Select a replay file in RunMode settings.");
                return;
            }

            Log.Error($"[BattleReplay] Replay startup failed: unable to create replay driver, path={plan.InputReplayPath}");
        }

        private static void SetupRecordWriter(BattleStartPlan plan, BattleContext ctx)
        {
            if (ctx == null) return;

            ctx.InputRecordWriter?.Dispose();

            var outPath = plan.InputRecordOutputPath;
            EnsureOutputDirectory(outPath);

            var meta = CreateRecordMeta(plan);
            ctx.InputRecordWriter = LockstepInputRecordCodecs.Current.CreateWriter(outPath, meta);
        }

        private static void EnsureOutputDirectory(string outPath)
        {
            var outDir = Path.GetDirectoryName(outPath);
            if (!string.IsNullOrEmpty(outDir)) Directory.CreateDirectory(outDir);
        }

        private static LockstepInputRecordMeta CreateRecordMeta(BattleStartPlan plan)
        {
            return new LockstepInputRecordMeta
            {
                WorldId = plan.WorldId,
                WorldType = plan.WorldType,
                TickRate = ResolveRecordTickRate(plan),
                RandomSeed = 0,
                PlayerId = plan.PlayerId,
                StartedAtUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            };
        }

        private static int ResolveRecordTickRate(BattleStartPlan plan)
        {
            return plan.TickRate > 0 ? plan.TickRate : 30;
        }

        private static void ValidateReplayStateHash(BattleSessionHandles handles, BattleContext ctx)
        {
            var replay = handles.Replay.Driver;
            if (replay == null) return;

            if (ctx.EntityNode.IsValid && ctx.EntityNode.TryGetRef(out BattleStateHashSnapshotComponent hs) && hs != null)
            {
                if (!replay.TryValidateStateHashOnce(hs.Frame, hs.Version, hs.Hash, out var expected))
                {
                    Log.Error($"[BattleReplay] State hash mismatch at frame={hs.Frame}, expected(version={expected.Version}, hash={expected.Hash}), actual(version={hs.Version}, hash={hs.Hash})");
                    replay.Pause();
                }
            }
        }

        private static void RecordFrameIfNeeded(BattleStartPlan plan, BattleSessionState state, BattleContext ctx, FramePacket packet)
        {
            if (!plan.EnableInputRecording || ctx.InputRecordWriter == null) return;

            if (packet.Snapshot.HasValue)
            {
                var s = packet.Snapshot.Value;
                ctx.InputRecordWriter.AppendSnapshot(state.Tick.LastFrame, s.OpCode, s.Payload);
            }

            RecordStateHashIfNeeded(state, ctx);
        }

        private static void RecordStateHashIfNeeded(BattleSessionState state, BattleContext ctx)
        {
            var interval = StateHashRecordIntervalFrames;
            if (interval <= 0) interval = 10;

            if ((state.Tick.LastFrame % interval) != 0) return;

            if (ctx.EntityNode.IsValid && ctx.EntityNode.TryGetRef(out BattleStateHashSnapshotComponent h) && h != null)
            {
                ctx.InputRecordWriter.AppendStateHash(h.Frame, h.Version, h.Hash);
            }
        }
    }
}
