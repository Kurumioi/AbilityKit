using System;
using System.Collections.Generic;
using AbilityKit.Ability.FrameSync;
using AbilityKit.Ability.Host;
using AbilityKit.Ability.World.Services;
using AbilityKit.Ability.World.Services.Attributes;

namespace AbilityKit.Demo.Moba.Services
{
    [WorldService(typeof(IMobaBattleInputPort))]
    [WorldService(typeof(IMobaBattleOutputPort))]
    [WorldService(typeof(MobaBattleIOPort))]
    public sealed class MobaBattleIOPort : IService, IMobaBattleInputPort, IMobaBattleOutputPort
    {
        private readonly IMobaInputCoordinator _input;
        private readonly IWorldStateSnapshotProvider _snapshots;

        public MobaBattleIOPort(MobaInputCoordinator input, MobaSnapshotRouter snapshots)
        {
            _input = input ?? throw new ArgumentNullException(nameof(input));
            _snapshots = snapshots ?? throw new ArgumentNullException(nameof(snapshots));
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

        public void Dispose()
        {
        }
    }
}
