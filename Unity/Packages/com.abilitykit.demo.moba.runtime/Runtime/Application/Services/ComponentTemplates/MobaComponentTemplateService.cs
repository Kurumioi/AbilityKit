using System;
using AbilityKit.Ability.FrameSync;
using AbilityKit.Demo.Moba.Config.Core;
using AbilityKit.Ability.World.Services;
using AbilityKit.Ability.World.Services.Attributes;

namespace AbilityKit.Demo.Moba.Services
{
    [WorldService(typeof(MobaComponentTemplateService))]
    public sealed class MobaComponentTemplateService : IService
    {
        [WorldInject] private MobaConfigDatabase _config;
        [WorldInject(required: false)] private IFrameTime _frameTime;
        [WorldInject(required: false)] private IWorldClock _clock;

        public bool TryApply(global::ActorEntity entity, int templateId)
        {
            if (entity == null) return false;
            if (templateId <= 0) return false;
            if (_config == null) return false;

            if (!_config.TryGetComponentTemplate(templateId, out var template) || template == null) return false;
            if (template.Ops == null || template.Ops.Count == 0) return true;

            for (int i = 0; i < template.Ops.Count; i++)
            {
                var op = template.Ops[i];
                if (op == null) continue;
                ApplyOp(entity, op.Kind, op.IntValue, op.FloatValue, op.BoolValue);
            }

            return true;
        }

        private void ApplyOp(global::ActorEntity entity, int kind, int intValue, float floatValue, bool boolValue)
        {
            switch ((MobaComponentOpKind)kind)
            {
                case MobaComponentOpKind.SetModelId:
                {
                    if (intValue > 0)
                    {
                        if (entity.hasModelId) entity.ReplaceModelId(intValue);
                        else entity.AddModelId(intValue);
                    }
                    break;
                }
                case MobaComponentOpKind.SetLifetimeMs:
                {
                    if (intValue > 0)
                    {
                        var endMs = NowMs() + intValue;
                        if (entity.hasLifetime) entity.ReplaceLifetime(endMs);
                        else entity.AddLifetime(endMs);
                    }
                    break;
                }
                default:
                    break;
            }
        }

        private long NowMs()
        {
            if (_frameTime != null)
            {
                return (long)MathF.Round(_frameTime.Time * 1000f);
            }
            if (_clock != null)
            {
                return (long)MathF.Round(_clock.Time * 1000f);
            }
            return 0L;
        }

        public void Dispose()
        {
        }
    }
}
