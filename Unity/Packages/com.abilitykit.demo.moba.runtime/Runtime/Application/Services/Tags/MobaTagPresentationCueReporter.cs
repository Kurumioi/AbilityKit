using System;
using AbilityKit.Ability.World.DI;
using AbilityKit.Ability.World.Services;
using AbilityKit.Ability.World.Services.Attributes;
using AbilityKit.Demo.Moba.Triggering;
using AbilityKit.GameplayTags;
using AbilityKit.Protocol.Moba.StateSync;

namespace AbilityKit.Demo.Moba.Services
{
    [WorldService(typeof(MobaTagPresentationCueReporter))]
    public sealed class MobaTagPresentationCueReporter : IService, IWorldInitializable, IDisposable
    {
        private const string CueKindTag = "Tag";
        private const string EventTagAdded = "tag.added";
        private const string EventTagRemoved = "tag.removed";
        private const int NumericTagId = 1;
        private const int NumericTagDepth = 2;
        private const int NumericChangeKind = 3;
        private const int NumericSourceValue = 4;

        private IGameplayTagService _tags;
        private MobaPresentationCueSnapshotService _presentationCues;

        public void OnInit(IWorldResolver services)
        {
            services?.TryResolve(out _tags);
            services?.TryResolve(out _presentationCues);

            if (_tags != null)
            {
                _tags.TagsChanged += OnTagsChanged;
            }
        }

        public void Dispose()
        {
            if (_tags != null)
            {
                _tags.TagsChanged -= OnTagsChanged;
            }

            _tags = null;
            _presentationCues = null;
        }

        private void OnTagsChanged(int ownerActorId, GameplayTagDelta delta, GameplayTagSource source)
        {
            if (_presentationCues == null || ownerActorId <= 0 || delta.IsEmpty) return;

            ReportTags(ownerActorId, delta.Added, source, MobaPresentationCueStage.Started, EventTagAdded, 1);
            ReportTags(ownerActorId, delta.Removed, source, MobaPresentationCueStage.Removed, EventTagRemoved, -1);
        }

        private void ReportTags(int ownerActorId, GameplayTagContainer tags, GameplayTagSource source, MobaPresentationCueStage stage, string eventId, int changeKind)
        {
            if (tags == null || tags.Count == 0) return;

            foreach (var tag in tags)
            {
                if (!tag.IsValid) continue;

                var tagName = tag.TagName ?? string.Empty;
                var simpleName = tag.SimpleName ?? string.Empty;
                var entry = new MobaPresentationCueSnapshotEntry
                {
                    Stage = (int)stage,
                    CueKind = CueKindTag,
                    RequestKey = BuildRequestKey(eventId, ownerActorId, tag),
                    SourceActorId = source.Value > 0 && source.Value <= int.MaxValue ? (int)source.Value : ownerActorId,
                    TargetActorId = ownerActorId,
                    Targets = new[] { ownerActorId },
                    OwnerKind = CueKindTag,
                    InstanceId = ComposeInstanceId(ownerActorId, tag),
                    InstanceKey = BuildInstanceKey(ownerActorId, tag),
                    LifecycleReason = changeKind,
                    ContextEventId = eventId,
                    NumericParamKeys = new[] { NumericTagId, NumericTagDepth, NumericChangeKind, NumericSourceValue },
                    NumericParamValues = new[] { (float)tag.Value, tag.GetDepth(), changeKind, (float)source.Value },
                    StringParamKeys = new[] { "tag", "simple", "event", "source" },
                    StringParamValues = new[] { tagName, simpleName, eventId, source.ToString() },
                };

                _presentationCues.Report(in entry);
            }
        }

        private static string BuildRequestKey(string eventId, int ownerActorId, GameplayTag tag)
        {
            return $"{eventId}:{ownerActorId}:{tag.Value}";
        }

        private static string BuildInstanceKey(int ownerActorId, GameplayTag tag)
        {
            return $"tag:{ownerActorId}:{tag.Value}";
        }

        private static long ComposeInstanceId(int ownerActorId, GameplayTag tag)
        {
            return ((long)ownerActorId << 32) ^ (uint)tag.Value;
        }
    }
}
