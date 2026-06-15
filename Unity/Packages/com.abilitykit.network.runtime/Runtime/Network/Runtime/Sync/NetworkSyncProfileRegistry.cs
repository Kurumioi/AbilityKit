using System;
using System.Collections.Generic;

namespace AbilityKit.Network.Runtime.Sync
{
    /// <summary>
    /// Single source of truth for the audit migration step 6 "enum convergence" decision:
    /// <see cref="NetworkSyncModel"/> is kept as a backward-compatible alias key, while the actual
    /// capability description lives in <see cref="NetworkSyncProfile"/>. This registry is the one
    /// place that knows every known model, its canonical profile and its stable display name, so
    /// the mapping no longer has to be maintained both as a hand-written switch and as a set of
    /// static properties.
    /// </summary>
    /// <remarks>
    /// The registry is gameplay- and protocol-agnostic: it only references frame/tick/policy level
    /// concepts. Game layers continue to pass a <see cref="NetworkSyncModel"/> through legacy APIs;
    /// new logic should read the resolved <see cref="NetworkSyncProfile"/> policy fields instead of
    /// switching on the alias.
    /// </remarks>
    public static class NetworkSyncProfileRegistry
    {
        private readonly struct Entry
        {
            public Entry(NetworkSyncModel model, string name, NetworkSyncProfile profile)
            {
                Model = model;
                Name = name;
                Profile = profile;
            }

            public NetworkSyncModel Model { get; }

            public string Name { get; }

            public NetworkSyncProfile Profile { get; }
        }

        // Ordered by the NetworkSyncModel enum value so callers that enumerate get a stable order.
        private static readonly Entry[] Entries =
        {
            new Entry(NetworkSyncModel.Unspecified, nameof(NetworkSyncModel.Unspecified), NetworkSyncProfiles.Unspecified),
            new Entry(NetworkSyncModel.Lockstep, nameof(NetworkSyncModel.Lockstep), NetworkSyncProfiles.Lockstep),
            new Entry(NetworkSyncModel.PredictRollback, nameof(NetworkSyncModel.PredictRollback), NetworkSyncProfiles.PredictRollback),
            new Entry(NetworkSyncModel.AuthoritativeInterpolation, nameof(NetworkSyncModel.AuthoritativeInterpolation), NetworkSyncProfiles.AuthoritativeInterpolation),
            new Entry(NetworkSyncModel.BatchStateSync, nameof(NetworkSyncModel.BatchStateSync), NetworkSyncProfiles.BatchStateSync),
            new Entry(NetworkSyncModel.MassBattleLodSync, nameof(NetworkSyncModel.MassBattleLodSync), NetworkSyncProfiles.MassBattleLodSync),
            new Entry(NetworkSyncModel.HybridHeroPrediction, nameof(NetworkSyncModel.HybridHeroPrediction), NetworkSyncProfiles.HybridHeroPrediction),
            new Entry(NetworkSyncModel.FastReconnect, nameof(NetworkSyncModel.FastReconnect), NetworkSyncProfiles.FastReconnect),
            new Entry(NetworkSyncModel.ServerRewindLagCompensation, nameof(NetworkSyncModel.ServerRewindLagCompensation), NetworkSyncProfiles.ServerRewindLagCompensation),
        };

        /// <summary>
        /// The number of registered compatibility models.
        /// </summary>
        public static int Count => Entries.Length;

        /// <summary>
        /// Resolves the canonical <see cref="NetworkSyncProfile"/> for a compatibility model.
        /// Throws <see cref="ArgumentOutOfRangeException"/> for unknown models so callers cannot
        /// silently run with an empty profile.
        /// </summary>
        public static NetworkSyncProfile Resolve(NetworkSyncModel model)
        {
            if (TryResolve(model, out var profile))
            {
                return profile;
            }

            throw new ArgumentOutOfRangeException(nameof(model), model, "Unknown network sync compatibility model.");
        }

        /// <summary>
        /// Attempts to resolve the canonical profile for a compatibility model without throwing.
        /// </summary>
        public static bool TryResolve(NetworkSyncModel model, out NetworkSyncProfile profile)
        {
            foreach (var entry in Entries)
            {
                if (entry.Model == model)
                {
                    profile = entry.Profile;
                    return true;
                }
            }

            profile = NetworkSyncProfiles.Unspecified;
            return false;
        }

        /// <summary>
        /// Returns the stable display name for a compatibility model (matches the enum member name).
        /// Throws <see cref="ArgumentOutOfRangeException"/> for unknown models.
        /// </summary>
        public static string GetName(NetworkSyncModel model)
        {
            foreach (var entry in Entries)
            {
                if (entry.Model == model)
                {
                    return entry.Name;
                }
            }

            throw new ArgumentOutOfRangeException(nameof(model), model, "Unknown network sync compatibility model.");
        }

        /// <summary>
        /// Enumerates every registered compatibility model in enum order.
        /// </summary>
        public static IEnumerable<NetworkSyncModel> Models()
        {
            foreach (var entry in Entries)
            {
                yield return entry.Model;
            }
        }

        /// <summary>
        /// Enumerates every registered profile in enum order. Useful for building capability matrices
        /// without hand-listing each profile.
        /// </summary>
        public static IEnumerable<NetworkSyncProfile> Profiles()
        {
            foreach (var entry in Entries)
            {
                yield return entry.Profile;
            }
        }
    }
}
