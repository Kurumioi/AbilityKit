using Entitas;
using Entitas.CodeGeneration.Attributes;

namespace AbilityKit.Demo.Moba.Components
{
    public enum ActorDespawnReason
    {
        Unknown = 0,
        ProjectileExpired = 1,
        ProjectileHitOrExit = 2,
        ProjectileLauncherCompleted = 3,
        SummonTimeout = 10,
        SummonOwnerDead = 11,
        SummonReplacedByLimit = 12,
        SummonManualRemove = 13,
        SummonKilled = 14,
        SceneCleanup = 50,
        RollbackCleanup = 51,
    }

    [Actor]
    public sealed class ActorDespawnRequestComponent : IComponent
    {
        public int RequestFrame;
        public int MinConfirmedFrame;
        public ActorDespawnReason Reason;
        public int SourceActorId;
        public long SourceContextId;
    }
}
