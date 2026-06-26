using System;

namespace AbilityKit.Demo.Moba.View.Abstractions.Shared.Types
{
    public readonly struct MobaQuaternion4 : IEquatable<MobaQuaternion4>
    {
        public MobaQuaternion4(float x, float y, float z, float w)
        {
            X = x;
            Y = y;
            Z = z;
            W = w;
        }

        public float X { get; }
        public float Y { get; }
        public float Z { get; }
        public float W { get; }

        public static MobaQuaternion4 Identity => new MobaQuaternion4(0f, 0f, 0f, 1f);

        public bool Equals(MobaQuaternion4 other)
        {
            return X.Equals(other.X) && Y.Equals(other.Y) && Z.Equals(other.Z) && W.Equals(other.W);
        }

        public override bool Equals(object obj)
        {
            return obj is MobaQuaternion4 other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(X, Y, Z, W);
        }

        public override string ToString()
        {
            return $"({X}, {Y}, {Z}, {W})";
        }
    }
}
