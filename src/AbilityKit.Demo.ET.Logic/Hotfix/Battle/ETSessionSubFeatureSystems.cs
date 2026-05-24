using System;

namespace ET.Logic
{
    /// <summary>
    /// Session SubFeature Systems
    ///
    /// These are stub implementations to maintain compatibility.
    /// The actual logic should be handled by Coordinator's SessionCoordinator.
    /// </summary>
    public static partial class ETSessionSubFeatureSystems
    {
        public static void OnSessionStarting(this ETSessionLifecycleSubFeature self)
        {
        }

        public static void OnSessionStopping(this ETSessionLifecycleSubFeature self)
        {
        }
    }
}
