using System;
using System.Text;

namespace Lucene.Net.Util
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
    /// Extension methods to spell out numbers into English. 
    /// <para/>
    /// Inspiration: https://stackoverflow.com/a/2601001
    /// </summary>
    public static class EnglishNumberFormatExtensions
    {
        private const long QUADRILLION = TRILLION * 1000;
        private const long TRILLION = BILLION * 1000;
        private const long BILLION = MILLION * 1000;
        private const long MILLION = THOUSAND * 1000;
        private const long THOUSAND = HUNDRED * 10;
        private const long HUNDRED = 100;

        /// <summary>
        /// Returns the spelled-out English words for the provided <paramref name="value"/>.
        /// </summary>
        public static string ToWords(this int value)
        {
            return ToWords((long)value);
        }

        /// <summary>
        /// Returns the spelled-out English words for the provided <paramref name="value"/>.
        /// </summary>
        public static string ToWords(this long value)
        {
            return ToWords(value, new StringBuilder()).ToString();
        }
        private static StringBuilder ToWords(long value, StringBuilder builder)
        {
            if (value == 0) builder.Append("zero");

            if (value < 0)
            {
                builder.Append("negative ");
                ToWords(Math.Abs(value), builder);
            }

            long unit;

            if (value >= QUADRILLION)
            {
                unit = (value / QUADRILLION);
                value -= unit * QUADRILLION;

                ToWords(unit, builder);
                builder.Append(" quadrillion");
                if (value > 0) builder.Append(' ');
            }

            if (value >= TRILLION)
            {
                unit = (value / TRILLION);
                value -= unit * TRILLION;

                ToWords(unit, builder);
                builder.Append(" trillion");
                if (value > 0) builder.Append(' ');
            }

            if (value >= BILLION)
            {
                unit = (value / BILLION);
                value -= unit * BILLION;

                ToWords(unit, builder);
                builder.Append(" billion");
                if (value > 0) builder.Append(' ');
            }

            if (value >= MILLION)
            {
                unit = (value / MILLION);
                value -= unit * MILLION;

                ToWords(unit, builder);
                builder.Append(" million");
                if (value > 0) builder.Append(' ');
            }

            if (value >= THOUSAND)
            {
                unit = (value / THOUSAND);
                value -= unit * THOUSAND;

                ToWords(unit, builder);
                builder.Append(" thousand");
                if (value > 0) builder.Append(' ');
            }

            if (value >= HUNDRED)
            {
                unit = (value / HUNDRED);
                value -= unit * HUNDRED;

                ToWords(unit, builder);
                builder.Append(" hundred");
                if (value > 0) builder.Append(' ');
            }

            if (value >= 90)
            {
                value -= 90;
                builder.Append("ninety");
                if (value > 0) builder.Append('-');
            }

            if (value >= 80)
            {
                value -= 80;
                builder.Append("eighty");
                if (value > 0) builder.Append('-');
            }

            if (value >= 70)
            {
                value -= 70;
                builder.Append("seventy");
                if (value > 0) builder.Append('-');
            }

            if (value >= 60)
            {
                value -= 60;
                builder.Append("sixty");
                if (value > 0) builder.Append('-');
            }

            if (value >= 50)
            {
                value -= 50;
                builder.Append("fifty");
                if (value > 0) builder.Append('-');
            }

            if (value >= 40)
            {
                value -= 40;
                builder.Append("forty");
                if (value > 0) builder.Append('-');
            }

            if (value >= 30)
            {
                value -= 30;
                builder.Append("thirty");
                if (value > 0) builder.Append('-');
            }

            if (value >= 20)
            {
                value -= 20;
                builder.Append("twenty");
                if (value > 0) builder.Append('-');
            }

            if (value == 19) builder.Append("nineteen");
            if (value == 18) builder.Append("eighteen");
            if (value == 17) builder.Append("seventeen");
            if (value == 16) builder.Append("sixteen");
            if (value == 15) builder.Append("fifteen");
            if (value == 14) builder.Append("fourteen");
            if (value == 13) builder.Append("thirteen");
            if (value == 12) builder.Append("twelve");
            if (value == 11) builder.Append("eleven");
            if (value == 10) builder.Append("ten");
            if (value == 9) builder.Append("nine");
            if (value == 8) builder.Append("eight");
            if (value == 7) builder.Append("seven");
            if (value == 6) builder.Append("six");
            if (value == 5) builder.Append("five");
            if (value == 4) builder.Append("four");
            if (value == 3) builder.Append("three");
            if (value == 2) builder.Append("two");
            if (value == 1) builder.Append("one");

            return builder;
        }
    }
}
