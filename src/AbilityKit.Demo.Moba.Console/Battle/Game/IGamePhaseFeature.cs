namespace AbilityKit.Demo.Moba.Console.Battle.Game
{
    /// <summary>
    /// 游戏阶段 Feature 接口
    /// 所有在特定阶段激活的 Feature 都应实现此接口
    /// </summary>
    public interface IGamePhaseFeature
    {
        /// <summary>
        /// Feature 附加到阶段时调用
        /// </summary>
        void OnAttach(in ConsoleGamePhaseContext ctx);

        /// <summary>
        /// Feature 从阶段分离时调用
        /// </summary>
        void OnDetach(in ConsoleGamePhaseContext ctx);

        /// <summary>
        /// 每帧调用
        /// </summary>
        void Tick(in ConsoleGamePhaseContext ctx, float deltaTime);
    }

    /// <summary>
    /// 游戏阶段 Feature 基类
    /// 提供默认实现
    /// </summary>
    public abstract class GamePhaseFeatureBase : IGamePhaseFeature
    {
        public abstract void OnAttach(in ConsoleGamePhaseContext ctx);
        public abstract void OnDetach(in ConsoleGamePhaseContext ctx);
        public abstract void Tick(in ConsoleGamePhaseContext ctx, float deltaTime);
    }
}
