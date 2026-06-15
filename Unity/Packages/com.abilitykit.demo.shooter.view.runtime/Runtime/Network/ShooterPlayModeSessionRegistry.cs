#nullable enable

using System;
using System.Collections.Generic;

namespace AbilityKit.Demo.Shooter.View.Network
{
    /// <summary>
    /// Runtime bridge that publishes the currently running Play-mode Shooter session host so the
    /// Editor window (Play Mode Attach channel) can discover it without holding a direct reference.
    /// <para>
    /// The formal Play-mode host registers a lightweight published endpoint during runtime
    /// initialization. The Editor window polls <see cref="Active"/> to decide whether a live session
    /// is available to attach to, then hot-tunes it through <see cref="ShooterNetworkConditionRegistry"/>.
    /// </para>
    /// </summary>
    public static class ShooterPlayModeSessionRegistry
    {
        private static readonly List<IShooterPlayModeSessionHost> _hosts = new();

        /// <summary>Raised whenever the set of registered hosts changes.</summary>
        public static event Action? HostsChanged;

        /// <summary>The most recently registered active host, or null when none is running.</summary>
        public static IShooterPlayModeSessionHost? Active
        {
            get
            {
                for (var i = _hosts.Count - 1; i >= 0; i--)
                {
                    if (_hosts[i].IsRunning)
                    {
                        return _hosts[i];
                    }
                }

                return _hosts.Count > 0 ? _hosts[_hosts.Count - 1] : null;
            }
        }

        /// <summary>All currently registered hosts.</summary>
        public static IReadOnlyList<IShooterPlayModeSessionHost> All => _hosts;

        public static void Register(IShooterPlayModeSessionHost host)
        {
            if (host == null) throw new ArgumentNullException(nameof(host));
            if (!_hosts.Contains(host))
            {
                _hosts.Add(host);
                HostsChanged?.Invoke();
            }
        }

        public static void Unregister(IShooterPlayModeSessionHost host)
        {
            if (_hosts.Remove(host))
            {
                HostsChanged?.Invoke();
            }
        }

        public static void NotifyHostsChanged()
        {
            HostsChanged?.Invoke();
        }
    }

    /// <summary>
    /// Surface a Play-mode session host exposes to the Editor window so the window can observe the
    /// live session (diagnostics) and apply network changes without referencing concrete runtime
    /// implementation details.
    /// </summary>
    public interface IShooterPlayModeSessionHost
    {
        /// <summary>True when a session is currently assembled and stepping.</summary>
        bool IsRunning { get; }

        /// <summary>Human-readable label for the host (scene/object name).</summary>
        string DisplayName { get; }

        /// <summary>The live acceptance session, or null before start / after stop.</summary>
        ShooterAcceptanceSession? Session { get; }

        /// <summary>
        /// Requests the host to tear down its live session and release any host-specific
        /// platform wiring (e.g. Unity PlayerLoop / input / network hooks). After this
        /// returns, <see cref="IsRunning"/> is false and <see cref="Session"/> is null.
        /// </summary>
        /// <remarks>
        /// Callers (e.g. the Editor window) should use this instead of referencing a
        /// concrete host implementation, so the window stays decoupled from whichever
        /// host is currently active.
        /// </remarks>
        void Stop();
    }
}
