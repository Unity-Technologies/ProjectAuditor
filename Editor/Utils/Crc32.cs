namespace Unity.ProjectAuditor.Editor.Utils
{
    public static class Crc32
    {
        static readonly uint[] s_Crc32Table = new uint[16 * 256];

        const uint k_Poly = 0xedb88320u;

        static Crc32()
        {
            for (uint i = 0; i < 256; i++)
            {
                uint res = i;
                for (int t = 0; t < 16; t++)
                {
                    for (int k = 0; k < 8; k++) res = (res & 1) == 1 ? k_Poly ^ (res >> 1) : (res >> 1);
                    s_Crc32Table[(t * 256) + i] = res;
                }
            }
        }

        public static uint Append(uint crc, byte[] buf, int offset, int size)
        {
            uint crcLocal = uint.MaxValue ^ crc;

            while (size >= 16)
            {
                var a = s_Crc32Table[(3 * 256) + buf[offset + 12]]
                        ^ s_Crc32Table[(2 * 256) + buf[offset + 13]]
                        ^ s_Crc32Table[(1 * 256) + buf[offset + 14]]
                        ^ s_Crc32Table[(0 * 256) + buf[offset + 15]];

                var b = s_Crc32Table[(7 * 256) + buf[offset + 8]]
                        ^ s_Crc32Table[(6 * 256) + buf[offset + 9]]
                        ^ s_Crc32Table[(5 * 256) + buf[offset + 10]]
                        ^ s_Crc32Table[(4 * 256) + buf[offset + 11]];

                var c = s_Crc32Table[(11 * 256) + buf[offset + 4]]
                        ^ s_Crc32Table[(10 * 256) + buf[offset + 5]]
                        ^ s_Crc32Table[(9 * 256) + buf[offset + 6]]
                        ^ s_Crc32Table[(8 * 256) + buf[offset + 7]];

                var d = s_Crc32Table[(15 * 256) + ((byte)crcLocal ^ buf[offset])]
                        ^ s_Crc32Table[(14 * 256) + ((byte)(crcLocal >> 8) ^ buf[offset + 1])]
                        ^ s_Crc32Table[(13 * 256) + ((byte)(crcLocal >> 16) ^ buf[offset + 2])]
                        ^ s_Crc32Table[(12 * 256) + ((crcLocal >> 24) ^ buf[offset + 3])];

                crcLocal = d ^ c ^ b ^ a;
                offset += 16;
                size -= 16;
            }

            while (--size >= 0)
                crcLocal = s_Crc32Table[(byte)(crcLocal ^ buf[offset++])] ^ crcLocal >> 8;

            return crcLocal ^ uint.MaxValue;
        }
    }
}
