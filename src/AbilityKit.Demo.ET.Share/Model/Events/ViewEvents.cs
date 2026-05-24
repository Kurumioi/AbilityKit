namespace ET.AbilityKit.Demo.ET.Share
{
    /// <summary>
    /// View binder ready event
    /// </summary>
    public struct ViewBinderReadyEvent
    {
        public int Frame;
    }

    /// <summary>
    /// Views rebound event
    /// </summary>
    public struct ViewsReboundEvent
    {
        public int Frame;
    }

    /// <summary>
    /// View frame aligned event
    /// </summary>
    public struct ViewFrameAlignedEvent
    {
        public int Frame;
        public long BattleId;
    }
}
