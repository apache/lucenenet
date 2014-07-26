//-----------------------------------------------------------------------
// <copyright file="HashHelpers.cs" company="Microsoft">
//     Copyright (c) Microsoft. All rights reserved.
//     Internal use only.
// </copyright>
//-----------------------------------------------------------------------

using System;

namespace Lucene.Net.Support
{
    public static class HashHelpers
    {
        public static int CombineHashCodes(int h1, int h2)
        {
            return ((h1 << 5) + h1) ^ h2;
        }

        public static int CombineHashCodes(int h1, int h2, int h3)
        {
            return HashHelpers.CombineHashCodes(h1, HashHelpers.CombineHashCodes(h2, h3));
        }

        public static int CombineHashCodes(int h1, int h2, int h3, int h4)
        {
            return HashHelpers.CombineHashCodes(
                HashHelpers.CombineHashCodes(h1, h2),
                HashHelpers.CombineHashCodes(h3, h4)
                );
        }

        public static int CombineHashCodes(int h1, int h2, int h3, int h4, int h5)
        {
            return HashHelpers.CombineHashCodes(
                HashHelpers.CombineHashCodes(h1, h2),
                HashHelpers.CombineHashCodes(h3, h4),
                h5
                );
        }

        private static uint[] crc32Table;

        public static int GetCRC32HashCode(this byte[] buffer, int offset, int count)
        {
            if (null == buffer)
            {
                throw new ArgumentNullException("buffer");
            }

            if ((offset + count) > buffer.Length)
            {
                count = (buffer.Length - offset);
            }

            if (null == HashHelpers.crc32Table)
            {
                uint[] localCrc32Table = new uint[256];

                for (uint i = 0; i < 256; i++)
                {
                    uint c = i;
                    for (int k = 0; k < 8; k++)
                    {
                        if (0 != (c & 1))
                        {
                            c = 0xedb88320 ^ (c >> 1);
                        }
                        else
                        {
                            c = c >> 1;
                        }
                    }

                    localCrc32Table[i] = c;
                }

                HashHelpers.crc32Table = localCrc32Table;
            }

            uint crc32Value = uint.MaxValue;
            for (int i = offset, max = offset + count; i < max; i++)
            {
                byte index = (byte)(crc32Value ^ buffer[i]);
                crc32Value = HashHelpers.crc32Table[index] ^ ((crc32Value >> 8) & 0xffffff);
            }

            return (int)crc32Value;
        }
    }
}