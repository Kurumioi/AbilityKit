namespace AbilityKit.Samples.Abstractions
{
    /// <summary>
    /// 资源加载器接口
    /// </summary>
    public interface IResourceProvider
    {
        /// <summary>
        /// 加载文本资源
        /// </summary>
        string LoadText(string path);

        /// <summary>
        /// 资源是否存在
        /// </summary>
        bool Exists(string path);
    }
}
