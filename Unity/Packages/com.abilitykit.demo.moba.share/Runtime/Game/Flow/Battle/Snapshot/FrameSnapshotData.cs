using System;
using System.Collections.Generic;

namespace AbilityKit.Demo.Moba.Share
{
    /// <summary>
    /// 快照帧数据
    /// 包含帧同步所需的所有数据
    /// </summary>
    public readonly struct FrameSnapshotData
    {
        /// <summary>
        /// 帧索引
        /// </summary>
        public int FrameIndex { get; }

        /// <summary>
        /// 快照时间戳
        /// </summary>
        public double Timestamp { get; }

        /// <summary>
        /// 快照类型
        /// </summary>
        public SnapshotType Type { get; }

        /// <summary>
        /// 进入游戏数据
        /// </summary>
        public EnterGameData EnterGame { get; }

        /// <summary>
        /// 角色变换数据列表
        /// </summary>
        public IReadOnlyList<ActorTransformData> ActorTransforms { get; }

        /// <summary>
        /// 弹道事件数据列表
        /// </summary>
        public IReadOnlyList<ProjectileEventData> ProjectileEvents { get; }

        /// <summary>
        /// 区域事件数据列表
        /// </summary>
        public IReadOnlyList<AreaEventData> AreaEvents { get; }

        /// <summary>
        /// 伤害事件数据列表
        /// </summary>
        public IReadOnlyList<DamageEventData> DamageEvents { get; }

        /// <summary>
        /// 状态哈希数据
        /// </summary>
        public StateHashData StateHash { get; }

        /// <summary>
        /// 角色生成数据列表
        /// </summary>
        public IReadOnlyList<ActorSpawnData> ActorSpawns { get; }

        public FrameSnapshotData(
            int frameIndex,
            double timestamp,
            SnapshotType type,
            EnterGameData enterGame = default,
            IReadOnlyList<ActorTransformData> actorTransforms = null,
            IReadOnlyList<ProjectileEventData> projectileEvents = null,
            IReadOnlyList<AreaEventData> areaEvents = null,
            IReadOnlyList<DamageEventData> damageEvents = null,
            StateHashData stateHash = default,
            IReadOnlyList<ActorSpawnData> actorSpawns = null)
        {
            FrameIndex = frameIndex;
            Timestamp = timestamp;
            Type = type;
            EnterGame = enterGame;
            ActorTransforms = actorTransforms ?? Array.Empty<ActorTransformData>();
            ProjectileEvents = projectileEvents ?? Array.Empty<ProjectileEventData>();
            AreaEvents = areaEvents ?? Array.Empty<AreaEventData>();
            DamageEvents = damageEvents ?? Array.Empty<DamageEventData>();
            StateHash = stateHash;
            ActorSpawns = actorSpawns ?? Array.Empty<ActorSpawnData>();
        }
    }

    /// <summary>
    /// 快照类型
    /// </summary>
    public enum SnapshotType
    {
        /// <summary>
        /// 全量快照
        /// </summary>
        Full = 0,

        /// <summary>
        /// 增量快照
        /// </summary>
        Delta = 1,

        /// <summary>
        /// 关键帧快照
        /// </summary>
        KeyFrame = 2,
    }

    /// <summary>
    /// 进入游戏数据
    /// </summary>
    public readonly struct EnterGameData
    {
        public bool HasValue { get; }
        public int MapId { get; }
        public int LocalPlayerId { get; }
        public IReadOnlyList<int> PlayerIds { get; }
        public IReadOnlyList<TeamData> Teams { get; }

        public EnterGameData(int mapId, int localPlayerId, IReadOnlyList<int> playerIds, IReadOnlyList<TeamData> teams)
        {
            MapId = mapId;
            LocalPlayerId = localPlayerId;
            PlayerIds = playerIds;
            Teams = teams;
            HasValue = true;
        }

        public static readonly EnterGameData Default = default;
    }

    /// <summary>
    /// 队伍数据
    /// </summary>
    public readonly struct TeamData
    {
        public int TeamId { get; }
        public IReadOnlyList<int> PlayerIds { get; }

        public TeamData(int teamId, IReadOnlyList<int> playerIds)
        {
            TeamId = teamId;
            PlayerIds = playerIds;
        }
    }

    /// <summary>
    /// 角色变换数据
    /// </summary>
    public readonly struct ActorTransformData
    {
        public int ActorId { get; }
        public float PositionX { get; }
        public float PositionY { get; }
        public float PositionZ { get; }
        public float RotationY { get; }
        public float Scale { get; }

        public ActorTransformData(int actorId, float x, float y, float z, float rotationY, float scale)
        {
            ActorId = actorId;
            PositionX = x;
            PositionY = y;
            PositionZ = z;
            RotationY = rotationY;
            Scale = scale;
        }
    }

    /// <summary>
    /// 弹道事件数据
    /// </summary>
    public readonly struct ProjectileEventData
    {
        public int ProjectileId { get; }
        public int OwnerId { get; }
        public ProjectileEventKind Kind { get; }
        public float PositionX { get; }
        public float PositionY { get; }
        public float PositionZ { get; }
        public int TargetId { get; }
        public float StartPosX { get; }
        public float StartPosY { get; }
        public float StartPosZ { get; }

        public ProjectileEventData(
            int projectileId, int ownerId, ProjectileEventKind kind,
            float x, float y, float z, int targetId,
            float startX, float startY, float startZ)
        {
            ProjectileId = projectileId;
            OwnerId = ownerId;
            Kind = kind;
            PositionX = x;
            PositionY = y;
            PositionZ = z;
            TargetId = targetId;
            StartPosX = startX;
            StartPosY = startY;
            StartPosZ = startZ;
        }
    }

    /// <summary>
    /// 弹道事件类型
    /// </summary>
    public enum ProjectileEventKind
    {
        Spawn = 0,
        Hit = 1,
        Destroy = 2,
    }

    /// <summary>
    /// 区域事件数据
    /// </summary>
    public readonly struct AreaEventData
    {
        public int AreaId { get; }
        public AreaEventKind Kind { get; }
        public float CenterX { get; }
        public float CenterY { get; }
        public float CenterZ { get; }
        public float Radius { get; }

        public AreaEventData(int areaId, AreaEventKind kind, float x, float y, float z, float radius)
        {
            AreaId = areaId;
            Kind = kind;
            CenterX = x;
            CenterY = y;
            CenterZ = z;
            Radius = radius;
        }
    }

    /// <summary>
    /// 区域事件类型
    /// </summary>
    public enum AreaEventKind
    {
        Appear = 0,
        Disappear = 1,
        Tick = 2,
    }

    /// <summary>
    /// 伤害事件数据
    /// </summary>
    public readonly struct DamageEventData
    {
        public int AttackerId { get; }
        public int TargetId { get; }
        public int SourceId { get; }
        public int DamageType { get; }
        public int DamageValue { get; }
        public int TargetHpAfter { get; }
        public bool IsKill { get; }

        public DamageEventData(
            int attackerId, int targetId, int sourceId,
            int damageType, int damageValue, int targetHpAfter, bool isKill)
        {
            AttackerId = attackerId;
            TargetId = targetId;
            SourceId = sourceId;
            DamageType = damageType;
            DamageValue = damageValue;
            TargetHpAfter = targetHpAfter;
            IsKill = isKill;
        }
    }

    /// <summary>
    /// 状态哈希数据
    /// </summary>
    public readonly struct StateHashData
    {
        public bool HasValue { get; }
        public int FrameIndex { get; }
        public uint StateHash { get; }

        public StateHashData(int frameIndex, uint stateHash)
        {
            FrameIndex = frameIndex;
            StateHash = stateHash;
            HasValue = true;
        }

        public static readonly StateHashData Default = default;
    }

    /// <summary>
    /// 角色生成数据
    /// </summary>
    public readonly struct ActorSpawnData
    {
        /// <summary>
        /// 运行时自增的 ActorId（唯一标识，从1开始）
        /// </summary>
        public int ActorId { get; }

        /// <summary>
        /// 配置表的模板ID（用于表现层读取配置）
        /// </summary>
        public int EntityCode { get; }

        public int CharacterId { get; }
        public string Name { get; }
        public float PositionX { get; }
        public float PositionY { get; }
        public float PositionZ { get; }
        public float RotationY { get; }
        public float Scale { get; }
        public int TeamId { get; }
        public float MaxHp { get; }
        public float Hp { get; }

        /// <summary>
        /// 玩家 ID（字符串，用于与 moba.core 中的 PlayerId 一致）
        /// 用于输入命令路由到正确的玩家
        /// </summary>
        public string PlayerId { get; }

        public ActorSpawnData(
            int actorId, int entityCode, int characterId, string name,
            float x, float y, float z,
            float rotationY, float scale,
            int teamId, float maxHp, float hp,
            string playerId = null)
        {
            ActorId = actorId;
            EntityCode = entityCode;
            CharacterId = characterId;
            Name = name;
            PositionX = x;
            PositionY = y;
            PositionZ = z;
            RotationY = rotationY;
            Scale = scale;
            TeamId = teamId;
            MaxHp = maxHp;
            Hp = hp;
            PlayerId = playerId ?? actorId.ToString();
        }
    }
}
