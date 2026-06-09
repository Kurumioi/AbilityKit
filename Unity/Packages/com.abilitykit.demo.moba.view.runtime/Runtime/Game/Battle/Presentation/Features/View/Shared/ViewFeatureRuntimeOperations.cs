using EC = AbilityKit.World.ECS;

namespace AbilityKit.Game.Flow
{
    internal sealed class ViewFeatureRuntimeOperations
    {
        private readonly ViewDirtyRuntimeOperation _dirty;
        private readonly ViewSeekableRegistry _seekables;
        private readonly ViewTimelineRuntimeOperation _timeline;

        public ViewFeatureRuntimeOperations(
            ViewDirtyRuntimeOperation dirty = null,
            ViewSeekableRegistry seekables = null,
            ViewTimelineRuntimeOperation timeline = null)
        {
            _seekables = seekables ?? new ViewSeekableRegistry();
            _timeline = timeline ?? new ViewTimelineRuntimeOperation();
            _dirty = dirty ?? new ViewDirtyRuntimeOperation(_seekables, _timeline);
        }

        public void RefreshDirtyViews(IViewFeatureRuntime runtime)
        {
            _dirty.Refresh(runtime);
        }

        public void RegisterAllSeekables(IViewFeatureRuntime runtime)
        {
            _seekables.RegisterAll(runtime);
        }

        public void SeekAllToCurrentFrame(IViewFeatureRuntime runtime)
        {
            _timeline.SeekAllToCurrentFrame(runtime);
        }

        public void RebindAllViews(IViewFeatureRuntime runtime)
        {
            var battleCtx = runtime?.Context;
            if (battleCtx?.EntityWorld == null) return;

            runtime.Binder?.RebindAll(battleCtx.EntityWorld, battleCtx);
        }

        public void TickVfx(IViewFeatureRuntime runtime)
        {
            if (runtime == null) return;
            if (runtime.VfxNode.IsValid) runtime.Vfx?.Tick(runtime.VfxNode, runtime.Binder);
        }

        public void TickFloatingTexts(IViewFeatureRuntime runtime, float deltaTime)
        {
            runtime?.FloatingTexts?.Tick(deltaTime);
        }

        public void OnEntityDestroyed(IViewFeatureRuntime runtime, EC.EntityDestroyed evt)
        {
            var id = evt.EntityId;
            runtime?.Context?.EntityLookup?.UnbindByEntityId(id);
            runtime?.Binder?.OnDestroyed(id);
        }
    }
}
