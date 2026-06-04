using System;
using System.Collections.Generic;
using AbilityKit.Protocol.Moba.Generated.GatewayFrameSync;
using AbilityKit.Protocol.Moba.StateSync;
using StateSyncOpCodes = AbilityKit.Protocol.Moba.StateSync.OpCodes;

namespace AbilityKit.Game.Battle.Transport.Moba.Client
{
    /// <summary>
    /// StateSync 快照编解码器。
    /// </summary>
    public static class StateSyncCodec
    {
        public static uint SnapshotPushedOpCode => StateSyncOpCodes.SnapshotPushed;

        public static byte[] EncodeWorldSnapshot(IWorldSnapshot snapshot)
        {
            if (snapshot == null)
            {
                var empty = new MobaWorldSnapshotPayload(0, 0, 0, false, Array.Empty<MobaActorSnapshotEntry>());
                return MobaWorldSnapshotCodec.Serialize(in empty);
            }

            var actors = snapshot.Actors;
            var entries = actors == null || actors.Count == 0
                ? Array.Empty<MobaActorSnapshotEntry>()
                : new MobaActorSnapshotEntry[actors.Count];

            if (actors != null)
            {
                for (int i = 0; i < actors.Count; i++)
                {
                    entries[i] = ToProtocolEntry(actors[i]);
                }
            }

            var payload = new MobaWorldSnapshotPayload(
                snapshot.WorldId,
                snapshot.Frame,
                snapshot.Timestamp,
                snapshot.IsFullSnapshot,
                entries);

            return MobaWorldSnapshotCodec.Serialize(in payload);
        }

        public static WorldSnapshot DecodeWorldSnapshot(byte[] data)
        {
            var payload = MobaWorldSnapshotCodec.Deserialize(data);
            var actors = payload.Actors == null || payload.Actors.Length == 0
                ? new List<IActorSnapshot>()
                : new List<IActorSnapshot>(payload.Actors.Length);

            if (payload.Actors != null)
            {
                for (int i = 0; i < payload.Actors.Length; i++)
                {
                    actors.Add(ToActorSnapshot(in payload.Actors[i]));
                }
            }

            return new WorldSnapshot
            {
                WorldId = payload.WorldId,
                Frame = payload.Frame,
                Timestamp = payload.Timestamp,
                IsFullSnapshot = payload.IsFullSnapshot,
                Actors = actors
            };
        }

        public static byte[] EncodeFrameInput(string roomId, ulong worldId, int frame, IEnumerable<IFrameInput> inputs)
        {
            var inputItems = ToWireInputs(inputs);
            var push = new WireFramePushedPush(ParseRoomId(roomId), worldId, frame, inputItems);
            var bytes = WireCustomBinary.Serialize(in push);
            return bytes.Array == null || bytes.Offset != 0 || bytes.Count != bytes.Array.Length
                ? CopySegment(bytes)
                : bytes.Array;
        }

        public static FrameData DecodeFrameData(byte[] payload)
        {
            if (payload == null || payload.Length == 0)
            {
                return new FrameData();
            }

            var push = WireCustomBinary.DeserializeFramePushedPush(payload);
            var inputs = push.Inputs == null || push.Inputs.Length == 0
                ? new List<IFrameInput>()
                : new List<IFrameInput>(push.Inputs.Length);

            if (push.Inputs != null)
            {
                for (int i = 0; i < push.Inputs.Length; i++)
                {
                    var input = push.Inputs[i];
                    inputs.Add(new FrameInput
                    {
                        PlayerId = input.PlayerId,
                        OpCode = unchecked((uint)input.OpCode),
                        Payload = input.Payload ?? Array.Empty<byte>()
                    });
                }
            }

            return new FrameData
            {
                Frame = push.Frame,
                Inputs = inputs
            };
        }

        private static MobaActorSnapshotEntry ToProtocolEntry(IActorSnapshot actor)
        {
            if (actor == null)
            {
                return default;
            }

            return new MobaActorSnapshotEntry(
                actor.ActorId,
                actor.PositionX,
                actor.PositionY,
                actor.PositionZ,
                actor.Rotation,
                actor.VelocityX,
                actor.VelocityZ,
                actor.Hp,
                actor.HpMax,
                actor.TeamId);
        }

        private static ActorSnapshot ToActorSnapshot(in MobaActorSnapshotEntry actor)
        {
            return new ActorSnapshot
            {
                ActorId = actor.ActorId,
                PositionX = actor.PositionX,
                PositionY = actor.PositionY,
                PositionZ = actor.PositionZ,
                Rotation = actor.Rotation,
                VelocityX = actor.VelocityX,
                VelocityZ = actor.VelocityZ,
                Hp = actor.Hp,
                HpMax = actor.HpMax,
                TeamId = actor.TeamId
            };
        }

        private static WireInputItem[] ToWireInputs(IEnumerable<IFrameInput> inputs)
        {
            if (inputs == null)
            {
                return Array.Empty<WireInputItem>();
            }

            var items = new List<WireInputItem>();
            foreach (var input in inputs)
            {
                if (input == null)
                {
                    continue;
                }

                items.Add(new WireInputItem(input.PlayerId, unchecked((int)input.OpCode), input.Payload ?? Array.Empty<byte>()));
            }

            return items.Count == 0 ? Array.Empty<WireInputItem>() : items.ToArray();
        }

        private static ulong ParseRoomId(string roomId)
        {
            return ulong.TryParse(roomId, out var parsed) ? parsed : 0UL;
        }

        private static byte[] CopySegment(ArraySegment<byte> segment)
        {
            if (segment.Array == null || segment.Count <= 0)
            {
                return Array.Empty<byte>();
            }

            var bytes = new byte[segment.Count];
            Buffer.BlockCopy(segment.Array, segment.Offset, bytes, 0, segment.Count);
            return bytes;
        }
    }
}
