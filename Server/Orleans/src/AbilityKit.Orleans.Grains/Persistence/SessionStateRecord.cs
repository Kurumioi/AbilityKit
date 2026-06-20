namespace AbilityKit.Orleans.Grains.Persistence;

public sealed record SessionStateRecord(
    string SessionToken,
    string AccountId,
    long IssuedAtUnixMs,
    long ExpireAtUnixMs);
