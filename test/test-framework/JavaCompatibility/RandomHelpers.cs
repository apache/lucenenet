using System;

namespace Lucene.Net.JavaCompatibility
{
    public static class RandomHelpers
    {
        public static int nextInt(this Random random, int maxValue)
        {
            return random.Next(maxValue);
        }
    }
}
