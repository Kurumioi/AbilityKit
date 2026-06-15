namespace AbilityKit.Network.Runtime.Sync
{
    public static class NetworkSyncProfiles
    {
        public static NetworkSyncProfile Unspecified { get; } = new NetworkSyncProfile(
            NetworkSyncModel.Unspecified,
            ClientPlaybackPolicy.None,
            InputPolicy.None,
            SnapshotPolicy.None,
            InterestPolicy.None,
            RecoveryPolicy.None,
            ServerValidationPolicy.None);

        public static NetworkSyncProfile Lockstep { get; } = new NetworkSyncProfile(
            NetworkSyncModel.Lockstep,
            ClientPlaybackPolicy.None,
            InputPolicy.DeterministicBroadcast,
            SnapshotPolicy.None,
            InterestPolicy.AllEntities,
            RecoveryPolicy.None,
            ServerValidationPolicy.ClientHashAudit);

        public static NetworkSyncProfile PredictRollback { get; } = new NetworkSyncProfile(
            NetworkSyncModel.PredictRollback,
            ClientPlaybackPolicy.PredictRollback,
            InputPolicy.ImmediateSubmit | InputPolicy.ServerRemapAcceptedFrame,
            SnapshotPolicy.FullSnapshot | SnapshotPolicy.AuthorityOverride,
            InterestPolicy.AllEntities,
            RecoveryPolicy.CatchUpToServerFrame | RecoveryPolicy.RequestFullSnapshot,
            ServerValidationPolicy.AuthoritativeOnly);

        public static NetworkSyncProfile AuthoritativeInterpolation { get; } = new NetworkSyncProfile(
            NetworkSyncModel.AuthoritativeInterpolation,
            ClientPlaybackPolicy.AuthoritativeInterpolation,
            InputPolicy.NoClientInput,
            SnapshotPolicy.FixedRateStateStream,
            InterestPolicy.AllEntities,
            RecoveryPolicy.RequestKeyFrame | RecoveryPolicy.RequestFullSnapshot,
            ServerValidationPolicy.AuthoritativeOnly);

        public static NetworkSyncProfile BatchStateSync { get; } = new NetworkSyncProfile(
            NetworkSyncModel.BatchStateSync,
            ClientPlaybackPolicy.AuthoritativeInterpolation,
            InputPolicy.NoClientInput,
            SnapshotPolicy.BatchSnapshot | SnapshotPolicy.KeyFrameSnapshot,
            InterestPolicy.AllEntities,
            RecoveryPolicy.RequestKeyFrame | RecoveryPolicy.RequestFullSnapshot,
            ServerValidationPolicy.AuthoritativeOnly);

        public static NetworkSyncProfile MassBattleLodSync { get; } = new NetworkSyncProfile(
            NetworkSyncModel.MassBattleLodSync,
            ClientPlaybackPolicy.AuthoritativeInterpolation,
            InputPolicy.ImmediateSubmit,
            SnapshotPolicy.BatchSnapshot | SnapshotPolicy.KeyFrameSnapshot | SnapshotPolicy.EventStream,
            InterestPolicy.DistanceAoi | InterestPolicy.TeamOrFactionAoi | InterestPolicy.PriorityBudget | InterestPolicy.LodFrequency,
            RecoveryPolicy.RequestAoiSlice,
            ServerValidationPolicy.AuthoritativeOnly | ServerValidationPolicy.InputValidation);

        public static NetworkSyncProfile HybridHeroPrediction { get; } = new NetworkSyncProfile(
            NetworkSyncModel.HybridHeroPrediction,
            ClientPlaybackPolicy.HybridLocalPredictRemoteInterpolate,
            InputPolicy.ImmediateSubmit | InputPolicy.ServerRemapAcceptedFrame,
            SnapshotPolicy.FullSnapshot | SnapshotPolicy.FixedRateStateStream | SnapshotPolicy.BatchSnapshot | SnapshotPolicy.EventStream,
            InterestPolicy.OwnerRelevant | InterestPolicy.DistanceAoi | InterestPolicy.PriorityBudget,
            RecoveryPolicy.RequestFullSnapshot | RecoveryPolicy.RequestKeyFrame | RecoveryPolicy.RequestAoiSlice,
            ServerValidationPolicy.AuthoritativeOnly | ServerValidationPolicy.InputValidation);

        public static NetworkSyncProfile FastReconnect { get; } = new NetworkSyncProfile(
            NetworkSyncModel.FastReconnect,
            ClientPlaybackPolicy.None,
            InputPolicy.None,
            SnapshotPolicy.FullSnapshot | SnapshotPolicy.KeyFrameSnapshot,
            InterestPolicy.AllEntities,
            RecoveryPolicy.ReconnectResume | RecoveryPolicy.RequestFullSnapshot,
            ServerValidationPolicy.AuthoritativeOnly);

        public static NetworkSyncProfile ServerRewindLagCompensation { get; } = new NetworkSyncProfile(
            NetworkSyncModel.ServerRewindLagCompensation,
            ClientPlaybackPolicy.None,
            InputPolicy.ImmediateSubmit | InputPolicy.ServerRemapAcceptedFrame,
            SnapshotPolicy.None,
            InterestPolicy.None,
            RecoveryPolicy.None,
            ServerValidationPolicy.LagCompensatedHitValidation);

        /// <summary>
        /// Resolves the canonical profile for a legacy compatibility model. The mapping is now owned
        /// by <see cref="NetworkSyncProfileRegistry"/> (audit migration step 6 enum convergence), so
        /// the model→profile relationship lives in exactly one place instead of being duplicated here
        /// as a hand-written switch. This method is retained as the established call site.
        /// </summary>
        public static NetworkSyncProfile FromCompatibilityModel(NetworkSyncModel compatibilityModel)
        {
            return NetworkSyncProfileRegistry.Resolve(compatibilityModel);
        }
    }
}
