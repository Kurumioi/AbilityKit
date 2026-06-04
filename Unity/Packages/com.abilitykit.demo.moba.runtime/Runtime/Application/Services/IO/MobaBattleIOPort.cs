using System;
using System.Collections.Generic;
using AbilityKit.Ability.FrameSync;
using AbilityKit.Ability.Host;
using AbilityKit.Ability.World.Services;
using AbilityKit.Ability.World.Services.Attributes;
using AbilityKit.Core.Common.Log;

namespace AbilityKit.Demo.Moba.Services
{
    [WorldService(typeof(IMobaBattleInputPort))]
    [WorldService(typeof(IMobaBattleOutputPort))]
    [WorldService(typeof(IMobaLogicWorldStateReadModel))]
    [WorldService(typeof(MobaBattleIOPort))]
    public sealed class MobaBattleIOPort : IService, IMobaBattleInputPort, IMobaBattleOutputPort, IMobaLogicWorldStateReadModel
    {
        private readonly IMobaInputCoordinator _input;
        private readonly IWorldStateSnapshotProvider _snapshots;
        private readonly MobaActorRegistry _actors;

        public MobaBattleIOPort(IMobaInputCoordinator input, IWorldStateSnapshotProvider snapshots, MobaActorRegistry actors)
        {
            _input = input ?? throw new ArgumentNullException(nameof(input));
            _snapshots = snapshots ?? throw new ArgumentNullException(nameof(snapshots));
            _actors = actors ?? throw new ArgumentNullException(nameof(actors));
        }

        public MobaInputSubmitResult Submit(FrameIndex frame, IReadOnlyList<PlayerInputCommand> inputs)
        {
            if (_input == null)
            {
                return MobaInputSubmitResult.Fail(MobaInputSubmitFailureCode.MissingInputCoordinator, "IMobaInputCoordinator is not resolved");
            }

            if (inputs == null || inputs.Count == 0)
            {
                return MobaInputSubmitResult.Fail(MobaInputSubmitFailureCode.NullOrEmptyCommands, "input command batch is null or empty");
            }

            if (frame.Value < 0)
            {
                return MobaInputSubmitResult.Fail(MobaInputSubmitFailureCode.InvalidFrame, $"target frame is negative: {frame.Value}");
            }

            PlayerInputCommand first = inputs[0];
            Log.Info($"[MobaBattleIOPort] Submit: Frame={frame.Value}, Count={inputs.Count}, FirstPlayer={first.Player.Value}, FirstOp={first.OpCode}");

            _input.Submit(frame, inputs);
            return MobaInputSubmitResult.Accepted(inputs.Count);
        }

        public bool TryGetSnapshot(FrameIndex frame, out WorldStateSnapshot snapshot)
        {
            return _snapshots.TryGetSnapshot(frame, out snapshot);
        }

        public int CollectSnapshots(FrameIndex frame, IList<WorldStateSnapshot> snapshots, int maxSnapshots = 32)
        {
            if (snapshots == null || maxSnapshots <= 0) return 0;

            if (_snapshots is IMobaSnapshotBatchProvider batchProvider)
            {
                return batchProvider.CollectSnapshots(frame, snapshots, maxSnapshots);
            }

            if (!_snapshots.TryGetSnapshot(frame, out WorldStateSnapshot snapshot)) return 0;

            snapshots.Add(snapshot);
            return 1;
        }

        public LogicWorldEntityState[] GetAllEntityStates()
        {
            var states = new List<LogicWorldEntityState>(8);

            foreach (var kv in _actors.Entries)
            {
                var actorId = kv.Key;
                var entity = kv.Value;
                if (entity == null) continue;

                var state = new LogicWorldEntityState(actorId);

                if (entity.hasTransform)
                {
                    var pos = entity.transform.Value.Position;
                    state.X = pos.X;
                    state.Y = pos.Y;
                    state.Z = pos.Z;
                }

                if (entity.hasTeam)
                {
                    state.TeamId = (int)entity.team.Value;
                }

                states.Add(state);
            }

            return states.ToArray();
        }

        public void Dispose()
        {
        }
    }
}
