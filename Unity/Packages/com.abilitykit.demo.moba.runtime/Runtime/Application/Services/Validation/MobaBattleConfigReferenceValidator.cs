using System;
using System.Collections.Generic;
using AbilityKit.Demo.Moba.Config.BattleDemo.MO;
using AbilityKit.Demo.Moba.Config.Core;
using AbilityKit.Demo.Moba.Share.Config;
using AbilityKit.Triggering.Runtime.Config.Plans;
using AbilityKit.Triggering.Runtime.Plan.Json;

namespace AbilityKit.Demo.Moba.Services
{
    public sealed class MobaBattleConfigReferenceValidator : IMobaRuntimeValidator
    {
        private const string Source = "battle.config.references";

        public string Name => Source;

        public void Validate(in MobaRuntimeValidationContext context, MobaRuntimeValidationReport report)
        {
            if (report == null) return;

            if (!context.TryResolve<MobaConfigDatabase>(out var config) || config == null)
            {
                report.Error(Source, "config.database", "MobaConfigDatabase is not resolved; battle config references cannot be validated.");
                return;
            }

            context.TryResolve<TriggerPlanJsonDatabase>(out var triggers);

            ValidateBattleAttributeTemplates(config, report);
            ValidateSkills(config, triggers, report);
            ValidatePassiveSkills(config, triggers, report);
            ValidateBuffs(config, triggers, report);
            ValidateCharacters(config, report);
            ValidateProjectiles(config, report);
            ValidateProjectileLaunchers(config, report);
            ValidateSummons(config, report);
            ValidateAreas(config, triggers, report);
            ValidateGameplay(config, triggers, report);
            ValidateTagTemplates(config, report);
            ValidateContinuousTagTemplates(config, report);
        }

        private static void ValidateBattleAttributeTemplates(MobaConfigDatabase config, MobaRuntimeValidationReport report)
        {
            foreach (var template in All<BattleAttributeTemplateMO>(config))
            {
                if (template == null) continue;
                var path = $"battleAttributeTemplate.{template.Id}";

                if (template.Hp < 0)
                {
                    report.Warning(Source, path + ".hp", "battle attribute template hp is negative.", template.Id.ToString());
                }

                if (template.MaxHp < 0)
                {
                    report.Warning(Source, path + ".maxHp", "battle attribute template max hp is negative.", template.Id.ToString());
                }

                if (template.MaxHp > 0 && template.Hp > template.MaxHp)
                {
                    report.Warning(Source, path + ".hp", "battle attribute template hp exceeds max hp.", template.Id.ToString());
                }

                ValidateRefs(Ref<SkillMO>(config.TryGetSkill), template.ActiveSkills, report, path + ".activeSkills", "skill", template.Id);
                ValidateRefs(Ref<PassiveSkillMO>(config.TryGetPassiveSkill), template.PassiveSkills, report, path + ".passiveSkills", "passive skill", template.Id);
            }
        }

        private static void ValidateCharacters(MobaConfigDatabase config, MobaRuntimeValidationReport report)
        {
            foreach (var character in All<CharacterMO>(config))
            {
                if (character == null) continue;
                var path = $"character.{character.Id}";

                OptionalRef(Ref<ModelMO>(config.TryGetModel), character.ModelId, report, path + ".modelId", "model", character.Id);
                RequiredRef(Ref<BattleAttributeTemplateMO>(config.TryGetAttributeTemplate), character.AttributeTemplateId, report, path + ".attributeTemplateId", "attribute template", character.Id);
                ValidateRefs(Ref<SkillMO>(config.TryGetSkill), character.SkillIds, report, path + ".skillIds", "skill", character.Id);
                ValidateRefs(Ref<PassiveSkillMO>(config.TryGetPassiveSkill), character.PassiveSkillIds, report, path + ".passiveSkillIds", "passive skill", character.Id);
            }
        }

        private static void ValidateSkills(MobaConfigDatabase config, TriggerPlanJsonDatabase triggers, MobaRuntimeValidationReport report)
        {
            foreach (var skill in All<SkillMO>(config))
            {
                if (skill == null) continue;
                var path = $"skill.{skill.Id}";

                OptionalRef(Ref<SkillButtonTemplateMO>(config.TryGetSkillButtonTemplate), skill.SkillButtonTemplateId, report, path + ".skillButtonTemplateId", "skill button template", skill.Id);
                OptionalRef(Ref<SkillLevelTableMO>(config.TryGetSkillLevelTable), skill.LevelTableId, report, path + ".levelTableId", "skill level table", skill.Id);
                OptionalRef(Ref<SkillFlowMO>(config.TryGetSkillFlow), skill.PreCastFlowId, report, path + ".preCastFlowId", "pre-cast skill flow", skill.Id);
                RequiredRef(Ref<SkillFlowMO>(config.TryGetSkillFlow), skill.CastFlowId, report, path + ".castFlowId", "cast skill flow", skill.Id);

                if (skill.CooldownMs < 0)
                {
                    report.Warning(Source, path + ".cooldownMs", "skill cooldown is negative.", skill.Id.ToString());
                }

                if (skill.Range < 0)
                {
                    report.Warning(Source, path + ".range", "skill range is negative.", skill.Id.ToString());
                }

                if (skill.CastFlowId > 0 && config.TryGetSkillFlow(skill.CastFlowId, out var castFlow))
                {
                    ValidateSkillFlow(config, triggers, report, castFlow, $"skill.{skill.Id}.castFlow.{skill.CastFlowId}", skill.Id);
                }

                if (skill.PreCastFlowId > 0 && config.TryGetSkillFlow(skill.PreCastFlowId, out var preCastFlow))
                {
                    ValidateSkillFlow(config, triggers, report, preCastFlow, $"skill.{skill.Id}.preCastFlow.{skill.PreCastFlowId}", skill.Id);
                }
            }
        }

        private static void ValidatePassiveSkills(MobaConfigDatabase config, TriggerPlanJsonDatabase triggers, MobaRuntimeValidationReport report)
        {
            foreach (var passive in All<PassiveSkillMO>(config))
            {
                if (passive == null) continue;
                ValidateTriggerRefs(triggers, passive.TriggerIds, report, $"passiveSkill.{passive.Id}.triggerIds", passive.Id, TriggerPlanScope.OwnerBound);
            }
        }

        private static void ValidateBuffs(MobaConfigDatabase config, TriggerPlanJsonDatabase triggers, MobaRuntimeValidationReport report)
        {
            foreach (var buff in All<BuffMO>(config))
            {
                if (buff == null) continue;
                var path = $"buff.{buff.Id}";

                if (buff.DurationMs < 0)
                {
                    report.Warning(Source, path + ".durationMs", "buff duration is negative; use zero only for explicit instant/permanent semantics.", buff.Id.ToString());
                }

                if (buff.MaxStacks < 0)
                {
                    report.Warning(Source, path + ".maxStacks", "buff max stacks is negative.", buff.Id.ToString());
                }

                if (buff.IntervalMs < 0)
                {
                    report.Warning(Source, path + ".intervalMs", "buff interval is negative.", buff.Id.ToString());
                }

                if (buff.IntervalMs <= 0 && buff.OnIntervalEffects != null && buff.OnIntervalEffects.Count > 0)
                {
                    report.Warning(Source, path + ".intervalMs", "buff has interval effects but no positive interval configured.", buff.Id.ToString());
                }

                if (buff.IntervalMs > 0 && (buff.OnIntervalEffects == null || buff.OnIntervalEffects.Count == 0))
                {
                    report.Warning(Source, path + ".onIntervalEffects", "buff interval is configured but no interval effects are defined.", buff.Id.ToString());
                }

                if (buff.StackingPolicy == BuffStackingPolicy.IgnoreIfExists && buff.MaxStacks > 1)
                {
                    report.Warning(Source, path + ".stackingPolicy", "ignore-if-exists buff should not rely on multi-stack semantics.", buff.Id.ToString());
                }

                ValidateTriggerRefs(triggers, buff.OnAddEffects, report, path + ".onAddEffects", buff.Id);
                ValidateTriggerRefs(triggers, buff.OnRemoveEffects, report, path + ".onRemoveEffects", buff.Id);
                ValidateTriggerRefs(triggers, buff.OnIntervalEffects, report, path + ".onIntervalEffects", buff.Id);
                ValidateTriggerRefs(triggers, buff.TriggerIds, report, path + ".triggerIds", buff.Id, TriggerPlanScope.OwnerBound);
                OptionalRef(Ref<ContinuousTagTemplateMO>(config.TryGetContinuousTagTemplate), buff.ContinuousTagTemplateId, report, path + ".continuousTagTemplateId", "continuous tag template", buff.Id);
                OptionalRef((int id, out PresentationTemplateMO value) => TryGetTableRef(config, id, out value), buff.PresentationTemplateId, report, path + ".presentationTemplateId", "presentation template", buff.Id);

                ValidateBuffModifiers(config, buff, report);
            }
        }

        private static void ValidateBuffModifiers(MobaConfigDatabase config, BuffMO buff, MobaRuntimeValidationReport report)
        {
            var modifiers = buff.Modifiers;
            if (modifiers == null || modifiers.Count == 0) return;

            for (int i = 0; i < modifiers.Count; i++)
            {
                var modifier = modifiers[i];
                if (modifier == null) continue;

                var path = $"buff.{buff.Id}.modifiers[{i}]";
                if (modifier.TargetKind <= 0)
                {
                    report.Error(Source, path + ".targetKind", "modifier target kind is empty.", buff.Id.ToString());
                }

                if (modifier.TargetId <= 0)
                {
                    report.Error(Source, path + ".targetId", "modifier target id is empty.", buff.Id.ToString());
                }

                if (modifier.TargetKind == MobaContinuousModifierTargetKind.Attribute)
                {
                    RequiredRef(Ref<AttrTypeMO>(config.TryGetAttrType), modifier.TargetId, report, path + ".targetId", "attribute type", buff.Id);
                }
                else if (modifier.TargetKind == MobaContinuousModifierTargetKind.SkillParameter)
                {
                    ValidateSkillParameterModifierTarget(modifier.TargetId, report, path + ".targetId", buff.Id);
                }
            }
        }

        private static void ValidateSkillParameterModifierTarget(int targetId, MobaRuntimeValidationReport report, string path, int businessId)
        {
            if (targetId >= 1 && targetId <= 5) return;

            if (targetId == 2) return;
            report.Warning(Source, path, "skill parameter modifier target is not one of the built-in projectile/summon parameter ids.", businessId.ToString());
        }

        private static void ValidateProjectiles(MobaConfigDatabase config, MobaRuntimeValidationReport report)
        {
            foreach (var projectile in All<ProjectileMO>(config))
            {
                if (projectile == null) continue;
                var path = $"projectile.{projectile.Id}";

                if (projectile.Speed < 0f)
                {
                    report.Warning(Source, path + ".speed", "projectile speed is negative.", projectile.Id.ToString());
                }

                if (projectile.LifetimeMs < 0)
                {
                    report.Warning(Source, path + ".lifetimeMs", "projectile lifetime is negative.", projectile.Id.ToString());
                }

                if (projectile.MaxDistance < 0f)
                {
                    report.Warning(Source, path + ".maxDistance", "projectile max distance is negative.", projectile.Id.ToString());
                }
            }
        }

        private static void ValidateProjectileLaunchers(MobaConfigDatabase config, MobaRuntimeValidationReport report)
        {
            foreach (var launcher in All<ProjectileLauncherMO>(config))
            {
                if (launcher == null) continue;
                var path = $"projectileLauncher.{launcher.Id}";

                if (launcher.EmitterType == ProjectileEmitterType.None)
                {
                    report.Error(Source, path + ".emitterType", "projectile launcher emitter type is None.", launcher.Id.ToString());
                }

                if (launcher.DurationMs < 0)
                {
                    report.Warning(Source, path + ".durationMs", "projectile launcher duration is negative.", launcher.Id.ToString());
                }

                if (launcher.IntervalMs < 0)
                {
                    report.Warning(Source, path + ".intervalMs", "projectile launcher interval is negative.", launcher.Id.ToString());
                }

                if (launcher.DurationMs > 0 && launcher.IntervalMs <= 0)
                {
                    report.Error(Source, path + ".intervalMs", "projectile launcher duration requires a positive interval.", launcher.Id.ToString());
                }

                if (launcher.CountPerShot <= 0)
                {
                    report.Error(Source, path + ".countPerShot", "projectile launcher count per shot must be greater than zero.", launcher.Id.ToString());
                }
            }
        }

        private static void ValidateSummons(MobaConfigDatabase config, MobaRuntimeValidationReport report)
        {
            foreach (var summon in All<SummonMO>(config))
            {
                if (summon == null) continue;
                var path = $"summon.{summon.Id}";

                OptionalRef(Ref<ModelMO>(config.TryGetModel), summon.ModelId, report, path + ".modelId", "model", summon.Id);
                RequiredRef(Ref<BattleAttributeTemplateMO>(config.TryGetAttributeTemplate), summon.AttributeTemplateId, report, path + ".attributeTemplateId", "attribute template", summon.Id);
                ValidateRefs(Ref<SkillMO>(config.TryGetSkill), summon.SkillIds, report, path + ".skillIds", "skill", summon.Id);
                ValidateRefs(Ref<PassiveSkillMO>(config.TryGetPassiveSkill), summon.PassiveSkillIds, report, path + ".passiveSkillIds", "passive skill", summon.Id);
                ValidateRefs(Ref<ComponentTemplateMO>(config.TryGetComponentTemplate), summon.DefaultComponentTemplateIds, report, path + ".defaultComponentTemplateIds", "component template", summon.Id);

                if (summon.LifetimeMs < 0)
                {
                    report.Warning(Source, path + ".lifetimeMs", "summon lifetime is negative.", summon.Id.ToString());
                }

                if (summon.MaxAlivePerOwner < 0)
                {
                    report.Warning(Source, path + ".maxAlivePerOwner", "summon max alive per owner is negative.", summon.Id.ToString());
                }

                var scales = summon.AttrScales;
                if (scales == null) continue;
                for (int i = 0; i < scales.Count; i++)
                {
                    var scale = scales[i];
                    if (scale == null) continue;
                    RequiredRef(Ref<AttrTypeMO>(config.TryGetAttrType), scale.AttrId, report, $"{path}.attrScales[{i}].attrId", "attribute type", summon.Id);
                }
            }
        }

        private static void ValidateAreas(MobaConfigDatabase config, TriggerPlanJsonDatabase triggers, MobaRuntimeValidationReport report)
        {
            foreach (var area in All<AoeMO>(config))
            {
                if (area == null) continue;
                var path = $"aoe.{area.Id}";

                OptionalRef(Ref<ModelMO>(config.TryGetModel), area.ModelId, report, path + ".modelId", "model", area.Id);
                ValidateTriggerRefs(triggers, area.OnDelayTriggerIds, report, path + ".onDelayTriggerIds", area.Id);

                if (area.Radius < 0f)
                {
                    report.Warning(Source, path + ".radius", "area radius is negative.", area.Id.ToString());
                }

                if (area.DelayMs < 0)
                {
                    report.Warning(Source, path + ".delayMs", "area delay is negative.", area.Id.ToString());
                }

                if (area.MaxTargets < 0)
                {
                    report.Warning(Source, path + ".maxTargets", "area max targets is negative.", area.Id.ToString());
                }
            }
        }

        private static void ValidateGameplay(MobaConfigDatabase config, TriggerPlanJsonDatabase triggers, MobaRuntimeValidationReport report)
        {
            foreach (var gameplay in All<GameplayMO>(config))
            {
                if (gameplay == null) continue;
                ValidateTriggerRefs(triggers, gameplay.TriggerIds, report, $"gameplay.{gameplay.Id}.triggerIds", gameplay.Id, TriggerPlanScope.Global);

                if (gameplay.DefaultDurationMs < 0)
                {
                    report.Warning(Source, $"gameplay.{gameplay.Id}.defaultDurationMs", "gameplay default duration is negative.", gameplay.Id.ToString());
                }
            }
        }

        private static void ValidateTagTemplates(MobaConfigDatabase config, MobaRuntimeValidationReport report)
        {
            foreach (var template in All<TagTemplateMO>(config))
            {
                if (template == null) continue;
                var path = $"tagTemplate.{template.Id}";
                WarnEmptyName(template.Name, report, path + ".name", template.Id);
            }
        }

        private static void ValidateContinuousTagTemplates(MobaConfigDatabase config, MobaRuntimeValidationReport report)
        {
            foreach (var template in All<ContinuousTagTemplateMO>(config))
            {
                if (template == null) continue;
                var path = $"continuousTagTemplate.{template.Id}";
                WarnEmptyName(template.Name, report, path + ".name", template.Id);
            }
        }

        private static void ValidateSkillFlow(MobaConfigDatabase config, TriggerPlanJsonDatabase triggers, MobaRuntimeValidationReport report, SkillFlowMO flow, string path, int businessId)
        {
            if (flow == null) return;
            if (flow.Phases == null || flow.Phases.Count == 0)
            {
                report.Warning(Source, path + ".phases", "skill flow has no phases.", businessId.ToString());
                return;
            }

            for (int i = 0; i < flow.Phases.Count; i++)
            {
                ValidateSkillPhase(config, triggers, report, flow.Phases[i], $"{path}.phases[{i}]", businessId);
            }
        }

        private static void ValidateSkillPhase(MobaConfigDatabase config, TriggerPlanJsonDatabase triggers, MobaRuntimeValidationReport report, SkillPhaseDTO phase, string path, int businessId)
        {
            if (phase == null)
            {
                report.Warning(Source, path, "skill phase is null.", businessId.ToString());
                return;
            }

            switch ((SkillPhaseType)phase.Type)
            {
                case SkillPhaseType.Checks:
                    report.Error(Source, path + ".type", "checks skill phase is deprecated; use RulePlan trigger conditions instead.", businessId.ToString());
                    break;
                case SkillPhaseType.Timeline:
                    ValidateTimelinePhase(triggers, report, phase.Timeline, path + ".timeline", businessId);
                    break;
                case SkillPhaseType.Handlers:
                    report.Error(Source, path + ".type", "handlers skill phase is deprecated; use RulePlan trigger actions instead.", businessId.ToString());
                    break;
                case SkillPhaseType.RulePlan:
                    ValidateTriggerRefs(triggers, phase.RulePlan?.TriggerIds, report, path + ".rulePlan.triggerIds", businessId);
                    break;
                case SkillPhaseType.Sequence:
                case SkillPhaseType.Parallel:
                    ValidateChildPhases(config, triggers, report, phase.Children, path + ".children", businessId);
                    break;
                case SkillPhaseType.Repeat:
                    if (phase.Repeat == null)
                    {
                        report.Error(Source, path + ".repeat", "repeat phase has no repeat config.", businessId.ToString());
                    }
                    else
                    {
                        if (phase.Repeat.RepeatCount <= 0) report.Error(Source, path + ".repeat.repeatCount", "repeat count must be greater than zero.", businessId.ToString());
                        if (phase.Repeat.IntervalMs < 0) report.Error(Source, path + ".repeat.intervalMs", "repeat interval is negative.", businessId.ToString());
                        if (phase.Repeat.Phase == null) report.Error(Source, path + ".repeat.phase", "repeat phase has no explicit child phase.", businessId.ToString());
                        else ValidateSkillPhase(config, triggers, report, phase.Repeat.Phase, path + ".repeat.phase", businessId);
                    }
                    break;
                case SkillPhaseType.Delay:
                    if (phase.Delay == null) report.Error(Source, path + ".delay", "delay phase has no delay config.", businessId.ToString());
                    else if (phase.Delay.DelayMs < 0) report.Error(Source, path + ".delay.delayMs", "delay is negative.", businessId.ToString());
                    break;
                default:
                    report.Warning(Source, path + ".type", "skill phase type is not recognized.", businessId.ToString());
                    break;
            }
        }

        private static void ValidateChildPhases(MobaConfigDatabase config, TriggerPlanJsonDatabase triggers, MobaRuntimeValidationReport report, IReadOnlyList<SkillPhaseDTO> phases, string path, int businessId)
        {
            if (phases == null || phases.Count == 0)
            {
                report.Warning(Source, path, "composite skill phase has no children.", businessId.ToString());
                return;
            }

            for (int i = 0; i < phases.Count; i++)
            {
                ValidateSkillPhase(config, triggers, report, phases[i], $"{path}[{i}]", businessId);
            }
        }

        private static void ValidateTimelinePhase(TriggerPlanJsonDatabase triggers, MobaRuntimeValidationReport report, SkillTimelinePhaseDTO timeline, string path, int businessId)
        {
            if (timeline == null)
            {
                report.Warning(Source, path, "timeline phase has no timeline config.", businessId.ToString());
                return;
            }

            if (timeline.DurationMs < 0)
            {
                report.Warning(Source, path + ".durationMs", "timeline duration is negative.", businessId.ToString());
            }

            var events = timeline.Events;
            if (events == null || events.Length == 0) return;

            for (int i = 0; i < events.Length; i++)
            {
                var item = events[i];
                if (item == null) continue;
                var itemPath = $"{path}.events[{i}]";
                if (item.AtMs < 0) report.Warning(Source, itemPath + ".atMs", "timeline event time is negative.", businessId.ToString());
                RequiredTriggerRef(triggers, item.EffectId, report, itemPath + ".effectId", businessId);
            }
        }

        private static void ValidateTriggerRefs(TriggerPlanJsonDatabase triggers, IReadOnlyList<int> ids, MobaRuntimeValidationReport report, string path, int businessId, TriggerPlanScope? expectedScope = null)
        {
            if (ids == null || ids.Count == 0) return;
            for (int i = 0; i < ids.Count; i++)
            {
                RequiredTriggerRef(triggers, ids[i], report, $"{path}[{i}]", businessId, expectedScope);
            }
        }

        private static void RequiredTriggerRef(TriggerPlanJsonDatabase triggers, int id, MobaRuntimeValidationReport report, string path, int businessId, TriggerPlanScope? expectedScope = null)
        {
            if (id <= 0)
            {
                report.Error(Source, path, "trigger id is empty.", businessId.ToString());
                return;
            }

            if (triggers == null)
            {
                report.Warning(Source, path, "TriggerPlanJsonDatabase is not resolved; trigger reference cannot be checked.", businessId.ToString());
                return;
            }

            if (!triggers.TryGetRecordByTriggerId(id, out var record))
            {
                report.Error(Source, path, $"trigger id '{id}' does not exist.", businessId.ToString());
                return;
            }

            if (expectedScope.HasValue && record.Scope != expectedScope.Value)
            {
                report.Error(Source, path, $"trigger id '{id}' scope is {record.Scope}; expected {expectedScope.Value}.", businessId.ToString());
            }
        }

        private static void ValidateRefs<T>(TryGetRef<T> tryGet, IReadOnlyList<int> ids, MobaRuntimeValidationReport report, string path, string label, int businessId) where T : class
        {
            if (ids == null || ids.Count == 0) return;
            for (int i = 0; i < ids.Count; i++)
            {
                RequiredRef(tryGet, ids[i], report, $"{path}[{i}]", label, businessId);
            }
        }

        private static void RequiredRef<T>(TryGetRef<T> tryGet, int id, MobaRuntimeValidationReport report, string path, string label, int businessId) where T : class
        {
            if (id <= 0)
            {
                report.Error(Source, path, label + " id is empty.", businessId.ToString());
                return;
            }

            if (tryGet == null || !tryGet(id, out var value) || value == null)
            {
                report.Error(Source, path, $"{label} id '{id}' does not exist.", businessId.ToString());
            }
        }

        private static void OptionalRef<T>(TryGetRef<T> tryGet, int id, MobaRuntimeValidationReport report, string path, string label, int businessId) where T : class
        {
            if (id <= 0) return;

            if (tryGet == null || !tryGet(id, out var value) || value == null)
            {
                report.Error(Source, path, $"{label} id '{id}' does not exist.", businessId.ToString());
            }
        }

        private static IEnumerable<T> All<T>(MobaConfigDatabase config) where T : class
        {
            if (config == null) return Array.Empty<T>();
            var table = config.GetTable<T>();
            return table != null ? table.All() : Array.Empty<T>();
        }

        private static bool TryGetTableRef<T>(MobaConfigDatabase config, int id, out T value) where T : class
        {
            value = null;
            if (config == null) return false;

            var table = config.GetTable<T>();
            return table != null && table.TryGet(id, out value);
        }

        private static void WarnEmptyName(string name, MobaRuntimeValidationReport report, string path, int businessId)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                report.Warning(Source, path, "template name is empty.", businessId.ToString());
            }
        }

        private static TryGetRef<T> Ref<T>(TryGetRef<T> tryGet) where T : class
        {
            return tryGet;
        }

        private delegate bool TryGetRef<T>(int id, out T value) where T : class;
    }
}
