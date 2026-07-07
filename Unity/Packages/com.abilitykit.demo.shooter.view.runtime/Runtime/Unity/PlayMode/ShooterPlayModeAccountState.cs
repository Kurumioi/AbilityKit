#nullable enable

using System;

namespace AbilityKit.Demo.Shooter.View.PlayMode
{
    internal sealed class ShooterPlayModeAccountState
    {
        private string _loggedAccountId = string.Empty;
        private string _sessionAccountId = string.Empty;
        private string _identitySuffix = string.Empty;

        public string LoggedAccountId => _loggedAccountId;

        public void RecordLogin(string accountId)
        {
            _loggedAccountId = accountId;
            _sessionAccountId = accountId;
        }

        public bool HasSessionToken(string sessionToken, string accountId)
        {
            return !string.IsNullOrWhiteSpace(sessionToken)
                && !string.Equals(sessionToken, ShooterRemoteStateSyncDefaults.DefaultSessionToken, StringComparison.Ordinal)
                && string.Equals(_sessionAccountId, accountId, StringComparison.Ordinal);
        }

        public void ClearSession()
        {
            _loggedAccountId = string.Empty;
            _sessionAccountId = string.Empty;
        }

        public void EnsureUniqueDefaultIdentity(ref string accountId, ref string guestId)
        {
            EnsureIdentitySuffix();
            if (string.IsNullOrWhiteSpace(accountId) || string.Equals(accountId.Trim(), "unity-account", StringComparison.Ordinal))
            {
                accountId = CreateDefaultAccountId();
            }

            if (string.IsNullOrWhiteSpace(guestId) || string.Equals(guestId.Trim(), "unity-guest", StringComparison.Ordinal))
            {
                guestId = CreateDefaultGuestId();
            }
        }

        public string CreateDefaultAccountId()
        {
            EnsureIdentitySuffix();
            return $"unity-account-{_identitySuffix}";
        }

        public string CreateDefaultGuestId()
        {
            EnsureIdentitySuffix();
            return $"unity-guest-{_identitySuffix}";
        }

        private void EnsureIdentitySuffix()
        {
            if (string.IsNullOrWhiteSpace(_identitySuffix))
            {
                _identitySuffix = Guid.NewGuid().ToString("N").Substring(0, 8);
            }
        }
    }
}
