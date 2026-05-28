using System.Collections.Generic;
using AbilityKit.Ability.FrameSync;
using AbilityKit.Ability.Host;
using AbilityKit.Coordinator;

namespace AbilityKit.Demo.Moba.Services
{
    public interface IMobaBattleOutputPort
    {
        bool TryGetSnapshot(FrameIndex frame, out WorldStateSnapshot snapshot);

        int CollectSnapshots(FrameIndex frame, IList<WorldStateSnapshot> snapshots, int maxSnapshots = 32);

        EntityState[] GetAllEntityStates();
    }
}
