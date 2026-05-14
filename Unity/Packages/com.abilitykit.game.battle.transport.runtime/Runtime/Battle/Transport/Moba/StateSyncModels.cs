using System;
using System.Collections.Generic;
using System.IO;

namespace AbilityKit.Game.Battle.Transport.Moba
{
}
/// <summary>
/// 客户端 StateSync 快照通知
/// </summary>
public sealed class ClientStateSyncNotification
{
    public ulong WorldId { get; set; }
    public int Frame { get; set; }
    public long Timestamp { get; set; }
    public List<ClientActorStateInfo> Actors { get; set; } = new();
    public bool IsFullSnapshot { get; set; } = true;
}

/// <summary>
/// 客户端 Actor 状态信息
/// </summary>
public sealed class ClientActorStateInfo
{
    public int ActorId { get; set; }
    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }
    public float Rotation { get; set; }
    public float VelocityX { get; set; }
    public float VelocityZ { get; set; }
    public float Hp { get; set; }
    public float HpMax { get; set; }
    public int TeamId { get; set; }
}

/// <summary>
/// 客户端 StateSync 快照解码器
/// 与服务端 StateSyncPayloadCodec 对应
/// </summary>
public static class ClientStateSyncPayloadCodec
{
    public static ClientStateSyncNotification Deserialize(byte[] data)
    {
        if (data == null || data.Length < 21)
        {
            return new ClientStateSyncNotification();
        }

        using var ms = new MemoryStream(data);
        using var reader = new BinaryReader(ms);

        var notification = new ClientStateSyncNotification
        {
            WorldId = reader.ReadUInt64(),
            Frame = reader.ReadInt32(),
            Timestamp = reader.ReadInt64(),
            IsFullSnapshot = data.Length >= 22 && reader.ReadByte() == 1,
            Actors = new List<ClientActorStateInfo>()
        };

        var actorCount = reader.ReadInt32();
        for (int i = 0; i < actorCount; i++)
        {
            notification.Actors.Add(new ClientActorStateInfo
            {
                ActorId = reader.ReadInt32(),
                X = reader.ReadSingle(),
                Y = reader.ReadSingle(),
                Z = reader.ReadSingle(),
                Rotation = reader.ReadSingle(),
                VelocityX = reader.ReadSingle(),
                VelocityZ = reader.ReadSingle(),
                Hp = reader.ReadSingle(),
                HpMax = reader.ReadSingle(),
                TeamId = reader.ReadInt32()
            });
        }

        return notification;
    }
}
