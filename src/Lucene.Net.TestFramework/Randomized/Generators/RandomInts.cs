using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Lucene.Net.Randomized.Generators
{
    public static class RandomInts
    {
        public static int NextIntBetween(this Random random, int min, int max)
        {
            Debug.Assert(min <= max, String.Format("Min must be less than or equal max int. min: {0}, max: {1}", min, max));
            var range = max - min;
            if (range < Int32.MaxValue)
                return min + random.Next(1 + range);

            return min + (int)Math.Round(random.NextDouble() * range);
        }

        public static Boolean NextBoolean(this Random random)
        {
            return random.NextDouble() > 0.5;
        }

        public static float NextFloat(this Random random)
        {
            return (float)random.NextDouble();
        }

        /* .NET has random.Next(max) which negates the need for randomInt(Random random, int max) as  */

        public static long NextLong(this Random random)
        {
            int i1 = random.Next();
            int i2 = random.Next();
            long l12 = ((i1 << 32) | i2);
            return l12;
        }

        public static T RandomFrom<T>(Random rand, ISet<T> set)
        {
            return set.ElementAt(rand.Next(0, set.Count - 1));
        }

        public static T RandomFrom<T>(Random rand, IList<T> set)
        {
            return set.ElementAt(rand.Next(0, set.Count - 1));
        }
    }
}