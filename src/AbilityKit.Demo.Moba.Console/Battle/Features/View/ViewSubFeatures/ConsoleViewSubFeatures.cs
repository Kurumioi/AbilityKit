using System;
using AbilityKit.Demo.Moba.Console.Battle.Context;
using AbilityKit.Demo.Moba.Console.Battle.Session;
using AbilityKit.Demo.Moba.Console.View;

namespace AbilityKit.Demo.Moba.Console.Battle.Features
{
    /// <summary>
    /// Timeline Module
    /// Manages ViewTimeline instance lifecycle
    /// 对齐 Unity TimelineModule
    /// </summary>
    public sealed class ConsoleTimelineModule : IConsoleViewModule
    {
        private const string ModuleId = "view_timeline";
        private ConsoleViewTimeline? _timeline;

        public string Id => ModuleId;
        public string[]? Dependencies => null;

        public void OnAttach(IConsoleViewFeatureModulesHost host)
        {
            _timeline = new ConsoleViewTimeline();
            Platform.Log.View("[TimelineModule] Created ViewTimeline");
        }

        public void OnDetach(IConsoleViewFeatureModulesHost host)
        {
            _timeline?.Dispose();
            _timeline = null;
        }

        public void Tick(IConsoleViewFeatureModulesHost host, float deltaTime)
        {
        }

        public void Rebind(IConsoleViewFeatureModulesHost host)
        {
        }
    }

    /// <summary>
    /// VFX Module
    /// Manages VFX database and VfxManager instance
    /// 对齐 Unity VfxModule
    /// </summary>
    public sealed class ConsoleVfxModule : IConsoleViewModule
    {
        private const string ModuleId = "view_vfx";
        private ConsoleVfxManager? _vfxManager;

        public string Id => ModuleId;
        public string[]? Dependencies => null;

        public void OnAttach(IConsoleViewFeatureModulesHost host)
        {
            var database = new ConsoleVfxDatabase();
            _vfxManager = new ConsoleVfxManager(database);
            Platform.Log.View("[VfxModule] Created VfxManager");
        }

        public void OnDetach(IConsoleViewFeatureModulesHost host)
        {
            _vfxManager?.Dispose();
            _vfxManager = null;
        }

        public void Tick(IConsoleViewFeatureModulesHost host, float deltaTime)
        {
        }

        public void Rebind(IConsoleViewFeatureModulesHost host)
        {
        }
    }

    /// <summary>
    /// Binding Module
    /// Manages ViewBinder instance lifecycle and triggers ViewBinderReady event
    /// 对齐 Unity ContextBindingModule + BindingModule
    /// </summary>
    public sealed class ConsoleBindingModule : IConsoleViewModule
    {
        private const string ModuleId = "view_binding";
        private IConsoleViewBinder? _binder;

        public string Id => ModuleId;
        public string[]? Dependencies => null;
        public IConsoleViewBinder? Binder => _binder;

        public void OnAttach(IConsoleViewFeatureModulesHost host)
        {
            // 创建 ViewBinder
            _binder = new ConsoleViewBinder();
            host.RegisterBinder(_binder);

            // 触发 ViewBinderReady 事件
            var frameIndex = host.Context.LastFrame;
            var evt = new ViewBinderReadyEvent(_binder, frameIndex);
            host.Hooks?.InvokeViewBinderReady(evt);

            Platform.Log.View($"[BindingModule] Created ViewBinder, ViewBinderReady fired at frame {frameIndex}");
        }

        public void OnDetach(IConsoleViewFeatureModulesHost host)
        {
            if (_binder != null)
            {
                host.UnregisterBinder(_binder);
                _binder.Dispose();
                _binder = null;
            }
            Platform.Log.View("[BindingModule] Detached, ViewBinder disposed");
        }

        public void Tick(IConsoleViewFeatureModulesHost host, float deltaTime)
        {
        }

        public void Rebind(IConsoleViewFeatureModulesHost host)
        {
            // 清除所有实体，准备重新绑定
            _binder?.Clear();

            // 触发 ViewsRebound 事件
            var frameIndex = host.Context.LastFrame;
            var evt = new ViewsReboundEvent(frameIndex);
            host.Hooks?.InvokeViewsRebound(evt);

            Platform.Log.View($"[BindingModule] Rebind triggered, ViewsRebound fired at frame {frameIndex}");
        }
    }

    /// <summary>
    /// EventSink Module
    /// Creates and manages BattleViewEventSink instance
    /// 对齐 Unity EventSinkModule
    /// </summary>
    public sealed class ConsoleEventSinkModule : IConsoleViewModule
    {
        private const string ModuleId = "view_event_sink";
        private ConsoleBattleViewEventSink? _eventSink;

        public string Id => ModuleId;
        public string[]? Dependencies => null;
        public ConsoleBattleViewEventSink? EventSink => _eventSink;

        public void OnAttach(IConsoleViewFeatureModulesHost host)
        {
            // 创建 EventSink
            _eventSink = new ConsoleBattleViewEventSink(host.BattleView);

            // 注册到 Feature
            host.RegisterEventSink(_eventSink);

            // 设置到 Context (FrameSnapshots)
            host.Context.FrameSnapshots = _eventSink;

            Platform.Log.View("[EventSinkModule] Created BattleViewEventSink");
        }

        public void OnDetach(IConsoleViewFeatureModulesHost host)
        {
            if (_eventSink != null)
            {
                host.UnregisterEventSink(_eventSink);
                _eventSink.Dispose();
                _eventSink = null;
            }
            Platform.Log.View("[EventSinkModule] Detached, EventSink disposed");
        }

        public void Tick(IConsoleViewFeatureModulesHost host, float deltaTime)
        {
        }

        public void Rebind(IConsoleViewFeatureModulesHost host)
        {
            // EventSink 不需要特殊 Rebind 处理
        }
    }

    /// <summary>
    /// Shared DirtySync Module
    /// Manages dirty entity synchronization
    /// 对齐 Unity SharedDirtySyncModule
    /// </summary>
    public sealed class ConsoleDirtySyncModule : IConsoleViewModule
    {
        private const string ModuleId = "view_dirty_sync";
        private bool _enabled = true;

        public string Id => ModuleId;
        public string[]? Dependencies => null;

        public void OnAttach(IConsoleViewFeatureModulesHost host)
        {
            Platform.Log.View("[DirtySyncModule] Attached");
        }

        public void OnDetach(IConsoleViewFeatureModulesHost host)
        {
            Platform.Log.View("[DirtySyncModule] Detached");
        }

        public void Tick(IConsoleViewFeatureModulesHost host, float deltaTime)
        {
        }

        public void Rebind(IConsoleViewFeatureModulesHost host)
        {
            _enabled = false;
            Platform.Log.View("[DirtySyncModule] Rebind - disabled sync");
        }
    }

    /// <summary>
    /// Shared Interpolation Module
    /// Manages position interpolation for smooth rendering
    /// 对齐 Unity SharedInterpolationModule
    /// </summary>
    public sealed class ConsoleInterpolationModule : IConsoleViewModule
    {
        private const string ModuleId = "view_interpolation";
        private bool _enabled = true;

        public string Id => ModuleId;
        public string[]? Dependencies => new[] { "view_binding" };

        public void OnAttach(IConsoleViewFeatureModulesHost host)
        {
            Platform.Log.View("[InterpolationModule] Attached");
        }

        public void OnDetach(IConsoleViewFeatureModulesHost host)
        {
            Platform.Log.View("[InterpolationModule] Detached");
        }

        public void Tick(IConsoleViewFeatureModulesHost host, float deltaTime)
        {
        }

        public void Rebind(IConsoleViewFeatureModulesHost host)
        {
            Platform.Log.View("[InterpolationModule] Rebind triggered");
        }
    }
}
