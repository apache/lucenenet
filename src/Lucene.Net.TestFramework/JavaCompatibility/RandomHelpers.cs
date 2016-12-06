using System;

namespace Lucene.Net
{
    public static class RandomHelpers
    {
        public static int nextInt(this Random random, int maxValue)
        {
            return random.Next(maxValue);
        }

        public static int nextInt(this Random random)
        {
            return random.Next();
        }

        public static bool nextBoolean(this Random random)
        {
            return (random.Next(1, 100) > 50);
        }

        // http://stackoverflow.com/a/6651656
        public static long nextLong(this Random random)
        {
            byte[] buffer = new byte[8];
            random.NextBytes(buffer);
            return BitConverter.ToInt64(buffer, 0);
        }

        public static float nextFloat(this Random random)
        {
            return (float)random.NextDouble();
        }
    }
}
