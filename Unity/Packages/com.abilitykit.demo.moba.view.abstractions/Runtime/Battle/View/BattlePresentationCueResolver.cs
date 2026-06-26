using AbilityKit.Demo.Moba.Share;
using AbilityKit.Demo.Moba.View.Abstractions.Shared.Types;

namespace AbilityKit.Demo.Moba.View.Abstractions.Battle.View
{
    public static class BattlePresentationCueResolver
    {
        public static BattlePresentationCueDecision Resolve(in PresentationCueData data)
        {
            var requestKey = BattlePresentationCueRequestKey.From(in data);
            if (requestKey.IsEmpty) return BattlePresentationCueDecision.None;

            if (ShouldStart(data.Stage) || ShouldKeepActive(data.Stage))
            {
                var spawnRequest = CreateSpawnRequest(requestKey, in data);
                if (spawnRequest.IsEmpty) return BattlePresentationCueDecision.None;

                return new BattlePresentationCueDecision(BattlePresentationCueDecisionKind.Play, requestKey, spawnRequest);
            }

            if (ShouldStop(data.Stage))
            {
                return new BattlePresentationCueDecision(BattlePresentationCueDecisionKind.Stop, requestKey, default);
            }

            return BattlePresentationCueDecision.None;
        }

        public static bool ShouldStart(PresentationCueStage stage)
        {
            return stage == PresentationCueStage.ConditionPassed
                || stage == PresentationCueStage.BeforeAction
                || stage == PresentationCueStage.Executed
                || stage == PresentationCueStage.Started;
        }

        public static bool ShouldKeepActive(PresentationCueStage stage)
        {
            return stage == PresentationCueStage.Ticked
                || stage == PresentationCueStage.Refreshed
                || stage == PresentationCueStage.StackChanged;
        }

        public static bool ShouldStop(PresentationCueStage stage)
        {
            return stage == PresentationCueStage.ConditionFailed
                || stage == PresentationCueStage.Interrupted
                || stage == PresentationCueStage.Skipped
                || stage == PresentationCueStage.Expired
                || stage == PresentationCueStage.Removed
                || stage == PresentationCueStage.Completed;
        }

        public static int ResolveVfxId(in PresentationCueData data)
        {
            if (data.VfxId > 0) return data.VfxId;
            if (data.TemplateId > 0) return data.TemplateId;
            return 0;
        }

        public static BattlePresentationCueSpawnRequest CreateSpawnRequest(
            BattlePresentationCueRequestKey requestKey,
            in PresentationCueData data)
        {
            var vfxId = ResolveVfxId(in data);
            if (requestKey.IsEmpty || vfxId <= 0) return default;

            return new BattlePresentationCueSpawnRequest(
                requestKey,
                vfxId,
                data.SourceActorId,
                data.TargetActorId,
                ResolveFirstTargetActorId(in data),
                data.Positions != null && data.Positions.Count > 0,
                data.Positions != null && data.Positions.Count > 0 ? new MobaFloat3(data.Positions[0].X, data.Positions[0].Y, data.Positions[0].Z) : default,
                new MobaFloat3(data.OffsetX, data.OffsetY, data.OffsetZ));
        }

        public static int ResolveFirstTargetActorId(in PresentationCueData data)
        {
            if (data.Targets != null && data.Targets.Count > 0) return data.Targets[0];
            return 0;
        }
    }
}
