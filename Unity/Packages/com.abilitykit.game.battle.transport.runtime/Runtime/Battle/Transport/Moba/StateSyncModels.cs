using System.Collections.Generic;
using AbilityKit.Protocol.Moba.StateSync;

namespace AbilityKit.Game.Battle.Transport.Moba
{
    /// <summary>
    /// 客户端 StateSync 快照通知。
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
    /// 客户端 Actor 状态信息。
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
    /// 客户端 StateSync 快照解码器。
    /// </summary>
    public static class ClientStateSyncPayloadCodec
    {
        public static ClientStateSyncNotification Deserialize(byte[] data)
        {
            var payload = MobaWorldSnapshotCodec.Deserialize(data);
            var actors = payload.Actors == null || payload.Actors.Length == 0
                ? new List<ClientActorStateInfo>()
                : new List<ClientActorStateInfo>(payload.Actors.Length);

            if (payload.Actors != null)
            {
                for (int i = 0; i < payload.Actors.Length; i++)
                {
                    var actor = payload.Actors[i];
                    actors.Add(new ClientActorStateInfo
                    {
                        ActorId = actor.ActorId,
                        X = actor.PositionX,
                        Y = actor.PositionY,
                        Z = actor.PositionZ,
                        Rotation = actor.Rotation,
                        VelocityX = actor.VelocityX,
                        VelocityZ = actor.VelocityZ,
                        Hp = actor.Hp,
                        HpMax = actor.HpMax,
                        TeamId = actor.TeamId
                    });
                }
            }

            return new ClientStateSyncNotification
            {
                WorldId = payload.WorldId,
                Frame = payload.Frame,
                Timestamp = payload.Timestamp,
                IsFullSnapshot = payload.IsFullSnapshot,
                Actors = actors
            };
        }
    }
}
