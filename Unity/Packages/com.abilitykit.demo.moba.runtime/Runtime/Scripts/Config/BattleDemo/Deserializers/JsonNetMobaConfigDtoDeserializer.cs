using System;
using System.Collections.Generic;
using AbilityKit.Ability.Config;
using AbilityKit.Demo.Moba.Config.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using AbilityKit.Demo.Moba.Share.Config;

namespace AbilityKit.Demo.Moba.Config.BattleDemo
{
    public sealed class JsonNetMobaConfigDtoDeserializer : IMobaConfigDtoDeserializer
    {
        public static readonly JsonNetMobaConfigDtoDeserializer Instance = new JsonNetMobaConfigDtoDeserializer();

        private static readonly HashSet<Type> SupportedTypes = new HashSet<Type>
        {
            typeof(CharacterDTO),
            typeof(SkillDTO),
            typeof(SkillButtonTemplateDTO),
            typeof(TagTemplateDTO),
            typeof(SearchQueryTemplateDTO),
            typeof(PassiveSkillDTO),
            typeof(SkillFlowDTO),
            typeof(SkillLevelTableDTO),
            typeof(BattleAttributeTemplateDTO),
            typeof(AttrTypeDTO),
            typeof(ModelDTO),
            typeof(BuffDTO),
            typeof(ProjectileLauncherDTO),
            typeof(ProjectileDTO),
            typeof(AoeDTO),
            typeof(EmitterDTO),
            typeof(SummonDTO),
            typeof(SpawnSummonActionTemplateDTO),
            typeof(ComponentTemplateDTO),
            typeof(OngoingEffectDTO),
            typeof(PresentationTemplateDTO),
        };

        private JsonNetMobaConfigDtoDeserializer() { }

        /// <summary>
        /// MOBA 涓撶敤锛氫粠 JSON 鏂囨湰鍙嶅簭鍒楀寲 DTO 鏁扮粍
        /// </summary>
        public Array DeserializeDtoArray(string text, Type dtoType)
        {
            if (dtoType == null) throw new ArgumentNullException(nameof(dtoType));
            if (string.IsNullOrEmpty(text)) return Array.CreateInstance(dtoType, 0);

            var token = JToken.Parse(text);
            if (token is not JArray array) return Array.CreateInstance(dtoType, 0);

            var list = new List<object>();
            foreach (var item in array)
            {
                var obj = item.ToObject(dtoType);
                if (obj != null)
                {
                    list.Add(obj);
                }
            }

            var result = Array.CreateInstance(dtoType, list.Count);
            for (int i = 0; i < list.Count; i++)
            {
                result.SetValue(list[i], i);
            }
            return result;
        }

        /// <summary>
        /// 閫氱敤 IConfigDeserializer 瀹炵幇
        /// </summary>
        public Array DeserializeBytes(byte[] bytes, Type targetType)
        {
            throw new NotSupportedException(
                $"[{nameof(JsonNetMobaConfigDtoDeserializer)}] Bytes deserialization not supported for: {targetType.FullName}");
        }

        public Array DeserializeText(string text, Type targetType)
        {
            if (string.IsNullOrEmpty(text)) return Array.CreateInstance(targetType, 0);
            return DeserializeDtoArray(text, targetType);
        }

        public bool CanHandle(Type targetType)
        {
            return SupportedTypes.Contains(targetType);
        }
    }
}
