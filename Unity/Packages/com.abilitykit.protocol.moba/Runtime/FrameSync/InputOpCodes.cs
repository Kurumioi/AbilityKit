namespace AbilityKit.Protocol.Moba.FrameSync
{
    /// <summary>
    /// 帧同步输入 OpCode 定义
    /// 定义客户端可以发送的输入类型
    /// 与服务端保持一致
    /// </summary>
    public static class InputOpCodes
    {
        // ========== 基础操作 ==========
        public const int Ready = 3001;
        public const int Unready = 3002;
        public const int Move = 3003;
        public const int Stop = 3005;
        public const int Attack = 3004;

        // ========== 技能操作 ==========
        public const int Skill1 = 3011;
        public const int Skill2 = 3012;
        public const int Skill3 = 3013;
        public const int SkillInput = 3020;

        // ========== 快照 OpCode (客户端接收) ==========
        public const int LobbySnapshot = 4001;
        public const int EnterGameSnapshot = 4002;
        public const int ActorTransformSnapshot = 4003;
        public const int StateHashSnapshot = 4004;
        public const int ActorSpawnSnapshot = 4005;
        public const int ProjectileEventSnapshot = 4006;
        public const int DamageEventSnapshot = 4007;
        public const int ActorDespawnSnapshot = 4008;
        public const int AreaEventSnapshot = 4009;
    }
}
