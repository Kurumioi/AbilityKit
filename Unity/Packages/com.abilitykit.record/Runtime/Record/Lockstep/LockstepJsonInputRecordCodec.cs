namespace AbilityKit.Core.Recording.Lockstep
{
    public sealed class LockstepJsonInputRecordCodec : ILockstepInputRecordCodec
    {
        public ILockstepInputRecordWriter CreateWriter(string outputPath, LockstepInputRecordMeta meta)
        {
            return new LockstepJsonInputRecordWriter(outputPath, meta);
        }

        public LockstepInputRecordFile Load(string path)
        {
            return LockstepJsonInputRecordReader.Load(path);
        }
    }
}
