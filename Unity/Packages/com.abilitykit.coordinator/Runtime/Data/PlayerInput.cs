using System;
using MemoryPack;

namespace AbilityKit.Coordinator
{
    /// <summary>
    /// 玩家输入命令。
    /// </summary>
    public struct PlayerInput
    {
        /// <summary>
        /// 输入帧。
        /// </summary>
        public int Frame;

        /// <summary>
        /// 玩家标识。
        /// </summary>
        public int PlayerId;

        /// <summary>
        /// 操作码。
        /// </summary>
        public int OpCode;

        /// <summary>
        /// 序列化后的载荷数据。
        /// </summary>
        public byte[] Payload;

        public PlayerInput(int frame, int playerId, int opCode, byte[] payload)
        {
            Frame = frame;
            PlayerId = playerId;
            OpCode = opCode;
            Payload = payload;
        }

        public static PlayerInput Create<TPayload>(int frame, int playerId, in TPayload payload)
        {
            var opCode = CoordinatorPayloadCodec.GetOpCode<TPayload>();
            return Create(frame, playerId, opCode, in payload);
        }

        public static PlayerInput Create<TPayload>(int frame, int playerId, int opCode, in TPayload payload)
        {
            return new PlayerInput(frame, playerId, opCode, CoordinatorPayloadCodec.Encode(in payload));
        }

        public static PlayerInput CreateMove(int frame, int playerId, float x, float z)
        {
            var payload = new MoveInputPayload(x, z);
            return Create(frame, playerId, in payload);
        }

        public static PlayerInput CreateSkill(int frame, int playerId, int slot, float x, float z)
        {
            var payload = new SkillInputPayload(slot, x, z);
            return Create(frame, playerId, in payload);
        }

        public static PlayerInput CreateStop(int frame, int playerId)
        {
            return new PlayerInput(frame, playerId, InputOpCodes.Stop, Array.Empty<byte>());
        }

        public bool TryGetPayload<TPayload>(out TPayload payload)
        {
            payload = default;
            if (CoordinatorPayloadCodec.TryGetOpCode<TPayload>(out var opCode) && OpCode != opCode)
            {
                return false;
            }

            return CoordinatorPayloadCodec.TryDecode(Payload, out payload);
        }

        public bool TryGetMoveTarget(out float x, out float z)
        {
            x = z = 0;
            if (!TryGetPayload<MoveInputPayload>(out var payload))
            {
                return false;
            }

            x = payload.X;
            z = payload.Z;
            return true;
        }

        public bool TryGetSkillTarget(out int slot, out float x, out float z)
        {
            slot = 0;
            x = z = 0;
            if (!TryGetPayload<SkillInputPayload>(out var payload))
            {
                return false;
            }

            slot = payload.Slot;
            x = payload.X;
            z = payload.Z;
            return true;
        }
    }

    [MemoryPackable]
    [CoordinatorPayload(InputOpCodes.Move)]
    public readonly partial struct MoveInputPayload
    {
        [MemoryPackOrder(0)] public readonly float X;
        [MemoryPackOrder(1)] public readonly float Z;

        public MoveInputPayload(float x, float z)
        {
            X = x;
            Z = z;
        }
    }

    [MemoryPackable]
    [CoordinatorPayload(InputOpCodes.Skill)]
    public readonly partial struct SkillInputPayload
    {
        [MemoryPackOrder(0)] public readonly int Slot;
        [MemoryPackOrder(1)] public readonly float X;
        [MemoryPackOrder(2)] public readonly float Z;

        public SkillInputPayload(int slot, float x, float z)
        {
            Slot = slot;
            X = x;
            Z = z;
        }
    }

    /// <summary>
    /// 标准输入操作码。
    /// </summary>
    public static class InputOpCodes
    {
        public const int Move = 1001;
        public const int Skill = 1002;
        public const int Stop = 1003;
        public const int UseItem = 1004;
        public const int Ping = 1005;
    }
}
