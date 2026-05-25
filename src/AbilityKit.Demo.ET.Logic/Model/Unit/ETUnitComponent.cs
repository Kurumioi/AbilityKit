using ET.AbilityKit.Demo.ET.Share;

namespace ET.Logic
{
    /// <summary>
    /// 单位管理器组件
    /// 管理所有 ETUnit 实例
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
