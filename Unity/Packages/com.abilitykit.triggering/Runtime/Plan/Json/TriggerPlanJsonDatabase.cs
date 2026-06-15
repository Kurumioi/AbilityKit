using System;
using System.Collections.Generic;
using AbilityKit.Core.Eventing;
using AbilityKit.Triggering.Eventing;
using AbilityKit.Triggering.Runtime;
using AbilityKit.Triggering.Runtime.Config;
using AbilityKit.Triggering.Runtime.Plan;
using AbilityKit.Triggering.Registry;
using AbilityKit.Triggering.Runtime.Config.Plans;

namespace AbilityKit.Triggering.Runtime.Plan.Json
{
    /// <summary>
    /// 触发器计划数据库（从 JSON 加载 TriggerPlan）
    /// 支持两种格式：
    /// 1. 运行时格式（Triggers 数组 + Strings 字典）- 直接加载
    /// 2. 源格式（triggers 数组 + actions/conditions 定义）- 自动转换后加载
    /// </summary>
    public sealed class TriggerPlanJsonDatabase
    {
        private static readonly TriggerPlanJsonParser _parser = new TriggerPlanJsonParser();
        /// <summary>
        /// Cue 工厂接口
        /// 负责将 JSON 中的 CueKind / CueVfxId / CueSfxId 解析为具体的 ITriggerCue 实例
        /// </summary>
        public interface ICueFactory
        {
            ITriggerCue Create(string cueKind, string cueVfxId, string cueSfxId);
        }

        /// <summary>
        /// 默认 Cue 工厂：始终返回 NullTriggerCue
        /// 业务项目可注册自定义实现
        /// </summary>
        public sealed class DefaultCueFactory : ICueFactory
        {
            public static readonly DefaultCueFactory Instance = new DefaultCueFactory();

            private DefaultCueFactory() { }

            public ITriggerCue Create(string cueKind, string cueVfxId, string cueSfxId)
            {
                return NullTriggerCue.Instance;
            }
        }

        public interface ITextLoader
        {
            bool TryLoad(string id, out string text);
        }

        [Serializable]
        internal sealed class TriggerPlanDatabaseDto
        {
            public int FormatVersion;

            public List<TriggerPlanDto> Triggers;

            public Dictionary<int, string> Strings;
        }

        [Serializable]
        internal sealed class TriggerPlanDto
        {
            public int TriggerId;
            public string EventName;
            public int EventId;
            public bool AllowExternal;
            public int Phase;
            public int Priority;
            public TriggerPlanScope Scope;
            public TriggerTemplateBindingDto Template;
            public PredicatePlanDto Predicate;
            public List<ActionCallPlanDto> Actions;
            public ExecutionControlPlanDto ExecutionControl;
            public ExecutionNodeDto ExecutionRoot;
 
            /// <summary>
            /// 表现 Cue 类型名，由 ICueFactory 解析为 ITriggerCue 实例
            /// </summary>
            public string CueKind;

            /// <summary>
            /// Cue VFX 标识（供工厂实现使用）
            /// </summary>
            public string CueVfxId;

            /// <summary>
            /// Cue SFX 标识（供工厂实现使用）
            /// </summary>
            public string CueSfxId;
        }

        [Serializable]
        internal sealed class TriggerTemplateBindingDto
        {
            public string TemplateId;
            public Dictionary<string, NumericValueRefDto> Bindings;
        }

        [Serializable]
        internal sealed class PredicatePlanDto
        {
            public string Kind;
            public List<BoolExprNodeDto> Nodes;
        }

        [Serializable]
        internal sealed class BoolExprNodeDto
        {
            public string Kind;
            public bool ConstValue;
            public string CompareOp;
            public NumericValueRefDto Left;
            public NumericValueRefDto Right;
        }

        [Serializable]
        internal sealed class ActionCallPlanDto
        {
            public int ActionId;
            public int Arity;
            public NumericValueRefDto Arg0;
            public NumericValueRefDto Arg1;

            /// <summary>
            /// 具名参数字典（key=参数名）
            /// 优先级高于 Arg0/Arg1
            /// </summary>
            public Dictionary<string, NumericValueRefDto> Args;
        }

        [Serializable]
        internal sealed class ExecutionControlPlanDto
        {
            public string Mode;
            public int MaxExecutions;
            public float CooldownMs;
        }

        [Serializable]
        internal sealed class ExecutionNodeDto
        {
            public string Kind;
            public ActionCallPlanDto Action;
            public PredicatePlanDto Condition;
            public PredicatePlanDto UntilCondition;
            public List<ExecutionNodeDto> Children;
            public List<ExecutionNodeDto> ElseChildren;
            public int Count = 1;
            public int MaxIterations = 1;
            public float Weight = 1f;
            public string Reason;
            public string SourceKind;
            public string SourceId;
            public string SourcePath;
        }

        [Serializable]
        internal sealed class NumericValueRefDto
        {
            public string Kind;
            public double ConstValue;
            public int BoardId;
            public int KeyId;
            public int FieldId;
            public string DomainId;
            public string Key;
            public string ExprText;
        }

        public readonly struct Record
        {
            public readonly int TriggerId;
            public readonly string EventName;
            public readonly int EventId;
            public readonly TriggerPlanScope Scope;
            public readonly TriggerPlan<object> Plan;
            public readonly ITriggerPlanExecutable ExecutionRoot;
 
            public Record(int triggerId, string eventName, int eventId, TriggerPlanScope scope, in TriggerPlan<object> plan, ITriggerPlanExecutable executionRoot = null)
            {
                TriggerId = triggerId;
                EventName = eventName;
                EventId = eventId;
                Scope = scope;
                Plan = plan;
                ExecutionRoot = executionRoot;
            }
        }

        private List<Record> _records = new List<Record>();
        private Dictionary<int, Record> _recordsByTriggerId = new Dictionary<int, Record>();
        private Dictionary<int, TriggerPlan<object>> _byTriggerId = new Dictionary<int, TriggerPlan<object>>();
        private Dictionary<int, ITriggerPlanExecutable> _executionRootsByTriggerId = new Dictionary<int, ITriggerPlanExecutable>();
        private Dictionary<int, string> _strings = new Dictionary<int, string>();
        private ICueFactory _cueFactory = DefaultCueFactory.Instance;

        public IReadOnlyList<Record> Records => _records;

        public ICueFactory CueFactory
        {
            get => _cueFactory ?? DefaultCueFactory.Instance;
            set => _cueFactory = value ?? DefaultCueFactory.Instance;
        }

        public bool TryGetString(int id, out string value)
        {
            value = null;
            if (id == 0) return false;
            return _strings != null && _strings.TryGetValue(id, out value);
        }

        public bool TryGetRecordByTriggerId(int triggerId, out Record record)
        {
            record = default;
            if (triggerId <= 0) return false;
            return _recordsByTriggerId != null && _recordsByTriggerId.TryGetValue(triggerId, out record);
        }

        public bool TryGetPlanByTriggerId(int triggerId, out TriggerPlan<object> plan)
        {
            plan = default;
            if (triggerId <= 0) return false;
            return _byTriggerId != null && _byTriggerId.TryGetValue(triggerId, out plan);
        }

        public bool TryGetExecutionRootByTriggerId(int triggerId, out ITriggerPlanExecutable root)
        {
            root = null;
            if (triggerId <= 0) return false;
            return _executionRootsByTriggerId != null && _executionRootsByTriggerId.TryGetValue(triggerId, out root) && root != null;
        }

        public void AddString(int id, string value, bool replaceExisting = true)
        {
            if (id == 0) return;
            if (_strings == null) _strings = new Dictionary<int, string>();
            if (replaceExisting || !_strings.ContainsKey(id))
            {
                _strings[id] = value;
            }
        }

        public void AddRecord(in Record record, bool replaceExisting = true)
        {
            if (record.TriggerId <= 0) return;
            if (_records == null) _records = new List<Record>();
            if (_recordsByTriggerId == null) _recordsByTriggerId = new Dictionary<int, Record>();
            if (_byTriggerId == null) _byTriggerId = new Dictionary<int, TriggerPlan<object>>();
            if (_executionRootsByTriggerId == null) _executionRootsByTriggerId = new Dictionary<int, ITriggerPlanExecutable>();

            var existingIndex = -1;
            for (int i = 0; i < _records.Count; i++)
            {
                if (_records[i].TriggerId == record.TriggerId)
                {
                    existingIndex = i;
                    break;
                }
            }

            if (existingIndex >= 0)
            {
                if (!replaceExisting) return;
                _records[existingIndex] = record;
            }
            else
            {
                _records.Add(record);
            }

            _recordsByTriggerId[record.TriggerId] = record;
            _byTriggerId[record.TriggerId] = record.Plan;
            if (record.ExecutionRoot != null)
            {
                _executionRootsByTriggerId[record.TriggerId] = record.ExecutionRoot;
            }
            else
            {
                _executionRootsByTriggerId.Remove(record.TriggerId);
            }
        }

        public void MergeFrom(TriggerPlanJsonDatabase source, bool replaceExisting = true)
        {
            if (source == null) return;

            if (source._strings != null)
            {
                foreach (var kvp in source._strings)
                {
                    AddString(kvp.Key, kvp.Value, replaceExisting);
                }
            }

            if (source._records != null)
            {
                for (int i = 0; i < source._records.Count; i++)
                {
                    AddRecord(source._records[i], replaceExisting);
                }
            }
        }
 
        public void Load(ITextLoader loader, string id)
        {
            if (loader == null) throw new ArgumentNullException(nameof(loader));
            if (string.IsNullOrEmpty(id)) throw new ArgumentException(nameof(id));

            if (!loader.TryLoad(id, out var json) || string.IsNullOrEmpty(json))
            {
                throw new InvalidOperationException($"Trigger plan json not found or empty: {id}");
            }

            LoadFromJson(json, id);
        }

        public void LoadFromJson(string json, string sourceName = null)
        {
            if (string.IsNullOrEmpty(json))
            {
                throw new InvalidOperationException($"Trigger plan json is empty: {sourceName ?? "<json>"}");
            }

            LoadFromDto(ParseRuntimeDto(json, sourceName));
        }

        public void RegisterAll<TCtx>(TriggerRunner<TCtx> runner)
        {
            RegisterAll(runner, TriggerPlanScope.Global);
        }

        public void RegisterAll<TCtx>(TriggerRunner<TCtx> runner, TriggerPlanScope scope)
        {
            if (runner == null) throw new ArgumentNullException(nameof(runner));

            for (int i = 0; i < _records.Count; i++)
            {
                var r = _records[i];
                if (r.EventId == 0) continue;
                if (r.Scope != scope) continue;
                var key = new EventKey<object>(r.EventId);
                runner.RegisterPlan<object, TCtx>(key, r.Plan);
            }
        }

        internal static TriggerPlanDatabaseDto ParseRuntimeDto(string json, string sourceName = null)
        {
            var result = _parser.Parse(json, sourceName);
            if (result.Success && result.Dto != null)
            {
                return result.Dto;
            }

            var error = result.FirstError;
            var message = string.IsNullOrEmpty(error.Message)
                ? "Unknown trigger plan json parse error"
                : error.Message;
            throw new InvalidOperationException($"Failed to parse trigger plan json: {sourceName ?? "<json>"}. {message}", error.Exception);
        }

        internal static bool IsSourceFormatJson(string json)
        {
            return _parser.DetectFormat(json) == TriggerPlanJsonFormat.Source;
        }

        internal void LoadFromDto(TriggerPlanDatabaseDto dto)
        {
            var next = new List<Record>();
            var byTriggerId = new Dictionary<int, TriggerPlan<object>>();
            var executionRootsByTriggerId = new Dictionary<int, ITriggerPlanExecutable>();
            var strings = dto?.Strings != null ? new Dictionary<int, string>(dto.Strings) : new Dictionary<int, string>();
            if (dto?.Triggers != null)
            {
                for (int i = 0; i < dto.Triggers.Count; i++)
                {
                    var t = dto.Triggers[i];
                    if (t == null) continue;
                    if (t.TriggerId <= 0) continue;

                    var eid = t.EventId;
                    if (eid == 0 && !string.IsNullOrEmpty(t.EventName))
                    {
                        eid = StableStringId.Get("event:" + t.EventName);
                    }

                    var cue = CueFactory.Create(t.CueKind, t.CueVfxId, t.CueSfxId) ?? NullTriggerCue.Instance;
                    var plan = _converter.Convert(t, cue);
                    var executionRoot = _converter.ConvertExecutionRoot(t);
                    next.Add(new Record(t.TriggerId, t.EventName, eid, NormalizeScope(t.Scope), in plan, executionRoot));
                    byTriggerId[t.TriggerId] = plan;
                    if (executionRoot != null)
                    {
                        executionRootsByTriggerId[t.TriggerId] = executionRoot;
                    }
                }
            }

            _records = next;
            _byTriggerId = byTriggerId;
            _executionRootsByTriggerId = executionRootsByTriggerId;
            _strings = strings;
        }

        private static TriggerPlanScope NormalizeScope(TriggerPlanScope scope)
        {
            return Enum.IsDefined(typeof(TriggerPlanScope), scope) ? scope : TriggerPlanScope.Global;
        }

        private static readonly TriggerPlanConverter _converter = new TriggerPlanConverter();
    }
}
