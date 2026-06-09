using UnityEngine;

namespace AbilityKit.Game.Flow
{
    internal sealed class BattleViewShellDestroyer
    {
        private readonly BattleViewShellHandleBinder _handleBinder;

        public BattleViewShellDestroyer(BattleViewShellHandleBinder handleBinder)
        {
            _handleBinder = handleBinder;
        }

        public void Destroy(BattleViewHandle handle, bool immediate)
        {
            if (handle == null) return;

            _handleBinder?.Unbind(handle);

            if (handle.GameObject != null)
            {
                if (immediate) Object.DestroyImmediate(handle.GameObject);
                else Object.Destroy(handle.GameObject);
            }

            handle.GameObject = null;
        }
    }
}
