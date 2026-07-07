using AbilityKit.Ability.World.Abstractions;
using AbilityKit.Core.Recording.FrameRecord;
using AbilityKit.Core.Snapshots.Routing;
using AbilityKit.Game.Battle;
using AbilityKit.Game.Battle.Entity;
using AbilityKit.Game.Flow.Battle.Modules;
using EC = AbilityKit.World.ECS;

namespace AbilityKit.Game.Flow
{
    /// <summary>
    /// 会话运行期只读上下文契约，避免功能模块直接依赖完整的 BattleContext 大对象。
    /// </summary>
    public interface IBattleRuntimeContext
    {
        BattleLogicSession Session { get; }
        BattleStartPlan Plan { get; }
        int LastFrame { get; }
        double LogicTimeSeconds { get; }
        int LocalActorId { get; }
        BattleSessionHooks Hooks { get; }
    }

    /// <summary>
    /// 表现实体层上下文契约。
    /// </summary>
    public interface IBattleEntityContext
    {
        WorldId RuntimeWorldId { get; }
        bool HasRuntimeWorldId { get; }
        EC.IEntity EntityNode { get; }
        EC.IECWorld EntityWorld { get; }
        BattleEntityLookup EntityLookup { get; }
        BattleEntityFactory EntityFactory { get; }
        IBattleEntityQuery EntityQuery { get; }
    }

    /// <summary>
    /// 输入层上下文契约。
    /// </summary>
    public interface IBattleInputContext
    {
        IFrameRecordWriter InputRecordWriter { get; }
        BattleLocalInputQueue LocalInputQueue { get; }
    }

    /// <summary>
    /// 快照路由上下文契约。
    /// </summary>
    public interface IBattleSnapshotRoutingContext
    {
        FrameSnapshotDispatcher FrameSnapshots { get; }
        SnapshotPipeline SnapshotPipeline { get; }
        SnapshotCmdHandler CmdHandler { get; }
    }
}
