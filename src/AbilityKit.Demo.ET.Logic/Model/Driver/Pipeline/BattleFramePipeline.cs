using System;
using System.Collections.Generic;
using AbilityKit.Pipeline;

namespace ET.Logic
{
    /// <summary>
    /// 战斗帧管线配置
    /// 实现 IAbilityPipelineConfig 接口
    /// </summary>
    public sealed class BattleFramePipelineConfig : IAbilityPipelineConfig
    {
        public int ConfigId => 0;
        public string ConfigName => "BattleFrame";
        public IReadOnlyList<IAbilityPhaseConfig> PhaseConfigs => Array.Empty<IAbilityPhaseConfig>();
        public bool AllowInterrupt => true;
        public bool AllowPause => true;
    }

    /// <summary>
    /// 战斗帧处理管线
    /// 统一管理一帧内的所有处理流程
    ///
    /// 设计:
    /// - 使用 AbilityPipeline 模块
    /// - 阶段按顺序执行:
    ///   PreTick -> ProcessETInput -> DriveWorld -> CollectSnapshot -> DispatchSnapshot -> PostTick
    /// - 每个阶段独立可测试
    /// - 支持条件执行 (ShouldExecute)
    /// - 支持扩展点 (Hooks)
    ///
    /// 帧处理流程:
    /// 1. PreTickPhase: 递增帧号、清空上下文
    /// 2. ProcessETInputPhase: 从 ETInputComponent 读取命令并提交到战斗输入端口
    /// 3. DriveWorldPhase: 驱动 moba.core 世界执行所有系统
    /// 4. CollectSnapshotPhase: 从 moba.core 收集实体状态快照
    /// 5. DispatchSnapshotPhase: 将快照分发给视图层
    /// 6. PostTickPhase: 后处理（调试统计等）
    /// </summary>
    public sealed class BattleFramePipeline : AbilityPipeline<BattleFrameContext>
    {
        private static readonly BattleFramePipelineConfig _config = new BattleFramePipelineConfig();

        /// <summary>
        /// 创建管线实例
        /// </summary>
        public BattleFramePipeline()
        {
            // 按顺序添加所有阶段
            AddPhase(new PreTickPhase());
            AddPhase(new ProcessETInputPhase());
            AddPhase(new DriveWorldPhase());
            AddPhase(new CollectSnapshotPhase());
            AddPhase(new DispatchSnapshotPhase());
            AddPhase(new PostTickPhase());
        }

        protected override void ReleaseContext(BattleFrameContext context)
        {
            // 清理上下文
            context?.Reset();
        }

        /// <summary>
        /// 执行单帧
        ///
        /// 设计说明：
        /// - 帧号由 PreTickPhase 递增，这里只负责执行管线
        /// - 帧号递增顺序：PreTickPhase → ProcessETInputPhase → DriveWorldPhase → ...
        /// </summary>
        public void Tick(ETMobaBattleDriver driver, float deltaTime)
        {
            if (driver == null || !driver.IsRunning)
            {
                return;
            }

            // 创建上下文
            // 注意：帧号已在 PreTickPhase 中递增
            var context = BattleFrameContext.Create(driver, driver.CurrentFrame, deltaTime, driver.LogicTimeSeconds);

            Log.Debug($"[BattleFramePipeline] Tick started: Frame={driver.CurrentFrame}, DeltaTime={deltaTime:F4}");

            // 启动并执行管线
            var run = Start(_config, context);
            run.Tick(deltaTime);

            // 更新 Driver 状态
            driver.CurrentFrame = context.CurrentFrame;
            driver.LogicTimeSeconds = context.LogicTimeSeconds;

            Log.Debug($"[BattleFramePipeline] Tick completed: Frame={driver.CurrentFrame}, TransformSnapshots.Count={context.TransformSnapshots.Count}");

            // 释放上下文
            ReleaseContext(context);
        }
    }
}
