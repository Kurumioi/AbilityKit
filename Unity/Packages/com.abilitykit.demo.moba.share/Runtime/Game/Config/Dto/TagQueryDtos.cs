using System;
using Newtonsoft.Json;

namespace AbilityKit.Demo.Moba.Share.Config
{
    [Serializable]
    public sealed class TagTemplateDTO
    {
        public int Id;
        public string Name;

        [JsonIgnore] public string[] RequiredTagNames;
        [JsonIgnore] public string[] BlockedTagNames;
        [JsonIgnore] public string[] GrantTagNames;
        [JsonIgnore] public string[] RemoveTagNames;

        public int[] RequiredTags;
        public int[] BlockedTags;

        public int[] GrantTags;
        public int[] RemoveTags;
    }

    [Serializable]
    public sealed class ContinuousTagTemplateDTO
    {
        public int Id;
        public string Name;

        public int[] ActivationRequiredTags;
        public int[] ActivationBlockedTags;
        public int[] ApplicationTags;
        public int[] RemovalRequiredTags;
        public int[] RemovalBlockedTags;
        public int[] OngoingRequiredTags;
        public int[] OngoingBlockedTags;
        public int[] RemovalTags;
    }

    public enum SearchTargetProviderKind
    {
        AllActors = 1,
        ExplicitTarget = 2,
    }

    public enum SearchTargetRuleKind
    {
        RequireValidId = 0x0204,
        RequireHasPosition = 0x0205,
        CircleShape = 0x0101,
        SectorShape = 0x0102,
        ExcludeCaster = 0x0301,
        ExcludeExplicitTarget = 0x0302,
        Whitelist = 0x0201,
        Blacklist = 0x0202,
    }

    public enum SearchTargetScorerKind
    {
        Zero = 0x2001,
        SeededHashRandom = 0x2002,
        DistanceToCaster = 0x2004,
        DistanceToExplicitTarget = 0x2005,
    }

    public enum SearchTargetSelectorKind
    {
        TopKByScore = 0x1001,
        StreamingTopKByScore = 0x1002,
    }

    public enum SearchTargetPointKind
    {
        Caster = 0,
        AimPosition = 1,
        ExplicitTarget = 2,
    }

    public enum SearchQueryExplicitTargetPolicy
    {
        PreferExplicitTarget = 0,
        SearchWhenMissing = 1,
        IgnoreExplicitTarget = 2,
    }

    [Serializable]
    public abstract class SearchTargetComponentDTO
    {
        public int Id;
        public int Kind;
    }

    [Serializable]
    public sealed class SearchTargetProviderDTO : SearchTargetComponentDTO
    {
    }

    [Serializable]
    public sealed class SearchTargetRuleDTO : SearchTargetComponentDTO
    {
        public int Center;
        public int Forward;
        public float Radius;
        public float HalfAngleDeg;
        public int[] ActorIds;
    }

    [Serializable]
    public sealed class SearchTargetScorerDTO : SearchTargetComponentDTO
    {
        public int Source;
        public int RandomSeed;
    }

    [Serializable]
    public sealed class SearchTargetSelectorDTO : SearchTargetComponentDTO
    {
    }

    [Serializable]
    public sealed class SearchQueryTemplateDTO
    {
        public int Id;
        public string Name;

        public int MaxCount;
        public int ExplicitTargetPolicy;

        public SearchTargetProviderDTO Provider;
        public SearchTargetRuleDTO[] Rules;
        public SearchTargetScorerDTO Scorer;
        public SearchTargetSelectorDTO Selector;

    }
}
