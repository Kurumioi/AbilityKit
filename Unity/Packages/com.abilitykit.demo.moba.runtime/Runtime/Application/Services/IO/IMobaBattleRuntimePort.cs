using System;
using System.Collections.Generic;
using AbilityKit.Ability.FrameSync;
using AbilityKit.Ability.Host;
using AbilityKit.Ability.Host.Extensions.Moba.Runtime;
using AbilityKit.Ability.World.Services;
using AbilityKit.Ability.World.Services.Attributes;
using AbilityKit.Protocol.Moba;

namespace AbilityKit.Demo.Moba.Services
{
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
                throw new InvalidOperationException("MobaBattleRuntimePort requires IMobaBattleOutputPort for snapshot output.");
            }

            return _output.TryGetSnapshot(frame, out snapshot);
        }

        public int CollectSnapshots(FrameIndex frame, IList<WorldStateSnapshot> snapshots, int maxSnapshots = 32)
        {
            if (_output == null)
            {
                throw new InvalidOperationException("MobaBattleRuntimePort requires IMobaBattleOutputPort for snapshot output.");
            }

            if (snapshots == null)
            {
                throw new ArgumentNullException(nameof(snapshots));
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
