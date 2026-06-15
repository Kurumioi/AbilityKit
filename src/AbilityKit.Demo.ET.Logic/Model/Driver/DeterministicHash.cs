namespace ET.Logic
{
    /// <summary>
    /// Deterministic hash utility for cross-platform frame-sync identifiers.
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
}
