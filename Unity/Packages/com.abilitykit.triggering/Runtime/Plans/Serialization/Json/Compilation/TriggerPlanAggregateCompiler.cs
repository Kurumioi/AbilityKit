using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace AbilityKit.Triggering.Runtime.Plan.Json
{
    /// <summary>
    /// Compiles split trigger plan documents into one deterministic runtime JSON artifact.
    /// </summary>
    public static class TriggerPlanAggregateCompiler
    {
        public readonly struct SourceDocument
        {
            public SourceDocument(string name, string content)
            {
                Name = name;
                Content = content;
            }

            public string Name { get; }

            public string Content { get; }
        }

        public static string Compile(IEnumerable<SourceDocument> documents)
        {
            if (documents == null)
            {
                throw new ArgumentNullException(nameof(documents));
            }

            var parser = new TriggerPlanJsonParser();
            var merged = new TriggerPlanJsonDatabase.TriggerPlanDatabaseDto
            {
                FormatVersion = 1,
                Triggers = new List<TriggerPlanJsonDatabase.TriggerPlanDto>(),
                Strings = new Dictionary<int, string>()
            };
            var triggerSources = new Dictionary<int, string>();
            var stringSources = new Dictionary<int, string>();

            foreach (var document in documents.OrderBy(
                         item => item.Name ?? string.Empty,
                         StringComparer.Ordinal))
            {
                if (string.IsNullOrWhiteSpace(document.Content))
                {
                    throw new InvalidOperationException(
                        $"Trigger plan source is empty: {DisplayName(document.Name)}");
                }

                var result = parser.Parse(document.Content, document.Name);
                if (!result.Success || result.Dto == null)
                {
                    var error = result.FirstError;
                    var message = string.IsNullOrEmpty(error.Message)
                        ? "Unknown trigger plan JSON parse error"
                        : error.Message;
                    throw new InvalidOperationException(
                        $"Failed to compile trigger plan source {DisplayName(document.Name)}: {message}",
                        error.Exception);
                }

                MergeTriggers(merged, result.Dto, document.Name, triggerSources);
                MergeStrings(merged, result.Dto, document.Name, stringSources);
                RejectUnsupportedAggregateSections(result.Dto, document.Name);
            }

            merged.Triggers.Sort((left, right) => left.TriggerId.CompareTo(right.TriggerId));
            var settings = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                NullValueHandling = NullValueHandling.Ignore,
                DefaultValueHandling = DefaultValueHandling.Ignore
            };
            var json = JsonConvert.SerializeObject(merged, settings);
            return json.Replace("\r\n", "\n") + "\n";
        }

        private static void MergeTriggers(
            TriggerPlanJsonDatabase.TriggerPlanDatabaseDto target,
            TriggerPlanJsonDatabase.TriggerPlanDatabaseDto source,
            string sourceName,
            Dictionary<int, string> triggerSources)
        {
            if (source.Triggers == null)
            {
                return;
            }

            foreach (var trigger in source.Triggers)
            {
                if (trigger == null)
                {
                    continue;
                }

                if (trigger.TriggerId <= 0)
                {
                    throw new InvalidOperationException(
                        $"Trigger plan source {DisplayName(sourceName)} contains a non-positive trigger ID: {trigger.TriggerId}.");
                }

                if (triggerSources.TryGetValue(trigger.TriggerId, out var existingSource))
                {
                    throw new InvalidOperationException(
                        $"Duplicate trigger ID {trigger.TriggerId} in {DisplayName(existingSource)} and {DisplayName(sourceName)}.");
                }

                triggerSources.Add(trigger.TriggerId, sourceName);
                target.Triggers.Add(trigger);
            }
        }

        private static void MergeStrings(
            TriggerPlanJsonDatabase.TriggerPlanDatabaseDto target,
            TriggerPlanJsonDatabase.TriggerPlanDatabaseDto source,
            string sourceName,
            Dictionary<int, string> stringSources)
        {
            if (source.Strings == null)
            {
                return;
            }

            foreach (var pair in source.Strings.OrderBy(item => item.Key))
            {
                if (target.Strings.TryGetValue(pair.Key, out var existingValue))
                {
                    if (!string.Equals(existingValue, pair.Value, StringComparison.Ordinal))
                    {
                        throw new InvalidOperationException(
                            $"Conflicting trigger string ID {pair.Key} in {DisplayName(stringSources[pair.Key])} and {DisplayName(sourceName)}.");
                    }

                    continue;
                }

                target.Strings.Add(pair.Key, pair.Value);
                stringSources.Add(pair.Key, sourceName);
            }
        }

        private static void RejectUnsupportedAggregateSections(
            TriggerPlanJsonDatabase.TriggerPlanDatabaseDto source,
            string sourceName)
        {
            if (HasItems(source.Templates)
                || HasItems(source.Modules)
                || HasItems(source.ModuleInstances)
                || HasItems(source.TemplateInstances)
                || HasItems(source.Behaviors)
                || HasItems(source.Nodes))
            {
                throw new InvalidOperationException(
                    $"Trigger plan source {DisplayName(sourceName)} contains module or behavior sections. "
                    + "Split aggregate compilation currently accepts triggers and strings only.");
            }
        }

        private static bool HasItems<T>(ICollection<T> values)
        {
            return values != null && values.Count > 0;
        }

        private static bool HasItems<TKey, TValue>(IDictionary<TKey, TValue> values)
        {
            return values != null && values.Count > 0;
        }

        private static string DisplayName(string sourceName)
        {
            return string.IsNullOrEmpty(sourceName) ? "<source>" : sourceName;
        }
    }
}
