using Lucene.Net.Support;
using System;
using Debug = Lucene.Net.Diagnostics.Debug; // LUCENENET NOTE: We cannot use System.Diagnostics.Debug because those calls will be optimized out of the release!

namespace Lucene.Net.Randomized.Generators
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
    /// Utility class for random integer and integer sequences.
    /// </summary>
    [ExceptionToNetNumericConvention]
    public static class RandomInts
    {
        /// <summary>
        /// A random integer from <paramref name="min"/> to <paramref name="max"/> (inclusive).
        /// </summary>
        public static int RandomInt32Between(Random random, int min, int max)
        {
            Debug.Assert(min <= max,
                $"Min must be less than or equal max int. min: {min.ToString()}, max: {max.ToString()}");
            var range = max - min;
            if (range < Int32.MaxValue)
                return min + random.Next(1 + range);

            return min + (int)Math.Round(random.NextDouble() * range);
        }

        /* .NET has random.Next(max) which negates the need for randomInt(Random random, int max) as  */
    }
}
