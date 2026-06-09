using System;

namespace AbilityKit.Demo.Shooter.Runtime.Tests;

internal static class TestByteSegments
{
    public static ArraySegment<byte> Copy(ArraySegment<byte> payload)
    {
        if (payload.Array == null || payload.Count == 0)
        {
            return new ArraySegment<byte>(Array.Empty<byte>());
        }

        var bytes = new byte[payload.Count];
        Buffer.BlockCopy(payload.Array, payload.Offset, bytes, 0, payload.Count);
        return new ArraySegment<byte>(bytes);
    }
}
