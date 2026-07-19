using Orleans;

namespace AbilityKit.Orleans.Contracts.FrameSync;

/// <summary>
/// Orleans 服务器侧帧同步通道。
/// 它与状态同步并存，让战斗房间可以选择帧同步或状态同步流程，同时在本地 grain 调用与网关网络请求之间共享协议模型。
/// </summary>
public interface IBattleFrameSyncGrain : IGrainWithStringKey
{
    Task InitializeAsync(FrameSyncStartOptions options);

    Task SubscribeAsync(IFrameSyncObserver observer);

    Task UnsubscribeAsync(IFrameSyncObserver observer);

    Task SubmitInputAsync(ulong worldId, int frame, FrameInputItem input);

    Task<FrameInputSubmitResult> SubmitInputWithResultAsync(ulong worldId, int frame, FrameInputItem input);
}
