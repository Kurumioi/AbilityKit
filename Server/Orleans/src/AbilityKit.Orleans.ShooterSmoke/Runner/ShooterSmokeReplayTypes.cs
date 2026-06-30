using AbilityKit.Core.Recording.FrameRecord;

internal enum ShooterSmokeReplayKind
{
    Unknown,
    InputState,
    InputLogic
}

internal static class ShooterSmokeReplayTypes
{
    public const string InputStateReplayWorldTypePrefix = "shooter.input-state-replay/";
    public const string InputLogicReplayWorldType = "shooter.input-logic-replay";
    public const string LegacyClientStateReplayWorldTypePrefix = "shooter.client-state-replay/";
    public const string LegacyServerFrameReplayWorldType = "shooter.server-frame-replay";

    public static string CreateInputStateWorldType(ShooterSmokeClientProcessMode mode) =>
        InputStateReplayWorldTypePrefix + mode.ToString().ToLowerInvariant();

    public static ShooterSmokeReplayKind ResolveKind(FrameRecordMeta? meta) => ResolveKind(meta?.WorldType);

    public static ShooterSmokeReplayKind ResolveKind(string? worldType)
    {
        if (string.IsNullOrWhiteSpace(worldType))
        {
            return ShooterSmokeReplayKind.Unknown;
        }

        if (worldType.StartsWith(InputStateReplayWorldTypePrefix, StringComparison.OrdinalIgnoreCase)
            || worldType.StartsWith(LegacyClientStateReplayWorldTypePrefix, StringComparison.OrdinalIgnoreCase))
        {
            return ShooterSmokeReplayKind.InputState;
        }

        if (string.Equals(worldType, InputLogicReplayWorldType, StringComparison.OrdinalIgnoreCase)
            || string.Equals(worldType, LegacyServerFrameReplayWorldType, StringComparison.OrdinalIgnoreCase))
        {
            return ShooterSmokeReplayKind.InputLogic;
        }

        return ShooterSmokeReplayKind.Unknown;
    }
}
