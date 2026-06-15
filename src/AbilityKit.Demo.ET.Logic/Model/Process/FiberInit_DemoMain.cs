namespace ET.Logic
{
    /// <summary>
    /// Initializes the demo process after the main fiber is created.
    /// </summary>
    [Invoke(SceneType.Main)]
    public class FiberInit_DemoMain: AInvokeHandler<FiberInit, ETTask>
    {
        public override async ETTask Handle(FiberInit fiberInit)
        {
            Scene root = fiberInit.Fiber.Root;
            
            Log.Info($"[Demo] Main fiber initialized");
            
            // Set scene type.
            root.SceneType = SceneType.Main;
            
            // Publish ET entry events.
            await EventSystem.Instance.PublishAsync(root, new EntryEvent1());
            await EventSystem.Instance.PublishAsync(root, new EntryEvent2());
            await EventSystem.Instance.PublishAsync(root, new EntryEvent3());
            
            // Attach demo process component.
            root.AddComponent<DemoProcessComponent>();
            
            // Enter login scene with explicit launch defaults selected by the App layer.
            var processComponent = root.GetComponent<DemoProcessComponent>();
            if (processComponent != null)
            {
                processComponent.LaunchOptions = ETDemoLaunchContext.LaunchOptions;
                await processComponent.ChangeToLoginScene();
            }
            
            Log.Info($"[Demo] Demo process initialized successfully");
        }
    }
}
