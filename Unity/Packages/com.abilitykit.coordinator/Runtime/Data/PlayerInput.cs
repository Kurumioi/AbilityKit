using System;

namespace AbilityKit.Coordinator
{
    /// <summary>
    /// Player input command
    /// </summary>
    public struct PlayerInput
    {
        /// <summary>
        /// Input frame
        /// </summary>
        public int Frame;

        /// <summary>
        /// Player identifier
        /// </summary>
        public int PlayerId;

        /// <summary>
        /// Operation code
        /// </summary>
        public int OpCode;

        /// <summary>
        /// Serialized payload data
        /// </summary>
        public byte[] Payload;

        public PlayerInput(int frame, int playerId, int opCode, byte[] payload)
        {
            Frame = frame;
            PlayerId = playerId;
            OpCode = opCode;
            Payload = payload;
        }

        public static PlayerInput CreateMove(int frame, int playerId, float x, float z)
        {
            var payload = new byte[sizeof(float) * 2];
            BitConverter.GetBytes(x).CopyTo(payload, 0);
            BitConverter.GetBytes(z).CopyTo(payload, sizeof(float));
            return new PlayerInput(frame, playerId, InputOpCodes.Move, payload);
        }

        public static PlayerInput CreateSkill(int frame, int playerId, int slot, float x, float z)
        {
            var payload = new byte[sizeof(int) + sizeof(float) * 2];
            BitConverter.GetBytes(slot).CopyTo(payload, 0);
            BitConverter.GetBytes(x).CopyTo(payload, sizeof(int));
            BitConverter.GetBytes(z).CopyTo(payload, sizeof(int) + sizeof(float));
            return new PlayerInput(frame, playerId, InputOpCodes.Skill, payload);
        }

        public static PlayerInput CreateStop(int frame, int playerId)
        {
            return new PlayerInput(frame, playerId, InputOpCodes.Stop, null);
        }

        public bool TryGetMoveTarget(out float x, out float z)
        {
            x = z = 0;
            if (OpCode != InputOpCodes.Move || Payload == null || Payload.Length < sizeof(float) * 2)
                return false;
            x = BitConverter.ToSingle(Payload, 0);
            z = BitConverter.ToSingle(Payload, sizeof(float));
            return true;
        }

        public bool TryGetSkillTarget(out int slot, out float x, out float z)
        {
            slot = 0;
            x = z = 0;
            if (OpCode != InputOpCodes.Skill || Payload == null || Payload.Length < sizeof(int) + sizeof(float) * 2)
                return false;
            slot = BitConverter.ToInt32(Payload, 0);
            x = BitConverter.ToSingle(Payload, sizeof(int));
            z = BitConverter.ToSingle(Payload, sizeof(int) + sizeof(float));
            return true;
        }
    }

    /// <summary>
    /// Standard input operation codes
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
