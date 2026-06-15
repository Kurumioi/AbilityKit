using AbilityKit.Demo.Moba.Share;

namespace ET.Logic
{
    /// <summary>
    /// 逻辑世界创建器接口
    /// 用于封装不同类型战斗逻辑世界的创建和配置逻辑
    ///
    /// 使用方式:
    /// 1. 创建实现类实现 ILogicWorldCreator 接口
    /// 2. 在 LogicWorldRegistry 中注册
    /// 3. 调用 creator.CreateAndInitialize() 创建 World
    /// </summary>
    public interface ILogicWorldCreator
    {
        /// <summary>
        /// 世界类型标识符
        /// </summary>
        string WorldType { get; }

        /// <summary>
        /// 创建并初始化 World
        /// </summary>
        void CreateAndInitialize(
            ETMobaBattleDriver driver,
            in BattleStartPlan plan);
    }
}
