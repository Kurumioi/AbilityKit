using AbilityKit.Network.Runtime.Conditioning;

namespace AbilityKit.Game.Flow
{
    /// <summary>
    /// BattleSessionFeature 的网络环境模拟集成 partial。
    /// 暴露 <see cref="NetworkConditionController"/> 供外部（调试面板、测试、自动化）运行时切换网络环境。
    /// </summary>
    public sealed partial class BattleSessionFeature
    {
        /// <summary>
        /// 网络环境模拟控制器。在网关连接建立后自动挂载到该连接的中间件管线。
        /// 即使未建立网关连接，也可提前调用 <see cref="NetworkConditionController.ApplyPreset"/>
        /// 预设档案，待连接建立时自动生效。
        /// </summary>
        public NetworkConditionController NetworkCondition { get; } = new NetworkConditionController();

        /// <summary>
        /// 将网络环境模拟控制器绑定到当前网关连接（若存在）。
        /// 在 <see cref="StartGatewayRoomPreparation"/> 创建连接后调用。
        /// </summary>
        private void AttachNetworkConditionToGatewayConnection()
        {
            if (_gatewayConn == null) return;

            // 未显式启用时仅绑定生命周期，不改变现有网络流量。
            NetworkCondition.Attach(_gatewayConn);
        }

        /// <summary>
        /// 解除网络环境模拟控制器与当前网关连接的绑定。
        /// 在 <see cref="StopGatewayRoomPreparation"/> 中调用。
        /// </summary>
        private void DetachNetworkConditionFromGatewayConnection()
        {
            NetworkCondition.Detach();
        }
    }
}
