using System;
using System.Collections.Generic;

namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit { }
}

namespace AbilityKit.Game.Battle.Transport.Moba.Client
{
    /// <summary>
    /// 实体快照实现
    /// </summary>
    public sealed class ActorSnapshot : IActorSnapshot
    {
        public int ActorId { get; init; }
        public float PositionX { get; init; }
        public float PositionY { get; init; }
        public float PositionZ { get; init; }
        public float Rotation { get; init; }
        public float VelocityX { get; init; }
        public float VelocityZ { get; init; }
        public float Hp { get; init; }
        public float HpMax { get; init; }
        public int TeamId { get; init; }

        public static ActorSnapshot FromInterface(IActorSnapshot other) => new()
        {
            ActorId = other.ActorId,
            PositionX = other.PositionX,
            PositionY = other.PositionY,
            PositionZ = other.PositionZ,
            Rotation = other.Rotation,
            VelocityX = other.VelocityX,
            VelocityZ = other.VelocityZ,
            Hp = other.Hp,
            HpMax = other.HpMax,
            TeamId = other.TeamId
        };
    }

    /// <summary>
    /// 世界快照实现
    /// </summary>
    public sealed class WorldSnapshot : IWorldSnapshot
    {
        public ulong WorldId { get; init; }
        public int Frame { get; init; }
        public long Timestamp { get; init; }
        public bool IsFullSnapshot { get; init; }
        public List<IActorSnapshot> Actors { get; init; } = new();

        IReadOnlyList<IActorSnapshot> IWorldSnapshot.Actors => Actors;
    }

    /// <summary>
    /// 帧输入项实现
    /// </summary>
    public sealed class FrameInput : IFrameInput
    {
        public uint PlayerId { get; init; }
        public uint OpCode { get; init; }
        public byte[] Payload { get; init; } = Array.Empty<byte>();
    }

    /// <summary>
    /// 帧数据实现
    /// </summary>
    public sealed class FrameData : IFrameData
    {
        public int Frame { get; init; }
        public List<IFrameInput> Inputs { get; init; } = new();

        IReadOnlyList<IFrameInput> IFrameData.Inputs => Inputs;
    }
}
