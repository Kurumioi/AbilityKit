#nullable enable

using System;
using AbilityKit.Demo.Shooter.View.Hosting;

namespace AbilityKit.Demo.Shooter.View.PlayMode
{
    public static class ShooterRemoteStateSyncDefaults
    {
        public const string DefaultHost = "127.0.0.1";
        public const int DefaultPort = 41001;
        public const string DefaultRegion = "local";
        public const string DefaultServerId = "dev";
        public const string DefaultSessionToken = "unity-shooter-state-sync";
    }

    public enum ShooterRemoteStateSyncLaunchMode
    {
        RestoreFirst = 0,
        CreateNew = 1,
        RestoreOnly = 2
    }

    public readonly struct ShooterRemoteStateSyncLaunchOptions
    {
        public ShooterRemoteStateSyncLaunchOptions(
            ShooterPlayModeSessionOptions sessionOptions,
            ShooterClientNetworkEndpoint endpoint,
            string sessionToken = ShooterRemoteStateSyncDefaults.DefaultSessionToken,
            string region = ShooterRemoteStateSyncDefaults.DefaultRegion,
            string serverId = ShooterRemoteStateSyncDefaults.DefaultServerId,
            ShooterRemoteStateSyncLaunchMode launchMode = ShooterRemoteStateSyncLaunchMode.RestoreFirst,
            TimeSpan? timeout = null)
        {
            SessionOptions = sessionOptions.Normalized();
            Endpoint = endpoint;
            SessionToken = string.IsNullOrWhiteSpace(sessionToken) ? ShooterRemoteStateSyncDefaults.DefaultSessionToken : sessionToken;
            Region = string.IsNullOrWhiteSpace(region) ? ShooterRemoteStateSyncDefaults.DefaultRegion : region;
            ServerId = string.IsNullOrWhiteSpace(serverId) ? ShooterRemoteStateSyncDefaults.DefaultServerId : serverId;
            LaunchMode = launchMode;
            Timeout = timeout ?? TimeSpan.FromSeconds(5);
        }

        public ShooterPlayModeSessionOptions SessionOptions { get; }
        public ShooterClientNetworkEndpoint Endpoint { get; }
        public string SessionToken { get; }
        public string Region { get; }
        public string ServerId { get; }
        public ShooterRemoteStateSyncLaunchMode LaunchMode { get; }
        public TimeSpan Timeout { get; }

        public static ShooterRemoteStateSyncLaunchOptions RestoreFirst(
            ShooterPlayModeSessionOptions sessionOptions,
            ShooterClientNetworkEndpoint endpoint,
            string sessionToken = ShooterRemoteStateSyncDefaults.DefaultSessionToken,
            string region = ShooterRemoteStateSyncDefaults.DefaultRegion,
            string serverId = ShooterRemoteStateSyncDefaults.DefaultServerId,
            TimeSpan? timeout = null)
        {
            return new ShooterRemoteStateSyncLaunchOptions(
                sessionOptions,
                endpoint,
                sessionToken,
                region,
                serverId,
                ShooterRemoteStateSyncLaunchMode.RestoreFirst,
                timeout);
        }
    }
}
