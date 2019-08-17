/*
 *
 * Licensed to the Apache Software Foundation (ASF) under one
 * or more contributor license agreements.  See the NOTICE file
 * distributed with this work for additional information
 * regarding copyright ownership.  The ASF licenses this file
 * to you under the Apache License, Version 2.0 (the
 * "License"); you may not use this file except in compliance
 * with the License.  You may obtain a copy of the License at
 *
 *   http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing,
 * software distributed under the License is distributed on an
 * "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
 * KIND, either express or implied.  See the License for the
 * specific language governing permissions and limitations
 * under the License.
 *
*/

using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Lucene.Net.Randomized.Generators
{
    [ExceptionToNetNumericConvention]
    public static class RandomInts
    {
        public static int NextInt32Between(this Random random, int min, int max)
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

        public static float NextSingle(this Random random)
        {
            return (float)random.NextDouble();
        }

        /* .NET has random.Next(max) which negates the need for randomInt(Random random, int max) as  */

        public static long NextInt64(this Random random)
        {
            int i1 = random.Next();
            int i2 = random.Next();
            long l12 = ((i1 << 32) | i2);
            return l12;
        }

        public static T RandomFrom<T>(Random rand, ISet<T> set)
        {
            return set.ElementAt(rand.Next(0, set.Count));
        }

        public static T RandomFrom<T>(Random rand, IList<T> set)
        {
            return set.ElementAt(rand.Next(0, set.Count));
        }
    }
}
