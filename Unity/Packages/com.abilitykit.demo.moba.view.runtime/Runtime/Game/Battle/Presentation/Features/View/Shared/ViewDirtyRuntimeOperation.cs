using AbilityKit.Game.Battle.Component;
using AbilityKit.Game.Battle.Entity;

namespace AbilityKit.Game.Flow
{
    internal static class ViewDirtyRuntimeOperation
    {
        public static void Refresh(IViewFeatureRuntime runtime)
        {
            if (runtime?.Query?.World == null) return;

            var battleCtx = runtime.Context;
            var dirty = battleCtx != null ? battleCtx.DirtyEntities : null;
            if (dirty == null || dirty.Count == 0) return;

            for (int i = 0; i < dirty.Count; i++)
            {
                var id = dirty[i];
                if (!runtime.Query.World.IsAlive(id)) continue;

                var entity = runtime.Query.World.Wrap(id);
                if (!entity.TryGetRef(out BattleNetIdComponent netIdComp)) continue;
                if (!entity.TryGetRef(out BattleTransformComponent transform)) continue;

                runtime.Binder?.Sync(entity, battleCtx);
                ViewSeekableRegistry.RegisterForEntity(runtime, id);
            }

            ViewTimelineRuntimeOperation.SeekAllToCurrentFrame(runtime);
            dirty.Clear();
        }
    }
}
