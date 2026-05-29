using System;
using AbilityKit.Ability.Host;
using AbilityKit.Demo.Moba.Share;

namespace ET.Logic
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class RuntimeSnapshotConverterAttribute : Attribute
    {
        public int OpCode { get; }

        public RuntimeSnapshotConverterAttribute(int opCode)
        {
            OpCode = opCode;
        }
    }

    public interface IRuntimeSnapshotConverter
    {
        int OpCode { get; }
        bool TryConvert(in WorldStateSnapshot snapshot, int frameIndex, double timestamp, out FrameSnapshotData frameSnapshot);
    }
}
