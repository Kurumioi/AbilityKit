using AbilityKit.Demo.Moba.Console.Core.Battle.Context;
using AbilityKit.Demo.Moba.Console.Flow;
using AbilityKit.Demo.Moba.Console.Platform;
using AbilityKit.Demo.Moba.Console.View;
using Console_ = AbilityKit.Demo.Moba.Console.Platform.Console_;

namespace AbilityKit.Demo.Moba.Console.Core.Battle.Features
{
    /// <summary>
    /// 战斗视图 Feature
    /// 对齐 Unity BattleViewFeature，管理视图绑定和事件处理
    /// </summary>
    public sealed class ConsoleViewFeature : IGameModule<ConsoleBattleContext>
    {
        private ConsoleBattleContext _context;
        private IConsoleBattleView _battleView;

        /// <summary>
        /// 战斗视图接口
        /// </summary>
        public IConsoleBattleView BattleView => _battleView;

        /// <summary>
        /// 渲染器（可选，如果未设置则使用默认渲染器）
        /// </summary>
        private IRenderer _renderer;

        /// <summary>
        /// 设置渲染器
        /// </summary>
        public void SetRenderer(IRenderer renderer)
        {
            _renderer = renderer;
        }

        /// <summary>
        /// OnAttach: 初始化视图
        /// </summary>
        public void OnAttach(ConsoleBattleContext context)
        {
            if (context == null)
            {
                Platform.Log.Error("[ConsoleViewFeature] OnAttach failed: context is null");
                return;
            }

            _context = context;

            // 创建或获取 BattleView
            _battleView = CreateBattleView(context);

            Platform.Log.View("[ConsoleViewFeature] View initialized");
        }

        /// <summary>
        /// OnDetach: 销毁视图
        /// </summary>
        public void OnDetach(ConsoleBattleContext context)
        {
            // 销毁视图
            if (_battleView is IDisposable disposable)
            {
                disposable.Dispose();
            }
            _battleView = null;

            _context = null;

            Platform.Log.View("[ConsoleViewFeature] View disposed");
        }

        /// <summary>
        /// 创建战斗视图
        /// </summary>
        private IConsoleBattleView CreateBattleView(ConsoleBattleContext context)
        {
            // 创建视图服务
            var entityDisplay = new ConsoleEntityDisplayService();
            var floatingText = new ConsoleFloatingTextSystem();
            var projectileDisplay = new ConsoleProjectileDisplayService();
            var areaView = new ConsoleAreaViewSystem();

            // 获取渲染器（优先使用注入的，否则创建默认的）
            var renderer = _renderer ?? new Console_.ConsoleRenderer(80, 40);

            // 创建并返回视图
            return new ConsoleBattleView(
                entityDisplay,
                floatingText,
                areaView,
                projectileDisplay,
                renderer);
        }
    }
}
