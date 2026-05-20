namespace AbilityKit.Demo.Moba.Console.Battle.Config
{

/// <summary>
/// 确定性哈希工具
/// 用于跨平台生成一致的哈希值（帧同步必需）
/// </summary>
public static class DeterministicHash
{
    /// <summary>
    /// 将字符串转换为 ActorId
    /// 使用 CRC32 保证跨平台一致性
    /// </summary>
    public static int StringToActorId(string input)
    {
        if (string.IsNullOrEmpty(input))
            return 0;

        var hash = Crc32.Hash(input);
        return (int)(hash & 0xFFFF);
    }

    /// <summary>
    /// 简单的 CRC32 实现
    /// </summary>
    private static class Crc32
    {
        private static readonly uint[] Table;

        static Crc32()
        {
            Table = new uint[256];
            const uint polynomial = 0xEDB88320;

            for (uint i = 0; i < 256; i++)
            {
                uint crc = i;
                for (int j = 0; j < 8; j++)
                {
                    crc = (crc & 1) != 0 ? (crc >> 1) ^ polynomial : crc >> 1;
                }
                Table[i] = crc;
            }
        }

        public static uint Hash(string input)
        {
            uint crc = 0xFFFFFFFF;

            foreach (char c in input)
            {
                byte b = (byte)c;
                crc = (crc >> 8) ^ Table[(crc ^ b) & 0xFF];
            }

            return crc ^ 0xFFFFFFFF;
        }
    }
}

/// <summary>
/// 网络连接配置
/// </summary>
public class NetworkConfig
{
    /// <summary>
    /// 服务器地址
    /// </summary>
    public string Host { get; set; } = "localhost";

    /// <summary>
    /// 服务器端口
    /// </summary>
    public int Port { get; set; } = 4000;

    /// <summary>
    /// 重连间隔（毫秒）
    /// </summary>
    public int ReconnectIntervalMs { get; set; } = 3000;

    /// <summary>
    /// 最大重连次数
    /// </summary>
    public int MaxReconnectAttempts { get; set; } = 5;

    /// <summary>
    /// 心跳间隔（毫秒）
    /// </summary>
    public int HeartbeatIntervalMs { get; set; } = 5000;

    /// <summary>
    /// 心跳超时（毫秒）
    /// </summary>
    public int HeartbeatTimeoutMs { get; set; } = 15000;
}
}
