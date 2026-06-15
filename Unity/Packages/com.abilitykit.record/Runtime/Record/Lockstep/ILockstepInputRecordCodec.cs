using AbilityKit.Ability.Host;

namespace AbilityKit.Core.Recording.Lockstep
{
    public interface ILockstepInputRecordCodec
    {
        ILockstepInputRecordWriter CreateWriter(string outputPath, LockstepInputRecordMeta meta);

        LockstepInputRecordFile Load(string path);
    }
}
