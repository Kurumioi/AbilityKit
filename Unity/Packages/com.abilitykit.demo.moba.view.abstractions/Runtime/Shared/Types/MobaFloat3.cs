using System;

namespace AbilityKit.Demo.Moba.View.Abstractions.Shared.Types
{
    public readonly struct MobaFloat3 : IEquatable<MobaFloat3>
    {
        public MobaFloat3(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public float X { get; }
        public float Y { get; }
        public float Z { get; }

        public static MobaFloat3 Zero => new MobaFloat3(0f, 0f, 0f);

        public bool Equals(MobaFloat3 other)
        {
            return X.Equals(other.X) && Y.Equals(other.Y) && Z.Equals(other.Z);
        }

        public override bool Equals(object obj)
        {
            return obj is MobaFloat3 other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(X, Y, Z);
        }

        public override string ToString()
        {
            return $"({X}, {Y}, {Z})";
        }
    }
}
