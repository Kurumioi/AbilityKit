using System;

namespace AbilityKit.Demo.Moba.View.Abstractions.Shared.Types
{
    public readonly struct MobaColor32 : IEquatable<MobaColor32>
    {
        public MobaColor32(byte r, byte g, byte b, byte a)
        {
            R = r;
            G = g;
            B = b;
            A = a;
        }

        public byte R { get; }
        public byte G { get; }
        public byte B { get; }
        public byte A { get; }

        public static MobaColor32 Transparent => new MobaColor32(0, 0, 0, 0);
        public static MobaColor32 White => new MobaColor32(255, 255, 255, 255);
        public static MobaColor32 Black => new MobaColor32(0, 0, 0, 255);

        public bool Equals(MobaColor32 other)
        {
            return R == other.R && G == other.G && B == other.B && A == other.A;
        }

        public override bool Equals(object obj)
        {
            return obj is MobaColor32 other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(R, G, B, A);
        }

        public override string ToString()
        {
            return $"({R}, {G}, {B}, {A})";
        }
    }
}
