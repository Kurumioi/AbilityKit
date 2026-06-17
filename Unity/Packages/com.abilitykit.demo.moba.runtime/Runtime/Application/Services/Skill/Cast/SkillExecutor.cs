using System;
using System.Collections.Generic;
using AbilityKit.Ability;
using AbilityKit.Ability.FrameSync;
using AbilityKit.Ability.Share.ECS;
using AbilityKit.ECS;
using AbilityKit.Ability.Share.ECS.Entitas;
using AbilityKit.Core.Mathematics;
using AbilityKit.Ability.Triggering;
using AbilityKit.Ability.World.DI;
using AbilityKit.Ability.World.Services;
using AbilityKit.Ability.World.Services.Attributes;
using AbilityKit.Demo.Moba;
using AbilityKit.Demo.Moba.Config.Core;
using AbilityKit.Protocol.Moba;
using AbilityKit.Pipeline;
using AbilityKit.Triggering.Eventing;
using AbilityKit.Trace;
using AbilityKit.Core.Logging;

namespace AbilityKit.Demo.Moba.Services
{
    public readonly struct SkillCastPolicy
    {
        public static readonly SkillCastPolicy Default = new SkillCastPolicy(allowParallel: false, interruptRunning: false);

        public SkillCastPolicy(bool allowParallel, bool interruptRunning)
        {
            AllowParallel = allowParallel;
            InterruptRunning = interruptRunning;
        }

        public bool AllowParallel { get; }
        public bool InterruptRunning { get; }

        public SkillCastPolicy WithAllowParallel(bool allowParallel)
        {
            return new SkillCastPolicy(allowParallel, InterruptRunning);
        }

        public SkillCastPolicy WithInterruptRunning(bool interruptRunning)
        {
            return new SkillCastPolicy(AllowParallel, interruptRunning);
        }
    }

    [WorldService(typeof(SkillExecutor))]
    public sealed class SkillExecutor : IService
    {
        private readonly IWorldResolver _services;
        private readonly IWorldClock _clock;
        private readonly IFrameTime _time;
        private readonly AbilityKit.Triggering.Eventing.IEventBus _eventBus;
        private readonly IUnitResolver _units;
        private readonly MobaSkillLoadoutService _loadout;
        private readonly MobaActorLookupService _actors;
        private readonly IMobaSkillPipelineLibrary _library;
        private readonly IMobaBattleDiagnosticsService _diagnostics;
        private readonly IMobaBattleExceptionPolicy _exceptions;
        private readonly ISkillLogger _skillLogger;
        private readonly SkillCastPreparationService _preparation;
        private readonly SkillCastPolicyResolver _policyResolver;

        private readonly Dictionary<int, SkillPipelineRunner> _runners = new Dictionary<int, SkillPipelineRunner>();
        private SkillCastPolicy _castPolicy = SkillCastPolicy.Default;

        public SkillCastPolicy CastPolicy
        {
            get => _castPolicy;
            set => _castPolicy = value;
        }

        public bool AllowParallel
        {
            get => _castPolicy.AllowParallel;
            set => _castPolicy = _castPolicy.WithAllowParallel(value);
        }

        public bool InterruptRunning
        {
            get => _castPolicy.InterruptRunning;
            set => _castPolicy = _castPolicy.WithInterruptRunning(value);
        }

        public SkillExecutor(
            IWorldResolver services,
            IWorldClock clock,
            IFrameTime time,
            AbilityKit.Triggering.Eventing.IEventBus eventBus,
            IUnitResolver units,
            MobaSkillLoadoutService loadout,
            MobaActorLookupService actors,
            IMobaSkillPipelineLibrary library,
            IMobaBattleDiagnosticsService diagnostics = null,
            IMobaBattleExceptionPolicy exceptions = null,
            ISkillLogger skillLogger = null)
        {
            _services = services ?? throw new ArgumentNullException(nameof(services));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _time = time ?? throw new ArgumentNullException(nameof(time));
            _eventBus = eventBus;
            _units = units ?? throw new ArgumentNullException(nameof(units));
            _loadout = loadout ?? throw new ArgumentNullException(nameof(loadout));
            _actors = actors ?? throw new ArgumentNullException(nameof(actors));
            _library = library ?? throw new ArgumentNullException(nameof(library));
            _diagnostics = diagnostics;
            _exceptions = exceptions;
            _skillLogger = skillLogger ?? SkillLogger.Instance;
            _preparation = new SkillCastPreparationService(_services, _eventBus, _units, _actors, _library);
            _policyResolver = new SkillCastPolicyResolver(_services);
        }

        private SkillPipelineRunner GetOrCreateRunner(int actorId)
        {
            if (!_runners.TryGetValue(actorId, out var r) || r == null)
            {
                r = new SkillPipelineRunner(actorId, _diagnostics, _exceptions, _skillLogger);
                _runners[actorId] = r;
            }

            return r;
        }

        public bool CastBySlot(int actorId, int slot)
        {
            return CastBySlot(actorId, slot, out _);
        }

        public bool CastBySlot(int actorId, int slot, out string failReason)
        {
            var result = TryCastBySlot(actorId, slot);
            failReason = result.FailReason;
            return result.Success;
        }

        public MobaSkillCastResult TryCastBySlot(int actorId, int slot)
        {
            if (!_loadout.TryGetSkillId(actorId, slot, out var skillId))
            {
                return MobaSkillCastResult.Failed("Skill not found in slot.");
            }

            return TryCastSkill(actorId, skillId, slot);
        }

        public bool HandleInput(int actorId, in SkillInputEvent evt)
        {
            return TryHandleInput(actorId, in evt, out _);
        }

        public bool TryHandleInput(int actorId, in SkillInputEvent evt, out string failReason)
        {
            if (!ValidateSkillInput(actorId, in evt, out failReason))
            {
                return false;
            }

            return DispatchSkillInputPhase(actorId, in evt, out failReason);
        }

        private static bool ValidateSkillInput(int actorId, in SkillInputEvent evt, out string failReason)
        {
            failReason = null;
            if (actorId <= 0)
            {
                failReason = "Invalid actor id.";
                return false;
            }

            if (evt.Slot <= 0)
            {
                failReason = "Invalid skill slot.";
                return false;
            }

            return true;
        }

        private bool DispatchSkillInputPhase(int actorId, in SkillInputEvent evt, out string failReason)
        {
            switch (evt.Phase)
            {
                case SkillInputPhase.Press:
                    return HandlePressInput(actorId, in evt, out failReason);
                case SkillInputPhase.Hold:
                    return HandleHoldInput(actorId, in evt, out failReason);
                case SkillInputPhase.Release:
                    return HandleReleaseInput(actorId, in evt, out failReason);
                case SkillInputPhase.Cancel:
                    return HandleCancelInput(actorId, evt.Slot, out failReason);
                default:
                    failReason = $"Unsupported skill input phase: {evt.Phase}.";
                    return false;
            }
        }

        private bool HandlePressInput(int actorId, in SkillInputEvent evt, out string failReason)
        {
            if (TryUpdateRunningInput(actorId, evt.Slot, in evt.AimPos, in evt.AimDir, evt.TargetActorId))
            {
                failReason = $"UpdatedRunningInput(Slot={evt.Slot},Target={evt.TargetActorId})";
                return true;
            }

            return TryStartCastFromInput(actorId, in evt, out failReason);
        }

        private bool HandleHoldInput(int actorId, in SkillInputEvent evt, out string failReason)
        {
            if (TryUpdateRunningInput(actorId, evt.Slot, in evt.AimPos, in evt.AimDir, evt.TargetActorId))
            {
                failReason = $"UpdatedRunningInput(Slot={evt.Slot},Target={evt.TargetActorId})";
                return true;
            }

            failReason = "No running skill for hold input.";
            return false;
        }

        private bool HandleReleaseInput(int actorId, in SkillInputEvent evt, out string failReason)
        {
            if (TryReleaseRunningInput(actorId, evt.Slot, in evt.AimPos, in evt.AimDir, evt.TargetActorId))
            {
                failReason = $"ReleasedRunningInput(Slot={evt.Slot},Target={evt.TargetActorId})";
                return true;
            }

            return TryStartCastFromInput(actorId, in evt, out failReason);
        }

        private bool HandleCancelInput(int actorId, int slot, out string failReason)
        {
            if (CancelBySlot(actorId, slot))
            {
                failReason = $"CancelledRunningInput(Slot={slot})";
                return true;
            }

            failReason = "No running skill for cancel input.";
            return false;
        }

        private bool TryStartCastFromInput(int actorId, in SkillInputEvent evt, out string failReason)
        {
            var cast = CastBySlot(actorId, evt.Slot, in evt.AimPos, in evt.AimDir, evt.TargetActorId, out failReason);
            if (cast && string.IsNullOrEmpty(failReason))
            {
                failReason = $"CastBySlotStarted(Slot={evt.Slot},Target={evt.TargetActorId})";
            }

            return cast;
        }

        public bool CastBySlot(int actorId, int slot, in Vec3 aimPos, in Vec3 aimDir, out string failReason)
        {
            return CastBySlot(actorId, slot, in aimPos, in aimDir, targetActorId: 0, out failReason);
        }

        public bool CastBySlot(int actorId, int slot, in Vec3 aimPos, in Vec3 aimDir, int targetActorId, out string failReason)
        {
            var result = TryCastBySlot(actorId, slot, in aimPos, in aimDir, targetActorId);
            failReason = result.FailReason;
            return result.Success;
        }

        public MobaSkillCastResult TryCastBySlot(int actorId, int slot, in Vec3 aimPos, in Vec3 aimDir)
        {
            return TryCastBySlot(actorId, slot, in aimPos, in aimDir, targetActorId: 0);
        }

        public MobaSkillCastResult TryCastBySlot(int actorId, int slot, in Vec3 aimPos, in Vec3 aimDir, int targetActorId)
        {
            if (!_loadout.TryGetSkillId(actorId, slot, out var skillId))
            {
                return MobaSkillCastResult.Failed(
                    "Skill not found in slot.",
                    new MobaSkillCastFailure("Preparation", null, "skill.cast.slotNotFound", "Skill not found in slot."));
            }

            return TryCastSkill(actorId, skillId, slot, in aimPos, in aimDir, targetActorId);
        }

        public bool CastSkill(int actorId, int skillId)
        {
            return TryCastSkill(actorId, skillId).Success;
        }

        public MobaSkillCastResult TryCastSkill(int actorId, int skillId)
        {
            return TryCastSkill(actorId, skillId, slot: 0);
        }

        public bool CastSkill(int actorId, int skillId, int slot, out string failReason)
        {
            var result = TryCastSkill(actorId, skillId, slot);
            failReason = result.FailReason;
            return result.Success;
        }

        public MobaSkillCastResult TryCastSkill(int actorId, int skillId, int slot)
        {
            return CastSkillInternal(actorId, skillId, slot, aimPos: default, aimDir: default, hasAim: false);
        }

        public bool CastSkill(int actorId, int skillId, int slot, in Vec3 aimPos, in Vec3 aimDir, out string failReason)
        {
            var result = TryCastSkill(actorId, skillId, slot, in aimPos, in aimDir);
            failReason = result.FailReason;
            return result.Success;
        }

        public MobaSkillCastResult TryCastSkill(int actorId, int skillId, int slot, in Vec3 aimPos, in Vec3 aimDir)
        {
            return TryCastSkill(actorId, skillId, slot, in aimPos, in aimDir, targetActorId: 0);
        }

        public MobaSkillCastResult TryCastSkill(int actorId, int skillId, int slot, in Vec3 aimPos, in Vec3 aimDir, int targetActorId)
        {
            return CastSkillInternal(actorId, skillId, slot, aimPos, aimDir, hasAim: true, targetActorId);
        }

        private MobaSkillCastResult CastSkillInternal(int actorId, int skillId, int slot, in Vec3 aimPos, in Vec3 aimDir, bool hasAim, int targetActorId = 0)
        {
            var input = new SkillCastPreparationInput(actorId, skillId, slot, in aimPos, in aimDir, hasAim, targetActorId);
            var prepared = _preparation.Prepare(in input);
            if (!prepared.Success)
            {
                return MobaSkillCastResult.Failed(
                    prepared.FailReason,
                    new MobaSkillCastFailure("Preparation", null, "skill.cast.prepareFailed", prepared.FailReason));
            }

            return StartPreparedCast(actorId, skillId, in prepared);
        }

        private MobaSkillCastResult StartPreparedCast(int actorId, int skillId, in SkillCastPreparationResult prepared)
        {
            var ctx = prepared.Context;
            var req = prepared.Request;
            var runner = GetOrCreateRunner(actorId);
            var policy = _policyResolver.Resolve(skillId, _castPolicy);
            var success = runner.Start(
                prepared.PreCastConfig,
                prepared.PreCastPhases,
                prepared.CastConfig,
                prepared.CastPhases,
                abilityInstance: this,
                in req,
                ctx,
                out var failReason,
                policy: policy);
            var failure = BuildCastFailure(runner, failReason);
            if (!success)
            {
                prepared.Runtimes.ForceTerminate(in ctx.RuntimeHandle, MobaSkillRuntimeEndReason.RollbackCleanup);
            }

            return MobaSkillCastResult.From(success, failReason, in ctx.RuntimeHandle, in failure);
        }

        private static MobaSkillCastFailure BuildCastFailure(SkillPipelineRunner runner, string failReason)
        {
            if (runner != null)
            {
                var startReject = runner.LastStartReject;
                if (startReject.HasValue)
                {
                    return new MobaSkillCastFailure("StartReject", null, startReject.Code, startReject.Message ?? failReason);
                }

                var pipelineFailure = runner.LastPipelineFailure;
                if (pipelineFailure.HasValue)
                {
                    return new MobaSkillCastFailure("Pipeline", pipelineFailure.Stage, pipelineFailure.Code, pipelineFailure.Message ?? failReason);
                }
            }

            return string.IsNullOrEmpty(failReason)
                ? MobaSkillCastFailure.None
                : new MobaSkillCastFailure("Unknown", null, "skill.cast.failed", failReason);
        }

        public bool TryGetRunningBySlot(int actorId, int slot, out SkillPipelineRunner.RunningSnapshot snapshot)
        {
            snapshot = default;
            if (actorId <= 0) return false;
            if (slot <= 0) return false;
            return _runners.TryGetValue(actorId, out var r) && r != null && r.TryGetLatestRunningBySlot(slot, out snapshot);
        }

        public bool TryGetRunningByInstanceId(int actorId, long instanceId, out SkillPipelineRunner.RunningSnapshot snapshot)
        {
            snapshot = default;
            if (actorId <= 0) return false;
            if (instanceId == 0L) return false;
            return _runners.TryGetValue(actorId, out var r) && r != null && r.TryGetRunningByInstanceId(instanceId, out snapshot);
        }

        private bool TryUpdateRunningInput(int actorId, int slot, in Vec3 aimPos, in Vec3 aimDir, int targetActorId)
        {
            if (actorId <= 0) return false;
            if (slot <= 0) return false;
            return _runners.TryGetValue(actorId, out var r) && r != null && r.UpdateInputBySlot(slot, in aimPos, in aimDir, targetActorId);
        }

        private bool TryReleaseRunningInput(int actorId, int slot, in Vec3 aimPos, in Vec3 aimDir, int targetActorId)
        {
            if (!TryUpdateRunningInput(actorId, slot, in aimPos, in aimDir, targetActorId)) return false;
            return _runners.TryGetValue(actorId, out var r) && r != null && r.MarkReleaseBySlot(slot);
        }

        public void CancelAll(int actorId)
        {
            if (actorId <= 0) return;
            if (_runners.TryGetValue(actorId, out var r) && r != null)
            {
                r.CancelAll();
            }
        }

        public bool CancelBySlot(int actorId, int slot)
        {
            if (actorId <= 0) return false;
            if (slot <= 0) return false;
            return _runners.TryGetValue(actorId, out var r) && r != null && r.CancelBySlot(slot);
        }

        public void CancelBySkillId(int actorId, int skillId)
        {
            if (actorId <= 0) return;
            if (skillId <= 0) return;
            if (_runners.TryGetValue(actorId, out var r) && r != null)
            {
                r.CancelBySkillId(skillId);
            }
        }

        public void Step(int actorId)
        {
            if (actorId <= 0) return;
            if (!_runners.TryGetValue(actorId, out var r) || r == null) return;

            var dt = _clock.DeltaTime;
            if (dt <= 0f)
            {
                var message = $"[SkillExecutor] Step skipped: deltaTime={dt:0.####}, actor={actorId}, hasRunning={r.HasRunning}";
                if (_diagnostics != null) _diagnostics.Warning("skill.executor.invalidDeltaTime", message);
                else Log.Warning(message);
                return;
            }

            r.Step(dt);
        }

        public void FillRunningSnapshots(int actorId, List<SkillPipelineRunner.RunningSnapshot> buffer)
        {
            if (actorId <= 0) throw new ArgumentOutOfRangeException(nameof(actorId));
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));

            if (!_runners.TryGetValue(actorId, out var r) || r == null)
            {
                buffer.Clear();
                return;
            }

            r.FillRunningSnapshots(buffer);
        }

        public void FillEndedSnapshots(int actorId, List<SkillPipelineRunner.RunningSnapshot> buffer)
        {
            if (actorId <= 0) throw new ArgumentOutOfRangeException(nameof(actorId));
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));

            if (!_runners.TryGetValue(actorId, out var r) || r == null)
            {
                buffer.Clear();
                return;
            }

            r.FillEndedSnapshots(buffer);
        }

        public void Dispose()
        {
            foreach (var kv in _runners)
            {
                kv.Value?.CancelAll();
            }
            _runners.Clear();
        }
    }

}

