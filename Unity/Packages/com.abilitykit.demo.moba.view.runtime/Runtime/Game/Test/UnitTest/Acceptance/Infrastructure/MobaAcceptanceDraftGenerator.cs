using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace AbilityKit.Game.Test.UnitTest
{
    public static class MobaAcceptanceDraftGenerator
    {
        public const string GeneratedFrom = "config-draft-generator-v1";

        public static MobaAcceptanceExpectation GenerateContractDraftForSkill(
            int skillId,
            string skillFlowsPath,
            string triggerDirectory,
            string caseIdSuffix = "contract_draft")
        {
            var resolvedSkillFlowsPath = MobaAcceptanceRunner.ResolveProjectRelativePath(skillFlowsPath);
            var resolvedTriggerDirectory = MobaAcceptanceRunner.ResolveProjectRelativePath(triggerDirectory);
            if (!File.Exists(resolvedSkillFlowsPath)) throw new FileNotFoundException("Skill flow config missing.", resolvedSkillFlowsPath);
            if (!Directory.Exists(resolvedTriggerDirectory)) throw new DirectoryNotFoundException("Skill trigger directory missing: " + resolvedTriggerDirectory);

            var skillFlowsJson = File.ReadAllText(resolvedSkillFlowsPath);
            var skillEntry = FindObjectContainingId(skillFlowsJson, skillId);
            if (string.IsNullOrEmpty(skillEntry)) throw new InvalidOperationException("Skill flow entry missing for skill " + skillId + ".");

            var effectIds = ReadIntProperties(skillEntry, "\"EffectId\"");
            if (effectIds.Length == 0) throw new InvalidOperationException("No timeline EffectId configured for skill " + skillId + ".");

            var effectId = 0;
            string triggerPath = null;
            for (var i = 0; i < effectIds.Length; i++)
            {
                var candidatePath = Path.Combine(resolvedTriggerDirectory, "trigger_" + effectIds[i] + ".json");
                if (!File.Exists(candidatePath)) continue;
                effectId = effectIds[i];
                triggerPath = candidatePath;
                break;
            }

            if (effectId <= 0 || string.IsNullOrEmpty(triggerPath)) throw new InvalidOperationException("No configured trigger file found for skill " + skillId + ".");

            var triggerJson = File.ReadAllText(triggerPath);
            var actionObjects = ReadObjectsInArray(triggerJson, "\"actions\"");
            var expectedActions = new List<MobaAcceptanceActionExpectation>();
            var mustContain = new List<MobaAcceptanceTraceExpectation>
            {
                new MobaAcceptanceTraceExpectation { kind = "SkillCast", configId = skillId, minCount = 1 },
                new MobaAcceptanceTraceExpectation { kind = "EffectExecution", configId = effectId, minCount = 1 }
            };
            var relationships = new List<MobaAcceptanceRelationshipExpectation>();
            MobaAcceptanceProjectileExpectation projectile = null;

            for (var i = 0; i < actionObjects.Length; i++)
            {
                var actionObject = actionObjects[i];
                var type = ReadStringProperty(actionObject, "\"type\"");
                if (string.IsNullOrEmpty(type)) continue;

                var actionId = StableActionId(type);
                expectedActions.Add(new MobaAcceptanceActionExpectation { actionId = actionId, type = type });
                mustContain.Add(new MobaAcceptanceTraceExpectation { kind = "EffectAction", configId = actionId, underEffectId = effectId, minCount = 1 });
                relationships.Add(new MobaAcceptanceRelationshipExpectation
                {
                    parentKind = "EffectExecution",
                    parentConfigId = effectId,
                    childKind = "EffectAction",
                    childConfigId = actionId
                });

                if (string.Equals(type, "add_buff", StringComparison.OrdinalIgnoreCase))
                {
                    AddTraceExpectations("BuffApply", ReadIntPropertiesByNames(actionObject, "\"buff_id\"", "\"buffId\"", "\"buff_ids\"", "\"buffIds\""), effectId, mustContain, relationships);
                }
                else if (string.Equals(type, "remove_buff", StringComparison.OrdinalIgnoreCase))
                {
                    AddTraceExpectations("BuffRemove", ReadIntPropertiesByNames(actionObject, "\"buff_id\"", "\"buffId\"", "\"buff_ids\"", "\"buffIds\""), effectId, mustContain, relationships);
                }
                else if (string.Equals(type, "spawn_area", StringComparison.OrdinalIgnoreCase))
                {
                    AddTraceExpectations("AreaSpawn", ReadIntPropertiesByNames(actionObject, "\"area_id\"", "\"areaId\"", "\"aoe_id\"", "\"aoeId\""), effectId, mustContain, relationships);
                }
                else if (string.Equals(type, "summon", StringComparison.OrdinalIgnoreCase) || string.Equals(type, "spawn_summon", StringComparison.OrdinalIgnoreCase))
                {
                    AddTraceExpectations("SummonSpawn", ReadIntPropertiesByNames(actionObject, "\"summon_id\"", "\"summonId\"", "\"unit_id\"", "\"unitId\""), effectId, mustContain, relationships);
                }
                else if (string.Equals(type, "give_damage", StringComparison.OrdinalIgnoreCase) || string.Equals(type, "take_damage", StringComparison.OrdinalIgnoreCase))
                {
                    AddTraceExpectations("DamageAttack", ReadIntPropertiesByNames(actionObject, "\"reason_param\"", "\"reasonParam\"", "\"damage_id\"", "\"damageId\""), effectId, mustContain, relationships);
                }
                else if (string.Equals(type, "heal", StringComparison.OrdinalIgnoreCase))
                {
                    AddTraceExpectations("DamageApply", ReadIntPropertiesByNames(actionObject, "\"reason_param\"", "\"reasonParam\"", "\"heal_id\"", "\"healId\""), effectId, mustContain, relationships);
                }
                else if (string.Equals(type, "play_presentation", StringComparison.OrdinalIgnoreCase) || string.Equals(type, "play_effect", StringComparison.OrdinalIgnoreCase) || string.Equals(type, "play_sound", StringComparison.OrdinalIgnoreCase))
                {
                    AddTraceExpectations("PresentationPlay", ReadIntPropertiesByNames(actionObject, "\"presentation_id\"", "\"presentationId\"", "\"effect_id\"", "\"effectId\"", "\"sound_id\"", "\"soundId\""), effectId, mustContain, relationships);
                }

                if (string.Equals(type, "shoot_projectile", StringComparison.OrdinalIgnoreCase)
                    && TryReadIntPropertyByNames(actionObject, out var projectileId, "\"projectileId\"", "\"projectile_id\"")
                    && TryReadIntPropertyByNames(actionObject, out var launcherId, "\"launcherId\"", "\"launcher_id\""))
                {
                    projectile = new MobaAcceptanceProjectileExpectation { launcherId = launcherId, projectileId = projectileId };
                    AddTraceExpectations("ProjectileLaunch", new[] { projectileId }, effectId, mustContain, relationships);
                }
            }

            return new MobaAcceptanceExpectation
            {
                caseId = "skill_" + skillId + "_" + caseIdSuffix,
                description = "Generated lightweight acceptance contract draft for skill " + skillId + ".",
                worldId = "skill_" + skillId + "_draft_world",
                tickRate = 30,
                accelerated = true,
                category = "draft",
                tags = new[] { "generated", "contract-draft" },
                generatedFrom = GeneratedFrom,
                input = new MobaAcceptanceInputExpectation
                {
                    playerId = "p1",
                    slot = 1,
                    phase = "Press"
                },
                config = new MobaAcceptanceConfigExpectation
                {
                    skillId = skillId,
                    castFlowId = skillId,
                    effectId = effectId,
                    triggerId = effectId,
                    expectedActions = expectedActions.ToArray(),
                    expectedProjectile = projectile
                },
                mustContain = mustContain.ToArray(),
                mustNotContain = new MobaAcceptanceTraceExpectation[0],
                relationships = relationships.ToArray()
            };
        }

        public static string ExportContractDraftForSkill(
            int skillId,
            string skillFlowsPath,
            string triggerDirectory,
            string outputDirectory,
            string caseIdSuffix = "contract_draft")
        {
            var draft = GenerateContractDraftForSkill(skillId, skillFlowsPath, triggerDirectory, caseIdSuffix);
            return ExportContractDraft(draft, outputDirectory);
        }

        public static string ExportContractDraft(MobaAcceptanceExpectation draft, string outputDirectory)
        {
            if (draft == null) throw new ArgumentNullException(nameof(draft));
            if (string.IsNullOrWhiteSpace(outputDirectory)) throw new ArgumentException("Output directory is required.", nameof(outputDirectory));

            var resolvedOutputDirectory = MobaAcceptanceRunner.ResolveProjectRelativePath(outputDirectory);
            Directory.CreateDirectory(resolvedOutputDirectory);
            var fileName = SanitizeFileName(string.IsNullOrWhiteSpace(draft.caseId) ? "contract_draft" : draft.caseId) + ".expected.json";
            var outputPath = Path.Combine(resolvedOutputDirectory, fileName);
            File.WriteAllText(outputPath, JsonUtility.ToJson(draft, true));
            return outputPath;
        }

        private static string SanitizeFileName(string value)
        {
            var chars = value == null ? Array.Empty<char>() : value.ToCharArray();
            var filtered = new List<char>(chars.Length);
            for (var i = 0; i < chars.Length; i++)
            {
                var ch = chars[i];
                if (char.IsLetterOrDigit(ch) || ch == '_' || ch == '-' || ch == '.') filtered.Add(ch);
            }

            return filtered.Count > 0 ? new string(filtered.ToArray()) : "contract_draft";
        }

        private static void AddTraceExpectations(string kind, int[] configIds, int effectId, List<MobaAcceptanceTraceExpectation> mustContain, List<MobaAcceptanceRelationshipExpectation> relationships)
        {
            if (string.IsNullOrEmpty(kind) || configIds == null || configIds.Length == 0) return;
            for (var i = 0; i < configIds.Length; i++)
            {
                var configId = configIds[i];
                if (configId <= 0) continue;
                mustContain.Add(new MobaAcceptanceTraceExpectation { kind = kind, configId = configId, underEffectId = effectId, minCount = 1 });
                relationships.Add(new MobaAcceptanceRelationshipExpectation
                {
                    parentKind = "EffectExecution",
                    parentConfigId = effectId,
                    childKind = kind,
                    childConfigId = configId
                });
            }
        }

        private static int StableActionId(string actionType)
        {
            return StableHash32("action:" + actionType);
        }

        private static int StableHash32(string value)
        {
            unchecked
            {
                const uint offsetBasis = 2166136261u;
                const uint prime = 16777619u;
                var hash = offsetBasis;
                for (var i = 0; i < value.Length; i++)
                {
                    hash ^= value[i];
                    hash *= prime;
                }

                return (int)(hash & 0x7FFFFFFF);
            }
        }

        private static string FindObjectContainingId(string json, int id)
        {
            var cursor = 0;
            while (cursor < json.Length)
            {
                var objectStart = json.IndexOf('{', cursor);
                if (objectStart < 0) return null;
                var objectEnd = FindMatchingBrace(json, objectStart);
                if (objectEnd < 0) return null;
                var candidate = json.Substring(objectStart, objectEnd - objectStart + 1);
                var idCursor = 0;
                if (TryReadIntProperty(candidate, "\"Id\"", ref idCursor, out var value) && value == id) return candidate;
                cursor = objectEnd + 1;
            }

            return null;
        }

        private static string[] ReadObjectsInArray(string json, string propertyName)
        {
            var propertyIndex = json.IndexOf(propertyName, StringComparison.Ordinal);
            if (propertyIndex < 0) return new string[0];
            var arrayStart = json.IndexOf('[', propertyIndex + propertyName.Length);
            if (arrayStart < 0) return new string[0];
            var arrayEnd = FindMatchingBracket(json, arrayStart);
            if (arrayEnd < 0) return new string[0];

            var objects = new List<string>();
            var cursor = arrayStart + 1;
            while (cursor < arrayEnd)
            {
                var objectStart = json.IndexOf('{', cursor);
                if (objectStart < 0 || objectStart > arrayEnd) break;
                var objectEnd = FindMatchingBrace(json, objectStart);
                if (objectEnd < 0 || objectEnd > arrayEnd) break;
                objects.Add(json.Substring(objectStart, objectEnd - objectStart + 1));
                cursor = objectEnd + 1;
            }

            return objects.ToArray();
        }

        private static int[] ReadIntProperties(string json, string propertyName)
        {
            var values = new List<int>();
            var cursor = 0;
            while (TryReadIntProperty(json, propertyName, ref cursor, out var value))
            {
                values.Add(value);
            }

            return values.ToArray();
        }

        private static int[] ReadIntPropertiesByNames(string json, params string[] propertyNames)
        {
            var values = new List<int>();
            for (var i = 0; i < propertyNames.Length; i++)
            {
                values.AddRange(ReadIntProperties(json, propertyNames[i]));
            }

            return values.ToArray();
        }

        private static bool TryReadIntPropertyByNames(string json, out int value, params string[] propertyNames)
        {
            for (var i = 0; i < propertyNames.Length; i++)
            {
                if (TryReadIntProperty(json, propertyNames[i], out value)) return true;
            }

            value = 0;
            return false;
        }

        private static bool TryReadIntProperty(string json, string propertyName, out int value)
        {
            var cursor = 0;
            return TryReadIntProperty(json, propertyName, ref cursor, out value);
        }

        private static bool TryReadIntProperty(string json, string propertyName, ref int cursor, out int value)
        {
            value = 0;
            var propertyIndex = json.IndexOf(propertyName, cursor, StringComparison.Ordinal);
            if (propertyIndex < 0) return false;

            var colonIndex = json.IndexOf(':', propertyIndex + propertyName.Length);
            if (colonIndex < 0) return false;

            var start = colonIndex + 1;
            while (start < json.Length && char.IsWhiteSpace(json[start])) start++;
            var end = start;
            if (end < json.Length && json[end] == '-') end++;
            while (end < json.Length && char.IsDigit(json[end])) end++;
            cursor = end;
            return int.TryParse(json.Substring(start, end - start), out value);
        }

        private static string ReadStringProperty(string json, string propertyName)
        {
            var propertyIndex = json.IndexOf(propertyName, StringComparison.Ordinal);
            if (propertyIndex < 0) return null;
            var colonIndex = json.IndexOf(':', propertyIndex + propertyName.Length);
            if (colonIndex < 0) return null;
            var startQuote = json.IndexOf('"', colonIndex + 1);
            if (startQuote < 0) return null;
            var endQuote = json.IndexOf('"', startQuote + 1);
            if (endQuote < 0) return null;
            return json.Substring(startQuote + 1, endQuote - startQuote - 1);
        }

        private static int FindMatchingBrace(string json, int start)
        {
            return FindMatching(json, start, '{', '}');
        }

        private static int FindMatchingBracket(string json, int start)
        {
            return FindMatching(json, start, '[', ']');
        }

        private static int FindMatching(string json, int start, char open, char close)
        {
            var depth = 0;
            var inString = false;
            for (var i = start; i < json.Length; i++)
            {
                var ch = json[i];
                if (ch == '"' && (i == 0 || json[i - 1] != '\\')) inString = !inString;
                if (inString) continue;
                if (ch == open) depth++;
                else if (ch == close)
                {
                    depth--;
                    if (depth == 0) return i;
                }
            }

            return -1;
        }
    }
}
