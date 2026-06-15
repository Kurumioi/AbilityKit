namespace ET.Logic
{
    /// <summary>
    /// Demo process coordinator.
    /// Owns scene transitions and explicit launch options for the ET MOBA demo.
    /// </summary>
    [ComponentOf(typeof(Scene))]
    public class DemoProcessComponent: Entity, IAwake, IUpdate
    {
        public Scene CurrentScene { get; set; }
        public DemoLoginComponent LoginComponent { get; set; }
        public ETDemoProcessLaunchOptions LaunchOptions { get; set; }

        public void Awake()
        {
        }

        public void Update(DemoProcessComponent self)
        {
        }
    }
}
