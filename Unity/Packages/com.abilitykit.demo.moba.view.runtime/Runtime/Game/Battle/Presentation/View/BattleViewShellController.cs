using UnityEngine;

namespace AbilityKit.Game.Flow
{
    internal sealed class BattleViewShellController
    {
        private readonly IBattleViewShellLoader _loader;
        private readonly IMonoViewHandleRegistry _registry;

        public BattleViewShellController(IBattleViewShellLoader loader, IMonoViewHandleRegistry registry)
        {
            _loader = loader;
            _registry = registry;
        }

        public void Recreate(BattleViewHandle handle, int actorId, int modelId)
        {
            Destroy(handle, immediate: false);

            handle.ModelId = modelId;

            var go = _loader != null ? _loader.CreateShellGameObject(actorId, modelId) : null;
            handle.GameObject = go;

            if (go == null) return;

            var viewHandle = go.GetComponent<MonoViewHandle>();
            if (viewHandle == null) viewHandle = go.AddComponent<MonoViewHandle>();
            viewHandle.ActorId = actorId;
            viewHandle.Registry = _registry;
            handle.ViewHandle = viewHandle;
        }

        public void Destroy(BattleViewHandle handle, bool immediate)
        {
            if (handle == null) return;

            if (handle.ViewHandle != null)
            {
                handle.ViewHandle.Registry = null;
            }

            if (handle.GameObject != null)
            {
                if (immediate) Object.DestroyImmediate(handle.GameObject);
                else Object.Destroy(handle.GameObject);
            }

            handle.GameObject = null;
            handle.ViewHandle = null;
        }
    }
}
