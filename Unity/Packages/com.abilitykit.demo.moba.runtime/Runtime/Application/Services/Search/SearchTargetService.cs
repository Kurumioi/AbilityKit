using System;
using System.Collections.Generic;
using AbilityKit.Ability.Share.ECS;
using AbilityKit.Ability.World.Services;
using AbilityKit.Ability.World.Services.Attributes;
using AbilityKit.Battle.SearchTarget;
using AbilityKit.Demo.Moba.Config.BattleDemo.MO;
using AbilityKit.Demo.Moba.Config.Core;
using AbilityKit.Demo.Moba.Services;
using AbilityKit.Core.Math;
using ST = AbilityKit.Battle.SearchTarget;

namespace AbilityKit.Demo.Moba.Services.Search
{
    /// <summary>
    /// 目标搜索服务
    /// 提供基于配置模板的单位目标搜索功能
    /// </summary>
    [WorldService(typeof(SearchTargetService))]
    public sealed class SearchTargetService : IService
    {
        private readonly MobaConfigDatabase _configs;
        private readonly TargetSearchEngine _engine = new TargetSearchEngine();
        private readonly SearchContext _context = new SearchContext();
        private readonly List<ST.IEntityId> _searchResults = new List<ST.IEntityId>(32);
        private readonly AllActorsCandidateProvider _allActorsProvider;
        private readonly MobaSearchQueryBuilder _queryBuilder;

        public SearchTargetService(MobaActorRegistry actors, MobaConfigDatabase configs = null, MobaCombatRulesService combatRules = null)
        {
            if (actors == null) throw new ArgumentNullException(nameof(actors));
            _configs = configs;
            _allActorsProvider = new AllActorsCandidateProvider(actors);
            _queryBuilder = new MobaSearchQueryBuilder(actors, _allActorsProvider, combatRules);
            _context.SetService<IPositionProvider>(new RegistryPositionProvider(actors));
            _context.SetService<IEntityKeyProvider>(ActorIdKeyProvider.Instance);
        }

        /// <summary>
        /// 搜索最近的单个目标
        /// </summary>
        public bool TrySearchFirstActorId(int queryTemplateId, int casterActorId, in Vec3 aimPos, out int targetActorId)
        {
            targetActorId = 0;
            if (queryTemplateId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(queryTemplateId), queryTemplateId, "Search query template id must be positive.");
            }

            var query = BuildQuery(queryTemplateId, casterActorId, in aimPos, explicitTargetActorId: 0, maxCountOverride: 1);

            _searchResults.Clear();
            _engine.SearchIds(in query, _context, _searchResults);
            if (_searchResults.Count == 0) return false;

            targetActorId = _searchResults[0].ActorId;
            return targetActorId > 0;
        }

        /// <summary>
        /// 搜索多个目标
        /// </summary>
        public bool TrySearchActorIds(int queryTemplateId, int casterActorId, in Vec3 aimPos, int explicitTargetActorId, List<int> results)
        {
            if (results == null) throw new ArgumentNullException(nameof(results));
            results.Clear();

            if (queryTemplateId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(queryTemplateId), queryTemplateId, "Search query template id must be positive.");
            }

            var query = BuildQuery(queryTemplateId, casterActorId, in aimPos, explicitTargetActorId, maxCountOverride: 0);
            return ExecuteSearch(in query, results);
        }

        public bool TrySearchActorIds(SearchQueryTemplateMO template, int casterActorId, in Vec3 aimPos, int explicitTargetActorId, List<int> results)
        {
            if (results == null) throw new ArgumentNullException(nameof(results));
            results.Clear();

            if (template == null)
            {
                throw new ArgumentNullException(nameof(template));
            }

            if (!_queryBuilder.TryBuild(template, _context, casterActorId, in aimPos, explicitTargetActorId, maxCountOverride: 0, out var query))
            {
                throw new InvalidOperationException($"Search query builder failed without diagnostics. templateId={template.Id}");
            }

            return ExecuteSearch(in query, results);
        }

        private bool ExecuteSearch(in SearchQuery query, List<int> results)
        {
            _searchResults.Clear();
            _engine.SearchIds(in query, _context, _searchResults);
            if (_searchResults.Count == 0) return false;

            for (int i = 0; i < _searchResults.Count; i++)
            {
                var id = _searchResults[i].ActorId;
                if (id > 0) results.Add(id);
            }

            return results.Count > 0;
        }

        private SearchQuery BuildQuery(
            int queryTemplateId,
            int casterActorId,
            in Vec3 aimPos,
            int explicitTargetActorId,
            int maxCountOverride)
        {
            var template = GetTemplate(queryTemplateId);
            if (!_queryBuilder.TryBuild(template, _context, casterActorId, in aimPos, explicitTargetActorId, maxCountOverride, out var query))
            {
                throw new InvalidOperationException($"Search query builder failed without diagnostics. templateId={queryTemplateId}");
            }

            return query;
        }

        private SearchQueryTemplateMO GetTemplate(int queryTemplateId)
        {
            if (queryTemplateId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(queryTemplateId), queryTemplateId, "Search query template id must be positive.");
            }

            if (_configs == null)
            {
                throw new InvalidOperationException("SearchTargetService requires MobaConfigDatabase for template queries.");
            }

            if (!_configs.TryGetSearchQueryTemplate(queryTemplateId, out var template) || template == null)
            {
                throw new InvalidOperationException($"Search query template not found. templateId={queryTemplateId}");
            }

            return template;
        }

        private sealed class RegistryPositionProvider : IPositionProvider
        {
            private readonly MobaActorRegistry _actors;

            public RegistryPositionProvider(MobaActorRegistry actors)
            {
                _actors = actors;
            }

            public bool TryGetPosition(ST.IEntityId entity, out IVec2 position)
            {
                position = default;
                if (!entity.IsValid) return false;
                if (_actors == null) return false;

                if (!_actors.TryGet(entity.ActorId, out var e) || e == null) return false;
                if (!e.hasTransform) return false;

                var p = e.transform.Value.Position;
                position = new ST.Vec2(p.X, p.Z);
                return true;
            }
        }

        private sealed class ActorIdKeyProvider : IEntityKeyProvider
        {
            public static readonly ActorIdKeyProvider Instance = new ActorIdKeyProvider();

            public ulong GetKey(ST.IEntityId id)
            {
                return (ulong)id.ActorId;
            }
        }

        private sealed class AllActorsCandidateProvider : ICandidateProvider
        {
            private readonly MobaActorRegistry _actors;

            public AllActorsCandidateProvider(MobaActorRegistry actors)
            {
                _actors = actors;
            }

            public bool RequiresPosition => false;

            public void ForEachCandidate<TConsumer>(in SearchQuery query, SearchContext context, ref TConsumer consumer)
                where TConsumer : struct, ICandidateConsumer
            {
                if (_actors == null) return;

                foreach (var kv in _actors.Entries)
                {
                    var id = kv.Key;
                    if (id <= 0) continue;
                    consumer.Consume(new ST.EntityId(id));
                }
            }
        }

        public void Dispose()
        {
        }
    }
}
