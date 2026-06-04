namespace AbilityKit.Coordinator
{
    /// <summary>
    /// Generic logic-world driver bridge used by sync adapters.
    /// </summary>
    public interface ILogicWorldDriverBridge
    {
        /// <summary>
        /// Current logic frame number.
        /// </summary>
        int CurrentFrame { get; }

        /// <summary>
        /// Logic time in seconds.
        /// </summary>
        double LogicTimeSeconds { get; }

        /// <summary>
        /// Is the driver running.
        /// </summary>
        bool IsRunning { get; }

        /// <summary>
        /// Start the driver lifecycle.
        /// </summary>
        void Start();

        /// <summary>
        /// Stop the driver lifecycle.
        /// </summary>
        void Stop();

        /// <summary>
        /// Submit inputs for processing.
        /// </summary>
        void SubmitInputs(PlayerInput[] inputs);

        /// <summary>
        /// Advance one logic frame.
        /// </summary>
        void AdvanceFrame(float deltaTime);

        /// <summary>
        /// Get all entity states for rendering or state sync.
        /// </summary>
        SnapshotEntityState[] GetAllEntityStates();
    }
}
