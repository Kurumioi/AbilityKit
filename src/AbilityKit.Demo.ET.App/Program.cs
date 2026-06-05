using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using AbilityKit.Ability.FrameSync;
using AbilityKit.Ability.Host;
using AbilityKit.Demo.Moba.Services;
using AbilityKit.Demo.Moba.Share;
using AbilityKit.Protocol.Moba;
using AbilityKit.Protocol.Moba.StateSync;
using ET.AbilityKit.Demo.ET.Share;
using ET.Logic;

namespace ET.AbilityKit.Demo.ET.App
{
    /// <summary>
    /// ET Demo 入口程序
    /// 使用 ET 标准的 Entry 系统
    /// </summary>
    public sealed class Program
    {
        private const int MainFiberId = 10000001;

        public static int Main(string[] args)
        {
            Console.WriteLine("=== AbilityKit ET Demo ===");
            Console.WriteLine("Starting ET Framework with Demo Process Component...");
            Console.WriteLine();

            try
            {
                var options = DemoRunOptions.Parse(args);

                DemoEntry.Init(args);
                DemoEntry.StartAsync().NoContext();

                Console.WriteLine();
                Console.WriteLine("=== ET Framework Started ===");
                Console.WriteLine(options.Smoke ? "Running ET battle smoke flow." : "Press Ctrl+C to exit.");
                Console.WriteLine();

                if (!options.Smoke)
                {
                    return RunInteractive();
                }

                var exitCode = RunSmoke(options);
                if (options.SmokeForceExit)
                {
                    Environment.Exit(exitCode);
                }

                return exitCode;
            }
            catch (Exception e)
            {
                Console.WriteLine();
                Console.WriteLine("=== ET Framework Initialization Failed ===");
                Console.WriteLine($"Error: {e.Message}");
                Console.WriteLine(e.StackTrace);
                return 1;
            }
        }

        private static int RunInteractive()
        {
            while (true)
            {
                TickEt();
                Thread.Sleep(16);
            }
        }

        private static int RunSmoke(DemoRunOptions options)
        {
            var probe = new EtBattleSmokeProbe();
            var stopwatch = Stopwatch.StartNew();
            var elapsedFrames = 0;

            for (int i = 0; i < options.SmokeFrames; i++)
            {
                elapsedFrames = i + 1;
                probe.Sample();
                TickEt();
                probe.Sample();

                if (probe.IsHealthy(options))
                {
                    DrainEt(options.SmokeDrainFrames, options.SleepMilliseconds);
                    Console.WriteLine(probe.FormatResult(elapsedFrames, stopwatch.ElapsedMilliseconds, options));
                    Console.WriteLine("=== ET Battle Smoke Passed ===");
                    return 0;
                }

                if (options.SmokeTimeoutMilliseconds > 0 && stopwatch.ElapsedMilliseconds >= options.SmokeTimeoutMilliseconds)
                {
                    break;
                }

                Thread.Sleep(options.SleepMilliseconds);
            }

            Console.WriteLine(probe.FormatResult(elapsedFrames, stopwatch.ElapsedMilliseconds, options));
            Console.WriteLine("=== ET Battle Smoke Failed ===");
            return 2;
        }

        private static void DrainEt(int frames, int sleepMilliseconds)
        {
            for (int i = 0; i < frames; i++)
            {
                TickEt();
                if (sleepMilliseconds > 0)
                {
                    Thread.Sleep(sleepMilliseconds);
                }
            }
        }

        private static void TickEt()
        {
            try
            {
                global::ET.FiberManager.Instance.Update();
                global::ET.FiberManager.Instance.LateUpdate();
            }
            catch (Exception ex)
            {
                global::ET.Log.Error($"Main loop error: {ex}");
                throw;
            }
        }

        private sealed class DemoRunOptions
        {
            public bool Smoke { get; private set; }
            public int SmokeFrames { get; private set; } = 600;
            public int SmokeMinBattleFrames { get; private set; } = 30;
            public int SmokeTimeoutMilliseconds { get; private set; } = 15000;
            public int SmokeDrainFrames { get; private set; } = 5;
            public int SleepMilliseconds { get; private set; } = 16;
            public bool SmokeForceExit { get; private set; } = true;

            public static DemoRunOptions Parse(string[] args)
            {
                var options = new DemoRunOptions();
                if (args == null)
                {
                    return options;
                }

                foreach (var arg in args)
                {
                    if (string.Equals(arg, "--smoke", StringComparison.OrdinalIgnoreCase))
                    {
                        options.Smoke = true;
                    }
                    else if (TryReadInt(arg, "--smoke-frames=", out var frames))
                    {
                        options.SmokeFrames = Math.Max(1, frames);
                    }
                    else if (TryReadInt(arg, "--smoke-min-battle-frames=", out var minBattleFrames))
                    {
                        options.SmokeMinBattleFrames = Math.Max(1, minBattleFrames);
                    }
                    else if (TryReadInt(arg, "--smoke-timeout-ms=", out var timeoutMilliseconds))
                    {
                        options.SmokeTimeoutMilliseconds = Math.Max(0, timeoutMilliseconds);
                    }
                    else if (TryReadInt(arg, "--smoke-drain-frames=", out var drainFrames))
                    {
                        options.SmokeDrainFrames = Math.Max(0, drainFrames);
                    }
                    else if (TryReadInt(arg, "--smoke-sleep-ms=", out var sleepMilliseconds))
                    {
                        options.SleepMilliseconds = Math.Max(0, sleepMilliseconds);
                    }
                    else if (string.Equals(arg, "--smoke-no-force-exit", StringComparison.OrdinalIgnoreCase))
                    {
                        options.SmokeForceExit = false;
                    }
                }

                return options;
            }

            private static bool TryReadInt(string arg, string prefix, out int value)
            {
                value = 0;
                if (string.IsNullOrEmpty(arg) || !arg.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                return int.TryParse(arg.Substring(prefix.Length), out value);
            }
        }

        private sealed class EtBattleSmokeProbe
        {
            private static readonly MethodInfo GetFiberMethod = typeof(global::ET.FiberManager).GetMethod(
                "Get",
                BindingFlags.Instance | BindingFlags.NonPublic);

            private readonly List<WorldStateSnapshot> _snapshots = new List<WorldStateSnapshot>(32);
            private readonly List<SkillPipelineRunner.RunningSnapshot> _runningSkills = new List<SkillPipelineRunner.RunningSnapshot>(8);

            private bool _inputsSubmitted;
            private float _targetX;
            private float _targetZ;
            private FrameSnapshotDispatcher? _subscribedDispatcher;
            private IDisposable? _damageEventSubscription;
            private IDisposable? _projectileEventSubscription;
            private IDisposable? _areaEventSubscription;

            public bool HasProcessScene { get; private set; }
            public bool HasBattleScene { get; private set; }
            public bool HasBattleComponent { get; private set; }
            public bool HasRuntimePort { get; private set; }
            public bool HasReadyRuntime { get; private set; }
            public bool HasStartedRuntime { get; private set; }
            public bool HasStartedBattle { get; private set; }
            public bool HasRuntimeEntities { get; private set; }
            public bool HasRuntimeSnapshots { get; private set; }
            public bool HasMoveInputSubmitted { get; private set; }
            public bool HasSkillInputSubmitted { get; private set; }
            public bool HasSkillTargetActor { get; private set; }
            public bool HasDecodedTransformSnapshot { get; private set; }
            public bool HasDecodedStateHashSnapshot { get; private set; }
            public bool HasDecodedEventSnapshot { get; private set; }
            public int MaxBattleFrame { get; private set; }
            public int MaxEntityCount { get; private set; }
            public int MaxSnapshotCount { get; private set; }
            public int InputTargetFrame { get; private set; }
            public int MaxTransformEntryCount { get; private set; }
            public int MaxStateHashFrame { get; private set; }
            public int DecodedEventSnapshotCount { get; private set; }
            public bool HasRunningSkillSnapshot { get; private set; }
            public int MaxRunningSkillElapsedMs { get; private set; }
            public int MaxRunningSkillNextEventIndex { get; private set; }
            public int LocalActorId { get; private set; }
            public int LocalTeamId { get; private set; }
            public int TargetActorId { get; private set; }
            public int TargetTeamId { get; private set; }
            public float TargetInitialHp { get; private set; }
            public float TargetMinHp { get; private set; }
            public bool TargetHasAttributeGroup { get; private set; }
            public bool TargetHasResourceContainer { get; private set; }
            public bool TargetHasSkillLoadout { get; private set; }
            public int TargetActiveSkillCount { get; private set; }
            public string RuntimeStatus { get; private set; } = "runtime port missing";
            public string PendingInputFrames { get; private set; } = "input missing";

            public void Sample()
            {
                var root = GetRootScene();
                if (root == null)
                {
                    return;
                }

                var process = root.GetComponent<DemoProcessComponent>();
                HasProcessScene = process != null;

                var battleScene = process?.CurrentScene;
                HasBattleScene = battleScene != null && battleScene.SceneType == global::ET.SceneType.DemoBattle;

                var battle = battleScene?.GetComponent<ETBattleComponent>();
                HasBattleComponent = battle != null;

                if (battle?.BattleDriver is not ETMobaBattleDriver driver)
                {
                    return;
                }

                HasStartedRuntime |= driver.RuntimeGameStarted;
                HasStartedBattle |= battle.State == BattleState.InProgress && driver.IsRunning;
                MaxBattleFrame = Math.Max(MaxBattleFrame, driver.CurrentFrame);
                SubscribeDriverSnapshots(driver);

                if (driver.TryResolve<IMobaBattleRuntimePort>(out var runtime) && runtime != null)
                {
                    HasRuntimePort = true;
                    var status = runtime.Status;
                    RuntimeStatus = status.ToString();
                    HasReadyRuntime |= status.IsReadyForGameStart && status.IsReadyForBattleLoop && status.Has(MobaBattleRuntimeCapability.StateReadModel);

                    var entityStates = runtime.GetAllEntityStates() ?? Array.Empty<LogicWorldEntityState>();
                    var entityCount = entityStates.Length;
                    MaxEntityCount = Math.Max(MaxEntityCount, entityCount);
                    HasRuntimeEntities |= entityCount > 0;
                    SelectSkillTarget(entityStates);
                    SampleTargetHealth(entityStates);

                    if (battleScene != null)
                    {
                        SampleInputBuffer(battleScene);
                        TrySubmitProtocolInputs(battleScene, battle, driver);
                    }

                    SampleRunningSkills(driver);

                    _snapshots.Clear();
                    var snapshotCount = runtime.CollectSnapshots(new FrameIndex(driver.CurrentFrame), _snapshots);
                    MaxSnapshotCount = Math.Max(MaxSnapshotCount, snapshotCount);
                    HasRuntimeSnapshots |= snapshotCount > 0;
                    DecodeRuntimeSnapshots();
                }
            }

            public bool IsHealthy(DemoRunOptions options)
            {
                return HasStartedBattle &&
                       HasReadyRuntime &&
                       HasRuntimeEntities &&
                       HasRuntimeSnapshots &&
                       HasMoveInputSubmitted &&
                       HasSkillInputSubmitted &&
                       HasSkillTargetActor &&
                       HasDecodedTransformSnapshot &&
                       HasDecodedStateHashSnapshot &&
                       HasDecodedEventSnapshot &&
                       MaxBattleFrame >= Math.Max(options.SmokeMinBattleFrames, InputTargetFrame + 1);
            }

            private void TrySubmitProtocolInputs(global::ET.Scene battleScene, ETBattleComponent battle, ETMobaBattleDriver driver)
            {
                if (_inputsSubmitted || !HasStartedBattle || !HasSkillTargetActor || battleScene == null)
                {
                    return;
                }

                var input = battleScene.GetComponent<ETInputComponent>();
                if (input == null)
                {
                    return;
                }

                var playerId = ResolveRuntimePlayerId(driver, battle);
                if (string.IsNullOrEmpty(playerId))
                {
                    return;
                }

                InputTargetFrame = driver.CurrentFrame + 1;
                input.AddMoveCommand(InputTargetFrame, playerId, 1f, 0f);
                input.AddSkillCommand(InputTargetFrame, playerId, 1, _targetX, _targetZ, TargetActorId);
                PendingInputFrames = input.FormatPendingFrames();

                _inputsSubmitted = true;
                HasMoveInputSubmitted = true;
                HasSkillInputSubmitted = true;
            }

            private static string ResolveRuntimePlayerId(ETMobaBattleDriver driver, ETBattleComponent battle)
            {
                if (driver?.PlayerSpawnData != null && driver.PlayerSpawnData.Count > 0)
                {
                    var playerId = driver.PlayerSpawnData[0]?.PlayerId;
                    if (!string.IsNullOrEmpty(playerId))
                    {
                        return playerId;
                    }
                }

                return battle != null && battle.PlayerId > 0 ? battle.PlayerId.ToString() : string.Empty;
            }

            private void SampleInputBuffer(global::ET.Scene battleScene)
            {
                var input = battleScene.GetComponent<ETInputComponent>();
                PendingInputFrames = input != null ? input.FormatPendingFrames() : "input missing";
            }

            private void SubscribeDriverSnapshots(ETMobaBattleDriver driver)
            {
                var dispatcher = driver?.SnapshotDispatcher;
                if (dispatcher == null || ReferenceEquals(dispatcher, _subscribedDispatcher))
                {
                    return;
                }

                UnsubscribeDriverSnapshots();
                _subscribedDispatcher = dispatcher;
                _damageEventSubscription = dispatcher.Subscribe<DamageEventData[]>(MobaOpCodes.Snapshot.DamageEvent, OnDamageEventSnapshot);
                _projectileEventSubscription = dispatcher.Subscribe<ProjectileEventData[]>(MobaOpCodes.Snapshot.ProjectileEvent, OnProjectileEventSnapshot);
                _areaEventSubscription = dispatcher.Subscribe<AreaEventData[]>(MobaOpCodes.Snapshot.AreaEvent, OnAreaEventSnapshot);
            }

            private void UnsubscribeDriverSnapshots()
            {
                if (_subscribedDispatcher == null)
                {
                    _damageEventSubscription = null;
                    _projectileEventSubscription = null;
                    _areaEventSubscription = null;
                    return;
                }

                _subscribedDispatcher.Unsubscribe(_damageEventSubscription);
                _subscribedDispatcher.Unsubscribe(_projectileEventSubscription);
                _subscribedDispatcher.Unsubscribe(_areaEventSubscription);
                _subscribedDispatcher = null;
                _damageEventSubscription = null;
                _projectileEventSubscription = null;
                _areaEventSubscription = null;
            }

            private void OnDamageEventSnapshot(int frameIndex, DamageEventData[] data)
            {
                RecordDispatchedEventSnapshot(data?.Length ?? 0);
            }

            private void OnProjectileEventSnapshot(int frameIndex, ProjectileEventData[] data)
            {
                RecordDispatchedEventSnapshot(data?.Length ?? 0);
            }

            private void OnAreaEventSnapshot(int frameIndex, AreaEventData[] data)
            {
                RecordDispatchedEventSnapshot(data?.Length ?? 0);
            }

            private void RecordDispatchedEventSnapshot(int count)
            {
                if (count <= 0)
                {
                    return;
                }

                DecodedEventSnapshotCount += count;
                HasDecodedEventSnapshot = true;
            }

            private void SelectSkillTarget(LogicWorldEntityState[] states)
            {
                if (HasSkillTargetActor || states == null || states.Length < 2)
                {
                    return;
                }

                var localIndex = -1;
                for (int i = 0; i < states.Length; i++)
                {
                    if (states[i].EntityId > 0 && states[i].TeamId == 1)
                    {
                        localIndex = i;
                        break;
                    }
                }

                if (localIndex < 0)
                {
                    return;
                }

                var local = states[localIndex];
                for (int i = 0; i < states.Length; i++)
                {
                    var candidate = states[i];
                    if (candidate.EntityId <= 0 || candidate.EntityId == local.EntityId || candidate.TeamId == local.TeamId)
                    {
                        continue;
                    }

                    LocalActorId = local.EntityId;
                    LocalTeamId = local.TeamId;
                    TargetActorId = candidate.EntityId;
                    TargetTeamId = candidate.TeamId;
                    _targetX = candidate.X;
                    _targetZ = candidate.Z;
                    HasSkillTargetActor = true;
                    return;
                }
            }

            private void SampleTargetHealth(LogicWorldEntityState[] states)
            {
                if (!HasSkillTargetActor || states == null || states.Length == 0)
                {
                    return;
                }

                for (int i = 0; i < states.Length; i++)
                {
                    var state = states[i];
                    if (state.EntityId != TargetActorId)
                    {
                        continue;
                    }

                    TargetHasAttributeGroup = state.HasAttributeGroup;
                    TargetHasResourceContainer = state.HasResourceContainer;
                    TargetHasSkillLoadout = state.HasSkillLoadout;
                    TargetActiveSkillCount = state.ActiveSkillCount;

                    if (TargetInitialHp <= 0f && state.Hp > 0f)
                    {
                        TargetInitialHp = state.Hp;
                        TargetMinHp = state.Hp;
                    }
                    else if (TargetMinHp <= 0f || state.Hp < TargetMinHp)
                    {
                        TargetMinHp = state.Hp;
                    }

                    return;
                }
            }

            private void SampleRunningSkills(ETMobaBattleDriver driver)
            {
                if (driver == null || LocalActorId <= 0)
                {
                    return;
                }

                if (!driver.TryResolve<SkillExecutor>(out var skills) || skills == null)
                {
                    return;
                }

                _runningSkills.Clear();
                skills.FillRunningSnapshots(LocalActorId, _runningSkills);
                if (_runningSkills.Count == 0)
                {
                    return;
                }

                HasRunningSkillSnapshot = true;
                for (int i = 0; i < _runningSkills.Count; i++)
                {
                    var snapshot = _runningSkills[i];
                    MaxRunningSkillElapsedMs = Math.Max(MaxRunningSkillElapsedMs, snapshot.ElapsedMs);
                    MaxRunningSkillNextEventIndex = Math.Max(MaxRunningSkillNextEventIndex, snapshot.NextEventIndex);
                }
            }

            private void DecodeRuntimeSnapshots()
            {
                for (int i = 0; i < _snapshots.Count; i++)
                {
                    var snapshot = _snapshots[i];
                    switch (snapshot.OpCode)
                    {
                        case MobaOpCodes.Snapshot.ActorTransform:
                            var transforms = MobaActorTransformSnapshotCodec.Deserialize(snapshot.Payload);
                            MaxTransformEntryCount = Math.Max(MaxTransformEntryCount, transforms.Length);
                            HasDecodedTransformSnapshot |= transforms.Length > 0;
                            break;
                        case MobaOpCodes.Snapshot.StateHash:
                            var hash = MobaStateHashSnapshotCodec.Deserialize(snapshot.Payload);
                            MaxStateHashFrame = Math.Max(MaxStateHashFrame, hash.Frame);
                            HasDecodedStateHashSnapshot |= hash.Version == MobaStateHashSnapshotCodec.Version && hash.Frame >= 0;
                            break;
                        case MobaOpCodes.Snapshot.DamageEvent:
                            var damages = MobaDamageEventSnapshotCodec.Deserialize(snapshot.Payload);
                            DecodedEventSnapshotCount += damages.Length;
                            HasDecodedEventSnapshot |= damages.Length > 0;
                            break;
                        case MobaOpCodes.Snapshot.ProjectileEvent:
                            var projectiles = MobaProjectileEventSnapshotCodec.Deserialize(snapshot.Payload);
                            DecodedEventSnapshotCount += projectiles.Length;
                            HasDecodedEventSnapshot |= projectiles.Length > 0;
                            break;
                        case MobaOpCodes.Snapshot.AreaEvent:
                            var areas = MobaAreaEventSnapshotCodec.Deserialize(snapshot.Payload);
                            DecodedEventSnapshotCount += areas.Length;
                            HasDecodedEventSnapshot |= areas.Length > 0;
                            break;
                    }
                }
            }

            public string FormatResult(int elapsedFrames, long elapsedMilliseconds, DemoRunOptions options)
            {
                return "[ETBattleSmoke] " +
                       $"ElapsedFrames={elapsedFrames}, " +
                       $"ElapsedMilliseconds={elapsedMilliseconds}, " +
                       $"ExpectedBattleFrames>={options.SmokeMinBattleFrames}, " +
                       $"TimeoutMilliseconds={options.SmokeTimeoutMilliseconds}, " +
                       $"HasProcessScene={HasProcessScene}, " +
                       $"HasBattleScene={HasBattleScene}, " +
                       $"HasBattleComponent={HasBattleComponent}, " +
                       $"HasRuntimePort={HasRuntimePort}, " +
                       $"HasReadyRuntime={HasReadyRuntime}, " +
                       $"HasStartedRuntime={HasStartedRuntime}, " +
                       $"HasStartedBattle={HasStartedBattle}, " +
                       $"HasRuntimeEntities={HasRuntimeEntities}, " +
                       $"HasRuntimeSnapshots={HasRuntimeSnapshots}, " +
                       $"HasMoveInputSubmitted={HasMoveInputSubmitted}, " +
                       $"HasSkillInputSubmitted={HasSkillInputSubmitted}, " +
                       $"HasSkillTargetActor={HasSkillTargetActor}, " +
                       $"HasDecodedTransformSnapshot={HasDecodedTransformSnapshot}, " +
                       $"HasDecodedStateHashSnapshot={HasDecodedStateHashSnapshot}, " +
                       $"HasDecodedEventSnapshot={HasDecodedEventSnapshot}, " +
                       $"MaxBattleFrame={MaxBattleFrame}, " +
                       $"InputTargetFrame={InputTargetFrame}, " +
                       $"MaxEntityCount={MaxEntityCount}, " +
                       $"MaxSnapshotCount={MaxSnapshotCount}, " +
                       $"MaxTransformEntryCount={MaxTransformEntryCount}, " +
                       $"MaxStateHashFrame={MaxStateHashFrame}, " +
                       $"DecodedEventSnapshotCount={DecodedEventSnapshotCount}, " +
                       $"HasRunningSkillSnapshot={HasRunningSkillSnapshot}, " +
                       $"MaxRunningSkillElapsedMs={MaxRunningSkillElapsedMs}, " +
                       $"MaxRunningSkillNextEventIndex={MaxRunningSkillNextEventIndex}, " +
                       $"PendingInputFrames={PendingInputFrames}, " +
                       $"TargetInitialHp={TargetInitialHp:F1}, " +
                       $"TargetMinHp={TargetMinHp:F1}, " +
                       $"TargetHasAttributeGroup={TargetHasAttributeGroup}, " +
                       $"TargetHasResourceContainer={TargetHasResourceContainer}, " +
                       $"TargetHasSkillLoadout={TargetHasSkillLoadout}, " +
                       $"TargetActiveSkillCount={TargetActiveSkillCount}, " +
                       $"LocalActorId={LocalActorId}, " +
                       $"LocalTeamId={LocalTeamId}, " +
                       $"TargetActorId={TargetActorId}, " +
                       $"TargetTeamId={TargetTeamId}, " +
                       $"RuntimeStatus={RuntimeStatus}";
            }

            private static global::ET.Scene GetRootScene()
            {
                if (GetFiberMethod == null)
                {
                    return null;
                }

                var fiber = GetFiberMethod.Invoke(global::ET.FiberManager.Instance, new object[] { MainFiberId });
                return fiber?.GetType().GetProperty("Root", BindingFlags.Instance | BindingFlags.Public)?.GetValue(fiber) as global::ET.Scene;
            }
        }
    }
}
