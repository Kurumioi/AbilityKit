namespace AbilityKit.Demo.Moba.Events.Summon
{
    /// <summary>
    /// 召唤物事件负载
    /// </summary>
    public sealed class SummonEventPayload
    {
        /// <summary>召唤物 ActorId</summary>
        public int SummonActorId;

        /// <summary>召唤物 ID</summary>
        public int SummonId;

        /// <summary>召唤者 ActorId</summary>
        public int OwnerActorId;

        /// <summary>根召唤者 ActorId</summary>
        public int RootOwnerActorId;

        /// <summary>原因</summary>
        public int Reason;
    }
}
