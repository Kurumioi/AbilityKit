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
    public abstract class ViewFeatureRuntimeHostBase : IViewFeatureRuntime
    {
        private IBattleEntityQuery _query;
        private BattleViewBinder _binder;
        private BattleVfxManager _vfx;
        private EC.IEntity _vfxNode;
        private ViewTimeline _timeline;
        private BattleFloatingTextSystem _floatingTexts;
        private BattleAreaViewSystem _areaViews;
        private IBattleViewEventSink _eventSink;
        private BattleSnapshotViewAdapter _snapshotAdapter;
        private BattleTriggerEventViewAdapter _triggerAdapter;
        private IDisposable _entityDestroyedSub;
        private int _lastAlignedFrame = int.MinValue;
        private BattlePresentationSessionContext _presentation;
        private readonly ViewFeatureRuntimeOperations _operations = new ViewFeatureRuntimeOperations();
        private readonly BattlePresentationSessionResolver _presentationSessions = new BattlePresentationSessionResolver();

        protected abstract BattleContext RuntimeContext { get; }
        protected abstract bool RuntimeIsConfirmed { get; }

        protected void SetRuntimeQuery(IBattleEntityQuery query)
        {
            _query = query;
        }

        protected void BindPresentationSession(in GamePhaseContext ctx)
        {
            _presentation = _presentationSessions.Resolve(ctx);
        }

        protected void ClearPresentationSession(in GamePhaseContext ctx)
        {
            _presentationSessions.Release(ctx, _presentation);
            _presentation = null;
        }

        BattleContext IViewSharedSubFeatureHost.Context => RuntimeContext;
        BattleViewBinder IViewSharedSubFeatureHost.Binder => _binder;
        bool IViewSharedSubFeatureHost.IsConfirmed => RuntimeIsConfirmed;
        WorldId IViewSharedSubFeatureHost.WorldId => RuntimeContext != null ? RuntimeContext.RuntimeWorldId : default;

        void IViewSharedSubFeatureHost.RefreshDirtyViews() => _operations.RefreshDirtyViews(this);
        void IViewSharedSubFeatureHost.RegisterAllSeekables() => _operations.RegisterAllSeekables(this);
        void IViewSharedSubFeatureHost.SeekAllToCurrentFrame() => _operations.SeekAllToCurrentFrame(this);
        void IViewSharedSubFeatureHost.RebindAllViews() => _operations.RebindAllViews(this);
        void IViewSharedSubFeatureHost.TickVfx() => _operations.TickVfx(this);
        void IViewSharedSubFeatureHost.TickFloatingTexts(float deltaTime) => _operations.TickFloatingTexts(this, deltaTime);

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

        BattleViewResourceProvider IViewFeatureRuntime.Resources => EnsurePresentationSession().Resources;

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

        void IViewFeatureRuntime.OnEntityDestroyed(EC.EntityDestroyed evt) => _operations.OnEntityDestroyed(this, evt);

        private BattlePresentationSessionContext EnsurePresentationSession()
        {
            return _presentation ?? (_presentation = BattlePresentationSessionContext.CreateFromDefaultResources());
        }
    }
}
