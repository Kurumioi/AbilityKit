namespace AbilityKit.Demo.Host.Console;

/// <summary>
/// Logical output channels shared by demo console hosts.
/// </summary>
public enum ConsoleOutputChannel
{
    /// <summary>General system messages.</summary>
    System,

    /// <summary>Lifecycle and phase messages.</summary>
    Phase,

    /// <summary>View and presentation messages.</summary>
    View,

    /// <summary>Input messages.</summary>
    Input,

    /// <summary>Battle simulation messages.</summary>
    Battle,

    /// <summary>Synchronization messages.</summary>
    Sync,

    /// <summary>Configuration messages.</summary>
    Config,

    /// <summary>Debug messages.</summary>
    Debug,

    /// <summary>Warning messages.</summary>
    Warning,

    /// <summary>Error messages.</summary>
    Error,

    /// <summary>Trace messages.</summary>
    Trace,
}
