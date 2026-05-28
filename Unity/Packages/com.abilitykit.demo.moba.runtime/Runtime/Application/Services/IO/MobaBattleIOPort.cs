using System;
using System.Collections.Generic;
using AbilityKit.Ability.FrameSync;
using AbilityKit.Ability.Host;
using AbilityKit.Ability.World.Services;
using AbilityKit.Ability.World.Services.Attributes;
using AbilityKit.Coordinator;

namespace AbilityKit.Demo.Moba.Services
{
    [WorldService(typeof(IMobaBattleInputPort))]
    [WorldService(typeof(IMobaBattleOutputPort))]
    [WorldService(typeof(MobaBattleIOPort))]
    public sealed class MobaBattleIOPort : IService, IMobaBattleInputPort, IMobaBattleOutputPort
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

        public void Submit(FrameIndex frame, IReadOnlyList<PlayerInputCommand> inputs)
        {
            _input.Submit(frame, inputs);
        }

        public bool TryGetSnapshot(FrameIndex frame, out WorldStateSnapshot snapshot)
        {
            return _snapshots.TryGetSnapshot(frame, out snapshot);
        }

        public int CollectSnapshots(FrameIndex frame, IList<WorldStateSnapshot> snapshots, int maxSnapshots = 32)
        {
            if (snapshots == null || maxSnapshots <= 0) return 0;

            int count = 0;
            while (count < maxSnapshots && _snapshots.TryGetSnapshot(frame, out WorldStateSnapshot snapshot))
            {
                snapshots.Add(snapshot);
                count++;
            }

            return count;
        }

        public EntityState[] GetAllEntityStates()
        {
            var states = new List<EntityState>(8);

            foreach (var kv in _actors.Entries)
            {
                var actorId = kv.Key;
                var entity = kv.Value;
                if (entity == null) continue;

                var state = new EntityState(actorId);

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
