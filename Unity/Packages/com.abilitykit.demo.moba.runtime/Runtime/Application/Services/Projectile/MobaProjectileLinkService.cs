using System;
using System.Collections.Generic;
using AbilityKit.Core.Common.Projectile;
using AbilityKit.Ability.World.Services;
using AbilityKit.Ability.World.Services.Attributes;
using AbilityKit.Demo.Moba.Services;

namespace AbilityKit.Demo.Moba.Services.Projectile
{
    [WorldService(typeof(MobaProjectileLinkService))]
    public sealed class MobaProjectileLinkService : IService
    {
        private readonly Dictionary<int, ProjectileId> _projectileByActorId = new Dictionary<int, ProjectileId>();
        private readonly Dictionary<ProjectileId, int> _actorIdByProjectile = new Dictionary<ProjectileId, int>();
        private readonly Dictionary<ProjectileId, ProjectileSourceContext> _sourceByProjectile = new Dictionary<ProjectileId, ProjectileSourceContext>();
        private readonly Dictionary<ProjectileId, MobaSkillRuntimeRetainHandle> _retainByProjectile = new Dictionary<ProjectileId, MobaSkillRuntimeRetainHandle>();
        private readonly Dictionary<int, ProjectileSourceContext> _sourceByLauncherActorId = new Dictionary<int, ProjectileSourceContext>();
        private readonly Dictionary<int, MobaSkillRuntimeRetainHandle> _retainByLauncherActorId = new Dictionary<int, MobaSkillRuntimeRetainHandle>();

        [WorldInject(required: false)] private IMobaTemporaryEntityLifecycleService _lifecycle;

        public int ActiveCount => _actorIdByProjectile.Count;

        public void Link(ProjectileId projectileId, int actorId)
        {
            if (actorId <= 0) throw new ArgumentOutOfRangeException(nameof(actorId));
            _projectileByActorId[actorId] = projectileId;
            _actorIdByProjectile[projectileId] = actorId;
            _lifecycle?.RecordSpawn(MobaTemporaryEntityKind.Projectile, ActiveCount);
        }

        public void BindSource(ProjectileId projectileId, in ProjectileSourceContext source)
        {
            if (projectileId.Value == 0) return;
            if (!source.IsValid) return;
            _sourceByProjectile[projectileId] = source;
        }

        public void BindRetain(ProjectileId projectileId, in MobaSkillRuntimeRetainHandle retainHandle)
        {
            if (projectileId.Value == 0) return;
            if (!retainHandle.IsValid) return;
            _retainByProjectile[projectileId] = retainHandle;
        }

        public void BindLauncherSource(int launcherActorId, in ProjectileSourceContext source)
        {
            if (launcherActorId <= 0) return;
            if (!source.IsValid) return;
            _sourceByLauncherActorId[launcherActorId] = source;
        }

        public void BindLauncherRetain(int launcherActorId, in MobaSkillRuntimeRetainHandle retainHandle)
        {
            if (launcherActorId <= 0) return;
            if (!retainHandle.IsValid) return;
            _retainByLauncherActorId[launcherActorId] = retainHandle;
        }

        public bool TryGetActorId(ProjectileId projectileId, out int actorId)
        {
            return _actorIdByProjectile.TryGetValue(projectileId, out actorId);
        }

        public bool TryGetSource(ProjectileId projectileId, out ProjectileSourceContext source)
        {
            return _sourceByProjectile.TryGetValue(projectileId, out source) && source.IsValid;
        }

        public bool TryGetRetain(ProjectileId projectileId, out MobaSkillRuntimeRetainHandle retainHandle)
        {
            return _retainByProjectile.TryGetValue(projectileId, out retainHandle) && retainHandle.IsValid;
        }

        public bool TryGetLauncherSource(int launcherActorId, out ProjectileSourceContext source)
        {
            return _sourceByLauncherActorId.TryGetValue(launcherActorId, out source) && source.IsValid;
        }

        public bool TryGetLauncherRetain(int launcherActorId, out MobaSkillRuntimeRetainHandle retainHandle)
        {
            return _retainByLauncherActorId.TryGetValue(launcherActorId, out retainHandle) && retainHandle.IsValid;
        }

        public bool TryGetProjectileId(int actorId, out ProjectileId projectileId)
        {
            return _projectileByActorId.TryGetValue(actorId, out projectileId);
        }

        public void UnlinkByActorId(int actorId)
        {
            if (actorId <= 0) return;
            if (_projectileByActorId.TryGetValue(actorId, out var pid))
            {
                _projectileByActorId.Remove(actorId);
                _actorIdByProjectile.Remove(pid);
                _sourceByProjectile.Remove(pid);
                _retainByProjectile.Remove(pid);
                _lifecycle?.RecordDespawn(MobaTemporaryEntityKind.Projectile, ActiveCount);
            }
        }

        public void UnlinkByProjectileId(ProjectileId projectileId)
        {
            var removed = false;
            if (_actorIdByProjectile.TryGetValue(projectileId, out var actorId))
            {
                _actorIdByProjectile.Remove(projectileId);
                _projectileByActorId.Remove(actorId);
                removed = true;
            }

            _sourceByProjectile.Remove(projectileId);
            _retainByProjectile.Remove(projectileId);
            if (removed) _lifecycle?.RecordDespawn(MobaTemporaryEntityKind.Projectile, ActiveCount);
        }

        public void UnlinkLauncher(int launcherActorId)
        {
            if (launcherActorId <= 0) return;
            _sourceByLauncherActorId.Remove(launcherActorId);
            _retainByLauncherActorId.Remove(launcherActorId);
        }

        public void Clear()
        {
            _projectileByActorId.Clear();
            _actorIdByProjectile.Clear();
            _sourceByProjectile.Clear();
            _retainByProjectile.Clear();
            _sourceByLauncherActorId.Clear();
            _retainByLauncherActorId.Clear();
            _lifecycle?.SetActive(MobaTemporaryEntityKind.Projectile, 0);
        }

        public void Dispose()
        {
            Clear();
        }
    }
}
