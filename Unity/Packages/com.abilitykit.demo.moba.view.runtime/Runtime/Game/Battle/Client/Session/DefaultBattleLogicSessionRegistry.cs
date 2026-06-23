using System;

namespace AbilityKit.Game.Battle
{
    public sealed class DefaultBattleLogicSessionRegistry : IBattleLogicSessionRegistry
    {
        private BattleLogicSession _current;

        public event Action<BattleLogicSession> SessionChanged;

        public BattleLogicSession Current => _current;
        public bool HasSession => _current != null;

        public BattleLogicSession Start(BattleLogicSessionOptions options, IBattleLogicTransport remoteTransport = null)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));

            BattleDebugFacadeProvider.Current = new DefaultBattleDebugFacade(() => _current);

            Stop();

            _current = new BattleLogicSession(options, remoteTransport);
            SessionChanged?.Invoke(_current);
            return _current;
        }

        public void Stop()
        {
            if (_current == null) return;

            try
            {
                _current.Dispose();
            }
            finally
            {
                _current = null;
                SessionChanged?.Invoke(null);
            }
        }
    }
}
