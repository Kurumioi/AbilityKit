using System.Collections.Generic;
using AbilityKit.GameplayTags;

namespace AbilityKit.Samples.Logic.Infrastructure.Config
{
    /// <summary>
    /// 寮虹被鍨嬮厤缃闂櫒
    /// </summary>
    public static class SampleConfig
    {
        private static readonly SampleConfigLoader _loader = SampleConfigLoader.Instance;

        /// <summary>
        /// 鍔犺浇绠＄嚎閰嶇疆
        /// </summary>
        public static List<T> LoadPipelines<T>() where T : class
        {
            var provider = _loader.Load(ConfigPaths.PipelineConfig);
            var section = provider as dynamic;
            return section?.GetSectionOrDefault<List<T>>("pipelineConfigs") ?? new List<T>();
        }

        /// <summary>
        /// 鍔犺浇鏍囩缁勯厤缃?        /// </summary>
        public static List<TagGroupConfig> LoadTagGroups()
        {
            var provider = _loader.Load(ConfigPaths.TagsConfig);
            return provider.GetSection<List<TagGroupConfig>>(ConfigSections.TagGroups);
        }

        /// <summary>
        /// 鍔犺浇鏍囩缁勫苟娉ㄥ唽鍒?GameplayTagManager
        /// </summary>
        public static List<GameplayTag> LoadAndRegisterTags()
        {
            var groups = LoadTagGroups();
            var registeredTags = new List<GameplayTag>();

            foreach (var group in groups)
            {
                foreach (var tagName in group.Tags)
                {
                    var tag = GameplayTagManager.Instance.RequestTag(tagName);
                    registeredTags.Add(tag);
                }
            }

            return registeredTags;
        }

        /// <summary>
        /// 鍔犺浇瑙掕壊鏍囩閰嶇疆
        /// </summary>
        public static List<T> LoadCharacterTags<T>() where T : class
        {
            var provider = _loader.Load(ConfigPaths.SampleConfigs);
            var section = provider as dynamic;
            return section?.GetSectionOrDefault<List<T>>(ConfigSections.CharacterTags) ?? new List<T>();
        }

        /// <summary>
        /// 閫氱敤閰嶇疆鍔犺浇鏂规硶
        /// </summary>
        public static List<T> Load<T>(string filePath, string sectionName) where T : class
        {
            var provider = _loader.Load(filePath);
            var section = provider as dynamic;
            return section?.GetSectionOrDefault<List<T>>(sectionName) ?? new List<T>();
        }
    }
}
