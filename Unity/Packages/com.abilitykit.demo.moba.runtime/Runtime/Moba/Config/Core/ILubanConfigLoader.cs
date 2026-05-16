using System;
using AbilityKit.Ability.Config;

namespace AbilityKit.Demo.Moba.Config.Core
{
    /// <summary>
    /// Luban 配置加载服务接口。
    /// 定义 Luban 配置数据的加载抽象。
    /// </summary>
    public interface ILubanConfigLoader
    {
        /// <summary>
        /// 加载所有 Luban 配置
        /// </summary>
        /// <returns>Luban Tables 实例，包含所有已加载的配置</returns>
        object LoadAll();

        /// <summary>
        /// 加载指定目录的 Luban 配置
        /// </summary>
        /// <param name="resourcesDir">资源目录</param>
        /// <returns>Luban Tables 实例</returns>
        object LoadAll(string resourcesDir);

        /// <summary>
        /// 获取指定类型的配置表
        /// </summary>
        T GetTable<T>() where T : class;

        /// <summary>
        /// 获取字符配置表
        /// </summary>
        object GetCharacters();

        /// <summary>
        /// 获取属性模板配置表
        /// </summary>
        object GetAttributeTemplates();

        /// <summary>
        /// 获取 Buff 配置表
        /// </summary>
        object GetBuffs();

        /// <summary>
        /// 获取技能配置表
        /// </summary>
        object GetSkills();

        /// <summary>
        /// 获取弹道配置表
        /// </summary>
        object GetProjectiles();
    }
}
