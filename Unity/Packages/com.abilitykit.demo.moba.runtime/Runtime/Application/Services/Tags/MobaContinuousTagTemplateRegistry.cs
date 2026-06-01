using System.Collections.Generic;
using AbilityKit.Demo.Moba.Config.BattleDemo.MO;
using AbilityKit.Demo.Moba.Config.Core;
using AbilityKit.GameplayTags;

namespace AbilityKit.Demo.Moba.Services
{
    public sealed class MobaContinuousTagTemplateRegistry : IMobaContinuousTagTemplateRegistry
    {
        private readonly MobaConfigDatabase _db;
        private readonly Dictionary<int, ContinuousTagRequirements> _cacheById = new Dictionary<int, ContinuousTagRequirements>();
        private readonly Dictionary<string, ContinuousTagRequirements> _cacheByName = new Dictionary<string, ContinuousTagRequirements>();

        public MobaContinuousTagTemplateRegistry(MobaConfigDatabase db)
        {
            _db = db;
        }

        public bool TryGet(int templateId, out ContinuousTagRequirements requirements)
        {
            requirements = null;
            if (templateId <= 0) return false;

            if (_cacheById.TryGetValue(templateId, out requirements) && requirements != null)
            {
                return true;
            }

            if (_db == null) return false;
            if (!_db.TryGetContinuousTagTemplate(templateId, out var mo) || mo == null) return false;

            requirements = CreateRequirements(mo);
            _cacheById[templateId] = requirements;
            return true;
        }

        public bool TryGet(string name, out ContinuousTagRequirements requirements)
        {
            requirements = null;
            if (string.IsNullOrEmpty(name)) return false;

            if (_cacheByName.TryGetValue(name, out requirements) && requirements != null)
            {
                return true;
            }

            if (_db == null) return false;
            if (!_db.TryGetContinuousTagTemplateByName(name, out var mo) || mo == null) return false;

            requirements = CreateRequirements(mo);
            _cacheByName[name] = requirements;
            _cacheById[mo.Id] = requirements;
            return true;
        }

        private static ContinuousTagRequirements CreateRequirements(ContinuousTagTemplateMO mo)
        {
            return new ContinuousTagRequirements
            {
                ActivationRequired = new GameplayTagRequirements(ToContainer(mo.ActivationRequiredTags), ToContainer(mo.ActivationBlockedTags), exact: false),
                ApplicationTags = ToContainer(mo.ApplicationTags) ?? new GameplayTagContainer(),
                RemovalRequired = new GameplayTagRequirements(ToContainer(mo.RemovalRequiredTags), ToContainer(mo.RemovalBlockedTags), exact: false),
                OngoingRequired = new GameplayTagRequirements(ToContainer(mo.OngoingRequiredTags), ToContainer(mo.OngoingBlockedTags), exact: false),
                RemovalTags = ToContainer(mo.RemovalTags) ?? new GameplayTagContainer()
            };
        }

        private static GameplayTagContainer ToContainer(IReadOnlyList<int> ids)
        {
            if (ids == null || ids.Count == 0) return null;

            var c = new GameplayTagContainer();
            for (int i = 0; i < ids.Count; i++)
            {
                var id = ids[i];
                if (id <= 0) continue;
                c.Add(GameplayTag.FromId(id));
            }

            return c.Count > 0 ? c : null;
        }
    }
}
