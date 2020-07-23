using Lucene.Net.Util;
using System;

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

    /// <summary>
    /// <para>
    /// Merge policy for testing, it is like an alcoholic.
    /// It drinks (merges) at night, and randomly decides what to drink.
    /// During the daytime it sleeps.
    /// </para>
    /// <para>
    /// If tests pass with this, then they are likely to pass with any
    /// bizarro merge policy users might write.
    /// </para>
    /// <para>
    /// It is a fine bottle of champagne (Ordered by Martijn).
    /// </para>
    /// </summary>
    public class AlcoholicMergePolicy : LogMergePolicy
    {
        private readonly Random random;
        private readonly DateTime calendar;

        public AlcoholicMergePolicy(TimeZoneInfo timeZone, Random random)
        {
            // LUCENENET NOTE: All we care about here is that we have a random distribution of "Hour", picking any valid
            // date at random achives this. We have no actual need to create a Calendar object in .NET.
            var randomTime = new DateTime(TestUtil.NextInt64(random, DateTime.MinValue.Ticks, DateTime.MaxValue.Ticks));
            this.calendar = TimeZoneInfo.ConvertTime(randomTime, TimeZoneInfo.Local, timeZone);
            this.random = random;
            m_maxMergeSize = TestUtil.NextInt32(random, 1024 * 1024, int.MaxValue);
        }

        protected override long Size(SegmentCommitInfo info)
        {
            int hourOfDay = calendar.Hour;
            if (hourOfDay < 6 || hourOfDay > 20 || random.Next(23) == 5)
            // its 5 o'clock somewhere
            {
                Drink[] values = (Drink[])Enum.GetValues(typeof(Drink));
                // pick a random drink during the day
                Drink drink = values[random.Next(values.Length - 1)];
                return (long)drink * info.GetSizeInBytes();
            }

            return info.GetSizeInBytes();
        }

        private enum Drink
        {
            Beer = 15,
            Wine = 17,
            Champagne = 21,
            WhiteRussian = 22,
            SingleMalt = 30
        }
    }
}