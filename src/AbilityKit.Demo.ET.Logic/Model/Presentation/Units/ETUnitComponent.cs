using System;
using System.Collections.Generic;
using ET.AbilityKit.Demo.ET.Share;

namespace ET.Logic
{
    /// <summary>
    /// 单位管理器组件
    /// 管理所有 ETUnit 实例
    ///
    /// 设计说明：
    /// - 使用 ET 原生的 AddChild/GetChild 管理子实体
    /// - ETUnit.Id 使用 moba.core 的 ActorId
    /// - 创建: AddChild&lt;ETUnit&gt;((long)actorId)
    /// - 查询: GetChild&lt;ETUnit&gt;(actorId)
    /// </summary>
    [ComponentOf(typeof(Scene))]
    public class ETUnitComponent : Entity, IAwake, IDestroy
    {
        public void Awake()
        {
        }

        public void Destroy()
        {
        }
    }
}
