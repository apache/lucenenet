using System;
using System.Globalization;
using System.Linq;

namespace Lucene.Net.Index
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

    using TestUtil = Lucene.Net.Util.TestUtil;

    /// <summary>
    /// <p>
    /// Merge policy for testing, it is like an alcoholic.
    /// It drinks (merges) at night, and randomly decides what to drink.
    /// During the daytime it sleeps.
    /// </p>
    /// <p>
    /// if tests pass with this, then they are likely to pass with any
    /// bizarro merge policy users might write.
    /// </p>
    /// <p>
    /// It is a fine bottle of champagne (Ordered by Martijn).
    /// </p>
    /// </summary>
    public class AlcoholicMergePolicy : LogMergePolicy
    {
        private readonly Random Random;
        private readonly DateTime Calendar;

        public AlcoholicMergePolicy(Random random)
        {
            this.Calendar = new GregorianCalendar().ToDateTime(1970, 1, 1, 0, 0, 0, (int)TestUtil.NextLong(random, 0, long.MaxValue));
            this.Random = random;
            MaxMergeSize = TestUtil.NextInt(random, 1024 * 1024, int.MaxValue);
        }

        protected internal override long Size(SegmentCommitInfo info)
        {
            int hourOfDay = Calendar.Hour;
            if (hourOfDay < 6 || hourOfDay > 20 || Random.Next(23) == 5)
            // its 5 o'clock somewhere
            {
                Drink.Drink_e[] values = Enum.GetValues(typeof(Drink.Drink_e)).Cast<Drink.Drink_e>().ToArray();
                // pick a random drink during the day
                Drink.Drink_e drink = values[Random.Next(values.Length - 1)];
                return (long)drink * info.SizeInBytes();
            }

            return info.SizeInBytes();
        }

        private class Drink
        {
            private const int NumDrinks = 5;

            internal enum Drink_e
            {
                Beer = 15,
                Wine = 17,
                Champagne = 21,
                WhiteRussian = 22,
                SingleMalt = 30
            }
        }
    }
}