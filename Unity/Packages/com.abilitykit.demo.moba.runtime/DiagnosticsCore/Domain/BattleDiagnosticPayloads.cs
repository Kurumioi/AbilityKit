using System;

namespace AbilityKit.Demo.Moba.Diagnostics
{
    /// <summary>
    /// 诊断事件结构化载荷的稳定判别值。已有数值不得复用或改变语义。
    /// </summary>
    public enum BattleDiagnosticPayloadKind
    {
        None = 0,
        SyncSnapshotReceived = 1
    }

    /// <summary>
    /// 收到权威状态哈希快照时记录的第一版结构化载荷。
    /// </summary>
    public readonly struct BattleDiagnosticSyncSnapshotReceivedPayload :
        IEquatable<BattleDiagnosticSyncSnapshotReceivedPayload>
    {
        public const int CurrentSchemaVersion = 1;

        public BattleDiagnosticSyncSnapshotReceivedPayload(
            int authoritativeFrame,
            uint stateHash)
        {
            AuthoritativeFrame = authoritativeFrame;
            StateHash = stateHash;
        }

        public int AuthoritativeFrame { get; }
        public uint StateHash { get; }

        public bool Equals(BattleDiagnosticSyncSnapshotReceivedPayload other)
        {
            return AuthoritativeFrame == other.AuthoritativeFrame &&
                   StateHash == other.StateHash;
        }

        public override bool Equals(object obj)
        {
            return obj is BattleDiagnosticSyncSnapshotReceivedPayload other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (AuthoritativeFrame * 397) ^ (int)StateHash;
            }
        }
    }

    /// <summary>
    /// 平台无关、无装箱的版本化诊断载荷判别联合。
    /// 未迁移事件使用 <see cref="None"/>，消费者必须按 Kind 通过专用 TryGet 读取。
    /// </summary>
    public readonly struct BattleDiagnosticEventPayload :
        IEquatable<BattleDiagnosticEventPayload>
    {
        private readonly int _int32Value;
        private readonly uint _uint32Value;

        private BattleDiagnosticEventPayload(
            BattleDiagnosticPayloadKind kind,
            int schemaVersion,
            int int32Value,
            uint uint32Value)
        {
            if (kind == BattleDiagnosticPayloadKind.None)
            {
                throw new ArgumentOutOfRangeException(nameof(kind));
            }

            if (schemaVersion < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(schemaVersion));
            }

            Kind = kind;
            SchemaVersion = schemaVersion;
            _int32Value = int32Value;
            _uint32Value = uint32Value;
        }

        public static BattleDiagnosticEventPayload None => default;

        public BattleDiagnosticPayloadKind Kind { get; }
        public int SchemaVersion { get; }
        public bool HasValue => Kind != BattleDiagnosticPayloadKind.None;

        public static BattleDiagnosticEventPayload FromSyncSnapshotReceived(
            in BattleDiagnosticSyncSnapshotReceivedPayload payload)
        {
            return new BattleDiagnosticEventPayload(
                BattleDiagnosticPayloadKind.SyncSnapshotReceived,
                BattleDiagnosticSyncSnapshotReceivedPayload.CurrentSchemaVersion,
                payload.AuthoritativeFrame,
                payload.StateHash);
        }

        public bool TryGetSyncSnapshotReceived(
            out BattleDiagnosticSyncSnapshotReceivedPayload payload)
        {
            if (Kind != BattleDiagnosticPayloadKind.SyncSnapshotReceived ||
                SchemaVersion != BattleDiagnosticSyncSnapshotReceivedPayload.CurrentSchemaVersion)
            {
                payload = default;
                return false;
            }

            payload = new BattleDiagnosticSyncSnapshotReceivedPayload(
                _int32Value,
                _uint32Value);
            return true;
        }

        public bool Equals(BattleDiagnosticEventPayload other)
        {
            return Kind == other.Kind &&
                   SchemaVersion == other.SchemaVersion &&
                   _int32Value == other._int32Value &&
                   _uint32Value == other._uint32Value;
        }

        public override bool Equals(object obj)
        {
            return obj is BattleDiagnosticEventPayload other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (int)Kind;
                hashCode = (hashCode * 397) ^ SchemaVersion;
                hashCode = (hashCode * 397) ^ _int32Value;
                hashCode = (hashCode * 397) ^ (int)_uint32Value;
                return hashCode;
            }
        }
    }
}
