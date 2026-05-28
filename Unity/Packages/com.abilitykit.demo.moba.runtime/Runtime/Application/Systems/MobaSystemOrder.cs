using AbilityKit.Ability.World;

namespace AbilityKit.Demo.Moba.Systems
{
    /// <summary>
    /// 【模板】游戏系统执行顺序常量
    ///
    /// 此文件定义了 moba.runtime 的系统执行顺序。
    /// 新建游戏世界层时应参考此模板定义自己的 SystemOrder。
    ///
    /// 设计原则:
    /// - Base = WorldSystemOrder.CoreBase + 1000 (业务层基准)
    /// - 使用 WorldSystemPhase 的偏移量 (Early, Normal, Late)
    /// - 每个功能模块预留 10-20 的偏移空间
    ///
    /// 参考文档: Docs/SystemOrderGuide.md
    /// </summary>
    public static class MobaSystemOrder
    {
        // ========== 基准值 ==========
        /// <summary>
        /// 业务层基准值
        /// 所有业务系统以此为基准计算执行顺序
        /// </summary>
        public const int Base = WorldSystemOrder.CoreBase + 1000;

        // ========== 实体管理层 (PreExecute/Early) ==========
        /// <summary>实体管理器同步</summary>
        public const int EntityManagerSync = Base + WorldSystemOrder.Early + 5;
        /// <summary>移动系统初始化</summary>
        public const int MotionInit = Base + WorldSystemOrder.Early + 10;

        // ========== 移动系统 (Execute/Normal) ==========
        /// <summary>移动输入处理</summary>
        public const int MotionLocomotionInput = Base + WorldSystemOrder.Normal + 10;
        /// <summary>移动 Tick</summary>
        public const int MotionTick = Base + WorldSystemOrder.Normal + 50;

        // ========== 技能系统 (Execute/Normal) ==========
        /// <summary>被动技能触发注册</summary>
        public const int PassiveSkillTriggers = Base + WorldSystemOrder.Normal + 85;
        /// <summary>效果监听器</summary>
        public const int EffectListeners = Base + WorldSystemOrder.Normal + 90;
        /// <summary>技能管道执行</summary>
        public const int SkillPipelines = Base + WorldSystemOrder.Normal + 100;

        // ========== 战斗系统 (Execute/Normal) ==========
        /// <summary>效果步骤</summary>
        public const int EffectsStep = Base + WorldSystemOrder.Normal + 200;

        // ========== Buff 系统 (Execute/Normal) ==========
        /// <summary>Buff 命令队列处理</summary>
        public const int BuffCommandsDrain = Base + WorldSystemOrder.Normal + 295;
        /// <summary>Buff 应用</summary>
        public const int BuffsApply = Base + WorldSystemOrder.Normal + 300;
        /// <summary>Buff 移除</summary>
        public const int BuffsRemove = Base + WorldSystemOrder.Normal + 305;
        /// <summary>Buff Tick</summary>
        public const int BuffsTick = Base + WorldSystemOrder.Normal + 310;

        // ========== 持续效果系统 (Execute/Normal) ==========
        /// <summary>持续触发器计划调和</summary>
        public const int OngoingTriggerPlansReconcile = Base + WorldSystemOrder.Normal + 312;
        /// <summary>持续效果 Tick</summary>
        public const int OngoingEffectsTick = Base + WorldSystemOrder.Normal + 315;

        // ========== 清理系统 (PostExecute/Late) ==========
        /// <summary>实体管理器清理</summary>
        public const int EntityManagerCleanup = Base + WorldSystemOrder.Late + 5;
        /// <summary>投射物同步</summary>
        public const int ProjectileSync = Base + WorldSystemOrder.Late + 10;
        /// <summary>投射物发射器清理</summary>
        public const int ProjectileLauncherCleanup = Base + WorldSystemOrder.Late + 12;
        /// <summary>召唤物生命周期</summary>
        public const int SummonLifecycle = Base + WorldSystemOrder.Late + 14;
    }
}
