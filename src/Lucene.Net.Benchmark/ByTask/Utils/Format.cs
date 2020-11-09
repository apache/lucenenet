using System;

namespace Lucene.Net.Benchmarks.ByTask.Utils
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
    /// Formatting utilities (for reports).
    /// </summary>
    public static class Formatter // LUCENENET specific - renamed from Format because of method name collision, CA1052 Static holder types should be Static or NotInheritable
    {
        private static readonly string[] numFormat = {
            "N0",
            "N1",
            "N2"
        };

        private const string padd = "                                                 ";

        /// <summary>
        /// Padd a number from left.
        /// </summary>
        /// <param name="numFracDigits">Number of digits in fraction part - must be 0 or 1 or 2.</param>
        /// <param name="f">Number to be formatted.</param>
        /// <param name="col">Column name (used for deciding on length).</param>
        /// <returns>Formatted string.</returns>
        public static string Format(int numFracDigits, float f, string col)
        {
            string res = padd + string.Format(numFormat[numFracDigits], f);
            return res.Substring(res.Length - col.Length);
        }

        public static string Format(int numFracDigits, double f, string col)
        {
            string res = padd + string.Format(numFormat[numFracDigits], f);
            return res.Substring(res.Length - col.Length);
        }

        /// <summary>
        /// Pad a number from right.
        /// </summary>
        /// <param name="numFracDigits">Number of digits in fraction part - must be 0 or 1 or 2.</param>
        /// <param name="f">Number to be formatted.</param>
        /// <param name="col">Column name (used for deciding on length).</param>
        /// <returns>Formatted string.</returns>
        public static string FormatPaddRight(int numFracDigits, float f, string col)
        {
            string res = string.Format(numFormat[numFracDigits], f) + padd;
            return res.Substring(0, col.Length - 0);
        }

        public static string FormatPaddRight(int numFracDigits, double f, string col)
        {
            string res = string.Format(numFormat[numFracDigits], f) + padd;
            return res.Substring(0, col.Length - 0);
        }

        /// <summary>
        /// Pad a number from left.
        /// </summary>
        /// <param name="n">Number to be formatted.</param>
        /// <param name="col">Column name (used for deciding on length).</param>
        /// <returns>Formatted string.</returns>
        public static string Format(int n, string col)
        {
            string res = padd + n;
            return res.Substring(res.Length - col.Length);
        }

        /// <summary>
        /// Pad a string from right.
        /// </summary>
        /// <param name="s">String to be formatted.</param>
        /// <param name="col">Column name (used for deciding on length).</param>
        /// <returns>Formatted string.</returns>
        public static string Format(string s, string col)
        {
            string s1 = (s + padd);
            return s1.Substring(0, Math.Min(col.Length, s1.Length));
        }

        /// <summary>
        /// Pad a string from left.
        /// </summary>
        /// <param name="s">String to be formatted.</param>
        /// <param name="col">Column name (used for deciding on length).</param>
        /// <returns>Formatted string.</returns>
        public static string FormatPaddLeft(string s, string col)
        {
            string res = padd + s;
            return res.Substring(res.Length - col.Length);
        }
    }
}
