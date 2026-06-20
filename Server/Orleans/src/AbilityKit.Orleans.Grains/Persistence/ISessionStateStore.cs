namespace AbilityKit.Orleans.Grains.Persistence;

public interface ISessionStateStore
{
    Task<IReadOnlyCollection<SessionStateRecord>> LoadActiveSessionsAsync(long nowUnixMs, CancellationToken cancellationToken = default);

    Task<SessionStateRecord?> GetByTokenAsync(string sessionToken, CancellationToken cancellationToken = default);

    Task<SessionStateRecord?> GetByAccountIdAsync(string accountId, CancellationToken cancellationToken = default);

    Task UpsertAsync(SessionStateRecord session, CancellationToken cancellationToken = default);

    Task RemoveByTokenAsync(string sessionToken, CancellationToken cancellationToken = default);

    Task RemoveByAccountIdAsync(string accountId, CancellationToken cancellationToken = default);

    Task CleanupExpiredAsync(long nowUnixMs, CancellationToken cancellationToken = default);
}
