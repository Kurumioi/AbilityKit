namespace AbilityKit.Demo.Moba.Console.Platform
{
    /// <summary>
    /// 日志通道
    /// </summary>
    public enum OutputChannel
    {
        System,
        Phase,
        View,
        Sync,
        Input,
        Battle,
        Skill,
        Damage,
        Buff,
        Projectile,
        Area,
        Entity,
        Config,
        Debug,
        Warning,
        Error,
        /// <summary>
        /// 追踪日志 - 用于排查流程完整性问题
        /// </summary>
        Trace
    }

    /// <summary>
    /// 输入按键枚举
    /// </summary>
    public enum InputKey
    {
        None = 0,
        Up,
        Down,
        Left,
        Right,
        Skill1,
        Skill2,
        Skill3,
        Skill4,
        Attack,
        Help,
        Quit,
        Confirm,
        Cancel,
        Menu,
        Pause
    }

    /// <summary>
    /// 输入事件
    /// </summary>
    public readonly struct InputEvent
    {
        public InputKey Key { get; init; }
        public bool IsPressed { get; init; }
        public bool IsHeld { get; init; }
        public bool IsReleased { get; init; }
    }

    /// <summary>
    /// 平台无关的输出接口
    /// 可用于 Console, MonoGame, Godot, Unity 等平台
    /// </summary>
    public interface IOutput
    {
        /// <summary>
        /// 输出消息
        /// </summary>
        void Write(OutputChannel channel, string message);

        /// <summary>
        /// 输出格式化消息
        /// </summary>
        void WriteFormat(OutputChannel channel, string format, params object[] args);

        /// <summary>
        /// 清空输出
        /// </summary>
        void Clear();

        /// <summary>
        /// 输出分隔线
        /// </summary>
        void WriteSeparator(OutputChannel channel = OutputChannel.System, char c = '=', int length = 60);

        /// <summary>
        /// 输出标题
        /// </summary>
        void WriteTitle(OutputChannel channel, string title, char borderChar = '=', int width = 60);
    }

    /// <summary>
    /// 渲染器接口
    /// </summary>
    public interface IRenderer
    {
        /// <summary>
        /// 渲染器宽度
        /// </summary>
        int Width { get; }

        /// <summary>
        /// 渲染器高度
        /// </summary>
        int Height { get; }

        /// <summary>
        /// 清空屏幕
        /// </summary>
        void Clear();

        /// <summary>
        /// 绘制文字
        /// </summary>
        void DrawText(int x, int y, string text);

        /// <summary>
        /// 绘制矩形边框
        /// </summary>
        void DrawRect(int x, int y, int width, int height);

        /// <summary>
        /// 绘制线
        /// </summary>
        void DrawLine(int x1, int y1, int x2, int y2);

        /// <summary>
        /// 刷新显示
        /// </summary>
        void Present();

        /// <summary>
        /// 将世界坐标转换为屏幕坐标
        /// </summary>
        (int px, int py) WorldToScreen(float worldX, float worldZ);
    }

    /// <summary>
    /// 输入接口
    /// </summary>
    public interface IInputSource
    {
        /// <summary>
        /// 是否有按键可用
        /// </summary>
        bool HasInputAvailable();

        /// <summary>
        /// 获取下一个按键
        /// </summary>
        bool TryReadKey(out InputKey key);

        /// <summary>
        /// 获取移动输入
        /// </summary>
        (float dx, float dz) GetMoveInput();

        /// <summary>
        /// 检查按键是否按下
        /// </summary>
        bool IsKeyDown(InputKey key);
    }
}
