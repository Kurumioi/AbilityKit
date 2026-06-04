using AbilityKit.Modifiers;

namespace AbilityKit.Demo.Moba.Services
{
    public sealed class MobaSummonParamGroupResolver
    {
        private readonly MobaSkillParamModifierService _service;

        internal MobaSummonParamGroupResolver(MobaSkillParamModifierService service)
        {
            _service = service;
        }

        public int ResolveSummonId(int actorId, int summonId, IModifierContext context = null)
        {
            return _service.ResolveInt(actorId, MobaSkillParamModifierKeys.Summon.SummonId, summonId, context);
        }
    }
}
