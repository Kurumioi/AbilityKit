using System;

namespace AbilityKit.Samples.Logic.Infrastructure.Config
{
    /// <summary>
    /// 璧勬簮鍔犺浇鍣ㄦ帴鍙?
    /// 鎶借薄璧勬簮閰嶇疆鍔犺浇锛屾敮鎸佷笉鍚屽钩鍙帮紙鏂囦欢绯荤粺銆乁nity Resources銆丄ddressables绛夛級
    /// </summary>
    public interface IResourceProvider
    {
        /// <summary>
        /// 鍔犺浇鏂囨湰璧勬簮
        /// </summary>
        string LoadText(string path);

        /// <summary>
        /// 灏濊瘯鍔犺浇鏂囨湰璧勬簮
        /// </summary>
        bool TryLoadText(string path, out string content);

        /// <summary>
        /// 妫€鏌ヨ祫婧愭槸鍚﹀瓨鍦?
        /// </summary>
        bool Exists(string path);

        /// <summary>
        /// 鑾峰彇璧勬簮璺緞锛堟爣鍑嗗寲锛?
        /// </summary>
        string NormalizePath(string path);
    }
}
