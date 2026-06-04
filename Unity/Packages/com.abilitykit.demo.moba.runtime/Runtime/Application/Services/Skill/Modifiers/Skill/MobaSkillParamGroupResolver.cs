using AbilityKit.Modifiers;

namespace AbilityKit.Demo.Moba.Services
{
    public sealed class MobaSkillParamGroupResolver
    {
        private readonly MobaSkillParamModifierService _service;

        internal MobaSkillParamGroupResolver(MobaSkillParamModifierService service)
        {
            _service = service;
        }

        public int ResolveSkillId(int actorId, int skillId, IModifierContext context = null)
        {
            return _service.ResolveInt(actorId, MobaSkillParamModifierKeys.Skill.SkillId, skillId, context);
        }
    }
}
