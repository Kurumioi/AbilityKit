using System.Collections.Generic;
using Entitas;
using Entitas.CodeGeneration.Attributes;

namespace AbilityKit.Demo.Moba.Components
{
    [Actor]
    public sealed class ShieldContainerComponent : IComponent
    {
        public ShieldContainer Value;
        public bool Initialized;
    }

    [Actor]
    public sealed class SharedShieldPoolsComponent : IComponent
    {
        public SharedShieldPoolContainer Value;
        public bool Initialized;
    }

    public sealed class ShieldContainer
    {
        public List<ShieldLayer> Layers;
        public int NextInstanceId;
        public float TotalRemaining;
        public bool Dirty;
    }

    public sealed class ShieldLayer
    {
        public int InstanceId;
        public int ShieldId;
        public int SourceActorId;
        public int OwnerActorId;
        public int TargetActorId;
        public long SourceContextId;
        public long RootContextId;
        public long OwnerContextId;

        public int SharedPoolId;
        public int SharedPoolMemberId;
        public bool UsesSharedPoolValue;

        public int TransferredFromActorId;
        public int TransferredToActorId;
        public int TransferredAtFrame;
        public float TransferRatio;

        public float CurrentValue;
        public float MaxValue;
        public float InitialValue;
        public float AbsorbRatio;

        public int Priority;
        public int DamageTypeMask;
        public int StartFrame;
        public int ExpireFrame;
        public bool RemoveWhenDepleted;

        public ShieldStackingPolicy StackingPolicy;
        public ShieldConsumePolicy ConsumePolicy;
        public ShieldSharePolicy SharePolicy;
        public ShieldTransferPolicy TransferPolicy;
    }

    public sealed class SharedShieldPoolContainer
    {
        public Dictionary<int, SharedShieldPool> Pools;
        public int NextPoolId;
        public bool Dirty;
    }

    public sealed class SharedShieldPool
    {
        public int PoolId;
        public int ShieldId;
        public int SourceActorId;
        public int OwnerActorId;
        public long SourceContextId;
        public long RootContextId;
        public long OwnerContextId;

        public List<SharedShieldPoolMember> Members;
        public float CurrentValue;
        public float MaxValue;
        public float InitialValue;
        public float AbsorbRatio;

        public int Priority;
        public int DamageTypeMask;
        public int StartFrame;
        public int ExpireFrame;
        public bool RemoveWhenDepleted;

        public ShieldStackingPolicy StackingPolicy;
        public ShieldConsumePolicy ConsumePolicy;
        public ShieldSharePolicy SharePolicy;
        public ShieldTransferPolicy TransferPolicy;
    }

    public sealed class SharedShieldPoolMember
    {
        public int MemberId;
        public int ActorId;
        public float Weight;
        public float MaxConsumeValue;
        public int JoinedFrame;
        public int LeftFrame;
        public bool Active;
    }

    public enum ShieldStackingPolicy
    {
        Independent = 0,
        MergeSameShieldAndSource = 1,
        RefreshSameShieldAndSource = 2,
        ReplaceLowerPriority = 3,
    }

    public enum ShieldConsumePolicy
    {
        PriorityThenOldest = 0,
        PriorityThenNewest = 1,
        OldestFirst = 2,
        NewestFirst = 3,
    }

    public enum ShieldSharePolicy
    {
        None = 0,
        SharedTotalPool = 1,
        WeightedMemberShare = 2,
        TargetPersonalThenShared = 3,
        LowestHpMemberFirst = 4,
    }

    public enum ShieldTransferPolicy
    {
        None = 0,
        MoveRemaining = 1,
        SplitRemaining = 2,
        CopySnapshot = 3,
        ReturnToOwnerOnExpire = 4,
    }
}
