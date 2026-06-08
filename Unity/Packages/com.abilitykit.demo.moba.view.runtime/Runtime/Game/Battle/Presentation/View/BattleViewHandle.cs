using AbilityKit.World.ECS;
using UnityEngine;

namespace AbilityKit.Game.Flow
{
    internal sealed class BattleViewHandle
    {
        public int Version;
        public bool Destroyed;
        public int ActorId;
        public int ModelId;
        public GameObject GameObject;
        public MonoViewHandle ViewHandle;
        public int VfxId;
        public IEntityId VfxEntityId;
        public Vector3 PendingPos;
        public bool HasPendingPos;
        public BattleViewPositionSampleBuffer Pos;
    }
}
