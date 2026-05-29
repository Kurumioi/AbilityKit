#if DOTNET
using System;
using System.IO;
using NLog;

namespace ET
{
    [Invoke]
    public class LogInvoker_NLog: AInvokeHandler<LogInvoker, ILog>
    {
        public override ILog Handle(LogInvoker args)
        {
            return new NLogger(args.SceneName, args.Process, args.Fiber);
        }
    }

    public class NLogger: ILog
    {
        private readonly NLog.Logger logger;

        static NLogger()
        {
            string configPath = ResolveConfigPath();
            LogManager.Configuration = new NLog.Config.XmlLoggingConfiguration(configPath);
            LogManager.Configuration.Variables["currentDir"] = Environment.CurrentDirectory;
        }

        private static string ResolveConfigPath()
        {
            string loaderConfigPath = Path.Combine("Packages", "cn.etetet.loader", "Scripts", "Loader", "Server", "NLog.config");
            string[] candidates =
            {
                Path.Combine(Environment.CurrentDirectory, loaderConfigPath),
                Path.Combine(AppContext.BaseDirectory, loaderConfigPath),
                Path.Combine(AppContext.BaseDirectory, "NLog.config"),
                Path.Combine(Environment.CurrentDirectory, "src", "AbilityKit.Demo.ET.App", loaderConfigPath),
                Path.Combine(Environment.CurrentDirectory, "src", "AbilityKit.Demo.ET.App", "NLog.config")
            };

            for (int i = 0; i < candidates.Length; i++)
            {
                if (File.Exists(candidates[i]))
                {
                    return candidates[i];
                }
            }

            return loaderConfigPath;
        }

        public NLogger(string name, int process, int fiber)
        {
            this.logger = LogManager.GetLogger($"{(uint)process:000000}.{(uint)fiber:0000000000}.{name}");
        }

        public void Trace(string message)
        {
            this.logger.Trace(message);
        }

        public void Warning(string message)
        {
            this.logger.Warn(message);
        }

        public void Info(string message)
        {
            this.logger.Info(message);
        }

        public void Debug(string message)
        {
            this.logger.Debug(message);
        }

        public void Error(string message)
        {
            this.logger.Error(message);
        }

        public void Error(Exception e)
        {
            this.logger.Error(e.ToString());
        }

        public void Trace(ref System.Runtime.CompilerServices.DefaultInterpolatedStringHandler message)
        {
            this.logger.Trace(message.ToStringAndClear());
        }

        public void Warning(ref System.Runtime.CompilerServices.DefaultInterpolatedStringHandler message)
        {
            this.logger.Warn(message.ToStringAndClear());
        }

        public void Info(ref System.Runtime.CompilerServices.DefaultInterpolatedStringHandler message)
        {
            this.logger.Info(message.ToStringAndClear());
        }

        public void Debug(ref System.Runtime.CompilerServices.DefaultInterpolatedStringHandler message)
        {
            this.logger.Debug(message.ToStringAndClear());
        }

        public void Error(ref System.Runtime.CompilerServices.DefaultInterpolatedStringHandler message)
        {
            this.logger.Error(message.ToStringAndClear());
        }
    }
}
#endif