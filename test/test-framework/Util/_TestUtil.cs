using System;
using Lucene.Net.JavaCompatibility;
using Lucene.Net.Randomized.Generators;

namespace Lucene.Net.Util
{
    public class _TestUtil
    {
        internal static void CheckIndex(Store.BaseDirectoryWrapper baseDirectoryWrapper, bool p)
        {
            throw new NotImplementedException();
        }

        public static int nextInt(Random r, int start, int end)
        {
            return RandomInts.randomIntBetween(r, start, end);
        }

        /** Returns random string, including full unicode range. */
        public static String randomUnicodeString(Random r)
        {
            return randomUnicodeString(r, 20);
        }

        /**
         * Returns a random string up to a certain length.
         */
        public static String randomUnicodeString(Random r, int maxLength)
        {
            int end = nextInt(r, 0, maxLength);
            if (end == 0)
            {
                // allow 0 length
                return "";
            }
            char[] buffer = new char[end];
            randomFixedLengthUnicodeString(r, buffer, 0, buffer.Length);
            return new String(buffer, 0, end);
        }


        /**
         * Fills provided char[] with valid random unicode code
         * unit sequence.
         */

        public static void randomFixedLengthUnicodeString(Random random, char[] chars, int offset, int length)
        {
            int i = offset;
            int end = offset + length;
            while (i < end)
            {
                int t = random.nextInt(5);
                if (0 == t && i < length - 1)
                {
                    // Make a surrogate pair
                    // High surrogate
                    chars[i++] = (char)nextInt(random, 0xd800, 0xdbff);
                    // Low surrogate
                    chars[i++] = (char)nextInt(random, 0xdc00, 0xdfff);
                }
                else if (t <= 1)
                {
                    chars[i++] = (char)random.nextInt(0x80);
                }
                else if (2 == t)
                {
                    chars[i++] = (char)nextInt(random, 0x80, 0x7ff);
                }
                else if (3 == t)
                {
                    chars[i++] = (char)nextInt(random, 0x800, 0xd7ff);
                }
                else if (4 == t)
                {
                    chars[i++] = (char)nextInt(random, 0xe000, 0xffff);
                }
            }
        }
    }
}
