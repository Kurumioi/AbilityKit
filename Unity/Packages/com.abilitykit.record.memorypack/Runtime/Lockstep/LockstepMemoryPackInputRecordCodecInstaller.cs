using AbilityKit.Core.Recording.Lockstep;

namespace AbilityKit.Record.MemoryPack
{
    public static class LockstepMemoryPackInputRecordCodecInstaller
    {
        public static void InstallAsCurrent()
        {
            LockstepInputRecordCodecs.Current = new LockstepMemoryPackInputRecordCodec();
        }
    }
}
