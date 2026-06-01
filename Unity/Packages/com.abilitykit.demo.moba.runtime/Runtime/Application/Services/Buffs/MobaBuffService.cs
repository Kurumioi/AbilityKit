using System;
using System.Collections.Generic;
using AbilityKit.Ability.FrameSync;
using AbilityKit.Ability.Triggering.Runtime;
using AbilityKit.Ability.World.DI;
using AbilityKit.Ability.World.Services;
using AbilityKit.Ability.World.Services.Attributes;
using AbilityKit.Core.Common.Log;
using AbilityKit.Core.Continuous;
using AbilityKit.Demo.Moba.Config.Core;
using AbilityKit.GameplayTags;
using AbilityKit.Trace;

namespace AbilityKit.Demo.Moba.Services
{
    [WorldService(typeof(MobaBuffService))]
    public sealed class MobaBuffService : IWorldInitializable
    {
        private enum BuffCommandKind
        {
            Apply = 0,
            Remove = 1,
        }

        private sealed class BuffCommand
        {
            public long Seq;
            public BuffCommandKind Kind;
            public BuffApplyRequest ApplyRequest;
            public BuffRemoveRequest RemoveRequest;

            public int BuffId => ApplyRequest != null ? ApplyRequest.BuffId : RemoveRequest != null ? RemoveRequest.BuffId : 0;
        }

        [WorldInject] private MobaConfigDatabase _configs;
        [WorldInject] private AbilityKit.Triggering.Eventing.IEventBus _eventBus;
        [WorldInject] private ITriggerActionRunner _actionRunner;
        [WorldInject] private MobaPeriodicEffectService _ongoing;
        [WorldInject] private MobaTraceRegistry _trace;
        [WorldInject] private IFrameTime _frameTime;
        [WorldInject] private MobaActorLookupService _actors;
        [WorldInject] private MobaEffectExecutionService _effects;
        [WorldInject(required: false)] private IGameplayTagService _tags;
        [WorldInject(required: false)] private IMobaContinuousTagTemplateRegistry _tagTemplates;
        [WorldInject(required: false)] private IContinuousManager _continuous;
        [WorldInject(required: false)] private MobaSkillCastRuntimeService _skillRuntimes;
        private BuffLifecycleExecutor _lifecycle;
        private long _nextCommandSeq;
        private readonly List<BuffCommand> _pending = new List<BuffCommand>(32);
        private int _draining;

        public void OnInit(IWorldResolver services)
        {
            var repo = new BuffRepository();
            var ctx = new BuffContextService(_trace, _actionRunner, _frameTime);
            var events = new BuffEventPublisher(_eventBus);
            var periodicBinder = new BuffPeriodicEffectBinder(_ongoing, _actionRunner);
            var stageEffects = new BuffStageEffectExecutor(_effects);
            var stacking = new BuffStackingPolicyApplier();
            _lifecycle = new BuffLifecycleExecutor(_configs, _actors, _ongoing, _tags, _tagTemplates, repo, ctx, events, periodicBinder, stageEffects, stacking, _continuous, _skillRuntimes);
        }

        public global::ActorEntity TryGetActorEntity(int actorId)
        {
            if (_actors != null && _actors.TryGetActorEntity(actorId, out var e) && e != null)
            {
                return e;
            }
            return null;
        }

        public bool ApplyBuffImmediate(global::ActorEntity target, int buffId, int sourceActorId, int durationOverrideMs)
        {
            if (target == null || !target.hasActorId) return false;
            return ApplyBuffImmediate(target.actorId.Value, buffId, sourceActorId, durationOverrideMs, default(BuffOriginContext));
        }

        public bool ApplyBuffImmediate(int targetActorId, int buffId, int sourceActorId, int durationOverrideMs)
        {
            return ApplyBuffImmediate(targetActorId, buffId, sourceActorId, durationOverrideMs, default(BuffOriginContext));
        }

        public bool ApplyBuffImmediate(int targetActorId, int buffId, int sourceActorId, int durationOverrideMs, in BuffOriginContext origin)
        {
            if (!EnqueueApply(targetActorId, buffId, sourceActorId, durationOverrideMs, origin))
            {
                return false;
            }

            DrainPending(maxCommands: 256);
            return true;
        }

        public bool RemoveBuffImmediate(global::ActorEntity target, int buffId, int sourceActorId, TraceLifecycleReason reason)
        {
            if (target == null || !target.hasActorId) return false;
            return RemoveBuffImmediate(target.actorId.Value, buffId, sourceActorId, reason);
        }

        public bool RemoveBuffImmediate(int targetActorId, int buffId, int sourceActorId, TraceLifecycleReason reason)
        {
            if (!EnqueueRemove(targetActorId, buffId, sourceActorId, reason))
            {
                return false;
            }

            DrainPending(maxCommands: 256);
            return true;
        }

        public void DrainPending(int maxCommands)
        {
            if (maxCommands <= 0) return;

            // Protect against re-entrancy if drain triggers effects that call ApplyBuffImmediate again.
            if (_draining > 0) return;

            _draining++;
            try
            {
                var executed = 0;
                var cursor = 0;
                while (cursor < _pending.Count)
                {
                    if (executed >= maxCommands)
                    {
                        Log.Warning($"[MobaBuffService] DrainPending exceeded maxCommands={maxCommands}. pending={_pending.Count}.");
                        break;
                    }

                    var cmd = _pending[cursor++];
                    if (cmd == null) continue;

                    try
                    {
                        switch (cmd.Kind)
                        {
                            case BuffCommandKind.Apply:
                                ExecuteApply(cmd.ApplyRequest);
                                break;
                            case BuffCommandKind.Remove:
                                ExecuteRemove(cmd.RemoveRequest);
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Exception(ex, $"[MobaBuffService] Execute buff command failed. kind={cmd.Kind} buffId={cmd.BuffId}");
                    }

                    executed++;
                }

                if (cursor > 0)
                {
                    _pending.RemoveRange(0, cursor);
                }
            }
            finally
            {
                _draining--;
            }
        }

        private bool EnqueueApply(int targetActorId, int buffId, int sourceActorId, int durationOverrideMs, in BuffOriginContext origin)
        {
            if (targetActorId <= 0) return false;
            if (buffId <= 0) return false;

            var request = BuffApplyRequestBuilder.Create()
                .WithTarget(targetActorId)
                .WithBuff(buffId)
                .WithSource(sourceActorId)
                .WithDurationOverride(durationOverrideMs)
                .WithOrigin(in origin)
                .Build();

            _pending.Add(new BuffCommand
            {
                Seq = ++_nextCommandSeq,
                Kind = BuffCommandKind.Apply,
                ApplyRequest = request,
            });
            return true;
        }

        private bool EnqueueRemove(int targetActorId, int buffId, int sourceActorId, TraceLifecycleReason reason)
        {
            if (targetActorId <= 0) return false;
            if (buffId <= 0) return false;

            var request = BuffRemoveRequestBuilder.Create()
                .WithTarget(targetActorId)
                .WithBuff(buffId)
                .WithSource(sourceActorId)
                .WithReason(reason)
                .Build();

            _pending.Add(new BuffCommand
            {
                Seq = ++_nextCommandSeq,
                Kind = BuffCommandKind.Remove,
                RemoveRequest = request,
            });
            return true;
        }

        private bool ExecuteApply(BuffApplyRequest request)
        {
            if (request == null || !request.IsValid) return false;

            var target = TryGetActorEntity(request.TargetActorId);
            if (target != null && target.hasApplyBuffRequest && target.applyBuffRequest != null && target.applyBuffRequest.BuffId == request.BuffId)
            {
                target.RemoveApplyBuffRequest();
            }

            return _lifecycle.Apply(request);
        }

        private bool ExecuteRemove(BuffRemoveRequest request)
        {
            if (request == null || !request.IsValid) return false;

            var target = TryGetActorEntity(request.TargetActorId);
            if (target != null && target.hasApplyBuffRequest && target.applyBuffRequest != null && target.applyBuffRequest.BuffId == request.BuffId)
            {
                target.RemoveApplyBuffRequest();
            }

            return _lifecycle.Remove(request);
        }

        public void Dispose()
        {
            _pending.Clear();
        }
    }
}
