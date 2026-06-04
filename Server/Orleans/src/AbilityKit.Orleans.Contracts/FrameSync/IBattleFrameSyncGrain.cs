using Orleans;

namespace AbilityKit.Orleans.Contracts.FrameSync;

/// <summary>
/// Orleans server-side frame synchronization channel.
/// Retained alongside state synchronization so battle rooms can choose frame-sync
/// or state-sync flows while sharing protocol models across local grain calls and
/// gateway network requests.
/// </summary>
public interface IBattleFrameSyncGrain : IGrainWithStringKey
{
    Task SubscribeAsync(IFrameSyncObserver observer);

    Task UnsubscribeAsync(IFrameSyncObserver observer);

    Task SubmitInputAsync(ulong worldId, int frame, FrameInputItem input);
}
