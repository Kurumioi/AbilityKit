using System;

// Alias for AbilityKit.Core.Logging to avoid conflict with ET.Log
using AKLog = AbilityKit.Core.Logging;

namespace ET.Logic
{
    /// <summary>
    /// ET version of AbilityKit.Core.Log Sink
    /// Bridges AbilityKit.Core.Logging to ET Log
    /// </summary>
    public sealed class ETLogSink : AKLog.ILogSink
    {
        public string Name => "ETLogSink";

        public void Info(string message)
        {
            global::ET.Log.Info($"[AK] {message}");
        }

        public void Warning(string message)
        {
            global::ET.Log.Warning($"[AK] {message}");
        }

        public void Error(string message)
        {
            global::ET.Log.Error($"[AK] {message}");
        }

        public void Exception(Exception exception, string message = null)
        {
            if (string.IsNullOrEmpty(message))
            {
                global::ET.Log.Error($"[AK] Exception: {exception}");
            }
            else
            {
                global::ET.Log.Error($"[AK] {message}: {exception}");
            }
        }
    }
}
