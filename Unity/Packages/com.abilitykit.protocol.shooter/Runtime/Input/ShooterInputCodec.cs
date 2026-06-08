using System;
using AbilityKit.Protocol.Serialization;
using MemoryPack;

namespace AbilityKit.Protocol.Shooter
{
    [MemoryPackable]
    public partial struct ShooterPlayerCommand
    {
        [MemoryPackOrder(0)] public int PlayerId;
        [MemoryPackOrder(1)] public float MoveX;
        [MemoryPackOrder(2)] public float MoveY;
        [MemoryPackOrder(3)] public float AimX;
        [MemoryPackOrder(4)] public float AimY;
        [MemoryPackOrder(5)] public bool Fire;

        public ShooterPlayerCommand(int playerId, float moveX, float moveY, float aimX, float aimY, bool fire)
        {
            PlayerId = playerId;
            MoveX = moveX;
            MoveY = moveY;
            AimX = aimX;
            AimY = aimY;
            Fire = fire;
        }
    }

    [MemoryPackable]
    public partial struct ShooterInputPayload
    {
        [MemoryPackOrder(0)] public ShooterPlayerCommand[] Commands;

        [MemoryPackConstructor]
        public ShooterInputPayload(ShooterPlayerCommand[] commands)
        {
            Commands = commands;
        }
    }

    public static class ShooterInputCodec
    {
        public static byte[] Serialize(ShooterPlayerCommand[] commands)
        {
            commands ??= Array.Empty<ShooterPlayerCommand>();
            var payload = new ShooterInputPayload(commands);
            return WireSerializer.Serialize(in payload);
        }

        public static ShooterPlayerCommand[] Deserialize(byte[] payload)
        {
            if (payload == null || payload.Length == 0)
            {
                return Array.Empty<ShooterPlayerCommand>();
            }

            var value = WireSerializer.Deserialize<ShooterInputPayload>(payload);
            return value.Commands ?? Array.Empty<ShooterPlayerCommand>();
        }
    }
}
