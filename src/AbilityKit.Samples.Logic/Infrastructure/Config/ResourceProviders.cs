using System;

namespace AbilityKit.Samples.Logic.Infrastructure.Config
{
    /// <summary>
    /// 璧勬簮閰嶇疆鎻愪緵鑰呭叏灞€璁块棶鍣?
    /// 閫氳繃渚濊禆娉ㄥ叆/Setter娉ㄥ叆鏉ュ垏鎹笉鍚岀殑 IResourceProvider 瀹炵幇
    /// </summary>
    public static class ResourceProviders
    {
        private static IResourceProvider _current;

        /// <summary>
        /// 褰撳墠璧勬簮閰嶇疆鎻愪緵鑰?
        /// </summary>
        public static IResourceProvider Current
        {
            get => _current ??= CreateDefault();
            set => _current = value;
        }

        /// <summary>
        /// 鍒涘缓榛樿鐨勮祫婧愭彁渚涜€?
        /// </summary>
        public static IResourceProvider CreateDefault()
        {
            // 鍦ㄨ繍琛屾椂鑷姩妫€娴嬬幆澧?
            // TODO: 鍚庣画鍙互閫氳繃鐜鍙橀噺鎴栧惎鍔ㄥ弬鏁板垏鎹?
            return new FileSystemResourceProvider();
        }

        /// <summary>
        /// 璁剧疆璧勬簮鎻愪緵鑰?
        /// </summary>
        /// <typeparam name="T">璧勬簮鎻愪緵鑰呯被鍨?/typeparam>
        public static void Set<T>() where T : IResourceProvider, new()
        {
            _current = new T();
        }

        /// <summary>
        /// 閲嶇疆涓洪粯璁よ祫婧愭彁渚涜€?
        /// </summary>
        public static void Reset()
        {
            _current = CreateDefault();
        }
    }
}
