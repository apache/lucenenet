using System;

namespace Lucene.Net
{
    public static class RandomHelpers
    {
        public static int nextInt(this Random random, int maxValue)
        {
            return random.Next(maxValue);
        }

        public static bool nextBoolean(this Random random)
        {
            return (random.Next(1, 100) > 50);
        }
    }
}
