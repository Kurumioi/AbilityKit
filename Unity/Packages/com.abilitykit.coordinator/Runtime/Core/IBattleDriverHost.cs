namespace AbilityKit.Coordinator
{
    /// <summary>
    /// Battle Driver Host Interface
    /// 
    /// Design:
    /// - Provides access to battle driver and services
    /// - Used by sync adapters to interact with battle logic
    /// - Abstracts the specific BattleDriver implementation
    /// </summary>
    public interface IBattleDriverHost
    {
        /// <summary>
        /// Current frame number
        /// </summary>
        int CurrentFrame { get; }

        /// <summary>
        /// Logic time in seconds
        /// </summary>
        double LogicTimeSeconds { get; }

        /// <summary>
        /// Is the driver running
        /// </summary>
        bool IsRunning { get; }

        /// <summary>
        /// Submit inputs for processing
        /// </summary>
        void SubmitInputs(PlayerInput[] inputs);

        /// <summary>
        /// Get all entity states for rendering
        /// </summary>
        EntityState[] GetAllEntityStates();
    }
}
