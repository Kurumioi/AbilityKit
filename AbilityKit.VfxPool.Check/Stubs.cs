// All stubs in one file. Names match production types' signatures.

using System.Collections.Generic;
using UnityEngine;

namespace AbilityKit.World.ECS
{
    public interface IECWorld
    {
        bool IsAlive(IEntityId id);
        IEntity Wrap(IEntityId id);
        IEntity CreateChild(IEntity parent);
        bool TryGetComponentRef<T>(IEntityId id, out T component) where T : class;
    }
    public interface IEntity
    {
        IEntityId Id { get; }
        IECWorld World { get; }
        bool IsValid { get; }
        bool TryGetRef<T>(out T component) where T : class;
        IEntity WithRef<T>(T component) where T : class;
        void Destroy();
        void SetName(string name);
    }
    public readonly struct IEntityId
    {
        public readonly int ActorId;
        public IEntityId(int actorId) { ActorId = actorId; }
    }
}

namespace AbilityKit.Game.Battle.Vfx
{
    public sealed class VfxDatabase
    {
        public bool TryGet(int id, out AbilityKit.Demo.Moba.Share.Config.VfxDTO dto) { dto = null; return false; }
    }
}

namespace AbilityKit.Game.Battle.Shared.Assets
{
    internal sealed class ResourcesAssetProvider
    {
        public static ResourcesAssetProvider Shared => null;
        public T Load<T>(string resource) where T : UnityEngine.Object => null;
    }
}

namespace AbilityKit.Game.Battle.Component
{
    public sealed class BattleVfxComponent { public int VfxId; }
    public sealed class BattleVfxLifetimeComponent { public float ExpireAtTime; }
    public sealed class BattleViewGameObjectComponent { public GameObject GameObject; }
    public sealed class BattleViewFollowComponent
    {
        public AbilityKit.World.ECS.IEntityId Target;
        public int TargetActorId;
        public Vector3 Offset;
    }
}

namespace AbilityKit.Game.Battle.Shared.Time
{
    public interface IBattleViewTimeSource { float TimeSeconds { get; } }
    internal sealed class UnityBattleViewTimeSource : IBattleViewTimeSource
    {
        public static UnityBattleViewTimeSource Shared => null;
        public float TimeSeconds => 0f;
    }
}

namespace AbilityKit.Game.Flow
{
    internal sealed class BattleViewPrimitiveFactory
    {
        public GameObject CreateVfxFallback(int vfxId) => null;
        public GameObject CreateProjectileFallback(int vfxId) => null;
    }

    internal static class BattleViewPlaceholderIds
    {
        public const int ProjectileVfx = 90000001;
        public const int ProjectileExpireVfx = 90000004;
    }
}

namespace AbilityKit.Demo.Moba.Share.Config
{
    public sealed class VfxDTO
    {
        public int Id;
        public string Resource;
        public int DurationMs;
    }
}
