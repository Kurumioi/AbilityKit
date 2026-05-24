namespace ET.Logic
{
    /// <summary>
    /// ET Battle Session Hooks
    ///
    /// Design:
    /// - Provides event hooks for session lifecycle
    /// - Used by SubFeatures to subscribe to events
    /// - Extends Coordinator's SessionHooks with ET-specific events
    /// </summary>
    [ComponentOf(typeof(Scene))]
    public class ETBattleSessionHooks : Entity, IAwake, IDestroy
    {
        // ============== Session Lifecycle Hooks ==============

        /// <summary>
        /// Called before each tick
        /// </summary>
        public System.Action<float> OnPreTick { get; set; }

        /// <summary>
        /// Called after each tick
        /// </summary>
        public System.Action<float> OnPostTick { get; set; }

        /// <summary>
        /// Called when first frame is received
        /// </summary>
        public System.Action OnFirstFrameReceived { get; set; }

        /// <summary>
        /// Called when session starts
        /// </summary>
        public System.Action OnSessionStarted { get; set; }

        /// <summary>
        /// Called when session fails
        /// </summary>
        public System.Action<System.Exception> OnSessionFailed { get; set; }

        /// <summary>
        /// Called when session is about to start
        /// </summary>
        public System.Action OnSessionStarting { get; set; }

        /// <summary>
        /// Called when session is stopping
        /// </summary>
        public System.Action OnSessionStopping { get; set; }

        public void Awake()
        {
        }

        public void Destroy()
        {
            OnPreTick = null;
            OnPostTick = null;
            OnFirstFrameReceived = null;
            OnSessionStarted = null;
            OnSessionFailed = null;
            OnSessionStarting = null;
            OnSessionStopping = null;
        }
    }
}
