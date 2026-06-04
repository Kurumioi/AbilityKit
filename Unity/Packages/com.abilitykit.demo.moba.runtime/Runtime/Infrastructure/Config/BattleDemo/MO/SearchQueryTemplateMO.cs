using System;
using AbilityKit.Demo.Moba.Share.Config;

namespace AbilityKit.Demo.Moba.Config.BattleDemo.MO
{
    public sealed class SearchQueryTemplateMO
    {
        public int Id { get; }
        public string Name { get; }
        public int MaxCount { get; }
        public int ExplicitTargetPolicy { get; }
        public SearchTargetProviderConfig Provider { get; }
        public SearchTargetRuleConfig[] Rules { get; }
        public SearchTargetScorerConfig Scorer { get; }
        public SearchTargetSelectorConfig Selector { get; }

        public SearchQueryTemplateMO(SearchQueryTemplateDTO dto)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));
            Id = dto.Id;
            Name = dto.Name;
            MaxCount = dto.MaxCount;
            ExplicitTargetPolicy = dto.ExplicitTargetPolicy;
            Provider = BuildProvider(dto);
            Rules = BuildRules(dto);
            Scorer = BuildScorer(dto);
            Selector = BuildSelector(dto);
        }

        private static SearchTargetProviderConfig BuildProvider(SearchQueryTemplateDTO dto)
        {
            return dto.Provider != null
                ? new SearchTargetProviderConfig(dto.Provider)
                : new SearchTargetProviderConfig(0, (int)SearchTargetProviderKind.AllActors);
        }

        private static SearchTargetRuleConfig[] BuildRules(SearchQueryTemplateDTO dto)
        {
            if (dto.Rules == null || dto.Rules.Length == 0) return Array.Empty<SearchTargetRuleConfig>();

            var rules = new SearchTargetRuleConfig[dto.Rules.Length];
            for (int i = 0; i < dto.Rules.Length; i++) rules[i] = new SearchTargetRuleConfig(dto.Rules[i]);
            return rules;
        }

        private static SearchTargetScorerConfig BuildScorer(SearchQueryTemplateDTO dto)
        {
            return dto.Scorer != null
                ? new SearchTargetScorerConfig(dto.Scorer)
                : new SearchTargetScorerConfig(0, (int)SearchTargetScorerKind.DistanceToCaster, (int)SearchTargetPointKind.Caster);
        }

        private static SearchTargetSelectorConfig BuildSelector(SearchQueryTemplateDTO dto)
        {
            return dto.Selector != null
                ? new SearchTargetSelectorConfig(dto.Selector)
                : new SearchTargetSelectorConfig(0, (int)SearchTargetSelectorKind.TopKByScore);
        }
    }

    public abstract class SearchTargetComponentConfig
    {
        public int Id { get; }
        public int Kind { get; }

        protected SearchTargetComponentConfig(int id, int kind)
        {
            Id = id;
            Kind = kind;
        }
    }

    public sealed class SearchTargetProviderConfig : SearchTargetComponentConfig
    {
        public SearchTargetProviderConfig(SearchTargetProviderDTO dto)
            : this(dto != null ? dto.Id : 0, dto != null ? dto.Kind : 0)
        {
        }

        public SearchTargetProviderConfig(int id, int kind)
            : base(id, kind)
        {
        }
    }

    public sealed class SearchTargetRuleConfig : SearchTargetComponentConfig
    {
        public int Center { get; }
        public int Forward { get; }
        public float Radius { get; }
        public float HalfAngleDeg { get; }
        public int[] ActorIds { get; }

        public SearchTargetRuleConfig(SearchTargetRuleDTO dto)
            : this(
                dto != null ? dto.Id : 0,
                dto != null ? dto.Kind : 0,
                dto != null ? dto.Center : 0,
                dto != null ? dto.Forward : 0,
                dto != null ? dto.Radius : 0f,
                dto != null ? dto.HalfAngleDeg : 0f,
                dto != null ? dto.ActorIds : null)
        {
        }

        public SearchTargetRuleConfig(int id, int kind, int center = 0, int forward = 0, float radius = 0f, float halfAngleDeg = 0f, int[] actorIds = null)
            : base(id, kind)
        {
            Center = center;
            Forward = forward;
            Radius = radius;
            HalfAngleDeg = halfAngleDeg;
            ActorIds = actorIds ?? Array.Empty<int>();
        }
    }

    public sealed class SearchTargetScorerConfig : SearchTargetComponentConfig
    {
        public int Source { get; }
        public int RandomSeed { get; }

        public SearchTargetScorerConfig(SearchTargetScorerDTO dto)
            : this(
                dto != null ? dto.Id : 0,
                dto != null ? dto.Kind : 0,
                dto != null ? dto.Source : 0,
                dto != null ? dto.RandomSeed : 0)
        {
        }

        public SearchTargetScorerConfig(int id, int kind, int source = 0, int randomSeed = 0)
            : base(id, kind)
        {
            Source = source;
            RandomSeed = randomSeed;
        }
    }

    public sealed class SearchTargetSelectorConfig : SearchTargetComponentConfig
    {
        public SearchTargetSelectorConfig(SearchTargetSelectorDTO dto)
            : this(dto != null ? dto.Id : 0, dto != null ? dto.Kind : 0)
        {
        }

        public SearchTargetSelectorConfig(int id, int kind)
            : base(id, kind)
        {
        }
    }
}
