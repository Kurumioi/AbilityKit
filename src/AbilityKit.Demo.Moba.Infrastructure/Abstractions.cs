// MOBA Demo 的基础设施抽象。
// 该项目提供可由不同平台实现的抽象（Unity、Console 等）。

namespace AbilityKit.Demo.Moba.Infrastructure
{
    /// <summary>
    /// 平台无关日志接口。
    /// </summary>
    public interface ILogger
    {
        void Info(string message);
        void Warning(string message);
        void Error(string message);
        void Exception(Exception exception, string message = null);
    }

    /// <summary>
    /// 文本资源加载接口。
    /// </summary>
    public interface ITextResourceProvider
    {
        bool TryGetText(string path, out string text);
    }

    /// <summary>
    /// 二进制资源加载接口。
    /// </summary>
    public interface IBinaryResourceProvider
    {
        bool TryGetBytes(string path, out byte[] bytes);
    }

    /// <summary>
    /// 用于加载配置数据的配置源接口。
    /// </summary>
    public interface IConfigSource
    {
        bool TryGetText(string path, out string text);
        bool TryGetBytes(string path, out byte[] bytes);
    }

    /// <summary>
    /// 简单配置加载接口。
    /// </summary>
    public interface IConfigLoader
    {
        T Load<T>(string path) where T : class;
        bool TryLoad<T>(string path, out T config) where T : class;
    }
}
