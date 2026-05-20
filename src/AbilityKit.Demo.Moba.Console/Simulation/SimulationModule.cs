using System;

namespace AbilityKit.Demo.Moba.Console.Simulation
{
    /// <summary>
    /// Simulation 模块入口
    ///
    /// 此模块包含 Console 项目的表现层模拟代码
    ///
    /// 架构说明：
    /// ┌─────────────────────────────────────────────────────────────┐
    /// │ Simulation Layer (模拟层)                                    │
    /// │ - ConsoleActorRepository: 角色数据存储（唯一数据源）          │
    /// │ - ActorState: 角色状态快照                                  │
    /// │ - ShareEntityAdapter: 适配 Share 层接口                      │
    /// └─────────────────────────────────────────────────────────────┘
    /// ┌─────────────────────────────────────────────────────────────┐
    /// │ View Layer (表现层)                                         │
    /// │ - ConsoleBattleView: 视图渲染                              │
    /// │ - ConsoleViewEventSink: 事件接收                            │
    /// │ - ConsoleViewBinder: 视图绑定和插值                          │
    /// │ 数据流：Simulation → ConsoleViewEventSink → ConsoleBattleView │
    /// └─────────────────────────────────────────────────────────────┘
    ///
    /// 生产环境应使用真正的 AbilityKit.Ability.Runtime
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
        public const string Version = "4.0.0";
    }
}
