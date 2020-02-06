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

using System;
using System.Globalization;

namespace Lucene.Net.Support
{
    /// <summary>
    /// A simple class for number conversions.
    /// </summary>
    internal static class Number
    {
        /// <summary>
        /// Min radix value.
        /// </summary>
        public const int MIN_RADIX = 2;

        /// <summary>
        /// Max radix value.
        /// </summary>
        public const int MAX_RADIX = 36;

        /*public const int CHAR_MIN_CODE_POINT =
        public const int CHAR_MAX_CODE_POINT = */

        private const string digits = "0123456789abcdefghijklmnopqrstuvwxyz";

        /// <summary>
        /// Converts a number to System.String.
        /// </summary>
        /// <param name="f"></param>
        /// <returns></returns>
        public static System.String ToString(float f)
        {
            if (((float)(int)f) == f)
            {
                // Special case: When we have an integer value,
                // the standard .NET formatting removes the decimal point
                // and everything to the right. But we need to always
                // have at least decimal place to match Lucene.
                return f.ToString("0.0", CultureInfo.InvariantCulture);
            }
            else
            {
                // LUCENENET NOTE: Although the MSDN documentation says that 
                // round-trip on float will be limited to 7 decimals, it appears
                // not to be the case. Also, when specifying "0.0######", we only
                // get a result to 6 decimal places maximum. So, we must round before
                // doing a round-trip format to guarantee 7 decimal places.
                return Math.Round(f, 7).ToString("R", CultureInfo.InvariantCulture);
            }
        }

        /// <summary>
        /// Parses a number in the specified radix.
        /// </summary>
        /// <param name="s">An input System.String.</param>
        /// <param name="radix">A radix.</param>
        /// <returns>The parsed number in the specified radix.</returns>
        public static long Parse(System.String s, int radix)
        {
            if (s == null)
            {
                throw new ArgumentException("null");
            }

            if (radix < MIN_RADIX)
            {
                throw new NotSupportedException("radix " + radix +
                                                " less than Number.MIN_RADIX");
            }
            if (radix > MAX_RADIX)
            {
                throw new NotSupportedException("radix " + radix +
                                                " greater than Number.MAX_RADIX");
            }

            long result = 0;
            long mult = 1;

            s = s.ToLowerInvariant(); // LUCENENET TODO: Do we need to deal with Turkish? If so, this won't work right...

            for (int i = s.Length - 1; i >= 0; i--)
            {
                int weight = digits.IndexOf(s[i]);
                if (weight == -1)
                    throw new FormatException("Invalid number for the specified radix");

                result += (weight * mult);
                mult *= radix;
            }

            return result;
        }
    }
}