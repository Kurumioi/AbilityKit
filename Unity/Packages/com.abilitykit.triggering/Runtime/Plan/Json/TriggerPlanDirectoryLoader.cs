using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AbilityKit.Triggering.Eventing;
using AbilityKit.Triggering.Registry;
using AbilityKit.Triggering.Runtime.Config;
using AbilityKit.Triggering.Runtime.Plan;
using AbilityKit.Triggering.Variables.Numeric;
using Newtonsoft.Json;

namespace AbilityKit.Triggering.Runtime.Plan.Json
{
    /// <summary>
    /// 触发器计划目录加载器实现
    /// 支持多文件分离的触发器配置加载
    /// </summary>
    public sealed class TriggerPlanDirectoryLoader : ITriggerPlanDirectoryLoader, ITriggerPlanFileEnumerator
    {
        private readonly TriggerPlanJsonDatabase.ITextLoader _textLoader;
        private readonly TriggerPlanSourceConverter _converter;
        private readonly TriggerPlanConverter _planConverter;

        /// <summary>
        /// 创建一个新的目录加载器
        /// </summary>
        /// <param name="textLoader">文本加载器（用于从 Resources 或文件系统加载）</param>
        public TriggerPlanDirectoryLoader(TriggerPlanJsonDatabase.ITextLoader textLoader)
        {
            _textLoader = textLoader ?? throw new ArgumentNullException(nameof(textLoader));
            _converter = new TriggerPlanSourceConverter();
            _planConverter = new TriggerPlanConverter();
        }

        /// <inheritdoc />
        public TriggerPlanJsonDatabase LoadDirectory(string directory, string pattern = "*.json")
        {
            if (string.IsNullOrEmpty(directory))
                throw new ArgumentException("directory cannot be null or empty", nameof(directory));

            var files = GetFiles(directory, pattern);
            return LoadFiles(files);
        }

        /// <inheritdoc />
        public TriggerPlanJsonDatabase LoadDirectories(IEnumerable<string> directories, string pattern = "*.json")
        {
            if (directories == null)
                throw new ArgumentNullException(nameof(directories));

            var allFiles = new List<string>();
            foreach (var dir in directories)
            {
                if (string.IsNullOrEmpty(dir)) continue;
                allFiles.AddRange(GetFiles(dir, pattern));
            }

            return LoadFiles(allFiles);
        }

        /// <inheritdoc />
        public TriggerPlanJsonDatabase LoadWithManifest(string manifestPath, string moduleDirectory)
        {
            if (string.IsNullOrEmpty(manifestPath))
                throw new ArgumentException("manifestPath cannot be null or empty", nameof(manifestPath));
            if (string.IsNullOrEmpty(moduleDirectory))
                throw new ArgumentException("moduleDirectory cannot be null or empty", nameof(moduleDirectory));

            if (!_textLoader.TryLoad(manifestPath, out var manifestContent) || string.IsNullOrEmpty(manifestContent))
            {
                throw new InvalidOperationException($"Manifest file not found or empty: {manifestPath}");
            }

            var manifest = JsonConvert.DeserializeObject<TriggerPlanManifest>(manifestContent);
            if (manifest?.Entries == null || manifest.Entries.Count == 0)
            {
                return new TriggerPlanJsonDatabase();
            }

            var allFiles = new List<string>();
            foreach (var entry in manifest.Entries)
            {
                if (string.IsNullOrEmpty(entry.Path)) continue;
                var fullPath = NormalizePath(moduleDirectory, entry.Path);
                allFiles.Add(fullPath);
            }

            return LoadFiles(allFiles);
        }

        /// <inheritdoc />
        public IEnumerable<string> GetFiles(string directory, string pattern)
        {
            if (string.IsNullOrEmpty(directory))
                return Enumerable.Empty<string>();

            if (_textLoader is IFileSystemTextLoader fsLoader)
            {
                return fsLoader.GetFiles(directory, pattern);
            }

#if UNITY_EDITOR || (!UNITY_IOS && !UNITY_ANDROID)
            try
            {
                if (Directory.Exists(directory))
                {
                    return Directory.GetFiles(directory, pattern, SearchOption.AllDirectories)
                        .Where(f => f.EndsWith(".json", StringComparison.OrdinalIgnoreCase));
                }
            }
            catch
            {
            }
#endif

            return Enumerable.Empty<string>();
        }

        /// <inheritdoc />
        public bool TryReadFile(string path, out string content)
        {
            return _textLoader.TryLoad(path, out content);
        }

        private TriggerPlanJsonDatabase LoadFiles(IEnumerable<string> files)
        {
            var db = new TriggerPlanJsonDatabase();
            var allRecords = new List<TriggerPlanJsonDatabase.Record>();
            var allById = new Dictionary<int, TriggerPlan<object>>();
            var allStrings = new Dictionary<int, string>();

            foreach (var file in files)
            {
                if (!_textLoader.TryLoad(file, out var content) || string.IsNullOrEmpty(content))
                {
                    continue;
                }

                try
                {
                    var runtimeJson = ConvertToRuntimeJson(content);
                    var runtimeDto = JsonConvert.DeserializeObject<RuntimeJsonDto>(runtimeJson);

                    if (runtimeDto?.Triggers != null)
                    {
                        foreach (var trigger in runtimeDto.Triggers)
                        {
                            if (trigger.TriggerId <= 0) continue;

                            var eventId = trigger.EventId;
                            if (eventId == 0 && !string.IsNullOrEmpty(trigger.EventName))
                            {
                                eventId = StableStringId.Get("event:" + trigger.EventName);
                            }

                            var plan = BuildPlan(trigger);
                            allRecords.Add(new TriggerPlanJsonDatabase.Record(trigger.TriggerId, trigger.EventName, eventId, in plan));
                            allById[trigger.TriggerId] = plan;
                        }
                    }

                    if (runtimeDto?.Strings != null)
                    {
                        foreach (var kvp in runtimeDto.Strings)
                        {
                            allStrings[kvp.Key] = kvp.Value;
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogWarning($"[TriggerPlanDirectoryLoader] Failed to load file {file}: {ex.Message}");
                }
            }

            SetDatabaseRecords(db, allRecords, allById, allStrings);
            return db;
        }

        private string ConvertToRuntimeJson(string content)
        {
            var trimmed = content.TrimStart();

            if (trimmed.StartsWith("{\"$schema\"") ||
                trimmed.StartsWith("{\"version\"") ||
                trimmed.StartsWith("{\"triggers\":"))
            {
                return _converter.ConvertSourceToRuntimeJson(content);
            }

            return content;
        }

        private TriggerPlan<object> BuildPlan(RuntimeTriggerDto dto)
        {
            var actions = BuildActions(dto.Actions);
            var pred = dto.Predicate;

            if (pred == null || string.Equals(pred.Kind, "none", StringComparison.OrdinalIgnoreCase))
            {
                return new TriggerPlan<object>(
                    phase: dto.Phase,
                    priority: dto.Priority,
                    triggerId: dto.TriggerId,
                    actions: actions,
                    interruptPriority: 0,
                    cue: null,
                    schedule: default);
            }

            if (string.Equals(pred.Kind, "expr", StringComparison.OrdinalIgnoreCase))
            {
                var nodes = BuildExprNodes(pred.Nodes);
                var expr = new PredicateExprPlan(nodes);
                return new TriggerPlan<object>(
                    phase: dto.Phase,
                    priority: dto.Priority,
                    triggerId: dto.TriggerId,
                    predicateExpr: expr,
                    actions: actions,
                    interruptPriority: 0,
                    cue: null,
                    schedule: default);
            }

            throw new NotSupportedException($"Predicate kind not supported: {pred.Kind}");
        }

        private ActionCallPlan[] BuildActions(List<RuntimeActionDto> actions)
        {
            if (actions == null || actions.Count == 0)
                return Array.Empty<ActionCallPlan>();

            var result = new ActionCallPlan[actions.Count];
            for (int i = 0; i < actions.Count; i++)
            {
                result[i] = BuildAction(actions[i]);
            }
            return result;
        }

        private ActionCallPlan BuildAction(RuntimeActionDto dto)
        {
            if (dto == null) return default;

            var id = new ActionId(dto.ActionId);

            if (dto.Args != null && dto.Args.Count > 0)
            {
                var namedArgs = new Dictionary<string, ActionArgValue>(dto.Args.Count, StringComparer.OrdinalIgnoreCase);
                foreach (var kv in dto.Args)
                {
                    namedArgs[kv.Key] = new ActionArgValue(BuildValueRef(kv.Value), kv.Key);
                }
                return ActionCallPlan.WithArgs(id, namedArgs);
            }

            return new ActionCallPlan(id);
        }

        private NumericValueRef BuildValueRef(RuntimeValueRefDto dto)
        {
            if (dto == null) return default;

            if (!Enum.TryParse<ENumericValueRefKind>(dto.Kind, out var kind))
            {
                throw new InvalidOperationException($"Unknown NumericValueRef kind: {dto.Kind}");
            }

            return kind switch
            {
                ENumericValueRefKind.Const => NumericValueRef.Const(dto.ConstValue),
                ENumericValueRefKind.Blackboard => NumericValueRef.Blackboard(dto.BoardId, dto.KeyId),
                ENumericValueRefKind.PayloadField => NumericValueRef.PayloadField(dto.FieldId),
                ENumericValueRefKind.Var => NumericValueRef.Var(dto.DomainId, dto.Key),
                ENumericValueRefKind.Expr => NumericValueRef.Expr(dto.ExprText),
                _ => throw new InvalidOperationException($"Unsupported NumericValueRef kind: {kind}")
            };
        }

        private BoolExprNode[] BuildExprNodes(List<RuntimeExprNodeDto> nodes)
        {
            if (nodes == null || nodes.Count == 0)
                return Array.Empty<BoolExprNode>();

            var result = new BoolExprNode[nodes.Count];
            for (int i = 0; i < nodes.Count; i++)
            {
                result[i] = BuildExprNode(nodes[i]);
            }
            return result;
        }

        private BoolExprNode BuildExprNode(RuntimeExprNodeDto dto)
        {
            if (dto == null) return BoolExprNode.Const(true);

            if (!Enum.TryParse<EBoolExprNodeKind>(dto.Kind, out var kind))
            {
                throw new InvalidOperationException($"Unknown expr node kind: {dto.Kind}");
            }

            return kind switch
            {
                EBoolExprNodeKind.Const => BoolExprNode.Const(dto.ConstValue),
                EBoolExprNodeKind.Not => BoolExprNode.Not(),
                EBoolExprNodeKind.And => BoolExprNode.And(),
                EBoolExprNodeKind.Or => BoolExprNode.Or(),
                EBoolExprNodeKind.CompareNumeric => BuildCompareNode(dto),
                _ => throw new InvalidOperationException($"Unsupported expr node kind: {kind}")
            };
        }

        private BoolExprNode BuildCompareNode(RuntimeExprNodeDto dto)
        {
            if (!Enum.TryParse<ECompareOp>(dto.CompareOp, out var op))
            {
                throw new InvalidOperationException($"Unknown compare op: {dto.CompareOp}");
            }

            return BoolExprNode.Compare(op, BuildValueRef(dto.Left), BuildValueRef(dto.Right));
        }

        private static string NormalizePath(string baseDir, string relativePath)
        {
            if (string.IsNullOrEmpty(relativePath)) return baseDir;
            return Path.Combine(baseDir, relativePath).Replace('\\', '/');
        }

        private static void SetDatabaseRecords(
            TriggerPlanJsonDatabase db,
            List<TriggerPlanJsonDatabase.Record> records,
            Dictionary<int, TriggerPlan<object>> byId,
            Dictionary<int, string> strings)
        {
            var type = typeof(TriggerPlanJsonDatabase);

            var recordsField = type.GetField("_records",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            recordsField?.SetValue(db, records);

            var byIdField = type.GetField("_byTriggerId",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            byIdField?.SetValue(db, byId);

            var stringsField = type.GetField("_strings",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            stringsField?.SetValue(db, strings);
        }

        private static void LogWarning(string message)
        {
#if UNITY_EDITOR
            UnityEngine.Debug.LogWarning(message);
#else
            Console.Error.WriteLine(message);
#endif
        }

        #region JSON DTOs

        private class TriggerPlanManifest
        {
            [JsonProperty("entries")]
            public List<ManifestEntry> Entries;
        }

        private class ManifestEntry
        {
            [JsonProperty("trigger_id")]
            public int TriggerId;

            [JsonProperty("path")]
            public string Path;
        }

        private class RuntimeJsonDto
        {
            [JsonProperty("Triggers")]
            public List<RuntimeTriggerDto> Triggers;

            [JsonProperty("Strings")]
            public Dictionary<int, string> Strings;
        }

        private class RuntimeTriggerDto
        {
            [JsonProperty("TriggerId")]
            public int TriggerId;

            [JsonProperty("EventName")]
            public string EventName;

            [JsonProperty("EventId")]
            public int EventId;

            [JsonProperty("AllowExternal")]
            public bool AllowExternal;

            [JsonProperty("Phase")]
            public int Phase;

            [JsonProperty("Priority")]
            public int Priority;

            [JsonProperty("Predicate")]
            public RuntimePredicateDto Predicate;

            [JsonProperty("Actions")]
            public List<RuntimeActionDto> Actions;
        }

        private class RuntimePredicateDto
        {
            [JsonProperty("Kind")]
            public string Kind;

            [JsonProperty("Nodes")]
            public List<RuntimeExprNodeDto> Nodes;
        }

        private class RuntimeExprNodeDto
        {
            [JsonProperty("Kind")]
            public string Kind;

            [JsonProperty("ConstValue")]
            public bool ConstValue;

            [JsonProperty("CompareOp")]
            public string CompareOp;

            [JsonProperty("Left")]
            public RuntimeValueRefDto Left;

            [JsonProperty("Right")]
            public RuntimeValueRefDto Right;
        }

        private class RuntimeActionDto
        {
            [JsonProperty("ActionId")]
            public int ActionId;

            [JsonProperty("Arity")]
            public int Arity;

            [JsonProperty("Arg0")]
            public RuntimeValueRefDto Arg0;

            [JsonProperty("Arg1")]
            public RuntimeValueRefDto Arg1;

            [JsonProperty("Args")]
            public Dictionary<string, RuntimeValueRefDto> Args;
        }

        private class RuntimeValueRefDto
        {
            [JsonProperty("Kind")]
            public string Kind;

            [JsonProperty("ConstValue")]
            public double ConstValue;

            [JsonProperty("BoardId")]
            public int BoardId;

            [JsonProperty("KeyId")]
            public int KeyId;

            [JsonProperty("FieldId")]
            public int FieldId;

            [JsonProperty("DomainId")]
            public string DomainId;

            [JsonProperty("Key")]
            public string Key;

            [JsonProperty("ExprText")]
            public string ExprText;
        }

        #endregion
    }

    /// <summary>
    /// 文件系统文本加载器接口
    /// </summary>
    public interface IFileSystemTextLoader : TriggerPlanJsonDatabase.ITextLoader
    {
        /// <summary>
        /// 获取目录下匹配的文件
        /// </summary>
        IEnumerable<string> GetFiles(string directory, string pattern);
    }
}
