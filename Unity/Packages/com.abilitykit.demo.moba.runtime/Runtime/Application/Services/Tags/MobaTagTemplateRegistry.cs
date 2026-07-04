using System.Collections.Generic;
using AbilityKit.Demo.Moba.Config.Core;
using AbilityKit.Demo.Moba.Config.BattleDemo.MO;
using AbilityKit.GameplayTags;
using ITagTemplateRegistry = AbilityKit.GameplayTags.ITagTemplateRegistry;
using TagTemplateRuntime = AbilityKit.GameplayTags.TagTemplateRuntime;
using GameplayTagRequirements = AbilityKit.GameplayTags.GameplayTagRequirements;

namespace AbilityKit.Demo.Moba.Services
{
    public sealed class MobaTagTemplateRegistry : ITagTemplateRegistry
    {
        private readonly MobaConfigDatabase _db;
        private readonly Dictionary<int, TagTemplateRuntime> _cacheById = new Dictionary<int, TagTemplateRuntime>();
        private readonly Dictionary<string, TagTemplateRuntime> _cacheByName = new Dictionary<string, TagTemplateRuntime>();

        public MobaTagTemplateRegistry(MobaConfigDatabase db)
        {
            _db = db;
        }

        public bool TryGet(int templateId, out TagTemplateRuntime template)
        {
            template = null;
            if (templateId <= 0) return false;

            if (_cacheById.TryGetValue(templateId, out template) && template != null)
            {
                return true;
            }

            if (_db == null) return false;
            if (!_db.TryGetTagTemplate(templateId, out var mo)) return false;

            template = CreateTemplate(mo);
            _cacheById[templateId] = template;
            return true;
        }

        public bool TryGet(string name, out TagTemplateRuntime template)
        {
            template = null;
            if (string.IsNullOrEmpty(name)) return false;

            if (_cacheByName.TryGetValue(name, out template) && template != null)
            {
                return true;
            }

            if (_db == null) return false;
            if (!_db.TryGetTagTemplateByName(name, out var mo)) return false;

            template = CreateTemplate(mo);
            _cacheByName[name] = template;
            _cacheById[mo.Id] = template;
            return true;
        }

        private static TagTemplateRuntime CreateTemplate(TagTemplateMO mo)
        {
            var req = new GameplayTagRequirements(mo.RequiredTags, mo.BlockedTags, exact: false);
            return new TagTemplateRuntime(mo.Id, mo.Name, req, mo.GrantTags, mo.RemoveTags);
        }
    }
}
