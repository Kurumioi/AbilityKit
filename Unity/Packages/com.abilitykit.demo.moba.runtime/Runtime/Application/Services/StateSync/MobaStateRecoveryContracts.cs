using System;
using AbilityKit.Ability.FrameSync;
using AbilityKit.Core.Serialization;

namespace AbilityKit.Demo.Moba.Services.StateSync
{
    public interface IMobaStateRecoveryProvider
    {
        int Key { get; }
        string Name { get; }
        byte[] ExportState(FrameIndex frame);
        void ImportState(FrameIndex frame, byte[] payload);
        void AddStateHash(FrameIndex frame, MobaStateHashBuilder hash);
    }

    public readonly struct MobaStateRecoverySnapshot
    {
        [BinaryMember(0)] public readonly int Version;
        [BinaryMember(1)] public readonly int Frame;
        [BinaryMember(2)] public readonly MobaStateRecoveryEntry[] Entries;

        public MobaStateRecoverySnapshot(int version, int frame, MobaStateRecoveryEntry[] entries)
        {
            Version = version;
            Frame = frame;
            Entries = entries ?? Array.Empty<MobaStateRecoveryEntry>();
        }
    }

    public readonly struct MobaStateRecoveryEntry
    {
        [BinaryMember(0)] public readonly int Key;
        [BinaryMember(1)] public readonly byte[] Payload;

        public MobaStateRecoveryEntry(int key, byte[] payload)
        {
            Key = key;
            Payload = payload ?? Array.Empty<byte>();
        }
    }
}
