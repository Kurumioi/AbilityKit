using AbilityKit.Demo.Moba.Diagnostics;

namespace AbilityKit.Demo.Moba.Services
{
    /// <summary>
    /// Effect 执行生命周期诊断事件草稿生成器。
    /// 从 <see cref="MobaEffectExecutionService"/> 抽离为独立静态类，
    /// 避免诊断测试直接依赖 WorldService 基类所在的程序集链路。
    /// </summary>
    internal static class MobaEffectDiagnosticProducer
    {
        public static MobaBattleDiagnosticEventDraft CreateEffectStartedDraft(
            int effectConfigId,
            int triggerId,
            int sourceActorId,
            int targetActorId,
            long effectContextId,
            long rootContextId)
        {
            var resolvedRoot = rootContextId != 0L ? rootContextId : effectContextId;
            var summary = $"effectConfigId={effectConfigId}, triggerId={triggerId}, contextId={effectContextId}";

            return new MobaBattleDiagnosticEventDraft(
                BattleDiagnosticEventKind.EffectStarted,
                BattleDiagnosticEventChannel.Effect,
                BattleDiagnosticEventOutcome.None,
                sourceActorId,
                targetActorId,
                effectConfigId,
                resolvedRoot,
                effectContextId,
                summary: summary);
        }

        public static MobaBattleDiagnosticEventDraft CreateEffectEndedDraft(
            int effectConfigId,
            int triggerId,
            int sourceActorId,
            int targetActorId,
            long effectContextId,
            long rootContextId,
            bool executed)
        {
            var resolvedRoot = rootContextId != 0L ? rootContextId : effectContextId;
            var outcome = executed
                ? BattleDiagnosticEventOutcome.Succeeded
                : BattleDiagnosticEventOutcome.Failed;
            var summary = $"effectConfigId={effectConfigId}, triggerId={triggerId}, contextId={effectContextId}, executed={executed}";

            return new MobaBattleDiagnosticEventDraft(
                BattleDiagnosticEventKind.EffectEnded,
                BattleDiagnosticEventChannel.Effect,
                outcome,
                sourceActorId,
                targetActorId,
                effectConfigId,
                resolvedRoot,
                effectContextId,
                summary: summary);
        }
    }
}
