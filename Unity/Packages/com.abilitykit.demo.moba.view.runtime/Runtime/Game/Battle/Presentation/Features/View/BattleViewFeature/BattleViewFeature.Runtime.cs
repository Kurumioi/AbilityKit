using System;
using AbilityKit.Ability.World.Abstractions;
using AbilityKit.Demo.Moba.Services;
using AbilityKit.Game.Battle.Entity;
using AbilityKit.Game.Battle.Vfx;
using AbilityKit.Game.Flow.Battle.View;
using AbilityKit.Game.Flow.Battle.ViewEvents;
using AbilityKit.Game.Flow.Battle.ViewEvents.Snapshot;
using AbilityKit.Game.Flow.Battle.ViewEvents.Triggering;
using EC = AbilityKit.World.ECS;

namespace AbilityKit.Game.Flow
{
    public sealed partial class BattleViewFeature : IViewFeatureRuntime
    {
        private IDisposable _entityDestroyedSub;
        private int _lastAlignedFrame = int.MinValue;

        BattleContext IViewSharedSubFeatureHost.Context => _ctx;
        BattleViewBinder IViewSharedSubFeatureHost.Binder => _binder;
        bool IViewSharedSubFeatureHost.IsConfirmed => false;
        WorldId IViewSharedSubFeatureHost.WorldId => _ctx != null ? _ctx.RuntimeWorldId : default;

        void IViewSharedSubFeatureHost.RefreshDirtyViews() => ViewFeatureRuntimeOperations.RefreshDirtyViews(this);
        void IViewSharedSubFeatureHost.RegisterAllSeekables() => ViewFeatureRuntimeOperations.RegisterAllSeekables(this);
        void IViewSharedSubFeatureHost.SeekAllToCurrentFrame() => ViewFeatureRuntimeOperations.SeekAllToCurrentFrame(this);
        void IViewSharedSubFeatureHost.RebindAllViews() => ViewFeatureRuntimeOperations.RebindAllViews(this);
        void IViewSharedSubFeatureHost.TickVfx() => ViewFeatureRuntimeOperations.TickVfx(this);
        void IViewSharedSubFeatureHost.TickFloatingTexts(float deltaTime) => ViewFeatureRuntimeOperations.TickFloatingTexts(this, deltaTime);

        IBattleEntityQuery IViewFeatureRuntime.Query
        {
            get => _query;
            set => _query = value;
        }

        BattleViewBinder IViewFeatureRuntime.Binder
        {
            get => _binder;
            set => _binder = value;
        }

        BattleVfxManager IViewFeatureRuntime.Vfx
        {
            get => _vfx;
            set => _vfx = value;
        }

        EC.IEntity IViewFeatureRuntime.VfxNode
        {
            get => _vfxNode;
            set => _vfxNode = value;
        }

        ViewTimeline IViewFeatureRuntime.Timeline
        {
            get => _timeline;
            set => _timeline = value;
        }

        BattleFloatingTextSystem IViewFeatureRuntime.FloatingTexts
        {
            get => _floatingTexts;
            set => _floatingTexts = value;
        }

        BattleAreaViewSystem IViewFeatureRuntime.AreaViews
        {
            get => _areaViews;
            set => _areaViews = value;
        }

        IBattleViewEventSink IViewFeatureRuntime.EventSink
        {
            get => _eventSink;
            set => _eventSink = value;
        }

        BattleSnapshotViewAdapter IViewFeatureRuntime.SnapshotAdapter
        {
            get => _snapshotAdapter;
            set => _snapshotAdapter = value;
        }

        BattleTriggerEventViewAdapter IViewFeatureRuntime.TriggerAdapter
        {
            get => _triggerAdapter;
            set => _triggerAdapter = value;
        }

        IDisposable IViewFeatureRuntime.EntityDestroyedSubscription
        {
            get => _entityDestroyedSub;
            set => _entityDestroyedSub = value;
        }

        int IViewFeatureRuntime.LastAlignedFrame
        {
            get => _lastAlignedFrame;
            set => _lastAlignedFrame = value;
        }

        void IViewFeatureRuntime.OnEntityDestroyed(EC.EntityDestroyed evt) => ViewFeatureRuntimeOperations.OnEntityDestroyed(this, evt);
    }
}
