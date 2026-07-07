using System;

namespace AbilityKit.Coordinator.Core
{
    /// <summary>
    /// 会话钩子。
    ///
    /// 提供会话生命周期事件的回调钩子。
    /// SubFeature 可订阅这些钩子完成协作。
    /// </summary>
    public class SessionHooks
    {
        // ============== 会话生命周期钩子 ==============

        /// <summary>
        /// 会话启动前调用。
        /// </summary>
        public Action<SessionConfig> OnSessionStarting;

        /// <summary>
        /// 会话启动时调用。
        /// </summary>
        public Action<SessionConfig> OnSessionStarted;

        /// <summary>
        /// 会话开始停止时调用。
        /// </summary>
        public Action OnSessionStopping;

        /// <summary>
        /// 会话停止完成时调用。
        /// </summary>
        public Action OnSessionStopped;

        /// <summary>
        /// 会话遇到错误时调用。
        /// </summary>
        public Action<Exception> OnSessionFailed;

        // ============== 帧同步钩子 ==============

        /// <summary>
        /// 每帧 Tick 之前调用。
        /// </summary>
        public Action<float> OnPreTick;

        /// <summary>
        /// 每帧 Tick 之后调用。
        /// </summary>
        public Action<float> OnPostTick;

        /// <summary>
        /// 收到第一帧时调用。
        /// </summary>
        public Action OnFirstFrameReceived;

        // ============== 视图钩子 ==============

        /// <summary>
        /// 视图绑定器就绪时调用。
        /// </summary>
        public Action<int> OnViewBinderReady;

        /// <summary>
        /// 视图重新绑定时调用。
        /// </summary>
        public Action OnViewsRebound;

        /// <summary>
        /// 帧对齐到渲染时调用。
        /// </summary>
        public Action<int> OnViewFrameAligned;

        // ============== 辅助方法 ==============

        /// <summary>
        /// 触发会话即将启动钩子。
        /// </summary>
        public void InvokeSessionStarting(SessionConfig config)
        {
            OnSessionStarting?.Invoke(config);
        }

        /// <summary>
        /// 触发会话已启动钩子。
        /// </summary>
        public void InvokeSessionStarted(SessionConfig config)
        {
            OnSessionStarted?.Invoke(config);
        }

        /// <summary>
        /// 触发会话即将停止钩子。
        /// </summary>
        public void InvokeSessionStopping()
        {
            OnSessionStopping?.Invoke();
        }

        /// <summary>
        /// 触发会话已停止钩子。
        /// </summary>
        public void InvokeSessionStopped()
        {
            OnSessionStopped?.Invoke();
        }

        /// <summary>
        /// 触发会话失败钩子。
        /// </summary>
        public void InvokeSessionFailed(Exception ex)
        {
            OnSessionFailed?.Invoke(ex);
        }

        /// <summary>
        /// 触发 PreTick 钩子。
        /// </summary>
        public void InvokePreTick(float deltaTime)
        {
            OnPreTick?.Invoke(deltaTime);
        }

        /// <summary>
        /// 触发 PostTick 钩子。
        /// </summary>
        public void InvokePostTick(float deltaTime)
        {
            OnPostTick?.Invoke(deltaTime);
        }

        /// <summary>
        /// 触发收到第一帧钩子。
        /// </summary>
        public void InvokeFirstFrameReceived()
        {
            OnFirstFrameReceived?.Invoke();
        }

        /// <summary>
        /// 触发视图绑定器就绪钩子。
        /// </summary>
        public void InvokeViewBinderReady(int frame)
        {
            OnViewBinderReady?.Invoke(frame);
        }

        /// <summary>
        /// 触发视图重新绑定钩子。
        /// </summary>
        public void InvokeViewsRebound()
        {
            OnViewsRebound?.Invoke();
        }

        /// <summary>
        /// 触发视图帧对齐钩子。
        /// </summary>
        public void InvokeViewFrameAligned(int frame)
        {
            OnViewFrameAligned?.Invoke(frame);
        }

        /// <summary>
        /// 清理所有钩子。
        /// </summary>
        public void Clear()
        {
            OnSessionStarting = null;
            OnSessionStarted = null;
            OnSessionStopping = null;
            OnSessionStopped = null;
            OnSessionFailed = null;
            OnPreTick = null;
            OnPostTick = null;
            OnFirstFrameReceived = null;
            OnViewBinderReady = null;
            OnViewsRebound = null;
            OnViewFrameAligned = null;
        }
    }
}
