using AbilityKit.Protocol.Moba.StateSync;
using UnityEngine;

namespace AbilityKit.Game.Flow
{
    internal sealed class BattleAreaViewHandleFactory
    {
        private readonly BattleViewResourceProvider _resources;
        private readonly BattleAreaViewObjectFactory _objects;
        private readonly BattleAreaAttachResolver _attachResolver;
        private readonly BattleAreaViewPositionResolver _positions;
        private readonly BattleAreaViewHandleBuilder _handles;

        public BattleAreaViewHandleFactory(
            BattleViewResourceProvider resources = null,
            BattleAreaViewObjectFactory objects = null,
            BattleAreaAttachResolver attachResolver = null,
            BattleAreaViewPositionResolver positions = null,
            BattleAreaViewHandleBuilder handles = null)
        {
            _resources = BattleViewResourceProvider.OrDefault(resources);
            _objects = objects ?? new BattleAreaViewObjectFactory(_resources);
            _attachResolver = attachResolver ?? new BattleAreaAttachResolver();
            _positions = positions ?? new BattleAreaViewPositionResolver();
            _handles = handles ?? new BattleAreaViewHandleBuilder();
        }

        public BattleAreaViewHandle TryCreate(BattleViewBinder binder, in MobaAreaEventSnapshotEntry evt)
        {
            var aoe = _resources.TryGetAoe(evt.TemplateId);
            if (aoe == null) return null;

            var pos = _positions.Resolve(in evt, aoe.OffsetX, aoe.OffsetY, aoe.OffsetZ);
            var attach = _attachResolver.Resolve(binder, evt.OwnerActorId, aoe.AttachMode);
            var handle = _handles.Create(in evt);

            handle.ModelGo = _objects.CreateModel(aoe.ModelId, attach, in pos);
            handle.VfxGo = _objects.CreateVfx(aoe.VfxId, attach, in pos);
            return handle;
        }
    }

    internal sealed class BattleAreaViewPositionResolver
    {
        public Vector3 Resolve(
            in MobaAreaEventSnapshotEntry evt,
            float offsetX,
            float offsetY,
            float offsetZ)
        {
            return new Vector3(
                evt.X + offsetX,
                evt.Y + offsetY,
                evt.Z + offsetZ);
        }
    }

    internal sealed class BattleAreaViewHandleBuilder
    {
        public BattleAreaViewHandle Create(in MobaAreaEventSnapshotEntry evt)
        {
            return new BattleAreaViewHandle
            {
                AreaId = evt.AreaId,
                TemplateId = evt.TemplateId,
            };
        }
    }
}
