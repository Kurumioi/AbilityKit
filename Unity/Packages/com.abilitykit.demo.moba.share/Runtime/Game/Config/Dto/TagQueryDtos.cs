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

    [Serializable]
    public sealed class SearchQueryTemplateDTO
    {
        public int Id;
        public string Name;

        public int CenterMode;
        public float Radius;
        public int MaxCount;

        public bool ExcludeCaster;
    }
}
