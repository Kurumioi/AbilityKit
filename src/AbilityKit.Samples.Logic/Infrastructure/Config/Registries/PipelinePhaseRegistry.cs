using System;
using System.Reflection;
using AbilityKit.Core.Markers;
using AbilityKit.Samples.Logic.Infrastructure.Config.Attributes;

namespace AbilityKit.Samples.Logic.Infrastructure.Config
{
    /// <summary>
    /// 绠＄嚎闃舵绫诲瀷娉ㄥ唽琛?
    /// 閫氳繃 PipelinePhaseTypeIdAttribute 鑷姩鍙戠幇鍜屾敞鍐岄樁娈电被鍨?
    /// 鏀寔閫氳繃绫诲瀷鍚嶇О浠?JSON 鍙嶅簭鍒楀寲
    /// </summary>
    public sealed class PipelinePhaseRegistry : KeyedMarkerRegistry<string, PipelinePhaseTypeIdAttribute>
    {
        private static readonly Lazy<PipelinePhaseRegistry> _instance = new(() => new PipelinePhaseRegistry());
        public static PipelinePhaseRegistry Instance => _instance.Value;

        private PipelinePhaseRegistry()
        {
            ScanCurrentAssembly();
        }

        private void ScanCurrentAssembly()
        {
            var assembly = typeof(PipelinePhaseRegistry).Assembly;
            MarkerScanner<PipelinePhaseTypeIdAttribute>.Scan(new[] { assembly }, this);
        }

        /// <summary>
        /// 閫氳繃 Attribute 娉ㄥ唽
        /// </summary>
        internal void RegisterByAttribute(PipelinePhaseTypeIdAttribute attr, Type implType)
        {
            if (attr == null || implType == null) return;
            Register(attr.TypeName, implType);
        }

        /// <summary>
        /// 鏍规嵁鍚嶇О鍒涘缓闃舵瀹炰緥
        /// </summary>
        public object CreatePhase(string phaseName)
        {
            return GetOrCreateInstance(phaseName);
        }

        /// <summary>
        /// 灏濊瘯鏍规嵁鍚嶇О鍒涘缓闃舵瀹炰緥
        /// </summary>
        public bool TryCreatePhase(string phaseName, out object phase)
        {
            if (TryGet(phaseName, out var type))
            {
                phase = Activator.CreateInstance(type);
                return true;
            }
            phase = null;
            return false;
        }

        /// <summary>
        /// 鏍规嵁鍚嶇О浠?JSON 鍙嶅簭鍒楀寲闃舵瀹炰緥
        /// </summary>
        public object CreateFromJson(string phaseName, System.Text.Json.JsonElement data)
        {
            var phase = CreatePhase(phaseName);
            ApplyJsonToObject(phase, data);
            return phase;
        }

        /// <summary>
        /// 灏濊瘯鏍规嵁鍚嶇О浠?JSON 鍙嶅簭鍒楀寲闃舵瀹炰緥
        /// </summary>
        public bool TryCreateFromJson(string phaseName, System.Text.Json.JsonElement data, out object phase)
        {
            if (!TryGet(phaseName, out var type))
            {
                phase = null;
                return false;
            }

            phase = Activator.CreateInstance(type);
            ApplyJsonToObject(phase, data);
            return true;
        }

        /// <summary>
        /// 灏?JsonElement 鐨勫睘鎬у簲鐢ㄥ埌瀵硅薄锛堥€氳繃鍙嶅皠锛?
        /// </summary>
        private void ApplyJsonToObject(object obj, System.Text.Json.JsonElement data)
        {
            if (data.ValueKind != System.Text.Json.JsonValueKind.Object)
                return;

            var properties = obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var prop in properties)
            {
                if (!prop.CanWrite)
                    continue;

                if (!data.TryGetProperty(prop.Name, out var element))
                    continue;

                try
                {
                    var value = ConvertJsonValue(element, prop.PropertyType);
                    prop.SetValue(obj, value);
                }
                catch
                {
                    // 蹇界暐鏃犳硶杞崲鐨勫睘鎬?
                }
            }
        }

        /// <summary>
        /// 灏?JsonElement 杞崲涓虹洰鏍囩被鍨?
        /// </summary>
        private object? ConvertJsonValue(System.Text.Json.JsonElement element, Type targetType)
        {
            if (targetType == typeof(bool))
                return element.GetBoolean();
            if (targetType == typeof(int))
                return element.GetInt32();
            if (targetType == typeof(long))
                return element.GetInt64();
            if (targetType == typeof(float))
                return element.GetSingle();
            if (targetType == typeof(double))
                return element.GetDouble();
            if (targetType == typeof(string))
                return element.GetString();
            if (targetType == typeof(bool?))
                return element.ValueKind == System.Text.Json.JsonValueKind.Null ? null : element.GetBoolean();
            if (targetType == typeof(int?))
                return element.ValueKind == System.Text.Json.JsonValueKind.Null ? null : element.GetInt32();
            if (targetType == typeof(float?))
                return element.ValueKind == System.Text.Json.JsonValueKind.Null ? null : element.GetSingle();
            if (targetType == typeof(string))
                return element.GetString();

            // 瀵逛簬鍏朵粬绫诲瀷锛屽皾璇?JSON 鍙嶅簭鍒楀寲
            var json = element.GetRawText();
            return System.Text.Json.JsonSerializer.Deserialize(json, targetType);
        }
    }
}
