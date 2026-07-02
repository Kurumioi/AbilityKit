using System;
using System.Collections.Generic;
using AbilityKit.Ability.World.DI;
using AbilityKit.Attributes.Core;
using AbilityKit.Core.Continuous;
using AbilityKit.Demo.Moba.Components;
using AbilityKit.Modifiers;

namespace AbilityKit.Demo.Moba.Services
{
    public sealed class MobaAttributeContinuousModifierProjector : IMobaContinuousModifierProjector
    {
        private MobaActorLookupService _actors;

        public int TargetKind => MobaContinuousModifierTargetKind.Attribute;

        public void OnInit(IWorldResolver services)
        {
            services?.TryResolve(out _actors);
        }

        public void Apply(IContinuous continuous, IMobaContinuousProjectionConfig projection, IReadOnlyList<IMobaContinuousModifierSpec> modifiers)
        {
            if (continuous == null || projection == null || modifiers == null || modifiers.Count == 0) return;
            if (projection.ModifierSourceId == 0) return;
            if (!TryGetAttributeTarget(projection.OwnerActorId, out var ctx, out var group)) return;
            RefreshResourceContext(projection.OwnerActorId, ctx);
 
            var stack = GetStack(continuous.Config);
            for (int i = 0; i < modifiers.Count; i++)
            {
                var spec = modifiers[i];
                if (!CanProject(spec)) continue;
 
                var attr = AttributeId.FromRaw(spec.TargetId);
                var data = CreateModifierData(continuous, spec, projection, stack, ctx);
                if (ctx != null) ctx.AddModifier(attr, data);
                else group?.AddModifier(attr, data);
            }
        }

        public void Clear(IMobaContinuousProjectionConfig projection)
        {
            if (projection == null) return;
            if (projection.ModifierSourceId == 0) return;
            if (!TryGetAttributeTarget(projection.OwnerActorId, out var ctx, out var group)) return;

            if (ctx != null) ctx.ClearModifiers(projection.ModifierSourceId);
            else group?.ClearModifiers(projection.ModifierSourceId);
            MobaContinuousModifierCaptureStore.Clear(projection.ModifierSourceId);
        }

        private void RefreshResourceContext(int actorId, AttributeContext ctx)
        {
            if (ctx == null || _actors == null) return;
            if (!_actors.TryGetActorEntity(actorId, out var entity) || entity == null) return;
            if (!entity.hasResourceContainer || entity.resourceContainer.Value == null || entity.resourceContainer.Value.Map == null) return;

            foreach (var pair in entity.resourceContainer.Value.Map)
            {
                var type = pair.Key;
                var state = pair.Value;
                if (type == ResourceType.None || state == null) continue;

                var name = type.ToString().ToLowerInvariant();
                var current = state.Current;
                var max = ResolveResourceMax(ctx, state);
                var ratio = max > 0f ? Math.Max(0f, Math.Min(1f, current / max)) : 0f;

                ctx.SetFloat($"resource.{name}.current", current);
                ctx.SetFloat($"resource.{name}.max", max);
                ctx.SetFloat($"resource.{name}.ratio", ratio);
            }
        }

        private static float ResolveResourceMax(AttributeContext ctx, ResourceState state)
        {
            if (state == null) return 0f;
            if (ctx != null && state.MaxAttribute.IsValid)
            {
                var max = ctx.GetValue(state.MaxAttribute);
                if (max > 0f) return max;
            }

            return state.LastMax;
        }

        private bool TryGetAttributeTarget(int actorId, out AttributeContext ctx, out AttributeGroup group)
        {
            ctx = null;
            group = null;

            if (_actors == null) return false;
            if (!_actors.TryGetActorEntity(actorId, out var entity) || entity == null) return false;
            if (!entity.hasAttributeGroup) return false;

            ctx = entity.attributeGroup.Ctx;
            group = entity.attributeGroup.Group;
            return ctx != null || group != null;
        }

        private static ModifierData CreateModifierData(IContinuous continuous, IMobaContinuousModifierSpec spec, IMobaContinuousProjectionConfig projection, int stack, AttributeContext context)
        {
            var key = ModifierKey.Create(ModifierKey.Categories.Attribute, ToByte(spec.TargetId));
            var sourceId = projection?.ModifierSourceId ?? 0;
            var stacked = ApplyStack(spec.Magnitude, stack);
            var magnitude = ApplyEvaluationPolicy(stacked, spec.EvaluationPolicy, context, out var capturedValue, out var hasCapturedValue);
            MobaContinuousModifierCaptureStore.Record(new MobaContinuousModifierCaptureRecord(
                continuous,
                projection?.OwnerActorId ?? 0,
                spec.TargetKind,
                spec.TargetId,
                spec.Op,
                spec.Value,
                spec.EvaluationPolicy,
                spec.Priority,
                stack,
                sourceId,
                spec.Magnitude,
                stacked,
                magnitude,
                capturedValue,
                hasCapturedValue,
                spec.EvaluationPolicy == MobaContinuousModifierEvaluationPolicy.OnApplySnapshot ? "OnApplySnapshot" : "Realtime",
                hasCapturedValue ? "Captured when continuous modifier was projected" : "Realtime magnitude kept for attribute calculation"));

            return new ModifierData
            {
                Key = key,
                Op = MobaContinuousModifierMath.ToModifierOp(spec.Op),
                Magnitude = magnitude,
                Priority = spec.Priority,
                SourceId = sourceId,
                SourceNameIndex = -1,
                Metadata = ModifierMetadata.CreateByIndex(-1, 0, sourceId)
            };
        }

        private static MagnitudeSource ApplyStack(MagnitudeSource magnitude, int stack)
        {
            if (stack <= 1) return magnitude;
            return magnitude.WithBaseValue(magnitude.BaseValue * stack);
        }

        private static MagnitudeSource ApplyEvaluationPolicy(MagnitudeSource magnitude, int policy, AttributeContext context, out float capturedValue, out bool hasCapturedValue)
        {
            capturedValue = 0f;
            hasCapturedValue = false;
            if (policy != MobaContinuousModifierEvaluationPolicy.OnApplySnapshot) return magnitude;

            capturedValue = magnitude.Calculate(context?.Level ?? 1f, context);
            hasCapturedValue = true;
            return MagnitudeSource.Fixed(capturedValue);
        }

        private static bool CanProject(IMobaContinuousModifierSpec spec)
        {
            return spec != null &&
                   spec.TargetKind == MobaContinuousModifierTargetKind.Attribute &&
                   spec.TargetId != 0;
        }

        private static int GetStack(IContinuousConfig config)
        {
            return config is IStackConfig stackConfig && stackConfig.Stack > 1 ? stackConfig.Stack : 1;
        }

        private static byte ToByte(int value)
        {
            if (value <= 0) return 0;
            if (value >= byte.MaxValue) return byte.MaxValue;
            return (byte)value;
        }
    }
}
