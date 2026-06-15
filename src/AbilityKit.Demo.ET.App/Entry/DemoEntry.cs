using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace ET.AbilityKit.Demo.ET.App
{
    /// <summary>
    /// Demo Entry 初始化
    /// </summary>
    public static class DemoEntry
    {
        public static void Init(string[] args)
        {
            Log.Info($"[DemoEntry] Initializing...");

            // 初始化 WinPeriod (必须在其他操作之前)
            global::ET.WinPeriod.Init();

            Log.Info($"[DemoEntry] WinPeriod initialized");

            // 初始化 Options 单例
            // 由于 Register 是 internal 的，我们需要使用反射来调用
            var optionsType = typeof(global::ET.Options);
            var registerMethod = optionsType.GetMethod("Register", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (registerMethod != null)
            {
                // 检查 Instance 是否已创建
                var currentInstance = global::ET.Options.Instance;
                if (currentInstance == null)
                {
                    // 需要先创建实例
                    var instance = System.Activator.CreateInstance(optionsType, nonPublic: true);
                    if (instance != null)
                    {
                        registerMethod.Invoke(instance, null);
                    }
                }
            }

            // 设置命令行参数
            var options = global::ET.Options.Instance;
            if (options != null)
            {
                options.SceneName = "Demo";
                options.Process = 1;
            }

            Log.Info($"[DemoEntry] Options initialized");

            // 首先注册 SceneTypeSingleton（必须在 CodeTypes.CodeProcess 之前）
            Log.Info($"[DemoEntry] Registering SceneTypeSingleton...");
            global::ET.World.Instance.AddSingleton<global::ET.SceneTypeSingleton, global::System.Type>(typeof(global::ET.SceneType));

            // 注册 Logger（使用 NLog，同时输出到文件和控制台）
            Log.Info($"[DemoEntry] Registering Logger...");
            var logger = global::ET.World.Instance.AddSingleton<global::ET.Logger>();
            logger.Log = new global::ET.NLogger("Demo", 1, 0);
            global::AbilityKit.Core.Logging.Log.SetSink(new global::ET.Logic.ETLogSink());

            // 加载程序集并初始化 CodeTypes
            var assemblies = new Assembly[]
            {
                typeof(DemoEntry).Assembly, // App
                typeof(global::ET.Logic.DemoLoginComponent).Assembly, // Logic
                typeof(global::ET.Logic.DemoProcessComponent).Assembly, // Logic
            };

            // 如果 View 层程序集存在，也加载它
            try
            {
                var viewAssembly = System.Reflection.Assembly.Load("AbilityKit.Demo.ET.View");
                var extendedAssemblies = new List<Assembly>(assemblies) { viewAssembly };
                assemblies = extendedAssemblies.ToArray();
            }
            catch (Exception)
            {
                // View assembly may not exist in this build
            }

            Log.Info($"[DemoEntry] Loading assemblies...");
            foreach (var ass in assemblies)
            {
                Log.Debug($"[DemoEntry]   - {ass.GetName().Name}");
            }

            try
            {
                Log.Info($"[DemoEntry] Initializing CodeTypes singleton...");

                // 首先创建并注册 CodeTypes 单例
                var codeTypesType = typeof(global::ET.CodeTypes);
                var codeTypesRegisterMethod = codeTypesType.GetMethod("Register", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                // 检查 CodeTypes 是否已注册
                var currentCodeTypes = global::ET.CodeTypes.Instance;
                if (currentCodeTypes == null)
                {
                    // 需要先创建实例并注册
                    var instance = System.Activator.CreateInstance(codeTypesType, nonPublic: true);
                    if (instance != null && codeTypesRegisterMethod != null)
                    {
                        codeTypesRegisterMethod.Invoke(instance, null);
                    }
                }

                Log.Info($"[DemoEntry] CodeTypes singleton created");

                // 加载程序集
                var codeAssemblies = new Assembly[]
                {
                    typeof(DemoEntry).Assembly, // App
                    typeof(global::ET.Logic.DemoLoginComponent).Assembly, // Logic
                    typeof(global::ET.Logic.DemoProcessComponent).Assembly, // Logic
                    typeof(global::ET.CodeTypes).Assembly, // ET.Share - 包含 LogInvokerHandler 等
                };

                // 如果 View 层程序集存在，也加载它
                try
                {
                    var viewAssembly = System.Reflection.Assembly.Load("AbilityKit.Demo.ET.View");
                    var extendedAssemblies = new List<Assembly>(codeAssemblies) { viewAssembly };
                    codeAssemblies = extendedAssemblies.ToArray();
                }
                catch (Exception)
                {
                    // View assembly may not exist in this build
                }

                Log.Info($"[DemoEntry] Loading assemblies...");
                foreach (var ass in codeAssemblies)
                {
                    Log.Debug($"[DemoEntry]   - {ass.GetName().Name}");
                }

                // 调用 CodeTypes.Instance.Awake 扫描程序集
                Log.Info($"[DemoEntry] Calling CodeTypes.Instance.Awake...");
                global::ET.CodeTypes.Instance.Awake(codeAssemblies);
                Log.Info($"[DemoEntry] CodeTypes initialized");

                // 重要：调用 CodeProcess 来初始化带 [CodeProcess] 标记的单例（如 EventSystem）
                Log.Info($"[DemoEntry] Calling CodeTypes.Instance.CodeProcess...");
                global::ET.CodeTypes.Instance.CodeProcess();
                Log.Info($"[DemoEntry] CodeProcess done");
            }
            catch (Exception ex)
            {
                Log.Error($"[DemoEntry] CodeTypes init failed: {ex.Message}");
                Log.Error(ex);
            }

            // 添加核心 Singletons
            Log.Info($"[DemoEntry] Adding core singletons...");
            // SceneTypeSingleton 已在上面注册
            global::ET.World.Instance.AddSingleton<global::ET.ObjectPool>();
            global::ET.World.Instance.AddSingleton<global::ET.IdGenerater>();
            global::ET.World.Instance.AddSingleton<global::ET.FiberManager>();
            global::ET.World.Instance.AddSingleton<global::ET.TimeInfo>();
            // 注意：EventSystem 已在 CodeTypes.CodeProcess() 中创建并注册到 World.Singleton
            // 不需要再次添加，否则会触发 EventSystem.Awake() 导致重复注册

            Log.Info($"[DemoEntry] Core singletons initialized");
        }

        public static async global::ET.ETTask StartAsync()
        {
            Log.Info($"[DemoEntry] Creating Main Fiber...");

            // 添加全局异常处理来捕获 FiberInit 中的异常
            AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
            {
                Log.Error($"[DemoEntry] Unhandled exception: {args.ExceptionObject}");
            };

            try
            {
                // 创建 Main Fiber
                // 使用重载: Create(SchedulerType, zone, sceneType, name) 自动生成 fiberId
                await global::ET.FiberManager.Instance.Create(
                    global::ET.SchedulerType.Main,
                    0,
                    global::ET.SceneType.Main,
                    "Demo");

                Log.Info($"[DemoEntry] Main Fiber created");
            }
            catch (Exception ex)
            {
                Log.Error($"[DemoEntry] Fiber creation failed: {ex.Message}");
                // 打印内部异常
                var inner = ex.InnerException;
                while (inner != null)
                {
                    Log.Error($"[DemoEntry]   Inner: {inner.Message}");
                    inner = inner.InnerException;
                }
                Log.Error(ex);
                throw;
            }
        }
    }
}
