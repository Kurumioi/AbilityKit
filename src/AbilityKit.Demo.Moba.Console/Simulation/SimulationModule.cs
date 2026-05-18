using System;

namespace AbilityKit.Demo.Moba.Console.Simulation
{
    /// <summary>
    /// Simulation 模块入口
    ///
    /// 此模块包含 Console 项目的"模拟逻辑层"代码
    /// 用于演示目的，模拟真实战斗系统的行为
    ///
    /// 架构说明：
    /// - Console 项目属于"表现层"，只负责渲染和输入采集
    /// - Simulation 模块提供简化的"模拟逻辑"，用于演示
    /// - 生产环境应使用真正的 AbilityKit.Ability.Runtime
    ///
    /// 模块内容：
    /// - SimulatedBattleSession：模拟战斗会话（逻辑层入口）
    /// </summary>
    public static class SimulationModule
    {
        /// <summary>
        /// 模块名称
        /// </summary>
        public const string Name = "Simulation";

        /// <summary>
        /// 模块版本
        /// </summary>
        public const string Version = "1.0.0";
    }
}
