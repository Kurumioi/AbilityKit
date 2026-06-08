using System;
using System.Collections.Generic;
using AbilityKit.Ability.FrameSync;
using AbilityKit.Ability.Share.ECS;
using AbilityKit.ECS;
using AbilityKit.Ability.Share.ECS.Entitas;
using AbilityKit.Core.Math;
using AbilityKit.Ability.Triggering;
using AbilityKit.Ability.World.DI;
using AbilityKit.Ability.World.Services;
using AbilityKit.Ability.World.Services.Attributes;
using AbilityKit.Demo.Moba;
using AbilityKit.Demo.Moba.Config.Core;
using AbilityKit.Protocol.Moba;
using AbilityKit.Triggering.Eventing;
using AbilityKit.Trace;
using AbilityKit.Core.Common.Log;

namespace AbilityKit.Demo.Moba.Services
{
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

        private readonly Dictionary<int, SkillPipelineRunner> _runners = new Dictionary<int, SkillPipelineRunner>();
        private readonly Dictionary<int, int> _castSequenceByActor = new Dictionary<int, int>();

        public bool AllowParallel { get; set; }
        public bool InterruptRunning { get; set; }

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
            IMobaBattleExceptionPolicy exceptions = null)
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
        }

        private SkillPipelineRunner GetOrCreateRunner(int actorId)
        {
            if (!_runners.TryGetValue(actorId, out var r) || r == null)
            {
                r = new SkillPipelineRunner(actorId, _diagnostics, _exceptions);
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
                return MobaSkillCastResult.Failed("Skill not found in slot.");
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
            string failReason = null;
            if (actorId <= 0) return MobaSkillCastResult.Failed($"Invalid caster actor id: {actorId}.");
            if (skillId <= 0) return MobaSkillCastResult.Failed($"Invalid skill id: {skillId}.");

            if (!_units.TryResolve(new EcsEntityId(actorId), out var caster) || caster == null)
            {
                failReason = "Caster not found.";
                return MobaSkillCastResult.Failed(failReason);
            }

            var casterPos = Vec3.Zero;
            var casterForward = Vec3.Forward;
            if (_actors.TryGetActorEntity(actorId, out var actorEntity) && actorEntity != null && actorEntity.hasTransform)
            {
                var t = actorEntity.transform.Value;
                casterPos = t.Position;
                casterForward = t.Rotation.Rotate(Vec3.Forward).Normalized;
            }

            var finalAimPos = hasAim ? aimPos : casterPos;
            var finalAimDir = hasAim ? aimDir : casterForward;
            if (finalAimDir.Equals(Vec3.Zero)) finalAimDir = casterForward;
            if (finalAimPos.Equals(Vec3.Zero)) finalAimPos = casterPos;

            var finalTargetActorId = targetActorId > 0 ? targetActorId : 0;
            IUnitFacade targetUnit = null;
            if (finalTargetActorId > 0)
            {
                if (!_units.TryResolve(new EcsEntityId(finalTargetActorId), out targetUnit) || targetUnit == null)
                {
                    failReason = $"Target not found. targetActorId={finalTargetActorId}.";
                    return MobaSkillCastResult.Failed(failReason);
                }
            }

            if (!_library.TryGet(skillId, out var preConfig, out var prePhases, out var castConfig, out var castPhases))
            {
                failReason = "Skill pipeline not found.";
                Log.Warning($"[SkillExecutor] Cast failed: pipeline missing. actor={actorId}, skillId={skillId}, slot={slot}, target={finalTargetActorId}");
                return MobaSkillCastResult.Failed(failReason);
            }

            var req = new SkillCastRequest(
                skillId: skillId,
                skillSlot: slot,
                casterActorId: actorId,
                targetActorId: finalTargetActorId,
                aimPos: in finalAimPos,
                aimDir: in finalAimDir,
                worldServices: _services,
                eventBus: _eventBus,
                casterUnit: caster,
                targetUnit: targetUnit
            );

            var skillLevel = 0;
            if (_actors != null && _actors.TryGetActorEntity(actorId, out var ae) && ae != null && ae.hasSkillLoadout)
            {
                var skills = ae.skillLoadout.ActiveSkills;
                var idx = slot - 1;
                if (skills != null && idx >= 0 && idx < skills.Length)
                {
                    var rt = skills[idx];
                    if (rt != null && rt.SkillId == skillId) skillLevel = rt.Level;
                }
            }
            var ctx = SkillCastContext.FromRequest(in req, skillLevel);

            if (_castSequenceByActor.TryGetValue(actorId, out var seq))
            {
                seq++;
            }
            else
            {
                seq = 1;
            }
            _castSequenceByActor[actorId] = seq;
            ctx.Sequence = seq;

            var trace = _services.Resolve<MobaTraceRegistry>();
            if (trace == null)
            {
                return MobaSkillCastResult.Failed("MobaTraceRegistry is required for formal skill cast tracing.");
            }

            ctx.SourceContextId = trace.CreateRootContext(
                MobaTraceKind.SkillCast,
                skillId,
                actorId,
                finalTargetActorId,
                TraceEndpoint.Actor(actorId),
                finalTargetActorId > 0 ? TraceEndpoint.Actor(finalTargetActorId) : default);
            if (ctx.SourceContextId == 0)
            {
                return MobaSkillCastResult.Failed("Skill cast trace root creation failed.");
            }

            var runtimes = _services.Resolve<MobaSkillCastRuntimeService>();
            if (runtimes == null)
            {
                return MobaSkillCastResult.Failed("MobaSkillCastRuntimeService is required for formal skill cast runtime tracking.");
            }

            var createRequest = MobaSkillCastRuntimeCreateRequestBuilder.Create()
                .FromCastContext(ctx)
                .Build();
            var runtime = runtimes.Create(in createRequest);
            ctx.RuntimeHandle = runtime.Handle;
            ctx.RuntimeId = runtime.RuntimeId;
            if (!ctx.RuntimeHandle.IsValid)
            {
                return MobaSkillCastResult.Failed("Skill cast runtime creation returned an invalid handle.");
            }

            var runner = GetOrCreateRunner(actorId);
            var success = runner.Start(preConfig, prePhases, castConfig, castPhases, abilityInstance: this, in req, ctx, out failReason, allowParallel: AllowParallel, interruptRunning: InterruptRunning);
            return MobaSkillCastResult.From(success, failReason, in ctx.RuntimeHandle);
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

            _castSequenceByActor.Clear();
        }
    }
}

