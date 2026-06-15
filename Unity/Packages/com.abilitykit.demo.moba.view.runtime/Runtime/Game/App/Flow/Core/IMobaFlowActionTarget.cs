namespace AbilityKit.Game.Flow
{
    /// <summary>
    /// Decouples <c>MobaFlowActionExecutor</c> and <c>MobaFlowSwitchExecutor</c>
    /// from the concrete flow domain so that dispatch logic can be tested in isolation.
    /// </summary>
    internal interface IMobaFlowActionTarget
    {
        void ResetBattleSessionRuntimeState();
        void ReturnLobbyAfterBattleEnd();

        void TryAdvanceOnConnectEnter();
        void TryAdvanceOnCreateOrJoinWorldEnter();
        void TryAdvanceOnLoadAssetsEnter();
    }
}
