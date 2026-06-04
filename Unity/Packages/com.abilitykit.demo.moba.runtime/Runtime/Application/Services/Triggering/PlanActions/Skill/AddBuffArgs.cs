using System;

namespace AbilityKit.Demo.Moba.Services.Triggering.PlanActions
{
    public struct AddBuffArgs
    {
        public int[] BuffIds;
        public int TargetActorId;
        public int QueryTemplateId;

        public AddBuffArgs(int[] buffIds, int targetActorId = 0, int queryTemplateId = 0)
        {
            BuffIds = buffIds;
            TargetActorId = targetActorId;
            QueryTemplateId = queryTemplateId;
        }
    }
}
