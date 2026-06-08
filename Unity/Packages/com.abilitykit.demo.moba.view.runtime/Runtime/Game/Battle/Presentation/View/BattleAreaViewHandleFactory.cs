using AbilityKit.Demo.Moba.Services;
using AbilityKit.Game.Battle.Entity;
using AbilityKit.Protocol.Moba.StateSync;
using UnityEngine;

namespace AbilityKit.Game.Flow
{
    internal static class BattleAreaViewHandleFactory
    {
        public static BattleAreaViewHandle TryCreate(BattleViewBinder binder, in MobaAreaEventSnapshotEntry evt)
        {
            var aoe = BattleViewFactory.TryGetAoe(evt.TemplateId);
            if (aoe == null) return null;

            var pos = new Vector3(evt.X, evt.Y, evt.Z);
            pos += new Vector3(aoe.OffsetX, aoe.OffsetY, aoe.OffsetZ);

            var attach = ResolveAttachRoot(binder, evt.OwnerActorId, aoe.AttachMode);
            var handle = new BattleAreaViewHandle
            {
                AreaId = evt.AreaId,
                TemplateId = evt.TemplateId,
            };

            handle.ModelGo = CreateAndPlace(aoe.ModelId, createVfx: false, attach, in pos);
            handle.VfxGo = CreateAndPlace(aoe.VfxId, createVfx: true, attach, in pos);
            return handle;
        }

        private static Transform ResolveAttachRoot(BattleViewBinder binder, int ownerActorId, int attachMode)
        {
            if (attachMode != 1) return null;
            if (ownerActorId <= 0) return null;
            if (binder == null) return null;

            return binder.TryGetAttachRoot(new BattleNetId(ownerActorId), out var attach) ? attach : null;
        }

        private static GameObject CreateAndPlace(int viewId, bool createVfx, Transform attach, in Vector3 pos)
        {
            if (viewId <= 0) return null;

            var go = createVfx
                ? BattleViewFactory.CreateVfxGo(viewId)
                : BattleViewFactory.CreateModelGo(viewId);
            if (go == null) return null;

            if (attach != null)
            {
                go.transform.SetParent(attach, worldPositionStays: false);
                go.transform.localPosition = Vector3.zero;
            }
            else
            {
                go.transform.position = pos;
            }

            return go;
        }
    }
}
