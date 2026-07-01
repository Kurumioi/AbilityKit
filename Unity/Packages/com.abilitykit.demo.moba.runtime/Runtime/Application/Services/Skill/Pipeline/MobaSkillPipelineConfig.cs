using System;
using System.Collections.Generic;
using AbilityKit.Pipeline;

namespace AbilityKit.Demo.Moba.Services
{
    public sealed class MobaSkillPipelineConfig : IAbilityPipelineConfig
    {
        private readonly IAbilityPipelineConfig _inner;

        public MobaSkillPipelineConfig(IAbilityPipelineConfig inner, int pipelineContinuousTagTemplateId)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            PipelineContinuousTagTemplateId = pipelineContinuousTagTemplateId > 0 ? pipelineContinuousTagTemplateId : 0;
        }

        public int ConfigId => _inner.ConfigId;
        public string ConfigName => _inner.ConfigName;
        public IReadOnlyList<IAbilityPhaseConfig> PhaseConfigs => _inner.PhaseConfigs;
        public bool AllowInterrupt => _inner.AllowInterrupt;
        public bool AllowPause => _inner.AllowPause;
        public int PipelineContinuousTagTemplateId { get; }
        public bool HasPipelineContinuousTagTemplate => PipelineContinuousTagTemplateId > 0;
    }
}
