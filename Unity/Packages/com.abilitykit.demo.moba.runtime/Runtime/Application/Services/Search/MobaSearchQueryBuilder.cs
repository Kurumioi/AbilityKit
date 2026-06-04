using System;
using System.Collections.Generic;
using AbilityKit.Ability.Share.ECS;
using AbilityKit.Battle.SearchTarget;
using AbilityKit.Battle.SearchTarget.Rules;
using AbilityKit.Battle.SearchTarget.Scorers;
using AbilityKit.Battle.SearchTarget.Selectors;
using AbilityKit.Demo.Moba.Config.BattleDemo.MO;
using AbilityKit.Demo.Moba.Share.Config;
using AbilityKit.Core.Math;
using ST = AbilityKit.Battle.SearchTarget;

namespace AbilityKit.Demo.Moba.Services.Search
{
    internal sealed class MobaSearchQueryBuilder
    {
        public const int RandomSeedContextKey = 0x5EED;
        private const float DefaultSearchRadius = 5f;
        private const float DefaultHalfAngleDeg = 45f;

        private static readonly ITargetRule[] ExplicitTargetRules = { RequireValidIdRule.Instance };

        private readonly MobaActorRegistry _actors;
        private readonly ICandidateProvider _allActorsProvider;
        private readonly List<ITargetRule> _rules = new List<ITargetRule>(8);
        private readonly ZeroScorer _zeroScorer = new ZeroScorer();
        private readonly TopKByScoreSelector _topKSelector = new TopKByScoreSelector();
        private readonly StreamingTopKByScoreSelector _streamingTopKSelector = new StreamingTopKByScoreSelector();

        public MobaSearchQueryBuilder(MobaActorRegistry actors, ICandidateProvider allActorsProvider)
        {
            _actors = actors ?? throw new ArgumentNullException(nameof(actors));
            _allActorsProvider = allActorsProvider ?? throw new ArgumentNullException(nameof(allActorsProvider));
        }

        public bool TryBuild(
            SearchQueryTemplateMO template,
            SearchContext context,
            int casterActorId,
            in Vec3 aimPos,
            int explicitTargetActorId,
            int maxCountOverride,
            out SearchQuery query)
        {
            query = default;
            if (template == null || context == null) return false;

            context.ClearData();
            var maxCount = maxCountOverride > 0 ? maxCountOverride : template.MaxCount;

            var explicitPolicy = (SearchQueryExplicitTargetPolicy)template.ExplicitTargetPolicy;
            if (explicitTargetActorId > 0 && explicitPolicy == SearchQueryExplicitTargetPolicy.PreferExplicitTarget)
            {
                query = new SearchQuery(
                    provider: new SingleActorCandidateProvider(explicitTargetActorId),
                    rules: ExplicitTargetRules,
                    scorer: _zeroScorer,
                    selector: _topKSelector,
                    maxCount: 1);
                return true;
            }

            var provider = BuildProvider(template.Provider, explicitTargetActorId);
            if (provider == null) return false;

            _rules.Clear();
            var configuredRules = template.Rules ?? Array.Empty<SearchTargetRuleConfig>();
            for (int i = 0; i < configuredRules.Length; i++)
            {
                var rule = BuildRule(configuredRules[i], casterActorId, explicitTargetActorId, in aimPos);
                if (rule != null) _rules.Add(rule);
            }

            var scorer = BuildScorer(template.Scorer, context, casterActorId, explicitTargetActorId);
            var selector = BuildSelector(template.Selector);

            query = new SearchQuery(
                provider: provider,
                rules: _rules,
                scorer: scorer,
                selector: selector,
                maxCount: maxCount);
            return true;
        }

        private ICandidateProvider BuildProvider(SearchTargetProviderConfig config, int explicitTargetActorId)
        {
            var kind = config != null ? (SearchTargetProviderKind)config.Kind : SearchTargetProviderKind.AllActors;
            switch (kind)
            {
                case SearchTargetProviderKind.ExplicitTarget:
                    return explicitTargetActorId > 0 ? new SingleActorCandidateProvider(explicitTargetActorId) : null;
                default:
                    return _allActorsProvider;
            }
        }

        private ITargetRule BuildRule(SearchTargetRuleConfig config, int casterActorId, int explicitTargetActorId, in Vec3 aimPos)
        {
            if (config == null) return null;

            var kind = (SearchTargetRuleKind)config.Kind;
            switch (kind)
            {
                case SearchTargetRuleKind.RequireValidId:
                    return RequireValidIdRule.Instance;
                case SearchTargetRuleKind.RequireHasPosition:
                    return RequireHasPositionRule.Instance;
                case SearchTargetRuleKind.CircleShape:
                {
                    var origin = ResolvePoint(config.Center, casterActorId, explicitTargetActorId, in aimPos);
                    var radius = config.Radius > 0f ? config.Radius : DefaultSearchRadius;
                    return new CircleShapeRule(origin, radius);
                }
                case SearchTargetRuleKind.SectorShape:
                {
                    var origin = ResolvePoint(config.Center, casterActorId, explicitTargetActorId, in aimPos);
                    var forward = ResolveForward(config.Forward, casterActorId, explicitTargetActorId, in aimPos, origin);
                    var radius = config.Radius > 0f ? config.Radius : DefaultSearchRadius;
                    var halfAngle = config.HalfAngleDeg > 0f ? config.HalfAngleDeg : DefaultHalfAngleDeg;
                    return new SectorShapeRule(origin, forward, radius, halfAngle);
                }
                case SearchTargetRuleKind.ExcludeCaster:
                    return casterActorId > 0 ? new ExcludeEntityRule(new ST.EntityId(casterActorId)) : null;
                case SearchTargetRuleKind.ExcludeExplicitTarget:
                    return explicitTargetActorId > 0 ? new ExcludeEntityRule(new ST.EntityId(explicitTargetActorId)) : null;
                case SearchTargetRuleKind.Whitelist:
                    return new WhitelistRule(new ArrayActorIdSet(config.ActorIds));
                case SearchTargetRuleKind.Blacklist:
                    return new BlacklistRule(new ArrayActorIdSet(config.ActorIds));
                default:
                    return null;
            }
        }

        private ITargetScorer BuildScorer(SearchTargetScorerConfig config, SearchContext context, int casterActorId, int explicitTargetActorId)
        {
            var kind = config != null ? (SearchTargetScorerKind)config.Kind : SearchTargetScorerKind.DistanceToCaster;
            switch (kind)
            {
                case SearchTargetScorerKind.Zero:
                    return _zeroScorer;
                case SearchTargetScorerKind.SeededHashRandom:
                    context.SetData(RandomSeedContextKey, config != null ? config.RandomSeed : 0);
                    return new SeededHashRandomScorer(RandomSeedContextKey);
                case SearchTargetScorerKind.DistanceToExplicitTarget:
                    return explicitTargetActorId > 0 ? new DistanceToEntityScorer(new ST.EntityId(explicitTargetActorId)) : _zeroScorer;
                default:
                    return casterActorId > 0 ? new DistanceToEntityScorer(new ST.EntityId(casterActorId)) : _zeroScorer;
            }
        }

        private ITargetSelector BuildSelector(SearchTargetSelectorConfig config)
        {
            var kind = config != null ? (SearchTargetSelectorKind)config.Kind : SearchTargetSelectorKind.TopKByScore;
            return kind == SearchTargetSelectorKind.StreamingTopKByScore ? _streamingTopKSelector : _topKSelector;
        }

        private ST.Vec2 ResolvePoint(int pointKind, int casterActorId, int explicitTargetActorId, in Vec3 aimPos)
        {
            var kind = (SearchTargetPointKind)pointKind;
            switch (kind)
            {
                case SearchTargetPointKind.AimPosition:
                    return new ST.Vec2(aimPos.X, aimPos.Z);
                case SearchTargetPointKind.ExplicitTarget:
                    if (TryGetActorPosition(explicitTargetActorId, out var targetPos)) return targetPos;
                    break;
            }

            if (TryGetActorPosition(casterActorId, out var casterPos)) return casterPos;
            return new ST.Vec2(aimPos.X, aimPos.Z);
        }

        private ST.Vec2 ResolveForward(int pointKind, int casterActorId, int explicitTargetActorId, in Vec3 aimPos, ST.Vec2 origin)
        {
            var toPoint = ResolvePoint(pointKind, casterActorId, explicitTargetActorId, in aimPos);
            var fx = toPoint.X - origin.X;
            var fy = toPoint.Y - origin.Y;
            if (fx * fx + fy * fy > 0.000001f) return new ST.Vec2(fx, fy);

            if (TryGetActorPosition(casterActorId, out var casterPos))
            {
                fx = origin.X - casterPos.X;
                fy = origin.Y - casterPos.Y;
                if (fx * fx + fy * fy > 0.000001f) return new ST.Vec2(fx, fy);
            }

            return ST.Vec2.Up;
        }

        private bool TryGetActorPosition(int actorId, out ST.Vec2 position)
        {
            position = default;
            if (actorId <= 0) return false;
            if (_actors.TryGet(actorId, out var actor) && actor != null && actor.hasTransform)
            {
                var p = actor.transform.Value.Position;
                position = new ST.Vec2(p.X, p.Z);
                return true;
            }

            return false;
        }

        private sealed class SingleActorCandidateProvider : ICandidateProvider
        {
            private readonly int _actorId;

            public SingleActorCandidateProvider(int actorId)
            {
                _actorId = actorId;
            }

            public bool RequiresPosition => false;

            public void ForEachCandidate<TConsumer>(in SearchQuery query, SearchContext context, ref TConsumer consumer)
                where TConsumer : struct, ICandidateConsumer
            {
                if (_actorId > 0) consumer.Consume(new ST.EntityId(_actorId));
            }
        }

        private sealed class ArrayActorIdSet : IActorIdSet
        {
            private readonly int[] _actorIds;

            public ArrayActorIdSet(int[] actorIds)
            {
                _actorIds = actorIds ?? Array.Empty<int>();
            }

            public int Count => _actorIds.Length;

            public bool Contains(int actorId)
            {
                for (int i = 0; i < _actorIds.Length; i++)
                {
                    if (_actorIds[i] == actorId) return true;
                }

                return false;
            }
        }
    }
}
