namespace AbilityKit.Demo.Moba.Util.Generator
{
    /// <summary>
    /// 【模板】游戏实体工厂接口
    ///
    /// 此文件提供了实体创建的标准接口。
    /// 新建游戏世界层时应参考此模板定义自己的实体工厂。
    ///
    /// 设计原则:
    /// - 通过配置（Specification）创建实体，而非直接实例化
    /// - 支持实体生命周期管理（创建、销毁）
    /// - 与快照系统集成
    ///
    /// 参考文档: 暂无（TODO: 创建 EntityFactoryGuide.md）
    /// </summary>
    public interface IGameEntityFactory
    {
        /// <summary>
        /// 从进入游戏请求创建所有实体
        ///
        /// 示例实现参考: ActorSpawnPipeline.BuildActorsFromEnterGameReqAndInitialize()
        /// </summary>
        /// <param name="context">Entitas 上下文</param>
        /// <param name="spec">游戏开始规格</param>
        /// <returns>创建的 ActorId 列表</returns>
        int[] CreateFromEnterGameSpec(object context, object spec);

        /// <summary>
        /// 销毁指定实体
        /// </summary>
        /// <param name="actorId">要销毁的 ActorId</param>
        void Destroy(int actorId);
    }

    /// <summary>
    /// 【模板】实体初始化上下文
    ///
    /// 在实体创建过程中传递初始化参数。
    /// 具体字段参考现有的 MobaPlayerLoadout 结构。
    /// </summary>
    public struct GameEntityInitContext
    {
        /// <summary>
        /// ActorId
        /// </summary>
        public int ActorId;

        /// <summary>
        /// 队伍
        /// </summary>
        public int Team;

        /// <summary>
        /// 实体主类型
        /// </summary>
        public int MainType;

        /// <summary>
        /// 实体子类型
        /// </summary>
        public int SubType;

        /// <summary>
        /// 位置 X
        /// </summary>
        public float PositionX;

        /// <summary>
        /// 位置 Z
        /// </summary>
        public float PositionZ;

        /// <summary>
        /// 旋转
        /// </summary>
        public float Rotation;

        /// <summary>
        /// 所属玩家
        /// </summary>
        public long OwnerPlayerId;
    }
}
