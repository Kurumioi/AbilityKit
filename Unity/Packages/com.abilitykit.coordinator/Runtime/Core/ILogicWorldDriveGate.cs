namespace AbilityKit.Coordinator
{
    /// <summary>
    /// Gameplay-level gate that decides whether a logic world may advance.
    /// </summary>
    public interface ILogicWorldDriveGate
    {
        bool CanDriveLogicWorld(float deltaTime);
    }
}
