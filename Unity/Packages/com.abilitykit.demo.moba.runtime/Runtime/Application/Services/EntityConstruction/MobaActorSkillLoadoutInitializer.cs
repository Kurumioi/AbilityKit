using System;
using System.Collections.Generic;
using AbilityKit.Core.Logging;
using AbilityKit.Demo.Moba.Components;
using AbilityKit.Demo.Moba.Config.Core;
using AbilityKit.Protocol.Moba;

namespace AbilityKit.Demo.Moba.Services.EntityConstruction
{
    public sealed class MobaActorSkillLoadoutInitializer
    {
        public void Initialize(global::ActorEntity entity, in MobaPlayerLoadout loadout, MobaConfigDatabase config)
        {
            if (entity == null || config == null) return;

            try
            {
                var character = config.GetCharacter(loadout.HeroId);
                var activeSkillIds = loadout.SkillIds;
                int[] passiveSkillIds = null;

                if (activeSkillIds == null || activeSkillIds.Length == 0)
                {
                    var attributeTemplateId = loadout.AttributeTemplateId > 0 ? loadout.AttributeTemplateId :
                        (character != null ? character.AttributeTemplateId : 0);

                    if (attributeTemplateId > 0 && config.TryGetAttributeTemplate(attributeTemplateId, out var attrTemplate) && attrTemplate != null)
                    {
                        activeSkillIds = ToArray(attrTemplate.ActiveSkills);
                        passiveSkillIds = ToArray(attrTemplate.PassiveSkills);
                    }
                }

                var activeSkills = CreateActiveSkillRuntimes(activeSkillIds);
                var passiveSkills = CreatePassiveSkillRuntimes(passiveSkillIds);

                if (entity.hasSkillLoadout)
                {
                    entity.ReplaceSkillLoadout(activeSkills, passiveSkills);
                }
                else
                {
                    entity.AddSkillLoadout(activeSkills, passiveSkills);
                }
            }
            catch (Exception ex)
            {
                Log.Exception(ex, "[ActorEntityInitPipeline] InitializeSkillLoadout failed");
            }
        }

        private static ActiveSkillRuntime[] CreateActiveSkillRuntimes(int[] skillIds)
        {
            if (skillIds == null || skillIds.Length == 0) return Array.Empty<ActiveSkillRuntime>();
            var list = new List<ActiveSkillRuntime>(skillIds.Length);
            for (int i = 0; i < skillIds.Length; i++)
            {
                var id = skillIds[i];
                if (id <= 0) continue;
                list.Add(new ActiveSkillRuntime { SkillId = id, Level = 1, CooldownDurationMs = 0, CooldownEndTimeMs = 0L });
            }

            return list.Count == 0 ? Array.Empty<ActiveSkillRuntime>() : list.ToArray();
        }

        private static PassiveSkillRuntime[] CreatePassiveSkillRuntimes(int[] passiveSkillIds)
        {
            if (passiveSkillIds == null || passiveSkillIds.Length == 0) return Array.Empty<PassiveSkillRuntime>();
            var list = new List<PassiveSkillRuntime>(passiveSkillIds.Length);
            for (int i = 0; i < passiveSkillIds.Length; i++)
            {
                var id = passiveSkillIds[i];
                if (id <= 0) continue;
                list.Add(new PassiveSkillRuntime { PassiveSkillId = id, Level = 1, CooldownDurationMs = 0, CooldownEndTimeMs = 0L });
            }

            return list.Count == 0 ? Array.Empty<PassiveSkillRuntime>() : list.ToArray();
        }

        private static int[] ToArray(IReadOnlyList<int> list)
        {
            if (list == null || list.Count == 0) return Array.Empty<int>();
            if (list is int[] array) return array;
            var result = new int[list.Count];
            for (int i = 0; i < list.Count; i++) result[i] = list[i];
            return result;
        }
    }
}
