using System;
using AbilityKit.Ability.Host;
using AbilityKit.Ability.Host.Framework;
using AbilityKit.Ability.World.Abstractions;
using AbilityKit.Ability.World.Management;
using AbilityKit.Core.Common;
using AbilityKit.Core.Common.Log;
using AbilityKit.Core.Common.SnapshotRouting;
using AbilityKit.Game.Battle.Agent;
using AbilityKit.Game.Flow.Battle.ViewEvents;
using AbilityKit.Game.Flow.Battle.ViewEvents.Snapshot;
using AbilityKit.Game.Flow.Battle.ViewEvents.Triggering;
using AbilityKit.Network.Abstractions;
using AbilityKit.Network.Protocol;

namespace AbilityKit.Game.Flow
{
    internal sealed partial class BattleSessionHandles
    {
        internal sealed class ConfirmedHandles
        {
            internal ConfirmedAuthorityWorldRuntime WorldRuntime;
            internal IWorldManager Worlds;
            internal HostRuntime Runtime;
            internal IWorld World;

            internal ConfirmedAuthorityInputRuntime InputRuntime;
            internal IRemoteFrameSource<PlayerInputCommand[]> InputSource;
            internal IConsumableRemoteFrameSource<PlayerInputCommand[]> Consumable;
            internal IRemoteFrameSink<PlayerInputCommand[]> Sink;

            internal ConfirmedViewEventPipeline ViewEventPipeline;
            internal FrameSnapshotDispatcher Snapshots;

            internal DebugBattleViewEventSink ViewEventSink;

            internal BattleSnapshotViewAdapter SnapshotViewAdapter;
            internal BattleTriggerEventViewBridge TriggerBridge;

            internal BattleContext ViewCtx;
            internal ConfirmedViewSnapshotRuntime ViewSnapshotRuntime;
            internal ConfirmedBattleViewFeature ViewFeature;

            internal void BindWorldRuntime(ConfirmedAuthorityWorldRuntime runtime)
            {
                WorldRuntime = runtime;
                Worlds = runtime != null ? runtime.Worlds : null;
                Runtime = runtime != null ? runtime.Runtime : null;
                World = runtime != null ? runtime.World : null;
            }

            internal void BindInputRuntime(ConfirmedAuthorityInputRuntime runtime)
            {
                InputRuntime = runtime;
                InputSource = runtime != null ? runtime.Source : null;
                Consumable = runtime != null ? runtime.Consumable : null;
                Sink = runtime != null ? runtime.Sink : null;
            }

            internal void BindViewEventPipeline(ConfirmedViewEventPipeline pipeline)
            {
                ViewEventPipeline = pipeline;
                Snapshots = pipeline != null ? pipeline.Snapshots : null;
                ViewEventSink = pipeline != null ? pipeline.EventSink : null;
                SnapshotViewAdapter = pipeline != null ? pipeline.SnapshotViewAdapter : null;
                TriggerBridge = pipeline != null ? pipeline.TriggerBridge : null;
            }

            internal void BindViewSideRuntime(ConfirmedViewSideRuntime runtime)
            {
                ViewCtx = runtime.Context;
                ViewSnapshotRuntime = runtime.SnapshotRuntime;
                ViewFeature = runtime.Feature;
            }

            internal bool HasViewFeature()
            {
                return ViewFeature != null;
            }

            internal void DestroyWorld(WorldId fallbackWorldId)
            {
                if (WorldRuntime != null)
                {
                    WorldRuntime.DestroyWorld();
                    return;
                }

                Runtime?.DestroyWorld(fallbackWorldId);
            }

            internal void ClearWorldRuntime()
            {
                Worlds = null;
                Runtime = null;
                World = null;
                WorldRuntime = null;
            }

            internal void DisposeInput()
            {
                if (InputRuntime != null)
                {
                    DisposeUtils.TryDispose(ref InputRuntime, ex => Log.Exception(ex));
                    InputSource = null;
                }
                else
                {
                    IDisposable inputSourceDisposable = InputSource;
                    InputSource = null;
                    DisposeUtils.TryDispose(ref inputSourceDisposable, ex => Log.Exception(ex));
                }

                Consumable = null;
                Sink = null;
            }

            internal void DisposeViewSnapshotRuntime()
            {
                DisposeUtils.TryDispose(ref ViewSnapshotRuntime, ex => Log.Exception(ex));
            }

            internal void DisposeViewEventPipeline()
            {
                if (ViewEventPipeline != null)
                {
                    DisposeUtils.TryDispose(ref ViewEventPipeline, ex => Log.Exception(ex));
                    Snapshots = null;
                    SnapshotViewAdapter = null;
                    TriggerBridge = null;
                }
                else
                {
                    DisposeUtils.TryDispose(ref Snapshots, ex => Log.Exception(ex));
                    DisposeUtils.TryDispose(ref SnapshotViewAdapter, ex => Log.Exception(ex));
                    DisposeUtils.TryDispose(ref TriggerBridge, ex => Log.Exception(ex));
                }

                ViewEventSink = null;
            }

            internal BattleContext TakeViewContext()
            {
                var ctx = ViewCtx;
                ViewCtx = null;
                return ctx;
            }

            internal ConfirmedBattleViewFeature TakeViewFeature()
            {
                var feature = ViewFeature;
                ViewFeature = null;
                return feature;
            }

            public void Reset()
            {
                ClearWorldRuntime();
                DisposeInput();
                DisposeViewSnapshotRuntime();
                DisposeViewEventPipeline();

                ViewCtx = null;
                ViewFeature = null;
            }
        }
    }
}
