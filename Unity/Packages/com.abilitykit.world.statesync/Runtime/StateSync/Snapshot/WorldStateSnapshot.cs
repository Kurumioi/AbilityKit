using System;
using System.Collections.Generic;
using MemoryPack;

namespace AbilityKit.Ability.StateSync.Snapshot
{
    [MemoryPackable]
    public partial struct EntityStateSnapshot
    {
        public long EntityId;
        public Vec3 Position;
        public Quat Rotation;
        public Vec3 Velocity;
        public byte HealthPercent;
        public uint StateFlags;
        public long ActiveAbilityMask;
        public Dictionary<int, float> Cooldowns;
        public Dictionary<int, float> BuffTimers;
        public int TeamId;
        public byte ControlFlags;

        public EntityStateSnapshot(long entityId)
        {
            EntityId = entityId;
            Position = Vec3.Zero;
            Rotation = Quat.Identity;
            Velocity = Vec3.Zero;
            HealthPercent = 100;
            StateFlags = 0;
            ActiveAbilityMask = 0;
            Cooldowns = new Dictionary<int, float>();
            BuffTimers = new Dictionary<int, float>();
            TeamId = 0;
            ControlFlags = 0;
        }

        public bool HasStateFlag(uint flag) => (StateFlags & flag) != 0;
        public bool HasAbility(long abilityMask) => (ActiveAbilityMask & abilityMask) != 0;
        public bool IsAlive => HealthPercent > 0;
        public bool IsImmobile => (ControlFlags & (byte)EntityControlFlags.Immobile) != 0;
        public bool IsStunned => (ControlFlags & (byte)EntityControlFlags.Stunned) != 0;
        public bool IsInvulnerable => (ControlFlags & (byte)EntityControlFlags.Invulnerable) != 0;
    }

    [Flags]
    public enum EntityControlFlags : byte
    {
        None = 0,
        Immobile = 1 << 0,
        Stunned = 1 << 1,
        Invulnerable = 1 << 2,
        Silenced = 1 << 3,
        Disarmed = 1 << 4,
        Rooted = 1 << 5,
        Feared = 1 << 6,
        Sleeping = 1 << 7,
    }

    [MemoryPackable]
    public partial struct Vec3
    {
        public float X, Y, Z;

        public Vec3(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public static readonly Vec3 Zero = new Vec3(0f, 0f, 0f);
        public static readonly Vec3 One = new Vec3(1f, 1f, 1f);
        public static readonly Vec3 Up = new Vec3(0f, 1f, 0f);

        public float MagnitudeSquared() => X * X + Y * Y + Z * Z;
        public float Magnitude() => (float)Math.Sqrt(MagnitudeSquared());

        public static Vec3 operator +(Vec3 a, Vec3 b) => new Vec3(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        public static Vec3 operator -(Vec3 a, Vec3 b) => new Vec3(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
        public static Vec3 operator *(Vec3 a, float s) => new Vec3(a.X * s, a.Y * s, a.Z * s);
        public static Vec3 operator /(Vec3 a, float s) => new Vec3(a.X / s, a.Y / s, a.Z / s);

        public bool ApproximatelyEquals(Vec3 other, float epsilon = 0.0001f)
        {
            return Math.Abs(X - other.X) < epsilon &&
                   Math.Abs(Y - other.Y) < epsilon &&
                   Math.Abs(Z - other.Z) < epsilon;
        }
    }

    [MemoryPackable]
    public partial struct Quat
    {
        public float X, Y, Z, W;

        public Quat(float x, float y, float z, float w)
        {
            X = x;
            Y = y;
            Z = z;
            W = w;
        }

        public static readonly Quat Identity = new Quat(0f, 0f, 0f, 1f);

        public bool ApproximatelyEquals(Quat other, float epsilon = 0.0001f)
        {
            return Math.Abs(X - other.X) < epsilon &&
                   Math.Abs(Y - other.Y) < epsilon &&
                   Math.Abs(Z - other.Z) < epsilon &&
                   Math.Abs(W - other.W) < epsilon;
        }
    }

    [MemoryPackable]
    public partial struct ProjectileStateSnapshot
    {
        public long ProjectileId;
        public long OwnerId;
        public Vec3 StartPosition;
        public Vec3 CurrentPosition;
        public Vec3 Direction;
        public float Speed;
        public float RemainingLifetime;
        public int ConfigId;
        public byte State;
    }

    [MemoryPackable]
    public partial struct AbilityStateSnapshot
    {
        public long EntityId;
        public int AbilityId;
        public byte BehaviorState;
        public float ElapsedMs;
        public float CooldownRemaining;
        public bool IsActive;
        public byte[] EffectData;
    }

    [MemoryPackable]
    public partial class WorldStateSnapshot
    {
        public ulong WorldId { get; set; }
        public int Version { get; set; }
        public int Frame { get; set; }
        public long Timestamp { get; set; }
        public List<EntityStateSnapshot> Entities { get; set; }
        public List<ProjectileStateSnapshot> Projectiles { get; set; }
        public List<AbilityStateSnapshot> Abilities { get; set; }
        public uint WorldFlags { get; set; }
        public int ActiveTriggerCount { get; set; }
        public bool IsFullSnapshot { get; set; } = true;

        public WorldStateSnapshot()
        {
            Version = CurrentVersion;
            Entities = new List<EntityStateSnapshot>();
            Projectiles = new List<ProjectileStateSnapshot>();
            Abilities = new List<AbilityStateSnapshot>();
        }

        public const int CurrentVersion = 1;

        public static byte[] Serialize(WorldStateSnapshot snapshot)
        {
            return MemoryPackSerializer.Serialize(snapshot);
        }

        public static WorldStateSnapshot Deserialize(byte[] data)
        {
            return MemoryPackSerializer.Deserialize<WorldStateSnapshot>(data);
        }

        public static WorldStateSnapshot FromBytes(byte[] data)
        {
            return Deserialize(data);
        }

        public byte[] ToBytes()
        {
            return Serialize(this);
        }

        public StateHash ComputeHash()
        {
            return StateHashComputer.Compute(this);
        }

        public WorldStateSnapshot Clone()
        {
            var bytes = ToBytes();
            var clone = FromBytes(bytes);
            return clone;
        }
    }
}
