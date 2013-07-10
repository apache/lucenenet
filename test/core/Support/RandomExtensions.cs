using System;

namespace Lucene.Net.Test.Support
{
    public static class RandomExtensions
    {
        private static bool BoolTieBreak = false;

        public static long NextLong(this Random random)
        {
            return random.NextLong(long.MaxValue);
        }

        public static long NextLong(this Random random, long max)
        {
            return random.NextLong(0, max);
        }

        public static long NextLong(this Random random, long min, long max)
        {
            return (long)((random.NextDouble() * max + min) % max);
        }

        public static bool NextBool(this Random random)
        {
            var randInt = random.Next();
            var adjusted = randInt - (int.MaxValue/2);
            if (adjusted == 0)
            {
                BoolTieBreak = !BoolTieBreak;
                return BoolTieBreak;
            }
            return adjusted > 0 ? true : false;
        }

        public static void NextBytes(this Random random, sbyte[] bytes)
        {
            var length = bytes.Length;
            var randBytes = new byte[length];
            random.NextBytes(randBytes);
            for (var i = 0; i < bytes.Length; i++)
            {
                bytes[i] = (sbyte) randBytes[i];
            }
        }
    }
}
