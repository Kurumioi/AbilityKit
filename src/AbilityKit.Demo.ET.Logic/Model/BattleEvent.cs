namespace ET.Logic
{
    /// <summary>
    /// 战斗事件
    /// 用于表现层事件通信
    /// </summary>
    public class BattleEvent
    {
        public string EventType { get; set; }
        public int Frame { get; set; }
        public double Timestamp { get; set; }
        public long ActorId { get; set; }
        public long TargetId { get; set; }
        public float Value { get; set; }
        public string Data { get; set; }
    }
}
