using System;
using System.Collections.Generic;
using AbilityKit.Ability.World.DI;
using AbilityKit.Ability.World.Services;
using AbilityKit.Ability.World.Services.Attributes;
using AbilityKit.Core.Continuous;

namespace AbilityKit.Demo.Moba.Services
{
    public interface IMobaContinuousModifierQueryService
    {
        IReadOnlyList<MobaContinuousModifierEntry> GetActiveModifiers(int ownerActorId);
        IReadOnlyList<MobaContinuousModifierEntry> GetActiveModifiers(int ownerActorId, int targetKind);
        IReadOnlyList<MobaContinuousModifierEntry> GetActiveModifiers(int ownerActorId, int targetKind, int targetId);
        float EvaluateNumeric(int ownerActorId, int targetKind, int targetId, float baseValue);
    }

    [WorldService(typeof(IMobaContinuousModifierQueryService))]
    [WorldService(typeof(MobaContinuousModifierQueryService))]
    public sealed class MobaContinuousModifierQueryService : IMobaContinuousModifierQueryService, IWorldInitializable, IService
    {
        private IContinuousManager _continuous;

        public void OnInit(IWorldResolver services)
        {
            services?.TryResolve(out _continuous);
        }

        public void Dispose()
        {
            _continuous = null;
        }

        public IReadOnlyList<MobaContinuousModifierEntry> GetActiveModifiers(int ownerActorId)
        {
            return Collect(ownerActorId, targetKind: 0, targetId: 0, filterKind: false, filterTarget: false);
        }

        public IReadOnlyList<MobaContinuousModifierEntry> GetActiveModifiers(int ownerActorId, int targetKind)
        {
            return Collect(ownerActorId, targetKind, targetId: 0, filterKind: true, filterTarget: false);
        }

        public IReadOnlyList<MobaContinuousModifierEntry> GetActiveModifiers(int ownerActorId, int targetKind, int targetId)
        {
            return Collect(ownerActorId, targetKind, targetId, filterKind: true, filterTarget: true);
        }

        public float EvaluateNumeric(int ownerActorId, int targetKind, int targetId, float baseValue)
        {
            return MobaContinuousModifierMath.EvaluateNumeric(baseValue, GetActiveModifiers(ownerActorId, targetKind, targetId));
        }

        private IReadOnlyList<MobaContinuousModifierEntry> Collect(int ownerActorId, int targetKind, int targetId, bool filterKind, bool filterTarget)
        {
            if (ownerActorId <= 0 || _continuous == null) return Array.Empty<MobaContinuousModifierEntry>();

            var active = _continuous.GetOwnerActiveContinuous(ownerActorId);
            if (active == null || active.Count == 0) return Array.Empty<MobaContinuousModifierEntry>();

            List<MobaContinuousModifierEntry> result = null;
            for (int i = 0; i < active.Count; i++)
            {
                var continuous = active[i];
                if (continuous == null || !continuous.IsActive) continue;
                if (!(continuous.Config is IMobaContinuousModifierConfig modifierConfig)) continue;
                if (!(continuous.Config is IMobaContinuousProjectionConfig projection)) continue;
                if (projection.OwnerActorId != ownerActorId) continue;

                var modifiers = modifierConfig.Modifiers;
                if (modifiers == null || modifiers.Count == 0) continue;

                var stack = GetStack(continuous.Config);
                for (int m = 0; m < modifiers.Count; m++)
                {
                    var spec = modifiers[m];
                    if (spec == null) continue;
                    if (filterKind && spec.TargetKind != targetKind) continue;
                    if (filterTarget && spec.TargetId != targetId) continue;

                    result ??= new List<MobaContinuousModifierEntry>();
                    result.Add(new MobaContinuousModifierEntry(continuous, projection, spec, stack));
                }
            }

            return result ?? (IReadOnlyList<MobaContinuousModifierEntry>)Array.Empty<MobaContinuousModifierEntry>();
        }

        private static int GetStack(IContinuousConfig config)
        {
            return config is IStackConfig stackConfig && stackConfig.Stack > 1 ? stackConfig.Stack : 1;
        }
    }
}
