using System;
using AbilityKit.Demo.Moba.Infrastructure;
using AbilityKit.Core.Common.Log;

namespace AbilityKit.Demo.Moba.Console
{
    class Program
    {
        static void Main(string[] args)
        {
            // Set up logging to console
            Log.SetSink(new ConsoleLogSink());

            Log.Info("AbilityKit MOBA Demo Console");
            Log.Info("==============================");
            Log.Info("This is a placeholder entry point.");
            Log.Info("The Core project contains:");
            Log.Info("  - Enums: Gameplay enums for the MOBA demo");
            Log.Info("  - Structs: Pure data structures (DTOs, events, etc.)");
            Log.Info("");
            Log.Info("The Infrastructure project provides:");
            Log.Info("  - ILogger abstraction");
            Log.Info("  - IConfigSource abstraction");
            Log.Info("  - IResourceProvider abstraction");
            Log.Info("");
            Log.Info("These abstractions can be implemented for Unity, Console, or any other platform.");
        }
    }

    /// <summary>
    /// Console implementation of ILogSink that outputs to console
    /// </summary>
    public sealed class ConsoleLogSink : ILogSink
    {
        public void Info(string message) => System.Console.WriteLine($"[INFO] {message}");
        public void Warning(string message) => System.Console.WriteLine($"[WARN] {message}");
        public void Error(string message) => System.Console.WriteLine($"[ERROR] {message}");
        public void Exception(Exception exception, string message)
        {
            System.Console.WriteLine($"[ERROR] {message ?? "Exception"}");
            System.Console.WriteLine(exception.ToString());
        }
    }
}
