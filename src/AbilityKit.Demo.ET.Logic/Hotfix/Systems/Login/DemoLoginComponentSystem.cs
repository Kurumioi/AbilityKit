namespace ET.Logic
{
    /// <summary>
    /// 登录组件 System
    /// </summary>
    [EntitySystemOf(typeof(DemoLoginComponent))]
    [FriendOf(typeof(DemoLoginComponent))]
    public static partial class DemoLoginComponentSystem
    {
        [EntitySystem]
        private static void Awake(this DemoLoginComponent self)
        {
            Log.Info($"[DemoLogin] DemoLoginComponent awake, EntityId={self.Id}, Scene={self.Scene().Name}");

        }

        /// <summary>
        /// 开始登录
        /// </summary>
        public static void StartLogin(this DemoLoginComponent self, string playerName)
        {
            if (self.State != LoginState.Idle)
            {
                Log.Info($"[DemoLogin] Already logging in, current state: {self.State}");
                return;
            }

            self.State = LoginState.Connecting;
            Log.Info($"[DemoLogin] Connecting to server... player: {playerName}");

            // 登录成功
            self.OnLoginSuccess(playerName);
        }

        /// <summary>
        /// 登录成功回调
        /// </summary>
        private static void OnLoginSuccess(this DemoLoginComponent self, string playerName)
        {
            self.State = LoginState.LoginSuccess;
            self.PlayerId = IdGenerater.Instance.GenerateId();
            self.PlayerName = playerName;

            Log.Info($"[DemoLogin] Login success! PlayerId: {self.PlayerId}, Name: {self.PlayerName}");

            // 发布登录完成事件
            EventSystem.Instance.Publish<Scene, DemoLoginFinish>(self.Scene(), new DemoLoginFinish()
            {
                PlayerId = self.PlayerId,
                PlayerName = self.PlayerName
            });
        }

        /// <summary>
        /// 请求进入战斗
        /// </summary>
        public static void RequestEnterBattle(this DemoLoginComponent self)
        {
            if (self.State != LoginState.LoginSuccess)
            {
                Log.Info($"[DemoLogin] Cannot enter battle, not logged in");
                return;
            }

            Log.Info($"[DemoLogin] Requesting to enter battle...");

            // 发布进入战斗请求事件
            EventSystem.Instance.Publish<Scene, DemoRequestEnterBattle>(self.Scene(), new DemoRequestEnterBattle()
            {
                PlayerId = self.PlayerId,
                PlayerName = self.PlayerName
            });
        }
    }
}
