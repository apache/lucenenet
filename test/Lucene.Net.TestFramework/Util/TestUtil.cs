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
namespace Lucene.Net.Util
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Lucene.Net.Random;


    public static class  TestUtil
    {

        /// <summary>
        /// Returns a random string up to the specified length.
        /// </summary>
        /// <param name="random">The random instance used to generate characters.</param>
        /// <param name="maxLength">The maximum length for the generated string.</param>
        /// <returns>A random string.</returns>
        public static string ToUnicodeString(this Random random, int maxLength = 20)
        {
            int end = random.NextBetween(0, maxLength);
            if (end == 0)
                return "";

            var buffer = new char[end];
            random.RandomFixedLengthUnicodeString(buffer, 0, buffer.Length);

            return new String(buffer, 0, end);
        }

        /// <summary>
        /// Fills <paramref name="chars"/> with a valid random unicode character sequence.
        /// </summary>
        /// <param name="random">The random instance used to generate characters.</param>
        /// <param name="chars">The character array that will be filled.</param>
        /// <param name="offset">The position to start the character fill.</param>
        /// <param name="length">The number of characters that should be generated.</param>
        public static void RandomFixedLengthUnicodeString(this Random random, char[] chars, int offset, int length)
        {
            int i = offset,
                end = offset + length;

            while(i < end)
            {
                int t = random.Next(5);
                if(0 == t && i < length - 1)
                {
                    // Make a surrogate pair
                    // High surrogate
                    chars[i++] = (char)random.NextBetween(0xd800, 0xdbff);
                    // low surrogate
                    chars[i++] = (char)random.NextBetween(0xdc00, 0xdfff);
                }
                else if (t <= 1)
                {
                    chars[i++] = (char)random.Next(0x80);
                }
                else if (t == 2)
                {
                    chars[i++] = (char)random.Next(0x80, 0x7ff);
                }
                else if (t == 3)
                {
                    chars[i++] = (char)random.Next(0x800, 0xd7ff);
                }
                else if (t == 4)
                {
                    chars[i++] = (char)random.Next(0xe000, 0xffff);
                }
            }
        }
    }
}
