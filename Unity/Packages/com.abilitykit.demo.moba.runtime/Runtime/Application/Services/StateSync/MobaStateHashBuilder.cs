using System;

namespace AbilityKit.Demo.Moba.Services.StateSync
{
    public struct MobaStateHashBuilder
    {
        private uint _hash;

        public MobaStateHashBuilder(uint seed)
        {
            _hash = seed == 0u ? 2166136261u : seed;
        }

        public uint Value => _hash;

        public void AddBool(bool value)
        {
            AddByte(value ? (byte)1 : (byte)0);
        }

        public void AddByte(byte value)
        {
            _hash ^= value;
            _hash *= 16777619u;
        }

        public void AddInt(int value)
        {
            unchecked
            {
                AddUInt((uint)value);
            }
        }

        public void AddLong(long value)
        {
            unchecked
            {
                AddUInt((uint)value);
                AddUInt((uint)(value >> 32));
            }
        }

        public void AddUInt(uint value)
        {
            AddByte((byte)(value & 0xFF));
            AddByte((byte)((value >> 8) & 0xFF));
            AddByte((byte)((value >> 16) & 0xFF));
            AddByte((byte)((value >> 24) & 0xFF));
        }

        public void AddFloat(float value)
        {
            AddInt(BitConverter.SingleToInt32Bits(value));
        }
    }
}
