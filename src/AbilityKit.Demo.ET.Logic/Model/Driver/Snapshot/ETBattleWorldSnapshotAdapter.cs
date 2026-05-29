using AbilityKit.Ability.Host;
using AbilityKit.Demo.Moba.Share;

namespace ET.Logic
{
    public static class ETBattleWorldSnapshotAdapter
    {
        public static bool TryConvert(in WorldStateSnapshot snapshot, int frameIndex, double timestamp, out FrameSnapshotData frameSnapshot)
        {
            return RuntimeSnapshotConverterRegistry.TryConvert(in snapshot, frameIndex, timestamp, out frameSnapshot);
        }
    }
}
