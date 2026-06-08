using System;
using AbilityKit.Protocol.Serialization;
using MemoryPack;

namespace AbilityKit.Protocol.Shooter
{
    [MemoryPackable]
    public partial struct ShooterPlayerSnapshot
    {
        [MemoryPackOrder(0)] public int PlayerId;
        [MemoryPackOrder(1)] public float X;
        [MemoryPackOrder(2)] public float Y;
        [MemoryPackOrder(3)] public float AimX;
        [MemoryPackOrder(4)] public float AimY;
        [MemoryPackOrder(5)] public int Hp;
        [MemoryPackOrder(6)] public int Score;
        [MemoryPackOrder(7)] public bool Alive;

        public ShooterPlayerSnapshot(int playerId, float x, float y, float aimX, float aimY, int hp, int score, bool alive)
        {
            PlayerId = playerId;
            X = x;
            Y = y;
            AimX = aimX;
            AimY = aimY;
            Hp = hp;
            Score = score;
            Alive = alive;
        }
    }

    [MemoryPackable]
    public partial struct ShooterBulletSnapshot
    {
        [MemoryPackOrder(0)] public int BulletId;
        [MemoryPackOrder(1)] public int OwnerPlayerId;
        [MemoryPackOrder(2)] public float X;
        [MemoryPackOrder(3)] public float Y;
        [MemoryPackOrder(4)] public float VelocityX;
        [MemoryPackOrder(5)] public float VelocityY;
        [MemoryPackOrder(6)] public int RemainingFrames;

        public ShooterBulletSnapshot(int bulletId, int ownerPlayerId, float x, float y, float velocityX, float velocityY, int remainingFrames)
        {
            BulletId = bulletId;
            OwnerPlayerId = ownerPlayerId;
            X = x;
            Y = y;
            VelocityX = velocityX;
            VelocityY = velocityY;
            RemainingFrames = remainingFrames;
        }
    }

    [MemoryPackable]
    public partial struct ShooterEventSnapshot
    {
        [MemoryPackOrder(0)] public int EventType;
        [MemoryPackOrder(1)] public int SourcePlayerId;
        [MemoryPackOrder(2)] public int TargetPlayerId;
        [MemoryPackOrder(3)] public int BulletId;
        [MemoryPackOrder(4)] public float X;
        [MemoryPackOrder(5)] public float Y;
        [MemoryPackOrder(6)] public int Value;

        public ShooterEventSnapshot(int eventType, int sourcePlayerId, int targetPlayerId, int bulletId, float x, float y, int value)
        {
            EventType = eventType;
            SourcePlayerId = sourcePlayerId;
            TargetPlayerId = targetPlayerId;
            BulletId = bulletId;
            X = x;
            Y = y;
            Value = value;
        }
    }

    [MemoryPackable]
    public partial struct ShooterStateSnapshotPayload
    {
        [MemoryPackOrder(0)] public int Frame;
        [MemoryPackOrder(1)] public ShooterPlayerSnapshot[] Players;
        [MemoryPackOrder(2)] public ShooterBulletSnapshot[] Bullets;
        [MemoryPackOrder(3)] public ShooterEventSnapshot[] Events;

        [MemoryPackConstructor]
        public ShooterStateSnapshotPayload(int frame, ShooterPlayerSnapshot[] players, ShooterBulletSnapshot[] bullets, ShooterEventSnapshot[] events)
        {
            Frame = frame;
            Players = players;
            Bullets = bullets;
            Events = events;
        }
    }

    public static class ShooterStateSnapshotCodec
    {
        public static byte[] Serialize(in ShooterStateSnapshotPayload snapshot)
        {
            return WireSerializer.Serialize(in snapshot);
        }

        public static ShooterStateSnapshotPayload Deserialize(byte[] payload)
        {
            if (payload == null || payload.Length == 0)
            {
                return new ShooterStateSnapshotPayload(0, Array.Empty<ShooterPlayerSnapshot>(), Array.Empty<ShooterBulletSnapshot>(), Array.Empty<ShooterEventSnapshot>());
            }

            var value = WireSerializer.Deserialize<ShooterStateSnapshotPayload>(payload);
            return new ShooterStateSnapshotPayload(
                value.Frame,
                value.Players ?? Array.Empty<ShooterPlayerSnapshot>(),
                value.Bullets ?? Array.Empty<ShooterBulletSnapshot>(),
                value.Events ?? Array.Empty<ShooterEventSnapshot>());
        }
    }
}
