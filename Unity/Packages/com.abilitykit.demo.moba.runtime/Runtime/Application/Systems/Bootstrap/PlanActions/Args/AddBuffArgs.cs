using System;

namespace AbilityKit.Demo.Moba.Systems
{
    public struct AddBuffArgs
    {
        public int[] BuffIds;
        public int TargetActorId;

        public AddBuffArgs(int[] buffIds, int targetActorId = 0)
        {
            BuffIds = buffIds;
            TargetActorId = targetActorId;
        }
    }
}
