using AbilityKit.Ability.FrameSync;
using AbilityKit.Ability.Host;

/// <summary>
/// 文件名称: IMobaSnapshotEmitter.cs
/// 
/// 功能描述: 定义可扩展的 MOBA 快照输出接口。
/// 
/// 创建日期: 2026-05-27
/// 修改日期: 2026-05-27
/// </summary>
namespace AbilityKit.Demo.Moba.Services
{
    /// <summary>
    /// MOBA 快照输出器接口，供快照路由按优先级统一调度。
    /// </summary>
    public interface IMobaSnapshotEmitter
    {
        /// <summary>
        /// 尝试生成当前帧快照。
        /// </summary>
        /// <param name="frame">当前帧</param>
        /// <param name="snapshot">输出快照</param>
        /// <returns>是否生成了快照</returns>
        bool TryGetSnapshot(FrameIndex frame, out WorldStateSnapshot snapshot);
    }
}