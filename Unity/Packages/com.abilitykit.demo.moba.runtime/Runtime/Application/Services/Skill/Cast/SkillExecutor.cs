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

            switch (evt.Phase)
            {
                case SkillInputPhase.Press:
                    if (TryUpdateRunningInput(actorId, evt.Slot, in evt.AimPos, in evt.AimDir, evt.TargetActorId))
                    {
                        failReason = $"UpdatedRunningInput(Slot={evt.Slot},Target={evt.TargetActorId})";
                        return true;
                    }
                    {
                        var cast = CastBySlot(actorId, evt.Slot, in evt.AimPos, in evt.AimDir, evt.TargetActorId, out failReason);
                        if (cast && string.IsNullOrEmpty(failReason))
                        {
                            failReason = $"CastBySlotStarted(Slot={evt.Slot},Target={evt.TargetActorId})";
                        }
                        return cast;
                    }
                case SkillInputPhase.Hold:
                    if (TryUpdateRunningInput(actorId, evt.Slot, in evt.AimPos, in evt.AimDir, evt.TargetActorId))
                    {
                        failReason = $"UpdatedRunningInput(Slot={evt.Slot},Target={evt.TargetActorId})";
                        return true;
                    }
                    failReason = "No running skill for hold input.";
                    return false;
                case SkillInputPhase.Release:
                    if (TryReleaseRunningInput(actorId, evt.Slot, in evt.AimPos, in evt.AimDir, evt.TargetActorId))
                    {
                        failReason = $"ReleasedRunningInput(Slot={evt.Slot},Target={evt.TargetActorId})";
                        return true;
                    }
                    {
                        var cast = CastBySlot(actorId, evt.Slot, in evt.AimPos, in evt.AimDir, evt.TargetActorId, out failReason);
                        if (cast && string.IsNullOrEmpty(failReason))
                        {
                            failReason = $"CastBySlotStarted(Slot={evt.Slot},Target={evt.TargetActorId})";
                        }
                        return cast;
                    }
                case SkillInputPhase.Cancel:
                    if (CancelBySlot(actorId, evt.Slot))
                    {
                        failReason = $"CancelledRunningInput(Slot={evt.Slot})";
                        return true;
                    }
                    failReason = "No running skill for cancel input.";
                    return false;
                default:
                    failReason = $"Unsupported skill input phase: {evt.Phase}.";
                    return false;
            }
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

    internal readonly struct SkillCastPreparationInput
    {
        public SkillCastPreparationInput(int actorId, int skillId, int slot, in Vec3 aimPos, in Vec3 aimDir, bool hasAim, int targetActorId)
        {
            ActorId = actorId;
            SkillId = skillId;
            Slot = slot;
            AimPos = aimPos;
            AimDir = aimDir;
            HasAim = hasAim;
            TargetActorId = targetActorId;
        }

        public int ActorId { get; }
        public int SkillId { get; }
        public int Slot { get; }
        public Vec3 AimPos { get; }
        public Vec3 AimDir { get; }
        public bool HasAim { get; }
        public int TargetActorId { get; }
    }

    internal readonly struct SkillCastPreparationResult
    {
        private SkillCastPreparationResult(
            bool success,
            string failReason,
            in SkillCastRequest request,
            SkillCastContext context,
            MobaSkillCastRuntimeService runtimes,
            IAbilityPipelineConfig preCastConfig,
            IReadOnlyList<IAbilityPipelinePhase<SkillPipelineContext>> preCastPhases,
            IAbilityPipelineConfig castConfig,
            IReadOnlyList<IAbilityPipelinePhase<SkillPipelineContext>> castPhases)
        {
            Success = success;
            FailReason = failReason;
            Request = request;
            Context = context;
            Runtimes = runtimes;
            PreCastConfig = preCastConfig;
            PreCastPhases = preCastPhases;
            CastConfig = castConfig;
            CastPhases = castPhases;
        }

        public bool Success { get; }
        public string FailReason { get; }
        public SkillCastRequest Request { get; }
        public SkillCastContext Context { get; }
        public MobaSkillCastRuntimeService Runtimes { get; }
        public IAbilityPipelineConfig PreCastConfig { get; }
        public IReadOnlyList<IAbilityPipelinePhase<SkillPipelineContext>> PreCastPhases { get; }
        public IAbilityPipelineConfig CastConfig { get; }
        public IReadOnlyList<IAbilityPipelinePhase<SkillPipelineContext>> CastPhases { get; }

        public static SkillCastPreparationResult Failed(string failReason)
        {
            return new SkillCastPreparationResult(false, failReason, default, null, null, null, null, null, null);
        }

        public static SkillCastPreparationResult Ready(
            in SkillCastRequest request,
            SkillCastContext context,
            MobaSkillCastRuntimeService runtimes,
            IAbilityPipelineConfig preCastConfig,
            IReadOnlyList<IAbilityPipelinePhase<SkillPipelineContext>> preCastPhases,
            IAbilityPipelineConfig castConfig,
            IReadOnlyList<IAbilityPipelinePhase<SkillPipelineContext>> castPhases)
        {
            return new SkillCastPreparationResult(true, null, in request, context, runtimes, preCastConfig, preCastPhases, castConfig, castPhases);
        }
    }

    internal sealed class SkillCastPolicyResolver
    {
        private readonly IWorldResolver _services;
        private MobaConfigDatabase _configs;
        private bool _configResolved;

        public SkillCastPolicyResolver(IWorldResolver services)
        {
            _services = services;
        }

        public SkillCastPolicy Resolve(int skillId, in SkillCastPolicy fallback)
        {
            if (skillId <= 0) return fallback;

            var configs = ResolveConfigs();
            if (configs == null) return fallback;
            if (!configs.TryGetSkill(skillId, out var skill) || skill == null) return fallback;

            return ResolveFromSkill(skill, in fallback);
        }

        private SkillCastPolicy ResolveFromSkill(AbilityKit.Demo.Moba.Config.BattleDemo.MO.SkillMO skill, in SkillCastPolicy fallback)
        {
            return fallback;
        }

        private MobaConfigDatabase ResolveConfigs()
        {
            if (_configResolved) return _configs;
            _configResolved = true;

            if (_services != null && _services.TryResolve<MobaConfigDatabase>(out var configs))
            {
                _configs = configs;
            }

            return _configs;
        }
    }

    internal sealed class SkillCastPreparationService
    {
        private readonly IWorldResolver _services;
        private readonly AbilityKit.Triggering.Eventing.IEventBus _eventBus;
        private readonly IUnitResolver _units;
        private readonly MobaActorLookupService _actors;
        private readonly IMobaSkillPipelineLibrary _library;
        private readonly Dictionary<int, int> _castSequenceByActor = new Dictionary<int, int>();

        public SkillCastPreparationService(
            IWorldResolver services,
            AbilityKit.Triggering.Eventing.IEventBus eventBus,
            IUnitResolver units,
            MobaActorLookupService actors,
            IMobaSkillPipelineLibrary library)
        {
            _services = services ?? throw new ArgumentNullException(nameof(services));
            _eventBus = eventBus;
            _units = units ?? throw new ArgumentNullException(nameof(units));
            _actors = actors ?? throw new ArgumentNullException(nameof(actors));
            _library = library ?? throw new ArgumentNullException(nameof(library));
        }

        public SkillCastPreparationResult Prepare(in SkillCastPreparationInput input)
        {
            var actorId = input.ActorId;
            var skillId = input.SkillId;
            var slot = input.Slot;
            if (actorId <= 0) return SkillCastPreparationResult.Failed($"Invalid caster actor id: {actorId}.");
            if (skillId <= 0) return SkillCastPreparationResult.Failed($"Invalid skill id: {skillId}.");

            if (!_units.TryResolve(new EcsEntityId(actorId), out var caster) || caster == null)
            {
                return SkillCastPreparationResult.Failed("Caster not found.");
            }

            ResolveCasterTransform(actorId, out var casterPos, out var casterForward);
            var finalAimPos = input.HasAim ? input.AimPos : casterPos;
            var finalAimDir = input.HasAim ? input.AimDir : casterForward;
            if (finalAimDir.Equals(Vec3.Zero)) finalAimDir = casterForward;
            if (finalAimPos.Equals(Vec3.Zero)) finalAimPos = casterPos;

            var finalTargetActorId = input.TargetActorId > 0 ? input.TargetActorId : 0;
            IUnitFacade targetUnit = null;
            if (finalTargetActorId > 0)
            {
                if (!_units.TryResolve(new EcsEntityId(finalTargetActorId), out targetUnit) || targetUnit == null)
                {
                    return SkillCastPreparationResult.Failed($"Target not found. targetActorId={finalTargetActorId}.");
                }
            }

            if (!_library.TryGet(skillId, out var preConfig, out var prePhases, out var castConfig, out var castPhases))
            {
                Log.Warning($"[SkillExecutor] Cast failed: pipeline missing. actor={actorId}, skillId={skillId}, slot={slot}, target={finalTargetActorId}");
                return SkillCastPreparationResult.Failed("Skill pipeline not found.");
            }

            var request = new SkillCastRequest(
                skillId: skillId,
                skillSlot: slot,
                casterActorId: actorId,
                targetActorId: finalTargetActorId,
                aimPos: in finalAimPos,
                aimDir: in finalAimDir,
                worldServices: _services,
                eventBus: _eventBus,
                casterUnit: caster,
                targetUnit: targetUnit);

            var skillLevel = ResolveSkillLevel(actorId, skillId, slot);
            var sequence = NextCastSequence(actorId);
            var context = SkillCastContextBuilder.Create()
                .FromRequest(in request)
                .WithSkillLevel(skillLevel)
                .WithSequence(sequence)
                .Build();

            var trace = _services.Resolve<MobaTraceRegistry>();
            if (trace == null)
            {
                return SkillCastPreparationResult.Failed("MobaTraceRegistry is required for formal skill cast tracing.");
            }

            context.SourceContextId = trace.CreateRootContext(
                MobaTraceKind.SkillCast,
                skillId,
                actorId,
                finalTargetActorId,
                TraceEndpoint.Actor(actorId),
                finalTargetActorId > 0 ? TraceEndpoint.Actor(finalTargetActorId) : default);
            if (context.SourceContextId == 0)
            {
                return SkillCastPreparationResult.Failed("Skill cast trace root creation failed.");
            }

            var runtimes = _services.Resolve<MobaSkillCastRuntimeService>();
            if (runtimes == null)
            {
                return SkillCastPreparationResult.Failed("MobaSkillCastRuntimeService is required for formal skill cast runtime tracking.");
            }

            var createRequest = MobaSkillCastRuntimeCreateRequestBuilder.Create()
                .FromCastContext(context)
                .Build();
            var runtime = runtimes.Create(in createRequest);
            context.RuntimeHandle = runtime.Handle;
            context.RuntimeId = runtime.RuntimeId;
            if (!context.RuntimeHandle.IsValid)
            {
                return SkillCastPreparationResult.Failed("Skill cast runtime creation returned an invalid handle.");
            }

            return SkillCastPreparationResult.Ready(in request, context, runtimes, preConfig, prePhases, castConfig, castPhases);
        }

        private void ResolveCasterTransform(int actorId, out Vec3 position, out Vec3 forward)
        {
            position = Vec3.Zero;
            forward = Vec3.Forward;
            if (_actors.TryGetActorEntity(actorId, out var actorEntity) && actorEntity != null && actorEntity.hasTransform)
            {
                var transform = actorEntity.transform.Value;
                position = transform.Position;
                forward = transform.Rotation.Rotate(Vec3.Forward).Normalized;
            }
        }

        private int ResolveSkillLevel(int actorId, int skillId, int slot)
        {
            if (!_actors.TryGetActorEntity(actorId, out var actor) || actor == null || !actor.hasSkillLoadout) return 0;

            var skills = actor.skillLoadout.ActiveSkills;
            var index = slot - 1;
            if (skills == null || index < 0 || index >= skills.Length) return 0;

            var runtime = skills[index];
            return runtime != null && runtime.SkillId == skillId ? runtime.Level : 0;
        }

        private int NextCastSequence(int actorId)
        {
            if (_castSequenceByActor.TryGetValue(actorId, out var sequence))
            {
                sequence++;
            }
            else
            {
                sequence = 1;
            }

            _castSequenceByActor[actorId] = sequence;
            return sequence;
        }
    }
}

