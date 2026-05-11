namespace AbilityKit.Samples.Common
{
    /// <summary>
    /// 日志器接口
    /// </summary>
    public interface ILogger
    {
        void Info(string message);
        void Warn(string message);
        void Error(string message);
        void Section(string title);
        void Line();
        void Divider();
        void Bullet(string text);
        void Numbered(int num, string text);
        void KeyValue(string key, string value);
        void Flush();
    }
}
