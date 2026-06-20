using System.Collections.Concurrent;

namespace AbilityKit.Orleans.Grains.Persistence;

public sealed class InMemorySessionStateStore : ISessionStateStore
{
    private readonly ConcurrentDictionary<string, SessionStateRecord> _sessionsByToken = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, string> _tokensByAccountId = new(StringComparer.Ordinal);

    public Task<IReadOnlyCollection<SessionStateRecord>> LoadActiveSessionsAsync(long nowUnixMs, CancellationToken cancellationToken = default)
    {
        CleanupExpired(nowUnixMs);
        IReadOnlyCollection<SessionStateRecord> sessions = _sessionsByToken.Values.ToArray();
        return Task.FromResult(sessions);
    }

    public Task<SessionStateRecord?> GetByTokenAsync(string sessionToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sessionToken))
        {
            return Task.FromResult<SessionStateRecord?>(null);
        }

        _sessionsByToken.TryGetValue(sessionToken, out var session);
        return Task.FromResult(session);
    }

    public Task<SessionStateRecord?> GetByAccountIdAsync(string accountId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(accountId) || !_tokensByAccountId.TryGetValue(accountId, out var token))
        {
            return Task.FromResult<SessionStateRecord?>(null);
        }

        return GetByTokenAsync(token, cancellationToken);
    }

    public Task UpsertAsync(SessionStateRecord session, CancellationToken cancellationToken = default)
    {
        if (session is null)
        {
            throw new ArgumentNullException(nameof(session));
        }

        _sessionsByToken[session.SessionToken] = session;
        _tokensByAccountId[session.AccountId] = session.SessionToken;
        return Task.CompletedTask;
    }

    public Task RemoveByTokenAsync(string sessionToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sessionToken))
        {
            return Task.CompletedTask;
        }

        if (_sessionsByToken.TryRemove(sessionToken, out var removed) &&
            _tokensByAccountId.TryGetValue(removed.AccountId, out var mappedToken) &&
            string.Equals(mappedToken, sessionToken, StringComparison.Ordinal))
        {
            _tokensByAccountId.TryRemove(removed.AccountId, out _);
        }

        return Task.CompletedTask;
    }

    public async Task RemoveByAccountIdAsync(string accountId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(accountId) || !_tokensByAccountId.TryGetValue(accountId, out var token))
        {
            return;
        }

        await RemoveByTokenAsync(token, cancellationToken);
    }

    public Task CleanupExpiredAsync(long nowUnixMs, CancellationToken cancellationToken = default)
    {
        CleanupExpired(nowUnixMs);
        return Task.CompletedTask;
    }

    private void CleanupExpired(long nowUnixMs)
    {
        foreach (var pair in _sessionsByToken)
        {
            if (pair.Value.ExpireAtUnixMs > nowUnixMs)
            {
                continue;
            }

            if (_sessionsByToken.TryRemove(pair.Key, out var removed) &&
                _tokensByAccountId.TryGetValue(removed.AccountId, out var mappedToken) &&
                string.Equals(mappedToken, pair.Key, StringComparison.Ordinal))
            {
                _tokensByAccountId.TryRemove(removed.AccountId, out _);
            }
        }
    }
}
