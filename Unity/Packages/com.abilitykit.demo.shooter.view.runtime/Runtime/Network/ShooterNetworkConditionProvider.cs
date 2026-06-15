#nullable enable

using System;
using System.Collections.Generic;
using AbilityKit.Network.Runtime.Conditioning;

namespace AbilityKit.Demo.Shooter.View.Network
{
    /// <summary>
    /// Abstraction for network condition sources. Both the Editor window (Editor-direct mode) and
    /// the Play-mode session host use this to obtain the current <see cref="NetworkConditionProfile"/>
    /// without knowing whether the values come from built-in sliders, a preset catalog, or an external
    /// tool (e.g. Clumsy, Network Link Conditioner, or a custom packet-loss injector).
    /// <para>
    /// Extension: implement this interface to plug in software-based network conditioning
    /// (OS-level packet loss, proxy-based latency injection, etc.) alongside the built-in
    /// parameter sliders. Subscribers pick up changes via <see cref="ProfileChanged"/>
    /// and forward them to <see cref="ShooterAcceptanceSession.ApplyNetwork"/>.
    /// </para>
    /// <para>
    /// This type lives in the runtime assembly (View.Runtime, no platform restriction) so that
    /// Play-mode runtime code can register and read providers. The Editor window references the
    /// same types so a single registry serves both the Editor-direct and Play-mode-attach channels.
    /// </para>
    /// </summary>
    public interface IShooterNetworkConditionProvider : IDisposable
    {
        /// <summary>Human-readable label for display in the Editor window.</summary>
        string DisplayName { get; }

        /// <summary>The current network condition profile.</summary>
        NetworkConditionProfile Profile { get; }

        /// <summary>
        /// True when this provider is actively controlling network conditions
        /// (e.g. an external tool is running). When false, the built-in parameter
        /// sliders are used instead.
        /// </summary>
        bool IsActive { get; }

        /// <summary>
        /// Raised when the profile changes (either from external tool feedback
        /// or from internal parameter changes). Subscribers forward changes to
        /// the running session.
        /// </summary>
        event Action<NetworkConditionProfile>? ProfileChanged;
    }

    /// <summary>
    /// Built-in provider driven by slider values. This is the default provider
    /// used when no external tool is connected.
    /// </summary>
    public sealed class ShooterBuiltinNetworkConditionProvider : IShooterNetworkConditionProvider
    {
        private NetworkConditionProfile _profile;

        public ShooterBuiltinNetworkConditionProvider(NetworkConditionProfile initialProfile)
        {
            _profile = initialProfile;
        }

        public string DisplayName => "Built-in (Parameters)";

        public NetworkConditionProfile Profile => _profile;

        public bool IsActive => true;

        public event Action<NetworkConditionProfile>? ProfileChanged;

        /// <summary>
        /// Updates the profile from slider values and notifies subscribers.
        /// Called by the Editor window when the user adjusts network parameter sliders.
        /// </summary>
        public void ApplyProfile(NetworkConditionProfile profile)
        {
            _profile = profile;
            ProfileChanged?.Invoke(_profile);
        }

        /// <summary>
        /// Applies a preset profile and notifies subscribers.
        /// </summary>
        public void ApplyPreset(NetworkConditionProfile profile, string presetName)
        {
            _profile = profile;
            ProfileChanged?.Invoke(_profile);
        }

        public void Dispose()
        {
            ProfileChanged = null;
        }
    }

    /// <summary>
    /// Registry of <see cref="IShooterNetworkConditionProvider"/> instances. The Editor window
    /// queries this to populate the network source dropdown. External tools register themselves
    /// here on enable and unregister on disable. The Play-mode session host subscribes so a
    /// running match is hot-tuned through the same registry the Editor writes to.
    /// <para>
    /// Extension point: to add a new network conditioning source (e.g. Clumsy integration,
    /// custom packet-loss middleware), implement <see cref="IShooterNetworkConditionProvider"/>
    /// and call <see cref="Register"/> from your tool's startup code.
    /// </para>
    /// </summary>
    public static class ShooterNetworkConditionRegistry
    {
        private static readonly List<IShooterNetworkConditionProvider> _providers = new();
        private static readonly ShooterBuiltinNetworkConditionProvider _builtin =
            new(NetworkConditionProfile.Ideal);

        /// <summary>The built-in slider-based provider, always available.</summary>
        public static ShooterBuiltinNetworkConditionProvider Builtin => _builtin;

        /// <summary>All registered providers including the built-in one.</summary>
        public static IReadOnlyList<IShooterNetworkConditionProvider> All
        {
            get
            {
                var result = new List<IShooterNetworkConditionProvider>(_providers.Count + 1) { _builtin };
                result.AddRange(_providers);
                return result;
            }
        }

        /// <summary>Registers an external network condition provider.</summary>
        public static void Register(IShooterNetworkConditionProvider provider)
        {
            if (provider == null) throw new ArgumentNullException(nameof(provider));
            if (!_providers.Contains(provider))
            {
                _providers.Add(provider);
            }
        }

        /// <summary>Unregisters an external network condition provider.</summary>
        public static void Unregister(IShooterNetworkConditionProvider provider)
        {
            _providers.Remove(provider);
        }
    }
}
