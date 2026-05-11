using System;
using System.IO;

namespace AbilityKit.Samples.Logic.Infrastructure.Config
{
    /// <summary>
    /// 鏂囦欢绯荤粺璧勬簮鍔犺浇鍣紙鐢ㄤ簬鐙珛绋嬪簭/缂栬緫鍣級
    /// </summary>
    public sealed class FileSystemResourceProvider : IResourceProvider
    {
        private readonly string _baseDirectory;

        public FileSystemResourceProvider() : this(AppDomain.CurrentDomain.BaseDirectory)
        {
        }

        public FileSystemResourceProvider(string baseDirectory)
        {
            _baseDirectory = Path.GetFullPath(baseDirectory);
        }

        public string LoadText(string path)
        {
            var fullPath = GetFullPath(path);
            if (!File.Exists(fullPath))
            {
                throw new FileNotFoundException($"Resource not found: {path}", fullPath);
            }
            return File.ReadAllText(fullPath);
        }

        public bool TryLoadText(string path, out string content)
        {
            try
            {
                var fullPath = GetFullPath(path);
                if (File.Exists(fullPath))
                {
                    content = File.ReadAllText(fullPath);
                    return true;
                }
            }
            catch
            {
                // 蹇界暐寮傚父
            }
            content = null;
            return false;
        }

        public bool Exists(string path)
        {
            var fullPath = GetFullPath(path);
            return File.Exists(fullPath);
        }

        public string NormalizePath(string path)
        {
            return path.Replace('\\', '/');
        }

        private string GetFullPath(string path)
        {
            if (Path.IsPathRooted(path))
            {
                return path;
            }
            return Path.Combine(_baseDirectory, path);
        }
    }
}
