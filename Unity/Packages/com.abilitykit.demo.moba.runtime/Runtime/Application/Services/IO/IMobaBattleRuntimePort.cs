using System;
using System.Collections.Generic;
using AbilityKit.Ability.FrameSync;
using AbilityKit.Ability.Host;
using AbilityKit.Ability.World.Services;
using AbilityKit.Ability.World.Services.Attributes;
using AbilityKit.Protocol.Moba;

namespace AbilityKit.Demo.Moba.Services
{
    [Flags]
    public enum MobaBattleRuntimeCapability
    {
        None = 0,
        GameStart = 1 << 0,
        Input = 1 << 1,
        SnapshotOutput = 1 << 2,
        StateReadModel = 1 << 3,
    }

    public readonly struct MobaBattleRuntimeStatus
    {
        public readonly MobaBattleRuntimeCapability Capabilities;
        public readonly string MissingServices;

        public MobaBattleRuntimeStatus(MobaBattleRuntimeCapability capabilities, string missingServices)
        {
            Capabilities = capabilities;
            MissingServices = missingServices;
        }

        public bool Has(MobaBattleRuntimeCapability capability)
        {
            return (Capabilities & capability) == capability;
        }

        public bool IsReadyForBattleLoop => Has(MobaBattleRuntimeCapability.Input | MobaBattleRuntimeCapability.SnapshotOutput);

        public bool IsReadyForGameStart => Has(MobaBattleRuntimeCapability.GameStart);

        public override string ToString()
        {
            return string.IsNullOrEmpty(MissingServices)
                ? $"Capabilities={Capabilities}"
                : $"Capabilities={Capabilities}, Missing={MissingServices}";
        }
    }

    public interface IMobaBattleRuntimePort : IService
    {
        MobaBattleRuntimeStatus Status { get; }

        MobaGameStartResult TryStartGame(in MobaGameStartSpec spec);

        MobaInputSubmitResult Submit(FrameIndex frame, IReadOnlyList<PlayerInputCommand> inputs);

        bool TryGetSnapshot(FrameIndex frame, out WorldStateSnapshot snapshot);

        int CollectSnapshots(FrameIndex frame, IList<WorldStateSnapshot> snapshots, int maxSnapshots = 32);

        LogicWorldEntityState[] GetAllEntityStates();
    }

    [WorldService(typeof(IMobaBattleRuntimePort))]
    [WorldService(typeof(MobaBattleRuntimePort))]
    public sealed class MobaBattleRuntimePort : IService, IMobaBattleRuntimePort
    {
        private readonly IMobaGameStartPort _gameStart;
        private readonly IMobaBattleInputPort _input;
        private readonly IMobaBattleOutputPort _output;
        private readonly IMobaLogicWorldStateReadModel _stateReadModel;

        public MobaBattleRuntimePort(
            IMobaGameStartPort gameStart,
            IMobaBattleInputPort input,
            IMobaBattleOutputPort output,
            IMobaLogicWorldStateReadModel stateReadModel)
        {
            _gameStart = gameStart;
            _input = input;
            _output = output;
            _stateReadModel = stateReadModel;
        }

        public MobaBattleRuntimeStatus Status => BuildStatus();

        public MobaGameStartResult TryStartGame(in MobaGameStartSpec spec)
        {
            if (_gameStart == null)
            {
                return MobaGameStartResult.Fail(MobaGameStartFailureCode.MissingGameStartPort, "IMobaGameStartPort is not resolved");
            }

            return _gameStart.TryStartGame(in spec);
        }

        public MobaInputSubmitResult Submit(FrameIndex frame, IReadOnlyList<PlayerInputCommand> inputs)
        {
            if (_input == null)
            {
                return MobaInputSubmitResult.Fail(MobaInputSubmitFailureCode.MissingInputPort, "IMobaBattleInputPort is not resolved");
            }

            return _input.Submit(frame, inputs);
        }

        public bool TryGetSnapshot(FrameIndex frame, out WorldStateSnapshot snapshot)
        {
            if (_output == null)
            {
                snapshot = default;
                return false;
            }

            return _output.TryGetSnapshot(frame, out snapshot);
        }

        public int CollectSnapshots(FrameIndex frame, IList<WorldStateSnapshot> snapshots, int maxSnapshots = 32)
        {
            if (_output == null || snapshots == null)
            {
                return 0;
            }

            return _output.CollectSnapshots(frame, snapshots, maxSnapshots);
        }

        public LogicWorldEntityState[] GetAllEntityStates()
        {
            return _stateReadModel?.GetAllEntityStates() ?? Array.Empty<LogicWorldEntityState>();
        }

        public void Dispose()
        {
        }

        private MobaBattleRuntimeStatus BuildStatus()
        {
            var capabilities = MobaBattleRuntimeCapability.None;
            var missing = new List<string>(4);

            if (_gameStart != null) capabilities |= MobaBattleRuntimeCapability.GameStart;
            else missing.Add(nameof(IMobaGameStartPort));

            if (_input != null) capabilities |= MobaBattleRuntimeCapability.Input;
            else missing.Add(nameof(IMobaBattleInputPort));

            if (_output != null) capabilities |= MobaBattleRuntimeCapability.SnapshotOutput;
            else missing.Add(nameof(IMobaBattleOutputPort));

            if (_stateReadModel != null) capabilities |= MobaBattleRuntimeCapability.StateReadModel;
            else missing.Add(nameof(IMobaLogicWorldStateReadModel));

            return new MobaBattleRuntimeStatus(capabilities, string.Join(",", missing));
        }
    }
}
