namespace AbilityKit.Game.Flow
{
    internal sealed class BattleViewModelSyncController
    {
        private readonly BattleViewHandleStore _handles;
        private readonly BattleViewShellController _shells;
        private readonly BattleViewTransformController _transforms;
        private readonly BattleViewResourceProvider _resources;

        public BattleViewModelSyncController(
            BattleViewHandleStore handles,
            BattleViewShellController shells,
            BattleViewTransformController transforms,
            BattleViewResourceProvider resources = null)
        {
            _handles = handles;
            _shells = shells;
            _transforms = transforms;
            _resources = BattleViewResourceProvider.OrDefault(resources);
        }

        public bool Sync(in BattleViewEntitySyncInput input, BattleContext ctx, out BattleViewHandle handle)
        {
            handle = _handles.GetOrCreate(input.Entity.Id);
            if (handle.Destroyed) return false;

            _handles.SetActorId(handle, input.ActorId, input.Entity.Id);

            var position = input.Transform.Position;
            _transforms.SampleEntity(input.Entity, in position, ctx);

            var desiredModelId = _resources.ResolveModelId(input.Meta);
            if (desiredModelId > 0 && (handle.GameObject == null || handle.ModelId != desiredModelId))
            {
                handle.Version++;
                _shells.Recreate(handle, input.ActorId, desiredModelId);
            }

            return true;
        }
    }
}
