// Infrastructure abstractions for MOBA demo
// This project provides abstractions that can be implemented for different platforms (Unity, Console, etc.)

namespace AbilityKit.Demo.Moba.Infrastructure
{
    /// <summary>
    /// Logger interface for platform-agnostic logging
    /// </summary>
    public interface ILogger
    {
        void Info(string message);
        void Warning(string message);
        void Error(string message);
        void Exception(Exception exception, string message = null);
    }

    /// <summary>
    /// Text resource loader interface
    /// </summary>
    public interface ITextResourceProvider
    {
        bool TryGetText(string path, out string text);
    }

    /// <summary>
    /// Binary resource loader interface
    /// </summary>
    public interface IBinaryResourceProvider
    {
        bool TryGetBytes(string path, out byte[] bytes);
    }

    /// <summary>
    /// Config source interface for loading configuration data
    /// </summary>
    public interface IConfigSource
    {
        bool TryGetText(string path, out string text);
        bool TryGetBytes(string path, out byte[] bytes);
    }

    /// <summary>
    /// Simple config loader interface
    /// </summary>
    public interface IConfigLoader
    {
        T Load<T>(string path) where T : class;
        bool TryLoad<T>(string path, out T config) where T : class;
    }
}
