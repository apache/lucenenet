//-----------------------------------------------------------------------
// <copyright file="HashHelpers.cs" company="Microsoft">
//     Copyright (c) Microsoft. All rights reserved.
//     Internal use only.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections;

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

        /// <summary>
        /// Gets a hash code for the valueOrEnumerable. If the valueOrEnumerable implements
        /// IEnumerable, it enumerates the values and makes a combined hash code representing
        /// all of the values in the order they occur in the set. The types of IEnumerable must also be
        /// the same, so for example a <see cref="int[]"/> and a <see cref="List{Int32}"/> containing
        /// the same values will have different hash codes.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="valueOrEnumerable">Any value type, reference type or IEnumerable type.</param>
        /// <returns>A combined hash code of the value and, if IEnumerable, any values it contains.</returns>
        public static int GetValueHashCode<T>(this T valueOrEnumerable)
        {
            if (valueOrEnumerable == null)
                return 0; // 0 for null

            if (!(valueOrEnumerable is IEnumerable) || valueOrEnumerable is string)
            {
                return valueOrEnumerable.GetHashCode();
            }

            int hashCode = valueOrEnumerable.GetType().GetHashCode();
            foreach (object value in valueOrEnumerable as IEnumerable)
            {
                if (value != null)
                {
                    hashCode = CombineHashCodes(hashCode, value.GetHashCode());
                }
                else
                {
                    hashCode = CombineHashCodes(hashCode, 0 /* 0 for null */);
                }
            }

            return hashCode;
        }
    }
}