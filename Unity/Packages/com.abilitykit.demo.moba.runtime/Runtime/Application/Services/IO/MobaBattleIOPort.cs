using System;
using System.Collections.Generic;
using AbilityKit.Ability.FrameSync;
using AbilityKit.Ability.Host;
using AbilityKit.Ability.Host.Extensions.Moba.Runtime;
using AbilityKit.Ability.World.Services;
using AbilityKit.Ability.World.Services.Attributes;
using AbilityKit.Demo.Moba.Services.LogicWorld;

namespace AbilityKit.Demo.Moba.Services
{
    [WorldService(typeof(IMobaBattleInputPort))]
    [WorldService(typeof(IMobaBattleOutputPort))]
    [WorldService(typeof(MobaBattleIOPort))]
    public sealed class MobaBattleIOPort : IService, IMobaBattleInputPort, IMobaBattleOutputPort
    {
        private readonly IMobaInputCoordinator _input;
        private readonly IWorldStateSnapshotProvider _snapshots;

        public MobaBattleIOPort(IMobaInputCoordinator input, IWorldStateSnapshotProvider snapshots)
        {
            _input = input ?? throw new ArgumentNullException(nameof(input));
            _snapshots = snapshots ?? throw new ArgumentNullException(nameof(snapshots));
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
            MobaRuntimeLog.Trace(MobaRuntimeLogModule.Input, MobaRuntimeLogPurpose.RuntimeTrace, nameof(MobaBattleIOPort), $"Submit: Frame={frame.Value}, Count={inputs.Count}, FirstPlayer={first.Player.Value}, FirstOp={first.OpCode}");

            LogicWorldInputSubmitResult result = _input.TrySubmit(frame, inputs);
            if (!result.Succeeded)
            {
                return MobaInputSubmitResult.Fail(MobaInputSubmitFailureCode.RejectedByInputCoordinator, result.Message);
            }

            if (result.HandledCount <= 0)
            {
                string detail = string.IsNullOrEmpty(result.Message) ? $"frame={frame.Value}, count={inputs.Count}" : result.Message;
                return MobaInputSubmitResult.Fail(MobaInputSubmitFailureCode.NoCommandHandled, $"input batch accepted but no command was handled: {detail}");
            }

            if (result.HandledCount < inputs.Count)
            {
                string detail = string.IsNullOrEmpty(result.Message) ? $"frame={frame.Value}, count={inputs.Count}, handled={result.HandledCount}" : result.Message;
                return MobaInputSubmitResult.Fail(MobaInputSubmitFailureCode.PartialCommandHandled, $"input batch partially handled: {detail}");
            }

            return MobaInputSubmitResult.Accepted(result.HandledCount, result.Message);
        }

        public bool TryGetSnapshot(FrameIndex frame, out WorldStateSnapshot snapshot)
        {
            return _snapshots.TryGetSnapshot(frame, out snapshot);
        }

        public int CollectSnapshots(FrameIndex frame, IList<WorldStateSnapshot> snapshots, int maxSnapshots = 32)
        {
            if (snapshots == null)
            {
                throw new ArgumentNullException(nameof(snapshots));
            }

            if (maxSnapshots <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxSnapshots), maxSnapshots, "maxSnapshots must be positive.");
            }

            if (_snapshots is IMobaSnapshotBatchProvider batchProvider)
            {
                return batchProvider.CollectSnapshots(frame, snapshots, maxSnapshots);
            }

            if (!_snapshots.TryGetSnapshot(frame, out WorldStateSnapshot snapshot)) return 0;

            snapshots.Add(snapshot);
            return 1;
        }

        public void Dispose()
        {
        }
    }

    [WorldService(typeof(IMobaLogicWorldStateReadModel))]
    [WorldService(typeof(MobaBattleStateQueryService))]
    public sealed class MobaBattleStateQueryService : IService, IMobaLogicWorldStateReadModel
    {
        private readonly MobaActorRegistry _actors;

        public MobaBattleStateQueryService(MobaActorRegistry actors)
        {
            _actors = actors ?? throw new ArgumentNullException(nameof(actors));
        }

        public LogicWorldEntityState[] GetAllEntityStates()
        {
            var states = new List<LogicWorldEntityState>(8);

            foreach (var kv in _actors.Entries)
            {
                var actorId = kv.Key;
                var entity = kv.Value;
                if (entity == null) continue;

                states.Add(CreateState(actorId, entity));
            }

            return states.ToArray();
        }

        private static LogicWorldEntityState CreateState(int actorId, ActorEntity entity)
        {
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

            state.HasAttributeGroup = entity.hasAttributeGroup;
            state.HasResourceContainer = entity.hasResourceContainer && entity.resourceContainer.Value != null;
            state.HasSkillLoadout = entity.hasSkillLoadout;
            state.ActiveSkillCount = entity.hasSkillLoadout && entity.skillLoadout.ActiveSkills != null
                ? entity.skillLoadout.ActiveSkills.Length
                : 0;

            if (state.HasAttributeGroup && state.HasResourceContainer)
            {
                var attrs = entity.GetMobaAttrs();
                state.Hp = attrs.Hp;
                state.HpMax = attrs.MaxHp;
                state.IsDead = attrs.Hp <= 0f;
            }
            else
            {
                state.IsDead = false;
            }

            return state;
        }

        public void Dispose()
        {
        }
    }
}
