namespace ET.Logic
{
    /// <summary>
    /// 玩家注册数据
    /// </summary>
    public struct PlayerRegistration
    {
        public int PlayerId;
        public int CharacterId;
        public string PlayerName;
        public int TeamId;
        public bool IsReady;
        public long RegisterTime;
    }
}
