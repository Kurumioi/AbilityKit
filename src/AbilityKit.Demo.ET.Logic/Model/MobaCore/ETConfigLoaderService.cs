using System;
using System.Collections.Generic;

namespace ET.Logic
{
    /// <summary>
    /// 配置加载服务
    /// 负责从 TextAsset 加载游戏配置（角色、属性模板等）
    /// </summary>
    public sealed class ETConfigLoaderService
    {
        public ETConfigLoaderService()
        {
        }

        /// <summary>
        /// 加载所有配置
        /// </summary>
        public void LoadAll()
        {
        }

        /// <summary>
        /// 获取角色配置
        /// </summary>
        public bool TryGetCharacter(int characterId, out object config)
        {
            config = null;
            return false;
        }

        /// <summary>
        /// 获取属性模板
        /// </summary>
        public Dictionary<int, object> AttributeTemplates => new Dictionary<int, object>();

        /// <summary>
        /// 获取角色列表
        /// </summary>
        public Dictionary<int, object> Characters => new Dictionary<int, object>();
    }
}
