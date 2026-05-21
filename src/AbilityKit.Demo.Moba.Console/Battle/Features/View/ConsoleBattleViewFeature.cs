using System;
using AbilityKit.Demo.Moba.Console.Battle.Context;
using AbilityKit.Demo.Moba.Console.Battle.Flow;
using AbilityKit.Demo.Moba.Console.Battle.Game;
using AbilityKit.Demo.Moba.Console.Battle.Session;
using AbilityKit.Demo.Moba.Console.Platform;
using AbilityKit.Demo.Moba.Console.View;
using Console_ = AbilityKit.Demo.Moba.Console.Platform.Console_;

namespace AbilityKit.Demo.Moba.Console.Battle.Features
{
    /// <summary>
    /// 战斗视图 Feature
    /// 对齐 Unity BattleViewFeature
    /// 实现 Host 接口，使用 ModuleHost 管理所有 Module 生命周期
    /// 支持嵌套 Module 层级结构
    /// </summary>
    public sealed class ConsoleViewFeature : 
        IGameModule<ConsoleBattleContext>, 
        IGamePhaseFeature, 
        IConsoleViewFeatureModulesHost
    {
        private ConsoleBattleContext _context = null!;
        private ConsoleSessionHooks? _hooks;

        // ViewFeature 子系统
        private IConsoleBattleView _battleView = null!;
        private IConsoleViewBinder? _binder;
        private ConsoleVfxManager? _vfxManager;
        private ConsoleViewTimeline? _timeline;

        // 已注册的 EventSink
        private ConsoleBattleViewEventSink? _registeredEventSink;

        // 渲染器
        private IRenderer _renderer = null!;

        // ModuleHost - 管理所有 View Module 生命周期
        // 支持嵌套：Module 可以包含自己的子 ModuleHost
        private readonly ModuleHost<IConsoleViewFeatureModulesHost, IConsoleViewModule> _moduleHost = new();

        #region IConsoleViewFeatureModulesHost 实现

        /// <inheritdoc />
        public ConsoleBattleContext Context => _context;

        /// <inheritdoc />
        public ConsoleSessionHooks? Hooks => _hooks;

        /// <inheritdoc />
        public IConsoleViewBinder Binder => _binder ?? throw new InvalidOperationException("Binder not initialized");

        /// <inheritdoc />
        public IConsoleBattleView BattleView => _battleView;

        /// <inheritdoc />
        public ConsoleVfxManager VfxManager => _vfxManager ?? throw new InvalidOperationException("VfxManager not initialized");

        /// <inheritdoc />
        public ConsoleBattleViewEventSink EventSink => _registeredEventSink ?? throw new InvalidOperationException("EventSink not initialized");

        /// <inheritdoc />
        public void RegisterBinder(IConsoleViewBinder binder)
        {
            _binder = binder;
        }

        /// <inheritdoc />
        public void UnregisterBinder(IConsoleViewBinder binder)
        {
            if (_binder == binder)
            {
                _binder = null;
            }
        }

        /// <inheritdoc />
        public void RegisterEventSink(ConsoleBattleViewEventSink eventSink)
        {
            _registeredEventSink = eventSink;
        }

        /// <inheritdoc />
        public void UnregisterEventSink(ConsoleBattleViewEventSink eventSink)
        {
            if (_registeredEventSink == eventSink)
            {
                _registeredEventSink = null;
            }
        }

        /// <inheritdoc />
        public void RefreshDirtyViews()
        {
            if (_context?.DirtyEntities != null && _binder != null)
            {
                foreach (var entityId in _context.DirtyEntities)
                {
                    // 刷新脏实体的视图
                }
            }
        }

        /// <inheritdoc />
        public void RegisterAllSeekables()
        {
            Platform.Log.View("[ConsoleViewFeature] RegisterAllSeekables called");
        }

        /// <inheritdoc />
        public void SeekAllToCurrentFrame()
        {
            if (_timeline != null && _context != null)
            {
                _timeline.SeekToFrame(_context.LastFrame);
            }
            Platform.Log.View("[ConsoleViewFeature] SeekAllToCurrentFrame called");
        }

        /// <inheritdoc />
        public void RebindAllViews()
        {
            // 通过 ModuleHost 触发所有 Module 的 Rebind
            _moduleHost.RebindAll(this);
        }

        /// <inheritdoc />
        public void TickFloatingTexts(float deltaTime)
        {
            // FloatingText 由 ConsoleBattleView.Tick() 处理
        }

        #endregion

        #region 渲染器设置

        /// <summary>
        /// 设置渲染器
        /// </summary>
        public void SetRenderer(IRenderer renderer)
        {
            _renderer = renderer;
        }

        #endregion

        #region IGamePhaseFeature 实现

        /// <inheritdoc />
        public void OnAttach(in ConsoleGamePhaseContext ctx)
        {
            OnAttach(ctx.BattleContext!);
            _hooks = ctx.SessionHooks;
        }

        /// <inheritdoc />
        public void OnDetach(in ConsoleGamePhaseContext ctx)
        {
            OnDetach(ctx.BattleContext!);
            _hooks = null;
        }

        /// <inheritdoc />
        public void Tick(in ConsoleGamePhaseContext ctx, float deltaTime)
        {
            // 通过 ModuleHost Tick 所有 Module（支持嵌套）
            _moduleHost.Tick(this, deltaTime);

            // Tick ViewBinder
            if (_binder != null && _context != null)
            {
                _binder.RenderTime = _context.LogicTimeSeconds;
                _binder.TickRender(deltaTime, _context.LogicTimeSeconds);
            }

            TickFloatingTexts(deltaTime);
        }

        #endregion

        #region IGameModule 实现

        /// <inheritdoc />
        public void OnAttach(ConsoleBattleContext context)
        {
            if (context == null)
            {
                Platform.Log.Error("[ConsoleViewFeature] OnAttach failed: context is null");
                return;
            }

            _context = context;

            // 初始化视图子系统
            InitializeViewSubsystems();

            // 注册 Module
            RegisterModules();

            // 附加所有 Module（通过 ModuleHost）
            _moduleHost.Attach(this);

            // 触发 ViewBinderReady
            if (_binder != null && _hooks != null)
            {
                var evt = new ViewBinderReadyEvent(_binder, context.LastFrame);
                _hooks.InvokeViewBinderReady(evt);
            }

            Platform.Log.View($"[ConsoleViewFeature] View initialized with ModuleHost, {_moduleHost.Count} modules");
        }

        /// <inheritdoc />
        public void OnDetach(ConsoleBattleContext context)
        {
            // 分离所有 Module（通过 ModuleHost，反向顺序）
            _moduleHost.Detach(this);

            // 销毁子系统
            _registeredEventSink?.Dispose();
            _registeredEventSink = null;

            _vfxManager?.Dispose();
            _vfxManager = null;

            _binder?.Dispose();
            _binder = null;

            _timeline?.Dispose();
            _timeline = null;

            if (_battleView is IDisposable disposable)
            {
                disposable.Dispose();
            }
            _battleView = null!;

            _context = null!;

            Platform.Log.View("[ConsoleViewFeature] View disposed");
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 初始化视图子系统
        /// </summary>
        private void InitializeViewSubsystems()
        {
            // 创建渲染器
            _renderer ??= new Console_.ConsoleRenderer(80, 40);

            // 创建视图服务
            var entityDisplay = new ConsoleEntityDisplayService();
            var floatingText = new ConsoleFloatingTextSystem();
            var projectileDisplay = new ConsoleProjectileDisplayService();
            var areaView = new ConsoleAreaViewSystem();

            // 创建 BattleView
            _battleView = new ConsoleBattleView(
                entityDisplay,
                floatingText,
                areaView,
                projectileDisplay,
                _renderer);

            // 创建 Timeline
            _timeline = new ConsoleViewTimeline();

            // 创建 VFX Manager
            var vfxDatabase = new ConsoleVfxDatabase();
            _vfxManager = new ConsoleVfxManager(vfxDatabase);

            Platform.Log.View("[ConsoleViewFeature] View subsystems initialized");
        }

        /// <summary>
        /// 注册 Module 到 ModuleHost
        /// ModuleHost 会自动处理依赖排序
        /// </summary>
        private void RegisterModules()
        {
            // 按依赖顺序添加（实际上 ModuleHost 会自动排序）
            // Core Modules
            _moduleHost.Add(new ConsoleBindingModule());
            _moduleHost.Add(new ConsoleEventSinkModule());
            _moduleHost.Add(new ConsoleEventAdaptersModule());

            // Utility Modules
            _moduleHost.Add(new ConsoleTimelineModule());
            _moduleHost.Add(new ConsoleVfxModule());

            // Shared Modules
            _moduleHost.Add(new ConsoleDirtySyncModule());
            _moduleHost.Add(new ConsoleInterpolationModule());
        }

        #endregion
    }
}
