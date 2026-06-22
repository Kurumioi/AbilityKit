using System;
using AbilityKit.Ability.World.DI;
using AbilityKit.Ability.World.Services;
using AbilityKit.Ability.World.Services.Attributes;
using AbilityKit.Demo.Moba.Config.Core;
using AbilityKit.Protocol.Moba;
using MO = AbilityKit.Demo.Moba.Config.BattleDemo.MO;

namespace AbilityKit.Demo.Moba.Services.EntityConstruction
{
    /// <summary>
    /// Actor 初始化编排服务：负责把读表结果和进入战斗 loadout 分发给属性、技能初始化器。
    /// </summary>
    [WorldService(typeof(ActorEntityInitPipeline), WorldLifetime.Scoped)]
    public sealed class ActorEntityInitPipeline : IService
    {
        private readonly IWorldResolver _services;
        private readonly MobaActorInitDiagnostics _diagnostics = new MobaActorInitDiagnostics();
        private readonly MobaActorAttributeInitializer _attributes = new MobaActorAttributeInitializer();
        private readonly MobaActorSkillLoadoutInitializer _skills = new MobaActorSkillLoadoutInitializer();
        private MobaConfigDatabase _config;

        public ActorEntityInitPipeline(IWorldResolver services)
        {
            _services = services;
            TryResolveConfig();
        }

        public void InitializeFromAttributeTemplate(global::ActorEntity entity, int attributeTemplateId)
        {
            if (entity == null) return;

            _attributes.EnsureContainers(entity);

            if (!EnsureConfig()) return;
            if (attributeTemplateId <= 0) return;

            var template = ResolveAttributeTemplate(attributeTemplateId);
            _attributes.ApplyTemplate(entity, template);
        }

        public void InitializeFromLoadout(global::ActorEntity entity, in MobaPlayerLoadout loadout)
        {
            if (entity == null) return;

            var templateId = ResolveAttributeTemplateId(in loadout);
            if (templateId <= 0)
            {
                _diagnostics.LogMissingAttributeTemplate(
                    templateId,
                    $"[ActorEntityInitPipeline] AttributeTemplateId is invalid. heroId={loadout.HeroId} loadoutTemplateId={loadout.AttributeTemplateId}");
            }

            InitializeFromAttributeTemplate(entity, templateId);

            if (EnsureConfig())
            {
                _skills.Initialize(entity, in loadout, _config);
            }
        }

        private bool EnsureConfig()
        {
            if (_config != null) return true;
            if (TryResolveConfig()) return true;

            _diagnostics.LogMissingConfig("[ActorEntityInitPipeline] MobaConfigDatabase is not available. Ensure it is registered when creating the world.");
            return false;
        }

        private bool TryResolveConfig()
        {
            if (_config != null) return true;
            if (_services == null) return false;

            if (_services.TryResolve<MobaConfigDatabase>(out var config) && config != null)
            {
                _config = config;
                return true;
            }

            try
            {
                _config = _services.Resolve<MobaConfigDatabase>();
                return _config != null;
            }
            catch (Exception ex)
            {
                _diagnostics.LogConfigResolveException(ex);
                return false;
            }
        }

        private int ResolveAttributeTemplateId(in MobaPlayerLoadout loadout)
        {
            if (loadout.AttributeTemplateId > 0) return loadout.AttributeTemplateId;
            if (!EnsureConfig()) return 0;

            try
            {
                var character = _config.GetCharacter(loadout.HeroId);
                return character != null ? character.AttributeTemplateId : 0;
            }
            catch (Exception ex)
            {
                _diagnostics.LogMissingCharacter(loadout.HeroId, ex);
                return 0;
            }
        }

        private MO.BattleAttributeTemplateMO ResolveAttributeTemplate(int attributeTemplateId)
        {
            try
            {
                return _config.GetAttributeTemplate(attributeTemplateId);
            }
            catch (Exception ex)
            {
                _diagnostics.LogMissingAttributeTemplate(attributeTemplateId, ex);
                return null;
            }
        }

        public void Dispose()
        {
        }
    }
}
