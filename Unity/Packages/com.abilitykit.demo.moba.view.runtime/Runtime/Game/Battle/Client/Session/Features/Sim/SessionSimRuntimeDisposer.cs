using System;
using AbilityKit.Ability.World.Abstractions;
using AbilityKit.Core.Common.Log;
using AbilityKit.World.ECS;

namespace AbilityKit.Game.Flow
{
    internal static class SessionSimRuntimeDisposer
    {
        public static void DestroyBattleWorlds(
            BattleStartPlan plan,
            BattleSessionHandles handles)
        {
            try
            {
                handles.RemoteDriven.DestroyWorld(new WorldId(plan.WorldId));
                handles.Confirmed.DestroyWorld(ConfirmedAuthorityWorldId.Create(plan));
            }
            catch (Exception ex)
            {
                Log.Exception(ex);
            }
        }

        public static void DisposeConfirmedView(
            GameFlowDomain flow,
            BattleSessionHandles.ConfirmedHandles handles,
            Action<IEntity> destroyEntityTree)
        {
            DetachConfirmedViewFeature(flow, handles);
            handles.DisposeViewSnapshotRuntime();
            DisposeConfirmedViewContext(handles, destroyEntityTree);
        }

        public static void DisposeRemoteDrivenWorld(
            BattleSessionHandles.RemoteDrivenHandles handles,
            Action resetTickState)
        {
            handles.ClearWorldRuntime();
            resetTickState?.Invoke();
            handles.DisposeInput();
        }

        public static void DisposeConfirmedWorld(
            BattleContext ctx,
            BattleSessionHandles.ConfirmedHandles handles,
            Action resetTickState)
        {
            handles.ClearWorldRuntime();
            resetTickState?.Invoke();
            handles.DisposeInput();
            handles.DisposeViewEventPipeline();
            ConfirmedAuthorityDebugStatsPublisher.Clear(ctx);
        }

        private static void DetachConfirmedViewFeature(
            GameFlowDomain flow,
            BattleSessionHandles.ConfirmedHandles handles)
        {
            var feature = handles.TakeViewFeature();
            if (flow != null && feature != null)
            {
                flow.Detach(feature);
            }
        }

        private static void DisposeConfirmedViewContext(
            BattleSessionHandles.ConfirmedHandles handles,
            Action<IEntity> destroyEntityTree)
        {
            ConfirmedViewContextDisposer.Dispose(handles.TakeViewContext(), destroyEntityTree);
        }
    }
}
