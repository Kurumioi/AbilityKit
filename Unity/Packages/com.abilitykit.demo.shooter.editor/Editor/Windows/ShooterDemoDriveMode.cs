#nullable enable

namespace AbilityKit.Demo.Shooter.Editor.Windows
{
    /// <summary>
    /// Determines how the <see cref="ShooterDemoWindow"/> drives and observes a session.
    /// </summary>
    public enum ShooterDemoDriveMode
    {
        /// <summary>
        /// The window owns the session and pumps it via <c>EditorApplication.update</c>,
        /// rendering entities through the SceneView gizmo sink. Best for quick acceptance.
        /// </summary>
        EditorDirect = 0,

        /// <summary>
        /// The window attaches to a live Play-mode session published through
        /// <c>ShooterPlayModeSessionRegistry</c>. It does not pump logic itself; instead it
        /// observes diagnostics and hot-tunes network conditions via the shared registry.
        /// </summary>
        PlayModeAttach = 1,
    }
}
