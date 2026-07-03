using System.Collections.Generic;
using AbilityKit.Ability.Config;
using AbilityKit.Demo.Moba.Share.Config;

namespace AbilityKit.Demo.Moba.Config.Core
{
    /// <summary>
    /// MOBA config group declarations and table membership.
    /// </summary>
    public static class MobaConfigGroups
    {
        private static readonly LegacyJsonConfigGroupDeserializer _legacyJsonDeserializer = LegacyJsonConfigGroupDeserializer.Instance;

        /// <summary>
        /// Legacy JSON group for tables that use the standard JSON DTO format.
        /// </summary>
        public static readonly ConfigGroup LegacyJson = new ConfigGroup(
            ConfigGroupNames.LegacyJson,
            MobaConfigPaths.DefaultResourcesDir,
            _legacyJsonDeserializer,
            new ConfigTableDefinition(MobaConfigPaths.CharactersFile, typeof(CharacterDTO), typeof(BattleDemo.MO.CharacterMO), ConfigGroupNames.LegacyJson),
            new ConfigTableDefinition(MobaConfigPaths.AttributeTemplatesFile, typeof(BattleAttributeTemplateDTO), typeof(BattleDemo.MO.BattleAttributeTemplateMO), ConfigGroupNames.LegacyJson),
            new ConfigTableDefinition(MobaConfigPaths.BuffsFile, typeof(BuffDTO), typeof(BattleDemo.MO.BuffMO), ConfigGroupNames.LegacyJson),
            new ConfigTableDefinition(MobaConfigPaths.ContinuousProcessesFile, typeof(ContinuousProcessDTO), typeof(BattleDemo.MO.ContinuousProcessMO), ConfigGroupNames.LegacyJson),
            new ConfigTableDefinition(MobaConfigPaths.SkillsFile, typeof(SkillDTO), typeof(BattleDemo.MO.SkillMO), ConfigGroupNames.LegacyJson),
            new ConfigTableDefinition(MobaConfigPaths.PassiveSkillsFile, typeof(PassiveSkillDTO), typeof(BattleDemo.MO.PassiveSkillMO), ConfigGroupNames.LegacyJson),
            new ConfigTableDefinition(MobaConfigPaths.SkillFlowsFile, typeof(SkillFlowDTO), typeof(BattleDemo.MO.SkillFlowMO), ConfigGroupNames.LegacyJson),
            new ConfigTableDefinition(MobaConfigPaths.SkillLevelTablesFile, typeof(SkillLevelTableDTO), typeof(BattleDemo.MO.SkillLevelTableMO), ConfigGroupNames.LegacyJson),
            new ConfigTableDefinition(MobaConfigPaths.AttributeTypesFile, typeof(AttrTypeDTO), typeof(BattleDemo.MO.AttrTypeMO), ConfigGroupNames.LegacyJson),
            new ConfigTableDefinition(MobaConfigPaths.ModelsFile, typeof(ModelDTO), typeof(BattleDemo.MO.ModelMO), ConfigGroupNames.LegacyJson),
            new ConfigTableDefinition(MobaConfigPaths.ProjectileLaunchersFile, typeof(ProjectileLauncherDTO), typeof(BattleDemo.MO.ProjectileLauncherMO), ConfigGroupNames.LegacyJson),
            new ConfigTableDefinition(MobaConfigPaths.ProjectilesFile, typeof(ProjectileDTO), typeof(BattleDemo.MO.ProjectileMO), ConfigGroupNames.LegacyJson),
            new ConfigTableDefinition(MobaConfigPaths.AoesFile, typeof(AoeDTO), typeof(BattleDemo.MO.AoeMO), ConfigGroupNames.LegacyJson),
            new ConfigTableDefinition(MobaConfigPaths.EmittersFile, typeof(EmitterDTO), typeof(BattleDemo.MO.EmitterMO), ConfigGroupNames.LegacyJson),
            new ConfigTableDefinition(MobaConfigPaths.SummonsFile, typeof(SummonDTO), typeof(BattleDemo.MO.SummonMO), ConfigGroupNames.LegacyJson),
            new ConfigTableDefinition(MobaConfigPaths.ComponentTemplatesFile, typeof(ComponentTemplateDTO), typeof(BattleDemo.MO.ComponentTemplateMO), ConfigGroupNames.LegacyJson),
            new ConfigTableDefinition(MobaConfigPaths.SkillButtonTemplatesFile, typeof(SkillButtonTemplateDTO), typeof(BattleDemo.MO.SkillButtonTemplateMO), ConfigGroupNames.LegacyJson),
            new ConfigTableDefinition(MobaConfigPaths.TagTemplatesFile, typeof(TagTemplateDTO), typeof(BattleDemo.MO.TagTemplateMO), ConfigGroupNames.LegacyJson),
            new ConfigTableDefinition(MobaConfigPaths.ContinuousTagTemplatesFile, typeof(ContinuousTagTemplateDTO), typeof(BattleDemo.MO.ContinuousTagTemplateMO), ConfigGroupNames.LegacyJson),
            new ConfigTableDefinition(MobaConfigPaths.SearchQueryTemplatesFile, typeof(SearchQueryTemplateDTO), typeof(BattleDemo.MO.SearchQueryTemplateMO), ConfigGroupNames.LegacyJson),
            new ConfigTableDefinition(MobaConfigPaths.SpawnSummonActionTemplatesFile, typeof(SpawnSummonActionTemplateDTO), typeof(BattleDemo.MO.SpawnSummonActionTemplateMO), ConfigGroupNames.LegacyJson),
            new ConfigTableDefinition(MobaConfigPaths.PresentationTemplatesFile, typeof(PresentationTemplateDTO), typeof(BattleDemo.MO.PresentationTemplateMO), ConfigGroupNames.LegacyJson),
            new ConfigTableDefinition(MobaConfigPaths.GameplaysFile, typeof(GameplayDTO), typeof(BattleDemo.MO.GameplayMO), ConfigGroupNames.LegacyJson)
        );

        /// <summary>
        /// Gets all config groups.
        /// </summary>
        public static IReadOnlyList<IConfigGroup> All => new IConfigGroup[] { LegacyJson };

        /// <summary>
        /// Gets a config group by name.
        /// </summary>
        public static IConfigGroup GetByName(string name)
        {
            foreach (var group in All)
            {
                if (group.Name == name)
                    return group;
            }
            return null;
        }

        /// <summary>
        /// Gets the table definition for a table file name.
        /// </summary>
        public static ConfigTableDefinition GetTableEntry(string tableName)
        {
            foreach (var group in All)
            {
                foreach (var entry in group.Tables)
                {
                    if (entry.FileWithoutExt == tableName)
                        return entry;
                }
            }
            return null;
        }
    }
}
