using UnityEngine;
using EC = AbilityKit.World.ECS;

namespace AbilityKit.Game.Flow
{
    internal sealed class BattleViewLifecycleController
    {
        private readonly BattleViewHandleStore _handles;
        private readonly BattleViewShellController _shells;
        private readonly BattleViewAttachedVfxController _attachedVfx;
        private readonly BattleViewTransformController _transforms;

        public BattleViewLifecycleController(
            BattleViewHandleStore handles,
            BattleViewShellController shells,
            BattleViewAttachedVfxController attachedVfx,
            BattleViewTransformController transforms)
        {
            _handles = handles;
            _shells = shells;
            _attachedVfx = attachedVfx;
            _transforms = transforms;
        }

        public void OnDestroyed(EC.IEntityId id)
        {
            if (!_handles.TryGet(id, out var handle)) return;

            handle.Destroyed = true;
            handle.Version++;
            handle.Pos.Clear();
            _shells.Destroy(handle, immediate: !Application.isPlaying);
            _attachedVfx.Destroy(handle);
            _handles.Remove(id);
        }

        public void Clear()
        {
            _handles.ForEach((_, handle) =>
            {
                _shells.Destroy(handle, immediate: !Application.isPlaying);

                if (handle != null)
                {
                    handle.Pos.Clear();
                }

                _attachedVfx.Destroy(handle);
            });

            _handles.Clear();
            _transforms.Reset();
        }
    }
}
