using System;
using AbilityKit.Core.Mathematics;
using AbilityKit.Demo.Moba.Services;
using AbilityKit.Protocol.Moba.StateSync;
using AbilityKit.Triggering.Runtime;

namespace AbilityKit.Demo.Moba.Triggering
{
    public sealed class MobaPresentationTriggerCue : ITriggerCue
    {
        private const int NumericParamScale = 1;
        private const int NumericParamRadius = 2;
        private const string StringParamColor = "color";

        private readonly MobaPresentationCueSnapshotService _snapshots;
        private readonly string _cueKind;
        private readonly string _cueVfxId;
        private readonly string _cueSfxId;
        private readonly int _vfxId;
        private readonly int _sfxId;
        private readonly int _templateId;

        public MobaPresentationTriggerCue(MobaPresentationCueSnapshotService snapshots, string cueKind, string cueVfxId, string cueSfxId)
        {
            _snapshots = snapshots;
            _cueKind = cueKind;
            _cueVfxId = cueVfxId;
            _cueSfxId = cueSfxId;
            _vfxId = ParseId(cueVfxId);
            _sfxId = ParseId(cueSfxId);
            _templateId = ParseId(cueKind);
        }

        public void OnConditionPassed(in TriggerCueContext context)
        {
            Publish(MobaPresentationCueStage.ConditionPassed, in context, -1);
        }

        public void OnConditionFailed(in TriggerCueContext context)
        {
            Publish(MobaPresentationCueStage.ConditionFailed, in context, -1);
        }

        public void OnBeforeAction(in TriggerCueContext context, int actionIndex)
        {
            Publish(MobaPresentationCueStage.BeforeAction, in context, actionIndex);
        }

        public void OnExecuted(in TriggerCueContext context)
        {
            Publish(MobaPresentationCueStage.Executed, in context, -1);
        }

        public void OnInterrupted(in TriggerCueContext context)
        {
            Publish(MobaPresentationCueStage.Interrupted, in context, -1);
        }

        public void OnSkipped(in TriggerCueContext context)
        {
            Publish(MobaPresentationCueStage.Skipped, in context, -1);
        }

        private void Publish(MobaPresentationCueStage stage, in TriggerCueContext context, int actionIndex)
        {
            if (_snapshots == null) return;
            if (IsEmptyCue()) return;

            var payload = BuildPayload(stage, in context, actionIndex);
            _snapshots.Report(in payload);
        }

        private MobaPresentationCueSnapshotEntry BuildPayload(MobaPresentationCueStage stage, in TriggerCueContext context, int actionIndex)
        {
            ResolveActors(context.Args, out var sourceActorId, out var targetActorId);
            ResolveTargets(context.Args, sourceActorId, targetActorId, out var targets);
            ResolvePositions(context.Args, out var positions);
            ResolveLineage(context.Args, out var contextKind, out var originKind, out var sourceContextId, out var rootContextId, out var ownerContextId, out var sourceConfigId);
            ResolvePresentationContext(context.Args, out var requestKey, out var durationMsOverride, out var contextEventId, out var numericParamKeys, out var numericParamValues, out var stringParamKeys, out var stringParamValues);

            return new MobaPresentationCueSnapshotEntry
            {
                Stage = (int)stage,
                CueKind = _cueKind,
                CueVfxId = _cueVfxId,
                CueSfxId = _cueSfxId,
                TemplateId = _templateId,
                VfxId = _vfxId,
                SfxId = _sfxId,
                RequestKey = !string.IsNullOrWhiteSpace(requestKey) ? requestKey : BuildRequestKey(context.TriggerId, context.Order),
                Targets = targets,
                Positions = FlattenPositions(positions),
                SourceActorId = sourceActorId,
                TargetActorId = targetActorId,
                TriggerEventId = context.EventId,
                TriggerEventName = context.EventName,
                TriggerId = context.TriggerId,
                Phase = context.Phase,
                Priority = context.Priority,
                Order = unchecked((int)context.Order),
                ActionIndex = actionIndex,
                InterruptReason = (int)context.InterruptReason,
                InterruptSourceName = context.InterruptSourceName,
                InterruptTriggerId = context.InterruptTriggerId,
                InterruptConditionPassed = context.InterruptConditionPassed,
                DurationMsOverride = durationMsOverride,
                ContextKind = contextKind,
                OriginKind = originKind,
                SourceContextId = sourceContextId,
                RootContextId = rootContextId,
                OwnerContextId = ownerContextId,
                SourceConfigId = sourceConfigId,
                ContextEventId = contextEventId,
                NumericParamKeys = numericParamKeys,
                NumericParamValues = numericParamValues,
                StringParamKeys = stringParamKeys,
                StringParamValues = stringParamValues,
                Scale = ResolveScale(numericParamKeys, numericParamValues),
                ColorR = 1f,
                ColorG = 1f,
                ColorB = 1f,
                ColorA = 1f
            };
        }

        private bool IsEmptyCue()
        {
            return string.IsNullOrWhiteSpace(_cueKind) && string.IsNullOrWhiteSpace(_cueVfxId) && string.IsNullOrWhiteSpace(_cueSfxId);
        }

        private static string BuildRequestKey(int triggerId, long order)
        {
            return triggerId > 0 ? $"cue:{triggerId}:{order}" : $"cue:{order}";
        }

        private static int ParseId(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return 0;
            return int.TryParse(value, out var id) ? id : 0;
        }

        private static void ResolveActors(object args, out int sourceActorId, out int targetActorId)
        {
            sourceActorId = 0;
            targetActorId = 0;
            if (args is IMobaActorContextProvider actorProvider)
            {
                actorProvider.TryGetSourceActorId(out sourceActorId);
                actorProvider.TryGetTargetActorId(out targetActorId);
            }
        }

        private static void ResolveLineage(object args, out int contextKind, out int originKind, out long sourceContextId, out long rootContextId, out long ownerContextId, out int sourceConfigId)
        {
            contextKind = 0;
            originKind = 0;
            sourceContextId = 0;
            rootContextId = 0;
            ownerContextId = 0;
            sourceConfigId = 0;

            if (args is IMobaTriggerLineageContextProvider lineageProvider && lineageProvider.TryGetLineageContext(out var lineage))
            {
                contextKind = (int)lineage.ContextKind;
                originKind = (int)lineage.OriginKind;
                sourceContextId = lineage.SourceContextId;
                rootContextId = lineage.RootContextId;
                ownerContextId = lineage.OwnerKey;
                sourceConfigId = lineage.SourceConfigId;
                return;
            }

            if (args is IMobaOriginContextProvider originProvider && originProvider.TryGetOrigin(out var origin))
            {
                originKind = (int)origin.ImmediateKind;
                sourceContextId = origin.EffectiveParentContextId;
                rootContextId = origin.EffectiveRootContextId;
                ownerContextId = origin.OwnerContextId;
                sourceConfigId = origin.ImmediateConfigId;
            }
        }

        private static void ResolveTargets(object args, int sourceActorId, int targetActorId, out int[] targets)
        {
            targets = null;
            if (args is PresentationEventArgs presentation && presentation.Targets != null && presentation.Targets.Length > 0)
            {
                targets = presentation.Targets;
                return;
            }

            if (targetActorId > 0)
            {
                targets = new[] { targetActorId };
                return;
            }

            if (sourceActorId > 0)
            {
                targets = new[] { sourceActorId };
            }
        }

        private static void ResolvePresentationContext(
            object args,
            out string requestKey,
            out int durationMsOverride,
            out string contextEventId,
            out int[] numericParamKeys,
            out float[] numericParamValues,
            out string[] stringParamKeys,
            out string[] stringParamValues)
        {
            requestKey = null;
            durationMsOverride = 0;
            contextEventId = null;
            numericParamKeys = null;
            numericParamValues = null;
            stringParamKeys = null;
            stringParamValues = null;

            if (args is PresentationEventArgs presentation)
            {
                requestKey = presentation.RequestKey;
                durationMsOverride = presentation.DurationMsOverride;
                contextEventId = presentation.EventId;
                BuildPresentationParams(presentation, out numericParamKeys, out numericParamValues, out stringParamKeys, out stringParamValues);
            }
        }

        private static void BuildPresentationParams(
            PresentationEventArgs presentation,
            out int[] numericParamKeys,
            out float[] numericParamValues,
            out string[] stringParamKeys,
            out string[] stringParamValues)
        {
            numericParamKeys = null;
            numericParamValues = null;
            stringParamKeys = null;
            stringParamValues = null;

            int numericCount = 0;
            var scale = TryReadFloat(presentation.Scale, out var scaleValue);
            var radius = TryReadFloat(presentation.Radius, out var radiusValue);
            if (scale) numericCount++;
            if (radius) numericCount++;

            if (numericCount > 0)
            {
                numericParamKeys = new int[numericCount];
                numericParamValues = new float[numericCount];
                int index = 0;
                if (scale)
                {
                    numericParamKeys[index] = NumericParamScale;
                    numericParamValues[index++] = scaleValue;
                }

                if (radius)
                {
                    numericParamKeys[index] = NumericParamRadius;
                    numericParamValues[index] = radiusValue;
                }
            }

            if (presentation.Color is string color && !string.IsNullOrWhiteSpace(color))
            {
                stringParamKeys = new[] { StringParamColor };
                stringParamValues = new[] { color };
            }
        }

        private static bool TryReadFloat(object value, out float result)
        {
            switch (value)
            {
                case float f:
                    result = f;
                    return true;
                case double d:
                    result = (float)d;
                    return true;
                case int i:
                    result = i;
                    return true;
                default:
                    result = 0f;
                    return false;
            }
        }

        private static float ResolveScale(int[] numericParamKeys, float[] numericParamValues)
        {
            if (numericParamKeys == null || numericParamValues == null) return 1f;
            var count = Math.Min(numericParamKeys.Length, numericParamValues.Length);
            for (int i = 0; i < count; i++)
            {
                if (numericParamKeys[i] == NumericParamScale && numericParamValues[i] > 0f)
                {
                    return numericParamValues[i];
                }
            }

            return 1f;
        }

        private static void ResolvePositions(object args, out Vec3[] positions)
        {
            positions = null;
            if (args is PresentationEventArgs presentation && presentation.Positions != null && presentation.Positions.Length > 0)
            {
                positions = presentation.Positions;
            }
        }

        private static float[] FlattenPositions(Vec3[] positions)
        {
            if (positions == null || positions.Length == 0) return null;

            var values = new float[positions.Length * 3];
            for (int i = 0; i < positions.Length; i++)
            {
                int offset = i * 3;
                values[offset] = positions[i].X;
                values[offset + 1] = positions[i].Y;
                values[offset + 2] = positions[i].Z;
            }

            return values;
        }
    }
}
