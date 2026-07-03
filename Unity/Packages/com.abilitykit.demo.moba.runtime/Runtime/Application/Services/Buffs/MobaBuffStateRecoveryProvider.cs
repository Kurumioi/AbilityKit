using System;
using System.Collections.Generic;
using AbilityKit.Ability.FrameSync;
using AbilityKit.Ability.World.Services.Attributes;
using AbilityKit.Core.Serialization;
using AbilityKit.Demo.Moba.Components;
using AbilityKit.Demo.Moba.Services.Buffs.Core;
using AbilityKit.Demo.Moba.Services.StateSync;

namespace AbilityKit.Demo.Moba.Services.Buffs
{
    [WorldService(typeof(MobaBuffStateRecoveryProvider))]
    public sealed class MobaBuffStateRecoveryProvider : IMobaStateRecoveryProvider
    {
        public const int DefaultKey = 10030;

        private readonly MobaActorRegistry _actors;
        private readonly MobaRuntimeContextService _runtimeContexts;

        public MobaBuffStateRecoveryProvider(MobaActorRegistry actors, MobaRuntimeContextService runtimeContexts)
        {
            _actors = actors ?? throw new ArgumentNullException(nameof(actors));
            _runtimeContexts = runtimeContexts ?? throw new ArgumentNullException(nameof(runtimeContexts));
        }

        public int Key => DefaultKey;

        public string Name => "Buff";

        public byte[] ExportState(FrameIndex frame)
        {
            var entries = new List<Entry>(16);
            foreach (var kv in _actors.Entries)
            {
                var actorId = kv.Key;
                var actor = kv.Value;
                if (actor == null || !actor.hasBuffs || actor.buffs.Active == null) continue;

                var active = actor.buffs.Active;
                for (int i = 0; i < active.Count; i++)
                {
                    var runtime = active[i];
                    if (runtime == null || runtime.BuffId <= 0) continue;
                    entries.Add(Entry.FromRuntime(actorId, runtime));
                }
            }

            entries.Sort(CompareEntries);
            return BinaryObjectCodec.Encode(new Payload(1, entries.Count == 0 ? Array.Empty<Entry>() : entries.ToArray()));
        }

        public void ImportState(FrameIndex frame, byte[] payload)
        {
            ClearAllBuffs(frame);

            if (payload == null || payload.Length == 0) return;

            var p = BinaryObjectCodec.Decode<Payload>(payload);
            if (p.Entries == null || p.Entries.Length == 0) return;

            Array.Sort(p.Entries, CompareEntries);
            for (int i = 0; i < p.Entries.Length; i++)
            {
                var entry = p.Entries[i];
                if (!_actors.TryGet(entry.TargetActorId, out var actor) || actor == null) continue;

                var list = actor.hasBuffs && actor.buffs.Active != null
                    ? actor.buffs.Active
                    : new BuffRepository().GetOrCreateList(actor);

                var runtime = BuffRepository.RentRuntime();
                entry.ApplyTo(runtime);
                list.Add(runtime);
                BuffRepository.RegisterRuntime(list, runtime);

                _runtimeContexts.EnsureBuffContext(
                    runtime,
                    MobaBuffRuntimeContextData.FromRuntime(runtime, entry.TargetActorId, frame.Value, MobaRuntimeContextLifecycleState.Active));
            }
        }

        public void AddStateHash(FrameIndex frame, MobaStateHashBuilder hash)
        {
            var payload = BinaryObjectCodec.Decode<Payload>(ExportState(frame));
            var entries = payload.Entries ?? Array.Empty<Entry>();

            hash.AddInt(Key);
            hash.AddInt(entries.Length);
            for (int i = 0; i < entries.Length; i++)
            {
                AddEntryHash(entries[i], hash);
            }
        }

        private void ClearAllBuffs(FrameIndex frame)
        {
            foreach (var kv in _actors.Entries)
            {
                var actor = kv.Value;
                if (actor == null || !actor.hasBuffs || actor.buffs.Active == null) continue;

                var active = actor.buffs.Active;
                for (int i = 0; i < active.Count; i++)
                {
                    var runtime = active[i];
                    if (runtime == null) continue;
                    _runtimeContexts.SnapshotAndDestroyBuffContext(runtime, MobaRuntimeContextLifecycleState.Destroyed, frame.Value);
                    BuffRepository.ReleaseRuntime(runtime);
                }

                BuffRepository.ReleaseList(actor);
            }
        }

        private static int CompareEntries(Entry a, Entry b)
        {
            var c = a.TargetActorId.CompareTo(b.TargetActorId);
            if (c != 0) return c;
            c = a.BuffId.CompareTo(b.BuffId);
            if (c != 0) return c;
            c = a.SourceActorId.CompareTo(b.SourceActorId);
            if (c != 0) return c;
            return a.SourceContextId.CompareTo(b.SourceContextId);
        }

        private static void AddEntryHash(in Entry entry, MobaStateHashBuilder hash)
        {
            hash.AddInt(entry.TargetActorId);
            hash.AddInt(entry.BuffId);
            hash.AddFloat(entry.RemainingSeconds);
            hash.AddFloat(entry.IntervalRemainingSeconds);
            hash.AddInt(entry.SourceActorId);
            hash.AddInt(entry.StackCount);
            hash.AddLong(entry.SourceContextId);
            hash.AddLong(entry.RuntimeContextId);
            hash.AddLong(entry.RuntimeContextVersion);
            hash.AddInt(entry.OriginSourceActorId);
            hash.AddInt(entry.OriginTargetActorId);
            hash.AddInt(entry.OriginTraceKind);
            hash.AddInt(entry.OriginConfigId);
            hash.AddLong(entry.OriginImmediateContextId);
            hash.AddLong(entry.OriginParentContextId);
            hash.AddLong(entry.OriginRootContextId);
            hash.AddLong(entry.OriginOwnerContextId);
            hash.AddLong(entry.SkillRuntimeId);
            hash.AddInt(entry.SkillRuntimeGeneration);
            hash.AddLong(entry.SkillRuntimeRootTraceContextId);
        }

        public readonly struct Payload
        {
            [BinaryMember(0)] public readonly int Version;
            [BinaryMember(1)] public readonly Entry[] Entries;

            public Payload(int version, Entry[] entries)
            {
                Version = version;
                Entries = entries ?? Array.Empty<Entry>();
            }
        }

        public readonly struct Entry
        {
            [BinaryMember(0)] public readonly int TargetActorId;
            [BinaryMember(1)] public readonly int BuffId;
            [BinaryMember(2)] public readonly float RemainingSeconds;
            [BinaryMember(3)] public readonly float IntervalRemainingSeconds;
            [BinaryMember(4)] public readonly int SourceActorId;
            [BinaryMember(5)] public readonly int StackCount;
            [BinaryMember(6)] public readonly long SourceContextId;
            [BinaryMember(7)] public readonly long RuntimeContextId;
            [BinaryMember(8)] public readonly long RuntimeContextVersion;
            [BinaryMember(9)] public readonly int OriginSourceActorId;
            [BinaryMember(10)] public readonly int OriginTargetActorId;
            [BinaryMember(11)] public readonly int OriginTraceKind;
            [BinaryMember(12)] public readonly int OriginConfigId;
            [BinaryMember(13)] public readonly long OriginImmediateContextId;
            [BinaryMember(14)] public readonly long OriginParentContextId;
            [BinaryMember(15)] public readonly long OriginRootContextId;
            [BinaryMember(16)] public readonly long OriginOwnerContextId;
            [BinaryMember(17)] public readonly long SkillRuntimeId;
            [BinaryMember(18)] public readonly int SkillRuntimeGeneration;
            [BinaryMember(19)] public readonly long SkillRuntimeRootTraceContextId;

            public Entry(
                int targetActorId,
                int buffId,
                float remainingSeconds,
                float intervalRemainingSeconds,
                int sourceActorId,
                int stackCount,
                long sourceContextId,
                long runtimeContextId,
                long runtimeContextVersion,
                int originSourceActorId,
                int originTargetActorId,
                int originTraceKind,
                int originConfigId,
                long originImmediateContextId,
                long originParentContextId,
                long originRootContextId,
                long originOwnerContextId,
                long skillRuntimeId,
                int skillRuntimeGeneration,
                long skillRuntimeRootTraceContextId)
            {
                TargetActorId = targetActorId;
                BuffId = buffId;
                RemainingSeconds = remainingSeconds;
                IntervalRemainingSeconds = intervalRemainingSeconds;
                SourceActorId = sourceActorId;
                StackCount = stackCount;
                SourceContextId = sourceContextId;
                RuntimeContextId = runtimeContextId;
                RuntimeContextVersion = runtimeContextVersion;
                OriginSourceActorId = originSourceActorId;
                OriginTargetActorId = originTargetActorId;
                OriginTraceKind = originTraceKind;
                OriginConfigId = originConfigId;
                OriginImmediateContextId = originImmediateContextId;
                OriginParentContextId = originParentContextId;
                OriginRootContextId = originRootContextId;
                OriginOwnerContextId = originOwnerContextId;
                SkillRuntimeId = skillRuntimeId;
                SkillRuntimeGeneration = skillRuntimeGeneration;
                SkillRuntimeRootTraceContextId = skillRuntimeRootTraceContextId;
            }

            public static Entry FromRuntime(int targetActorId, BuffRuntime runtime)
            {
                var origin = runtime.Origin;
                var skill = runtime.SkillRuntimeHandle.IsValid ? runtime.SkillRuntimeHandle : origin.SkillRuntimeHandle;
                return new Entry(
                    targetActorId,
                    runtime.BuffId,
                    runtime.Remaining,
                    runtime.IntervalRemainingSeconds,
                    runtime.SourceId,
                    runtime.StackCount,
                    runtime.SourceContextId,
                    runtime.RuntimeContextId,
                    runtime.RuntimeContextVersion,
                    origin.SourceActorId,
                    origin.TargetActorId,
                    (int)origin.ImmediateKind,
                    origin.ImmediateConfigId,
                    origin.ImmediateContextId,
                    origin.ParentContextId,
                    origin.RootContextId,
                    origin.OwnerContextId,
                    skill.RuntimeId,
                    skill.Generation,
                    skill.RootTraceContextId);
            }

            public void ApplyTo(BuffRuntime runtime)
            {
                if (runtime == null) return;

                runtime.BuffId = BuffId;
                runtime.Remaining = RemainingSeconds;
                runtime.IntervalRemainingSeconds = IntervalRemainingSeconds;
                runtime.SourceId = SourceActorId;
                runtime.StackCount = StackCount;
                runtime.SourceContextId = SourceContextId;
                runtime.RuntimeContextId = RuntimeContextId;
                runtime.RuntimeContextVersion = RuntimeContextVersion;

                var skill = SkillRuntimeId != 0L && SkillRuntimeGeneration > 0
                    ? new MobaSkillCastRuntimeHandle(SkillRuntimeId, SkillRuntimeGeneration, SkillRuntimeRootTraceContextId)
                    : default;

                runtime.Origin = new MobaGameplayOrigin(
                    OriginSourceActorId,
                    OriginTargetActorId,
                    (MobaTraceKind)OriginTraceKind,
                    OriginConfigId,
                    OriginImmediateContextId,
                    OriginParentContextId,
                    OriginRootContextId,
                    OriginOwnerContextId,
                    skill);
                runtime.ContextSource = MobaContextSourceView.FromOrigin(runtime.Origin, MobaContextSourceResolveKind.Origin, MobaContextSourceBoundary.Snapshot, hasLiveRuntime: true, runtimeKind: "Buff", runtimeConfigId: BuffId);
                runtime.SkillRuntimeHandle = skill;
                runtime.SkillRuntimeRetainHandle = default;
                runtime.Continuous = null;
                runtime.TagRequirements = null;
                runtime.ModifierBindings?.Clear();
                runtime.ModifierBindings = null;
            }
        }
    }
}
