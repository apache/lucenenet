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

namespace Lucene.Net.Random
{
    using Lucene.Net.Util;
    using System;
    using System.Diagnostics;

    /// <summary>
    /// Summary description for RandomExtensions
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Java <see href="https://github.com/carrotsearch/randomizedtesting/blob/master/randomized-runner/src/main/java/com/carrotsearch/randomizedtesting/generators/RandomInts.java">Source</see>
    ///     </para>
    /// </remarks>
    public static class RandomExtensions
    {


        public static int AtLeast(this LuceneTestCase instance, int minimumValue)
        {
            int min = (LuceneTestCase.TEST_NIGHTLY ? 2 * minimumValue : minimumValue) * LuceneTestCase.RANDOM_MULTIPLIER;
            int max = min + (min / 2);
            return instance.Random.NextBetween(min, max);
        }

	    /// <summary>
	    /// Returns an integer between the min and max value. This is compatable 
        /// with the Java Lucene version of the NextIntBetween method.
	    /// </summary>
        /// <remarks>
        ///    <para>
        ///        .NET has a default overloade for <see cref="System.Random.Next"/> that has a min and
        ///        max value. However, this method exists to keep capatablity with the Java Version of Lucene.
        ///    </para>
        /// </remarks>
	    /// <param name="random">The instance of random.</param>
        /// <param name="minValue">The minimum value that the random may use.</param>
        /// <param name="maxValue">The maximum value that the random may use.</param>
	    /// <returns>A random integer.</returns>
        public static int NextBetween(this Random random, int minValue, int maxValue)
        {
            Debug.Assert(maxValue >= minValue, string.Format("maxValue must be greater than minValue" +
                "Max value as {0}. Min value was {1}", minValue, maxValue));

            var range = maxValue - minValue;

            if (range < int.MaxValue)
                return minValue + random.Next(1 + range);
            else
                return minValue + (int)Math.Round(random.NextDouble() * range);
        }

        public static bool NextBoolean(this Random random)
        {
            return random.NextDouble() < 0.5;
        }

        public static int RandomInt(this Random random, int maxValue)
        {
            if (maxValue == 0)
                return 0;

            if (maxValue == int.MaxValue)
                return random.Next() & 0x7fffffff;

            return random.Next(maxValue + 1);
        }

        public static Boolean Rarely(this Random random)
        {
            // TODO: implement system properties and TEST_NIGHTLY & RANDOM_MULTIPLIER
            var p = 10;
            p += (int)(p * Math.Log((double)1));
            var min = 100 - Math.Min(p, 50);

            return random.Next(100) >= min;
        }
    }
}