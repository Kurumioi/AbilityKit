namespace AbilityKit.Game.Flow
{
    internal sealed class BattleViewShellController
    {
        private readonly IBattleViewShellLoader _loader;
        private readonly BattleViewShellHandleBinder _handleBinder;
        private readonly BattleViewShellDestroyer _destroyer;

        public BattleViewShellController(
            IBattleViewShellLoader loader,
            IMonoViewHandleRegistry registry,
            BattleViewShellHandleBinder handleBinder = null,
            BattleViewShellDestroyer destroyer = null)
        {
            _loader = loader;
            _handleBinder = handleBinder ?? new BattleViewShellHandleBinder(registry);
            _destroyer = destroyer ?? new BattleViewShellDestroyer(_handleBinder);
        }

        public void Recreate(BattleViewHandle handle, int actorId, int modelId)
        {
            Destroy(handle, immediate: false);

            handle.ModelId = modelId;

            var go = _loader != null ? _loader.CreateShellGameObject(actorId, modelId) : null;
            handle.GameObject = go;

            if (go == null) return;

            _handleBinder.Bind(handle, go, actorId);
        }

        public void Destroy(BattleViewHandle handle, bool immediate)
        {
            _destroyer.Destroy(handle, immediate);
        }
    }
}
