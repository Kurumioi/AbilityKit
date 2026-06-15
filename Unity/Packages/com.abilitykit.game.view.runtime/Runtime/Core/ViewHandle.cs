using System.Numerics;
using AbilityKit.Core.Pooling;

namespace AbilityKit.Game.View
{
    public sealed class ViewHandle : IViewHandle, IPoolable
    {
        public uint EntityId { get; set; }
        public int ModelId { get; set; }
        public object Shell { get; set; }
        public bool HasPendingPosition { get; set; }
        public Vector3 PendingPosition { get; set; }
        public bool IsDestroyed { get; set; }
        public int Version { get; set; }

        public void OnPoolGet()
        {
            EntityId = 0;
            ModelId = 0;
            Shell = null;
            HasPendingPosition = false;
            PendingPosition = default;
            IsDestroyed = false;
            Version = 0;
        }

        public void OnPoolRelease()
        {
        }

        public void OnPoolDestroy()
        {
        }
    }
}