using System;
using ET.AbilityKit.Demo.ET.Share;

namespace ET.AbilityKit.Demo.ET.View
{
    /// <summary>
    /// ETViewEventListener System
    /// Handles view event listener logic
    /// </summary>
    [EntitySystemOf(typeof(ETViewEventListener))]
    [FriendOf(typeof(ETViewEventListener))]
    public static partial class ETViewEventListenerSystem
    {
        [EntitySystem]
        private static void Awake(this ETViewEventListener self)
        {
            Log.Info("[ETView] ViewEventListener system awake");
        }
    }
}
