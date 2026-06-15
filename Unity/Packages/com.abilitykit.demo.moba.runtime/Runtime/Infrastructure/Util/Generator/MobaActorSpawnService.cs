using System;
using AbilityKit.Ability.World.Services;
using AbilityKit.Ability.World.Services.Attributes;
using AbilityKit.Core.Logging;
using AbilityKit.Demo.Moba.Services;
using AbilityKit.Demo.Moba.Services.EntityManager;

namespace AbilityKit.Demo.Moba.Util.Generator
{
    public interface IMobaActorSpawnService : IService
    {
        bool TrySpawn(in MobaActorSpawnRequest request, out MobaActorSpawnResult result);
    }

    public sealed class MobaActorSpawnRequest
    {
        public MobaActorBuildSpec Spec;
        public bool AllocateActorIdIfMissing;
        public bool RegisterActor = true;
        public bool RegisterEntityManager = true;
        public bool RegisterEntityManagerFromEntity = true;
        public MobaActorSpawnPostSetup PostSetup;
        public Action<global::ActorEntity, MobaActorBuildSpec> Initializer;
        public Action<global::ActorEntity, MobaActorBuildSpec> OnActorBuilt;

        public static MobaActorSpawnRequest FromSpec(in MobaActorBuildSpec spec)
        {
            return new MobaActorSpawnRequest { Spec = spec };
        }
    }

    public readonly struct MobaActorSpawnResult
    {
        public readonly bool Success;
        public readonly int ActorId;
        public readonly global::ActorEntity Entity;
        public readonly MobaActorBuildSpec Spec;
        public readonly string Error;

        public MobaActorSpawnResult(bool success, int actorId, global::ActorEntity entity, in MobaActorBuildSpec spec, string error)
        {
            Success = success;
            ActorId = actorId;
            Entity = entity;
            Spec = spec;
            Error = error;
        }

        public static MobaActorSpawnResult Failed(string error)
        {
            return new MobaActorSpawnResult(false, 0, null, default, error);
        }
    }

    public struct MobaActorSpawnPostSetup
    {
        public bool SetOwnerLink;
        public int OwnerActorId;
        public int RootOwnerActorId;

        public bool SetLifetime;
        public long LifetimeEndTimeMs;

        public bool SetSummonMeta;
        public int SummonId;
        public bool DespawnOnOwnerDie;

        public bool SetModelId;
        public int ModelId;

        public bool SetFlyingProjectileTag;

        public bool SetProjectileLauncher;
        public int LauncherId;
        public int ProjectileId;
        public int ProjectileRootActorId;
        public long ProjectileLauncherEndTimeMs;
        public int ProjectileLauncherActiveBullets;
        public int ProjectileLauncherScheduleId;
        public int ProjectileLauncherIntervalFrames;
        public int ProjectileLauncherTotalCount;
    }

    [WorldService(typeof(MobaActorSpawnService))]
    [WorldService(typeof(IMobaActorSpawnService))]
    public sealed class MobaActorSpawnService : IMobaActorSpawnService
    {
        [WorldInject(required: false)] private global::Entitas.IContexts _contexts;
        [WorldInject(required: false)] private ActorIdAllocator _actorIds;
        [WorldInject(required: false)] private MobaActorRegistry _registry;
        [WorldInject(required: false)] private MobaEntityManager _entities;

        public bool TrySpawn(in MobaActorSpawnRequest request, out MobaActorSpawnResult result)
        {
            if (request == null)
            {
                result = MobaActorSpawnResult.Failed("request is required");
                return false;
            }

            var actorContext = ResolveActorContext();
            if (actorContext == null)
            {
                result = MobaActorSpawnResult.Failed("ActorContext is required");
                return false;
            }

            var spec = request.Spec;
            if (spec.Info.ActorId <= 0)
            {
                if (!request.AllocateActorIdIfMissing || _actorIds == null)
                {
                    result = MobaActorSpawnResult.Failed("actorId is required");
                    return false;
                }

                spec = WithActorId(in spec, _actorIds.Next());
            }

            try
            {
                var built = ActorSpawnPipeline.BuildActor(
                    actorContext,
                    in spec,
                    request.Initializer,
                    request.OnActorBuilt);

                var entity = built.Entity;
                if (entity == null)
                {
                    result = MobaActorSpawnResult.Failed("spawn returned null entity");
                    return false;
                }

                ApplyPostSetup(entity, in request.PostSetup);
                Register(entity, in spec, request.RegisterActor, request.RegisterEntityManager, request.RegisterEntityManagerFromEntity);

                result = new MobaActorSpawnResult(true, spec.Info.ActorId, entity, in spec, null);
                return true;
            }
            catch (Exception ex)
            {
                Log.Exception(ex, $"[MobaActorSpawnService] spawn failed. kind={spec.Info.Kind} actorId={spec.Info.ActorId} sourceKind={spec.SourceKind} sourceId={spec.SourceId}");
                result = MobaActorSpawnResult.Failed(ex.Message);
                return false;
            }
        }

        private global::ActorContext ResolveActorContext()
        {
            return (_contexts as global::Contexts)?.actor;
        }

        private void Register(global::ActorEntity entity, in MobaActorBuildSpec spec, bool registerActor, bool registerEntityManager, bool registerEntityManagerFromEntity)
        {
            if (registerActor)
            {
                _registry?.Register(spec.Info.ActorId, entity);
            }

            if (!registerEntityManager || _entities == null) return;

            if (registerEntityManagerFromEntity)
            {
                try
                {
                    if (_entities.TryRegisterFromEntity(entity)) return;
                }
                catch (Exception ex)
                {
                    Log.Exception(ex, $"[MobaActorSpawnService] TryRegisterFromEntity failed. actorId={spec.Info.ActorId} kind={spec.Info.Kind}");
                }
            }

            _entities.Register(
                actorId: spec.Info.ActorId,
                entity: entity,
                team: spec.Info.Team,
                mainType: spec.Info.MainType,
                unitSubType: spec.Info.UnitSubType,
                ownerPlayer: spec.Info.OwnerPlayer);
        }

        private static void ApplyPostSetup(global::ActorEntity entity, in MobaActorSpawnPostSetup setup)
        {
            if (entity == null) return;

            if (setup.SetOwnerLink)
            {
                if (entity.hasOwnerLink) entity.ReplaceOwnerLink(setup.OwnerActorId, setup.RootOwnerActorId);
                else entity.AddOwnerLink(setup.OwnerActorId, setup.RootOwnerActorId);
            }

            if (setup.SetLifetime)
            {
                if (entity.hasLifetime) entity.ReplaceLifetime(setup.LifetimeEndTimeMs);
                else entity.AddLifetime(setup.LifetimeEndTimeMs);
            }

            if (setup.SetSummonMeta)
            {
                if (entity.hasSummonMeta) entity.ReplaceSummonMeta(setup.SummonId, setup.DespawnOnOwnerDie);
                else entity.AddSummonMeta(setup.SummonId, setup.DespawnOnOwnerDie);
            }

            if (setup.SetModelId)
            {
                if (entity.hasModelId) entity.ReplaceModelId(setup.ModelId);
                else entity.AddModelId(setup.ModelId);
            }

            if (setup.SetFlyingProjectileTag)
            {
                entity.isFlyingProjectileTag = true;
            }

            if (setup.SetProjectileLauncher)
            {
                if (entity.hasProjectileLauncher)
                {
                    entity.ReplaceProjectileLauncher(
                        setup.LauncherId,
                        setup.ProjectileId,
                        setup.ProjectileRootActorId,
                        setup.ProjectileLauncherEndTimeMs,
                        setup.ProjectileLauncherActiveBullets,
                        setup.ProjectileLauncherScheduleId,
                        setup.ProjectileLauncherIntervalFrames,
                        setup.ProjectileLauncherTotalCount);
                }
                else
                {
                    entity.AddProjectileLauncher(
                        setup.LauncherId,
                        setup.ProjectileId,
                        setup.ProjectileRootActorId,
                        setup.ProjectileLauncherEndTimeMs,
                        setup.ProjectileLauncherActiveBullets,
                        setup.ProjectileLauncherScheduleId,
                        setup.ProjectileLauncherIntervalFrames,
                        setup.ProjectileLauncherTotalCount);
                }
            }
        }

        private static MobaActorBuildSpec WithActorId(in MobaActorBuildSpec spec, int actorId)
        {
            var info = spec.Info;
            var nextInfo = new MobaEntityInfo(
                actorId: actorId,
                kind: info.Kind,
                transform: info.Transform,
                team: info.Team,
                mainType: info.MainType,
                unitSubType: info.UnitSubType,
                ownerPlayer: info.OwnerPlayer,
                templateId: info.TemplateId);

            return new MobaActorBuildSpec(in nextInfo, spec.SourceKind, spec.SourceId, spec.OwnerActorId);
        }

        public void Dispose()
        {
        }
    }
}
