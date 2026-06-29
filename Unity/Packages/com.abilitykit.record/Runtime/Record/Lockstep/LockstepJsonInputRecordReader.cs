using System;
using System.IO;
using Newtonsoft.Json;

namespace AbilityKit.Core.Recording.Lockstep
{
    public static class LockstepJsonInputRecordReader
    {
        public static LockstepInputRecordFile Load(string path)
        {
            var resolved = ResolvePath(path);
            var json = File.ReadAllText(resolved);
            return JsonConvert.DeserializeObject<LockstepInputRecordFile>(json);
        }

        private static string ResolvePath(string path)
        {
            if (string.IsNullOrEmpty(path)) return path;
            if (Path.IsPathRooted(path)) return path;

#if UNITY_2020_3_OR_NEWER
            var baseDir = UnityEngine.Application.persistentDataPath;
            if (string.IsNullOrEmpty(baseDir)) baseDir = UnityEngine.Application.dataPath;
#else
            var baseDir = Environment.CurrentDirectory;
#endif
            return Path.Combine(baseDir, path);
        }
    }
}
