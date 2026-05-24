using System;

namespace ET.Logic
{
    /// <summary>
    /// ET Battle View Feature Component
    ///
    /// Manages view-related features for the battle session
    /// </summary>
    [ComponentOf(typeof(Scene))]
    public class ETBattleViewFeature : Entity, IAwake, IDestroy
    {
        public ETBattleComponent Owner { get; set; }
        public bool IsInitialized { get; set; }

        /// <summary>
        /// On view binder ready callback
        /// </summary>
        public Action<ET.AbilityKit.Demo.ET.Share.ViewBinderReadyEvent> OnViewBinderReady { get; set; }

        /// <summary>
        /// On views rebound callback
        /// </summary>
        public Action<ET.AbilityKit.Demo.ET.Share.ViewsReboundEvent> OnViewsRebound { get; set; }

        /// <summary>
        /// On view frame aligned callback
        /// </summary>
        public Action<ET.AbilityKit.Demo.ET.Share.ViewFrameAlignedEvent> OnViewFrameAligned { get; set; }

        public void Awake()
        {
            IsInitialized = false;
        }

        public void Destroy()
        {
            Owner = null;
            IsInitialized = false;
            OnViewBinderReady = null;
            OnViewsRebound = null;
            OnViewFrameAligned = null;
        }

        public void OnAttach(ETBattleComponent owner)
        {
            Owner = owner;
        }

        public void Initialize()
        {
            IsInitialized = true;
        }
    }
}
