using AbilityKit.Network.Runtime.Conditioning;
using AbilityKit.Protocol.Shooter;

namespace AbilityKit.Orleans.Grains.Gameplays.Shooter.Battle;

internal enum ShooterStateSyncPushPayloadMode
{
    Packed = 0,
    PureState = 1
}

internal sealed class ShooterStateSyncPushOptions
{
    public const string PayloadModeEnvironmentVariable = "ABILITYKIT_SHOOTER_STATE_SYNC_PAYLOAD_MODE";

    private const int LimitedBandwidthKbps = 256;
    private const int HighLatencyMs = 120;
    private const double LossyLinkRate = 0.02d;

    private ShooterStateSyncPushOptions(
        ShooterStateSyncPushPayloadMode payloadMode,
        NetworkConditionProfile networkCondition,
        ShooterPureStateSyncSettings? pureStateSettings,
        float aoiVisibleRadius,
        float aoiBoundaryRadius)
    {
        PayloadMode = payloadMode;
        NetworkCondition = networkCondition;
        PureStateSettings = pureStateSettings;
        AoiVisibleRadius = aoiVisibleRadius > 0f ? aoiVisibleRadius : 24f;
        AoiBoundaryRadius = aoiBoundaryRadius >= AoiVisibleRadius ? aoiBoundaryRadius : AoiVisibleRadius;
    }

    public ShooterStateSyncPushPayloadMode PayloadMode { get; }

    public NetworkConditionProfile NetworkCondition { get; }

    public ShooterPureStateSyncSettings? PureStateSettings { get; }

    public float AoiVisibleRadius { get; }

    public float AoiBoundaryRadius { get; }

    public static ShooterStateSyncPushOptions PackedDefault { get; } = new ShooterStateSyncPushOptions(
        ShooterStateSyncPushPayloadMode.Packed,
        NetworkConditionProfile.Ideal,
        null,
        24f,
        30f);

    public static ShooterStateSyncPushOptions PureState(
        NetworkConditionProfile networkCondition,
        ShooterPureStateSyncSettings? settings = null,
        float aoiVisibleRadius = 24f,
        float aoiBoundaryRadius = 30f)
    {
        return new ShooterStateSyncPushOptions(ShooterStateSyncPushPayloadMode.PureState, networkCondition, settings, aoiVisibleRadius, aoiBoundaryRadius);
    }

    public static ShooterStateSyncPushOptions FromEnvironmentDefault()
    {
        var value = Environment.GetEnvironmentVariable(PayloadModeEnvironmentVariable);
        return TryParsePayloadMode(value, out var payloadMode) && payloadMode == ShooterStateSyncPushPayloadMode.PureState
            ? PureState(NetworkConditionProfile.Ideal)
            : PackedDefault;
    }

    public static bool TryParsePayloadMode(string? value, out ShooterStateSyncPushPayloadMode payloadMode)
    {
        if (string.Equals(value, "pure-state", StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, "purestate", StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, "pure_state", StringComparison.OrdinalIgnoreCase))
        {
            payloadMode = ShooterStateSyncPushPayloadMode.PureState;
            return true;
        }

        if (string.IsNullOrWhiteSpace(value)
            || string.Equals(value, "packed", StringComparison.OrdinalIgnoreCase))
        {
            payloadMode = ShooterStateSyncPushPayloadMode.Packed;
            return true;
        }

        payloadMode = ShooterStateSyncPushPayloadMode.Packed;
        return false;
    }

    public ShooterPureStateSyncSettings ResolvePureStateSettings()
    {
        if (PureStateSettings.HasValue)
        {
            return PureStateSettings.Value;
        }

        var defaults = ShooterPureStateSyncSettings.Default;
        if (NetworkCondition.BandwidthKbps > 0 && NetworkCondition.BandwidthKbps <= LimitedBandwidthKbps)
        {
            return new ShooterPureStateSyncSettings(
                defaults.MaxEntityCount,
                128,
                defaults.BaselineIntervalFrames,
                4,
                30,
                6);
        }

        if (NetworkCondition.PacketLossRate >= LossyLinkRate || NetworkCondition.JitterMs >= 50)
        {
            return new ShooterPureStateSyncSettings(
                defaults.MaxEntityCount,
                256,
                defaults.BaselineIntervalFrames,
                3,
                24,
                6);
        }

        if (NetworkCondition.BaseLatencyMs >= HighLatencyMs)
        {
            return new ShooterPureStateSyncSettings(
                defaults.MaxEntityCount,
                384,
                defaults.BaselineIntervalFrames,
                3,
                20,
                5);
        }

        return defaults;
    }
}
