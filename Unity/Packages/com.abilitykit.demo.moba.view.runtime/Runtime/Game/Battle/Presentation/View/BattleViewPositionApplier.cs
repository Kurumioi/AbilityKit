using UnityEngine;

namespace AbilityKit.Game.Flow
{
    internal sealed class BattleViewPositionApplier
    {
        private readonly BattleViewHandleStore _handles;
        private readonly BattleViewAttachedVfxController _attachedVfx;

        public BattleViewPositionApplier(BattleViewHandleStore handles, BattleViewAttachedVfxController attachedVfx)
        {
            _handles = handles;
            _attachedVfx = attachedVfx;
        }

        public void ApplyPendingPositions()
        {
            _handles.ForEach((_, handle) =>
            {
                if (handle == null || handle.Destroyed) return;
                if (handle.GameObject == null) return;
                if (!handle.HasPendingPos) return;

                var pos = handle.PendingPos;
                ApplyPosition(handle, in pos);
            });
        }

        public void ApplyInterpolatedPositions(double renderTime)
        {
            _handles.ForEach((_, handle) =>
            {
                if (handle == null || handle.Destroyed) return;
                if (handle.GameObject == null) return;

                if (!handle.Pos.TryEvaluate(renderTime, out var pos))
                {
                    if (handle.HasPendingPos) pos = handle.PendingPos;
                    else return;
                }

                ApplyPosition(handle, in pos);
            });
        }

        private void ApplyPosition(BattleViewHandle handle, in Vector3 pos)
        {
            handle.GameObject.transform.position = pos;
            _attachedVfx.SyncPosition(handle, in pos);
        }
    }
}
