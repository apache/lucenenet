using J2N.Numerics;
using System;
using System.Diagnostics;
using System.Numerics;

namespace Lucene.Net.BenchmarkDotNet.Util
{
    /*
     * Licensed to the Apache Software Foundation (ASF) under one or more
     * contributor license agreements.  See the NOTICE file distributed with
     * this work for additional information regarding copyright ownership.
     * The ASF licenses this file to You under the Apache License, Version 2.0
     * (the "License"); you may not use this file except in compliance with
     * the License.  You may obtain a copy of the License at
     *
     *     http://www.apache.org/licenses/LICENSE-2.0
     *
     * Unless required by applicable law or agreed to in writing, software
     * distributed under the License is distributed on an "AS IS" BASIS,
     * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
     * See the License for the specific language governing permissions and
     * limitations under the License.
     */

    /// <summary>
    /// Utility classes for selecting random numbers from within a range or the 
    /// numeric domain for a given type.
    /// </summary>
    /// <seealso cref="BiasedNumbers"/>
    public static class RandomNumbers
    {
        /// <summary>
        /// Returns a random <see cref="int"/> from <paramref name="minValue"/> (inclusive) to <paramref name="maxValue"/> (inclusive).
        /// </summary>
        /// <param name="random">A <see cref="Random"/> instance.</param>
        /// <param name="minValue">The inclusive start of the range.</param>
        /// <param name="maxValue">The inclusive end of the range.</param>
        /// <returns>A random <see cref="int"/> from <paramref name="minValue"/> (inclusive) to <paramref name="maxValue"/> (inclusive).</returns>
        /// <exception cref="ArgumentException"><paramref name="minValue"/> is greater than <paramref name="maxValue"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="random"/> is <c>null</c>.</exception>
        public static int RandomInt32Between(Random random, int minValue, int maxValue)
        {
            if (random is null)
                throw new ArgumentNullException(nameof(random));
            if (minValue > maxValue)
                throw new ArgumentException($"{nameof(minValue)} must be less than or equal to {nameof(maxValue)}. {nameof(minValue)}: {minValue}, {nameof(maxValue)}: {maxValue}");
            var range = maxValue - minValue;
            if (range < int.MaxValue)
                return minValue + random.Next(1 + range);

            return minValue + (int)Math.Round(random.NextDouble() * range);
        }

        /// <summary>
        /// Returns a random <see cref="long"/> from <paramref name="minValue"/> to <paramref name="maxValue"/> (inclusive).
        /// </summary>
        /// <param name="random">A <see cref="Random"/> instance.</param>
        /// <param name="minValue">The inclusive start of the range.</param>
        /// <param name="maxValue">The inclusive end of the range.</param>
        /// <returns>A random <see cref="long"/> from <paramref name="minValue"/> to <paramref name="maxValue"/> (inclusive).</returns>
        /// <exception cref="ArgumentException"><paramref name="minValue"/> is greater than <paramref name="maxValue"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="random"/> is <c>null</c>.</exception>
        public static long RandomInt64Between(Random random, long minValue, long maxValue)
        {
            if (random is null)
                throw new ArgumentNullException(nameof(random));
            if (minValue > maxValue)
                throw new ArgumentException($"{nameof(minValue)} must be less than or equal to {nameof(maxValue)}. {nameof(minValue)}: {minValue}, {nameof(maxValue)}: {maxValue}");

            BigInteger range = (BigInteger)maxValue + (BigInteger)1 - (BigInteger)minValue;
            if (range.CompareTo((BigInteger)int.MaxValue) <= 0)
            {
                return minValue + random.Next((int)range);
            }
            else
            {
                // probably not evenly distributed when range is large, but OK for tests
                //BigInteger augend = BigInteger.Multiply(range,  new BigInteger(r.NextDouble()));
                //long result = start + (long)augend;

                // NOTE: Using BigInteger/Decimal doesn't work because r.NextDouble() is always
                // rounded down to 0, which makes the result always the same as start. This alternative solution was
                // snagged from https://stackoverflow.com/a/13095144. All we really care about here is that we get
                // a pretty good random distribution of values between start and end.

                //Working with ulong so that modulo works correctly with values > long.MaxValue
                ulong uRange = (ulong)unchecked(maxValue - minValue);

                //Prevent a modolo bias; see https://stackoverflow.com/a/10984975/238419
                //for more information.
                //In the worst case, the expected number of calls is 2 (though usually it's
                //much closer to 1) so this loop doesn't really hurt performance at all.
                ulong ulongRand;
                do
                {
                    byte[] buf = new byte[8];
                    random.NextBytes(buf);
                    ulongRand = (ulong)BitConverter.ToInt64(buf, 0);
                } while (ulongRand > ulong.MaxValue - ((ulong.MaxValue % uRange) + 1) % uRange);

                long result = (long)(ulongRand % uRange) + minValue + random.Next(0, 1); // Randomly decide whether to increment by 1 to make the second parameter "inclusive"

                Debug.Assert(result >= minValue);
                Debug.Assert(result <= maxValue);
                return result;
            }
        }

        /// <summary>
        /// Similar to <see cref="Random.Next(int)"/>, but returns a <see cref="long"/> between
        /// 0 (inclusive) and <paramref name="maxValue"/> (exclusive).
        /// </summary>
        /// <param name="random">A <see cref="Random"/> instance.</param>
        /// <param name="maxValue">The bound on the random number to be returned. Must be positive.</param>
        /// <returns>A random <see cref="long"/> between 0 and <paramref name="maxValue"/> - 1.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxValue"/> is less than 1.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="random"/> is <c>null</c>.</exception>
        public static long NextInt64(Random random, long maxValue)
        {
            if (random is null)
                throw new ArgumentNullException(nameof(random));
            if (maxValue <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxValue), maxValue, $"{nameof(maxValue)} must be greater than or equal to 0");

            long value = random.NextInt64();
            long range = maxValue - 1;
            if ((maxValue & range) == 0L)
            {
                value &= range;
            }
            else
            {
                for (long u = value.TripleShift(1); u + range - (value = u % maxValue) < 0L;)
                {
                    u = random.NextInt64().TripleShift(1);
                }
            }
            return value;
        }
    }

    internal static class RandomExtensions
    {
        /// <summary>
        /// Generates a random <see cref="long"/>.
        /// </summary>
        /// <param name="random">This <see cref="Random"/>.</param>
        /// <returns>A random <see cref="long"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="random"/> is <c>null</c>.</exception>
        // http://stackoverflow.com/a/6651656
        public static long NextInt64(this Random random) // .NET specific to cover missing member from Java
        {
            if (random is null)
                throw new ArgumentNullException(nameof(random));

            byte[] buffer = new byte[8];
            random.NextBytes(buffer);
            return BitConverter.ToInt64(buffer, 0);
        }
    }
}
