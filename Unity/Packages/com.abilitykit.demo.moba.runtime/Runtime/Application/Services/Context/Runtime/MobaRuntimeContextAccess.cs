using AbilityKit.Context;

namespace AbilityKit.Demo.Moba.Services
{
    public readonly struct MobaRuntimeContextReference
    {
        public MobaRuntimeContextReference(long contextId, long version)
        {
            ContextId = contextId;
            Version = version;
        }

        public long ContextId { get; }
        public long Version { get; }
        public bool IsValid => ContextId != 0L;
    }

    public enum MobaRuntimeContextValueFailure
    {
        None = 0,
        MissingContextService = 1,
        MissingPayload = 2,
        MissingRuntimeContext = 3,
        InvalidRuntimeContext = 4,
        VersionUnavailable = 5,
        VersionMismatch = 6,
        ValueMissing = 7
    }

    public readonly struct MobaRuntimeContextValueResult<TValue>
    {
        public MobaRuntimeContextValueResult(
            bool found,
            TValue value,
            ContextValueSource source,
            MobaRuntimeContextValueFailure failure,
            MobaRuntimeContextReference reference,
            long expectedVersion,
            long actualVersion)
        {
            Found = found;
            Value = value;
            Source = source;
            Failure = failure;
            Reference = reference;
            ExpectedVersion = expectedVersion;
            ActualVersion = actualVersion;
        }

        public bool Found { get; }
        public TValue Value { get; }
        public ContextValueSource Source { get; }
        public MobaRuntimeContextValueFailure Failure { get; }
        public MobaRuntimeContextReference Reference { get; }
        public long ExpectedVersion { get; }
        public long ActualVersion { get; }
        public bool IsVersionChecked => ExpectedVersion > 0L;

        public static MobaRuntimeContextValueResult<TValue> Success(TValue value, ContextValueSource source, in MobaRuntimeContextReference reference, long actualVersion)
        {
            return new MobaRuntimeContextValueResult<TValue>(true, value, source, MobaRuntimeContextValueFailure.None, reference, reference.Version, actualVersion);
        }

        public static MobaRuntimeContextValueResult<TValue> Fail(
            MobaRuntimeContextValueFailure failure,
            in MobaRuntimeContextReference reference = default,
            long expectedVersion = 0L,
            long actualVersion = 0L)
        {
            return new MobaRuntimeContextValueResult<TValue>(false, default, ContextValueSource.None, failure, reference, expectedVersion, actualVersion);
        }
    }

    public interface IMobaRuntimeContextPayload
    {
        bool TryGetRuntimeContext(out MobaRuntimeContextReference reference);
    }

    public static class MobaRuntimeContextAccessExtensions
    {
        public static bool TryResolveRuntimeContext(this object payload, out MobaRuntimeContextReference reference)
        {
            reference = default;
            if (payload == null) return false;

            if (payload is MobaRuntimeContextReference direct && direct.IsValid)
            {
                reference = direct;
                return true;
            }

            if (payload is MobaCombatExecutionContext executionContext)
            {
                return executionContext.TryGetRuntimeContext(out reference);
            }

            if (payload is IMobaRuntimeContextPayload provider && provider.TryGetRuntimeContext(out reference) && reference.IsValid)
                return true;

            return false;
        }

        public static MobaRuntimeContextValueResult<TValue> GetRuntimeContextValue<TValue, TProperty>(
            this object payload,
            MobaRuntimeContextService contexts,
            string key,
            ContextValueReadMode mode = ContextValueReadMode.RealtimeThenSnapshot)
            where TProperty : class, IProperty
        {
            if (contexts == null)
                return MobaRuntimeContextValueResult<TValue>.Fail(MobaRuntimeContextValueFailure.MissingContextService);

            if (payload == null)
                return MobaRuntimeContextValueResult<TValue>.Fail(MobaRuntimeContextValueFailure.MissingPayload);

            if (!payload.TryResolveRuntimeContext(out var reference))
                return MobaRuntimeContextValueResult<TValue>.Fail(MobaRuntimeContextValueFailure.MissingRuntimeContext);

            if (!reference.IsValid)
                return MobaRuntimeContextValueResult<TValue>.Fail(MobaRuntimeContextValueFailure.InvalidRuntimeContext, in reference);

            var actualVersion = 0L;
            if (reference.Version > 0L)
            {
                if (!contexts.Resolver.TryGetValue<long, TProperty>(reference.ContextId, MobaRuntimeContextKeys.Version, out actualVersion, mode))
                    return MobaRuntimeContextValueResult<TValue>.Fail(MobaRuntimeContextValueFailure.VersionUnavailable, in reference, reference.Version);

                if (actualVersion != reference.Version)
                    return MobaRuntimeContextValueResult<TValue>.Fail(MobaRuntimeContextValueFailure.VersionMismatch, in reference, reference.Version, actualVersion);
            }

            var result = contexts.Resolver.GetValue<TValue, TProperty>(reference.ContextId, key, default, mode);
            if (result.Found && result.Source != ContextValueSource.DefaultValue)
                return MobaRuntimeContextValueResult<TValue>.Success(result.Value, result.Source, in reference, actualVersion);

            return MobaRuntimeContextValueResult<TValue>.Fail(MobaRuntimeContextValueFailure.ValueMissing, in reference, reference.Version, actualVersion);
        }

        public static bool TryGetRuntimeContextValue<TValue, TProperty>(
            this object payload,
            MobaRuntimeContextService contexts,
            string key,
            out TValue value,
            ContextValueReadMode mode = ContextValueReadMode.RealtimeThenSnapshot)
            where TProperty : class, IProperty
        {
            var result = payload.GetRuntimeContextValue<TValue, TProperty>(contexts, key, mode);
            value = result.Value;
            return result.Found;
        }
    }
}
