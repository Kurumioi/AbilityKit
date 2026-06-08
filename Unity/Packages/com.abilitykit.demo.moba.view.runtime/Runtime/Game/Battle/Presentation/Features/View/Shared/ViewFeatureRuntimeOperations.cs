using EC = AbilityKit.World.ECS;

namespace AbilityKit.Game.Flow
{
    internal static class ViewFeatureRuntimeOperations
    {
        public static void RefreshDirtyViews(IViewFeatureRuntime runtime)
        {
            ViewDirtyRuntimeOperation.Refresh(runtime);
        }

        public static void RegisterAllSeekables(IViewFeatureRuntime runtime)
        {
            ViewSeekableRegistry.RegisterAll(runtime);
        }

        public static void SeekAllToCurrentFrame(IViewFeatureRuntime runtime)
        {
            ViewTimelineRuntimeOperation.SeekAllToCurrentFrame(runtime);
        }

        public static void RebindAllViews(IViewFeatureRuntime runtime)
        {
            var battleCtx = runtime?.Context;
            if (battleCtx?.EntityWorld == null) return;

            runtime.Binder?.RebindAll(battleCtx.EntityWorld, battleCtx);
        }

        public static void TickVfx(IViewFeatureRuntime runtime)
        {
            if (runtime == null) return;
            if (runtime.VfxNode.IsValid) runtime.Vfx?.Tick(runtime.VfxNode, runtime.Binder);
        }

        public static void TickFloatingTexts(IViewFeatureRuntime runtime, float deltaTime)
        {
            runtime?.FloatingTexts?.Tick(deltaTime);
        }

        public static void OnEntityDestroyed(IViewFeatureRuntime runtime, EC.EntityDestroyed evt)
        {
            var id = evt.EntityId;
            runtime?.Context?.EntityLookup?.UnbindByEntityId(id);
            runtime?.Binder?.OnDestroyed(id);
        }
    }
}
