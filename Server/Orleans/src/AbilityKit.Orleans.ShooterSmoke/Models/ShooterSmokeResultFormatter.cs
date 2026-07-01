internal static class ShooterSmokeResultFormatter
{
    public static string FormatPassed(ShooterSmokeResult result)
    {
        return "Shooter TCP Gateway smoke passed. " +
            $"RoomId={result.RoomId}, " +
            $"BattleId={result.BattleId}, " +
            $"WorldId={result.WorldId}, " +
            $"TargetFrame={result.TargetFrame}, " +
            $"Inputs={result.InputCount}, " +
            $"LastInput={result.LastInputStatus}@{result.LastAcceptedFrame}/{result.LastCurrentFrame}, " +
            $"ServerTicks={result.LastServerTicks}, " +
            $"ShouldResync={result.ShouldResync}, " +
            $"Frame={result.Frame}, " +
            $"Players={result.ActorCount}, " +
            $"StateHash={result.StateHash}, " +
            $"Snapshot={result.SnapshotApplyResult}@{result.SnapshotFrame}, " +
            $"SnapshotServerTicks={result.SnapshotServerTicks}, " +
            $"SnapshotHash={result.SnapshotStateHash}, " +
            $"SnapshotEntities={result.SnapshotEntityCount}, " +
            $"StaleSnapshot={result.StaleSnapshotResult}, " +
            $"ProjectionApplies={result.ProjectionApplyCount}, " +
            $"ProjectionFullSyncs={result.ProjectionFullSyncApplyCount}, " +
            $"ProjectionAdded={result.ProjectionAddedEntities}, " +
            $"ProjectionRemoved={result.ProjectionRemovedEntities}, " +
            $"ProjectionEntities={result.ProjectionFinalEntityCount}, " +
            $"ProjectionPlayers={result.ProjectionFinalPlayerCount}, " +
            $"ProjectionBullets={result.ProjectionFinalBulletCount}, " +
            $"LateJoinEntry={result.LateJoinEntryKind}, " +
            $"LateJoinTargetFrame={result.LateJoinTargetFrame}, " +
            $"LateJoinProjectionFullSyncs={result.LateJoinProjectionFullSyncApplyCount}, " +
            $"LateJoinProjectionAdded={result.LateJoinProjectionAddedEntities}, " +
            $"LateJoinProjectionEntities={result.LateJoinProjectionFinalEntityCount}, " +
            $"LateJoinProjectionPlayers={result.LateJoinProjectionFinalPlayerCount}, " +
            $"LateJoinProjectionBullets={result.LateJoinProjectionFinalBulletCount}, " +
            $"ReconnectEntry={result.ReconnectEntryKind}, " +
            $"ReconnectTargetFrame={result.ReconnectTargetFrame}, " +
            $"ReconnectProjectionFullSyncs={result.ReconnectProjectionFullSyncApplyCount}, " +
            $"ReconnectProjectionAdded={result.ReconnectProjectionAddedEntities}, " +
            $"ReconnectProjectionEntities={result.ReconnectProjectionFinalEntityCount}, " +
            $"ReconnectProjectionPlayers={result.ReconnectProjectionFinalPlayerCount}, " +
            $"ReconnectProjectionBullets={result.ReconnectProjectionFinalBulletCount}, " +
            $"GameplayStarted={result.GameplayStartFrame}, " +
            $"GameplayFinalFrame={result.GameplayFinalFrame}, " +
            $"GameplayFinalState={result.GameplayFinalMatchState}, " +
            $"GameplayFinal={result.GameplayMatchFinal}, " +
            $"GameplayVictory={result.GameplayMatchVictory}, " +
            $"GameplayCompletedFrame={result.GameplayMatchCompletedFrame}, " +
            $"GameplayDefeatedEnemies={result.GameplayDefeatedEnemies}, " +
            $"GameplayVictoryTarget={result.GameplayVictoryTargetDefeats}, " +
            $"GameplayRemainingTime={result.GameplayRemainingTimeFrames}, " +
            $"GameplayMoved={result.GameplayMoved}, " +
            $"GameplayFired={result.GameplayFired}, " +
            $"GameplayDefeatedEnemy={result.GameplayDefeatedEnemy}, " +
            $"InputLogicReplayPath=\"{Escape(result.InputLogicReplayPath)}\", " +
            $"MinimizedInputLogicReplayPath=\"{Escape(result.MinimizedInputLogicReplayPath)}\", " +
            $"InputLogicReplayConsumed={result.InputLogicReplayValidation.Consumed}, " +
            $"InputLogicReplayInputs={result.InputLogicReplayValidation.InputCount}, " +
            $"InputLogicReplaySnapshots={result.InputLogicReplayValidation.SnapshotCount}, " +
            $"InputLogicReplayHashes={result.InputLogicReplayValidation.StateHashCount}, " +
            $"InputLogicReplayFrame={result.InputLogicReplayValidation.ReplayFrame}, " +
            $"InputLogicReplayStateHash={result.InputLogicReplayValidation.ReplayStateHash}, " +
            $"InputLogicReplayRoundTripMatched={result.InputLogicReplayValidation.ReplayRoundTripMatched}, " +
            $"InputLogicReplayFirstFrame={result.InputLogicReplayValidation.Summary.FirstFrame}, " +
            $"InputLogicReplayLastFrame={result.InputLogicReplayValidation.Summary.LastFrame}, " +
            $"InputLogicReplayOpCodes=\"{Escape(result.InputLogicReplayValidation.Summary.InputOpCodeDistribution)}\", " +
            $"InputLogicReplaySnapshotOpCodes=\"{Escape(result.InputLogicReplayValidation.Summary.SnapshotOpCodeDistribution)}\", " +
            $"InputLogicReplayPureStateSnapshots={result.InputLogicReplayValidation.Summary.PureStateRelatedSnapshotCount}, " +
            $"InputLogicReplayPackedStateSnapshots={result.InputLogicReplayValidation.Summary.PackedStateRelatedSnapshotCount}, " +
            $"ServerFrameReplayPath=\"{Escape(result.InputLogicReplayPath)}\", " +
            $"MinimizedServerFrameReplayPath=\"{Escape(result.MinimizedInputLogicReplayPath)}\", " +
            $"ServerFrameReplayConsumed={result.InputLogicReplayValidation.Consumed}, " +
            $"ServerFrameReplayInputs={result.InputLogicReplayValidation.InputCount}, " +
            $"ServerFrameReplaySnapshots={result.InputLogicReplayValidation.SnapshotCount}, " +
            $"ServerFrameReplayHashes={result.InputLogicReplayValidation.StateHashCount}, " +
            $"ServerFrameReplayFrame={result.InputLogicReplayValidation.ReplayFrame}, " +
            $"ServerFrameReplayStateHash={result.InputLogicReplayValidation.ReplayStateHash}, " +
            $"ServerFrameReplayRoundTripMatched={result.InputLogicReplayValidation.ReplayRoundTripMatched}, " +
            $"ServerFrameReplayFirstFrame={result.InputLogicReplayValidation.Summary.FirstFrame}, " +
            $"ServerFrameReplayLastFrame={result.InputLogicReplayValidation.Summary.LastFrame}, " +
            $"ServerFrameReplayOpCodes=\"{Escape(result.InputLogicReplayValidation.Summary.InputOpCodeDistribution)}\", " +
            $"ServerFrameReplaySnapshotOpCodes=\"{Escape(result.InputLogicReplayValidation.Summary.SnapshotOpCodeDistribution)}\", " +
            $"ServerFrameReplayPureStateSnapshots={result.InputLogicReplayValidation.Summary.PureStateRelatedSnapshotCount}, " +
            $"ServerFrameReplayPackedStateSnapshots={result.InputLogicReplayValidation.Summary.PackedStateRelatedSnapshotCount}";
    }

    private static string Escape(string value) => (value ?? string.Empty).Replace("\\", "\\\\").Replace("\"", "\\\"");
}
