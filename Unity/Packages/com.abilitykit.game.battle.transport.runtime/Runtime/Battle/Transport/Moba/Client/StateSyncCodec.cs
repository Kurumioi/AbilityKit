using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AbilityKit.Game.Battle.Transport.Moba.Client
{
    /// <summary>
    /// StateSync 快照编解码器
    /// 使用简化实现，替代对 world.statesync 包的依赖
    /// </summary>
    public static class StateSyncCodec
    {
        public static uint SnapshotPushedOpCode => StateSyncOpCodes.SnapshotPushed;

        public static byte[] EncodeWorldSnapshot(IWorldSnapshot snapshot)
        {
            using var ms = new MemoryStream();
            using var writer = new BinaryWriter(ms);

            writer.Write(snapshot.WorldId);
            writer.Write(snapshot.Frame);
            writer.Write(snapshot.Timestamp);
            writer.Write(snapshot.IsFullSnapshot);

            var actors = snapshot.Actors;
            writer.Write(actors.Count);

            foreach (var actor in actors)
            {
                writer.Write(actor.ActorId);
                writer.Write(actor.PositionX);
                writer.Write(actor.PositionY);
                writer.Write(actor.PositionZ);
                writer.Write(actor.Rotation);
                writer.Write(actor.VelocityX);
                writer.Write(actor.VelocityZ);
                writer.Write(actor.Hp);
                writer.Write(actor.HpMax);
                writer.Write(actor.TeamId);
            }

            return ms.ToArray();
        }

        public static WorldSnapshot DecodeWorldSnapshot(byte[] data)
        {
            if (data == null || data.Length < 21)
            {
                return new WorldSnapshot();
            }

            using var ms = new MemoryStream(data);
            using var reader = new BinaryReader(ms);

            var snapshot = new WorldSnapshot
            {
                WorldId = reader.ReadUInt64(),
                Frame = reader.ReadInt32(),
                Timestamp = reader.ReadInt64(),
                IsFullSnapshot = reader.ReadBoolean(),
                Actors = new List<IActorSnapshot>()
            };

            var actorCount = reader.ReadInt32();
            for (int i = 0; i < actorCount; i++)
            {
                snapshot.Actors.Add(new ActorSnapshot
                {
                    ActorId = reader.ReadInt32(),
                    PositionX = reader.ReadSingle(),
                    PositionY = reader.ReadSingle(),
                    PositionZ = reader.ReadSingle(),
                    Rotation = reader.ReadSingle(),
                    VelocityX = reader.ReadSingle(),
                    VelocityZ = reader.ReadSingle(),
                    Hp = reader.ReadSingle(),
                    HpMax = reader.ReadSingle(),
                    TeamId = reader.ReadInt32()
                });
            }

            return snapshot;
        }

        public static byte[] EncodeFrameInput(string roomId, ulong worldId, int frame,
            IEnumerable<IFrameInput> inputs)
        {
            using var ms = new MemoryStream();
            using var writer = new BinaryWriter(ms);

            writer.Write(roomId ?? string.Empty);
            writer.Write(worldId);
            writer.Write(frame);

            var inputList = new List<IFrameInput>(inputs);
            writer.Write(inputList.Count);

            foreach (var input in inputList)
            {
                writer.Write(input.PlayerId);
                writer.Write(input.OpCode);
                var payload = input.Payload ?? Array.Empty<byte>();
                writer.Write(payload.Length);
                if (payload.Length > 0) writer.Write(payload);
            }

            return ms.ToArray();
        }

        public static FrameData DecodeFrameData(byte[] payload)
        {
            var frameData = new FrameData();
            var inputs = new List<IFrameInput>();

            if (payload == null || payload.Length < 16) return frameData;

            using var ms = new MemoryStream(payload);
            using var reader = new BinaryReader(ms);

            var roomId = reader.ReadString();
            var worldId = reader.ReadUInt64();
            var frame = reader.ReadInt32();

            var inputCount = reader.ReadInt32();
            for (int i = 0; i < inputCount; i++)
            {
                var playerId = reader.ReadUInt32();
                var opCode = reader.ReadUInt32();
                var payloadLen = reader.ReadInt32();
                var inputPayload = payloadLen > 0 ? reader.ReadBytes(payloadLen) : Array.Empty<byte>();

                inputs.Add(new FrameInput
                {
                    PlayerId = playerId,
                    OpCode = opCode,
                    Payload = inputPayload
                });
            }

            return new FrameData
            {
                Frame = frame,
                Inputs = inputs
            };
        }
    }
}
