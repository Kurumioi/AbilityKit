#nullable enable

using System;
using System.Collections.Generic;

namespace AbilityKit.Demo.Shooter.View.Network
{
    /// <summary>
    /// Host-neutral runtime bridge that publishes the currently running Shooter session host so
    /// tooling can discover and observe it without referencing a concrete platform host.
    /// </summary>
    public static class ShooterHostSessionRegistry
    {
        private static readonly List<IShooterSessionHost> _hosts = new();

        /// <summary>Raised whenever the set of registered hosts changes.</summary>
        public static event Action? HostsChanged;

        /// <summary>The most recently registered active host, or null when none is running.</summary>
        public static IShooterSessionHost? Active
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
        public static IReadOnlyList<IShooterSessionHost> All => _hosts;

        public static void Register(IShooterSessionHost host)
        {
            if (host == null) throw new ArgumentNullException(nameof(host));
            if (!_hosts.Contains(host))
            {
                _hosts.Add(host);
                NotifyHostsChanged();
            }
        }

        public static void Unregister(IShooterSessionHost host)
        {
            if (_hosts.Remove(host))
            {
                NotifyHostsChanged();
            }
        }

        public static void NotifyHostsChanged()
        {
            HostsChanged?.Invoke();
        }
    }

    /// <summary>
    /// Surface a session host exposes to tools so they can observe diagnostics and request teardown
    /// without referencing concrete runtime or platform implementation details.
    /// </summary>
    public interface IShooterSessionHost
    {
        /// <summary>True when a session is currently assembled and stepping.</summary>
        bool IsRunning { get; }

        /// <summary>Human-readable label for the host (scene/object name).</summary>
        string DisplayName { get; }

        /// <summary>The live acceptance session, or null before start / after stop.</summary>
        ShooterAcceptanceSession? Session { get; }

        /// <summary>
        /// Requests the host to tear down its live session and release any host-specific
        /// platform wiring (e.g. PlayerLoop / input / network hooks).
        /// </summary>
        void Stop();
    }

    /// <summary>
    /// Backward-compatible Play-mode registry facade. New tooling should use
    /// <see cref="ShooterHostSessionRegistry"/>.
    /// </summary>
    public static class ShooterPlayModeSessionRegistry
    {
        /// <summary>Raised whenever the set of registered hosts changes.</summary>
        public static event Action? HostsChanged
        {
            add => ShooterHostSessionRegistry.HostsChanged += value;
            remove => ShooterHostSessionRegistry.HostsChanged -= value;
        }

        /// <summary>The most recently registered active host, or null when none is running.</summary>
        public static IShooterPlayModeSessionHost? Active => ShooterHostSessionRegistry.Active as IShooterPlayModeSessionHost;

        /// <summary>All currently registered Play-mode hosts.</summary>
        public static IReadOnlyList<IShooterPlayModeSessionHost> All
        {
            get
            {
                var hosts = ShooterHostSessionRegistry.All;
                var result = new List<IShooterPlayModeSessionHost>(hosts.Count);
                for (var i = 0; i < hosts.Count; i++)
                {
                    if (hosts[i] is IShooterPlayModeSessionHost playModeHost)
                    {
                        result.Add(playModeHost);
                    }
                }

                return result;
            }
        }

        public static void Register(IShooterPlayModeSessionHost host)
        {
            ShooterHostSessionRegistry.Register(host);
        }

        public static void Unregister(IShooterPlayModeSessionHost host)
        {
            ShooterHostSessionRegistry.Unregister(host);
        }

        public static void NotifyHostsChanged()
        {
            ShooterHostSessionRegistry.NotifyHostsChanged();
        }
    }

    /// <summary>
    /// Backward-compatible Play-mode host contract. New hosts should implement
    /// <see cref="IShooterSessionHost"/> directly unless they need legacy Editor integration.
    /// </summary>
    public interface IShooterPlayModeSessionHost : IShooterSessionHost
    {
    }
}
