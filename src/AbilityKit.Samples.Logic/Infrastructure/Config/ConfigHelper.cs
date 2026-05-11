using System;
using System.Collections.Generic;
using System.Text.Json;

namespace AbilityKit.Samples.Logic.Infrastructure.Config
{
    /// <summary>
    /// 閰嶇疆璇诲彇杈呭姪绫?- 鎻愪緵渚挎嵎鐨勯厤缃闂柟娉?
    /// </summary>
    public static class ConfigHelper
    {
        /// <summary>
        /// 瀹夊叏鑾峰彇瀛楃涓插€?
        /// </summary>
        public static string GetString(JsonElement element, string propertyName, string defaultValue = "")
        {
            if (element.TryGetProperty(propertyName, out var prop))
            {
                return prop.GetString() ?? defaultValue;
            }
            return defaultValue;
        }

        /// <summary>
        /// 瀹夊叏鑾峰彇鏁存暟鍊?
        /// </summary>
        public static int GetInt(JsonElement element, string propertyName, int defaultValue = 0)
        {
            if (element.TryGetProperty(propertyName, out var prop))
            {
                return prop.GetInt32();
            }
            return defaultValue;
        }

        /// <summary>
        /// 瀹夊叏鑾峰彇娴偣鍊?
        /// </summary>
        public static float GetFloat(JsonElement element, string propertyName, float defaultValue = 0f)
        {
            if (element.TryGetProperty(propertyName, out var prop))
            {
                if (prop.ValueKind == JsonValueKind.Number)
                {
                    return prop.GetSingle();
                }
            }
            return defaultValue;
        }

        /// <summary>
        /// 瀹夊叏鑾峰彇甯冨皵鍊?
        /// </summary>
        public static bool GetBool(JsonElement element, string propertyName, bool defaultValue = false)
        {
            if (element.TryGetProperty(propertyName, out var prop))
            {
                return prop.GetBoolean();
            }
            return defaultValue;
        }

        /// <summary>
        /// 鑾峰彇鏋氫妇鍊?
        /// </summary>
        public static T GetEnum<T>(JsonElement element, string propertyName, T defaultValue = default) where T : struct, Enum
        {
            if (element.TryGetProperty(propertyName, out var prop))
            {
                var str = prop.GetString();
                if (str != null && Enum.TryParse<T>(str, true, out var result))
                {
                    return result;
                }
            }
            return defaultValue;
        }

        /// <summary>
        /// 瀹夊叏鑾峰彇鏁扮粍
        /// </summary>
        public static List<T> GetList<T>(JsonElement element, string propertyName)
        {
            var result = new List<T>();
            if (element.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in prop.EnumerateArray())
                {
                    try
                    {
                        var obj = JsonSerializer.Deserialize<T>(item.GetRawText());
                        if (obj != null)
                        {
                            result.Add(obj);
                        }
                    }
                    catch
                    {
                        // 璺宠繃鏃犳硶瑙ｆ瀽鐨勫厓绱?
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// 瀹夊叏鑾峰彇瀛楀吀
        /// </summary>
        public static Dictionary<string, T> GetDict<T>(JsonElement element, string propertyName) where T : class
        {
            var result = new Dictionary<string, T>();
            if (element.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.Object)
            {
                foreach (var kvp in prop.EnumerateObject())
                {
                    try
                    {
                        var obj = JsonSerializer.Deserialize<T>(kvp.Value.GetRawText());
                        if (obj != null)
                        {
                            result[kvp.Name] = obj;
                        }
                    }
                    catch
                    {
                        // 璺宠繃鏃犳硶瑙ｆ瀽鐨勫厓绱?
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// 鑾峰彇瀛楀吀鍊硷紙绠€鍗曠被鍨嬶級
        /// </summary>
        public static Dictionary<string, float> GetFloatDict(JsonElement element, string propertyName)
        {
            var result = new Dictionary<string, float>();
            if (element.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.Object)
            {
                foreach (var kvp in prop.EnumerateObject())
                {
                    if (kvp.Value.ValueKind == JsonValueKind.Number)
                    {
                        result[kvp.Name] = kvp.Value.GetSingle();
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// 鑾峰彇瀛楃涓插垪琛?
        /// </summary>
        public static List<string> GetStringList(JsonElement element, string propertyName)
        {
            var result = new List<string>();
            if (element.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in prop.EnumerateArray())
                {
                    var str = item.GetString();
                    if (str != null)
                    {
                        result.Add(str);
                    }
                }
            }
            return result;
        }
    }
}
