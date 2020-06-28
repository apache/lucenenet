using Lucene.Net.Randomized.Generators;
using Lucene.Net.Search;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using J2N.Numerics;

namespace Lucene.Net
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
    /// Extensions to <see cref="Random"/> in order to randomly generate
    /// types and specially formatted strings that assist with testing
    /// custom extensions to Lucene.Net.
    /// </summary>
    public static class RandomExtensions
    {
        /// <summary>
        /// Generates a random <see cref="bool"/>, with a random distribution of
        /// approximately 50/50.
        /// </summary>
        /// <param name="random">This <see cref="Random"/>.</param>
        /// <returns>A random <see cref="bool"/>.</returns>
        public static bool NextBoolean(this Random random)
        {
            return (random.Next(1, 100) > 50);
        }

        // LUCENENET NOTE: NextInt32() is basically covered by the overloads of Next(),
        // the only difference is that the maximum is exclusive in .NET, not inclusive.

        /// <summary>
        /// Generates a random <see cref="long"/>.
        /// </summary>
        /// <param name="random">This <see cref="Random"/>.</param>
        /// <returns>A random <see cref="long"/>.</returns>
        // http://stackoverflow.com/a/6651656
        public static long NextInt64(this Random random)
        {
            byte[] buffer = new byte[8];
            random.NextBytes(buffer);
            return BitConverter.ToInt64(buffer, 0);
        }

        /// <summary>
        /// Generates a random <see cref="long"/>. <paramref name="start"/> and <paramref name="end"/> are BOTH inclusive.
        /// </summary>
        /// <param name="random">This <see cref="Random"/>.</param>
        /// <param name="start">The inclusive start.</param>
        /// <param name="end">The inclusive end.</param>
        /// <returns>A random <see cref="long"/>.</returns>
        public static long NextInt64(this Random random, long start, long end)
        {
            return TestUtil.NextInt64(random, start, end);
        }

        /// <summary>
        /// Generates a random <see cref="long"/> between <c>0</c> (inclusive)
        /// and <paramref name="n"/> (exclusive).
        /// </summary>
        /// <param name="random">This <see cref="Random"/>.</param>
        /// <param name="n">The bound on the random number to be returned. Must be positive.</param>
        /// <returns>A random <see cref="long"/> between 0 and <paramref name="n"/>-1.</returns>
        public static long NextInt64(this Random random, long n)
        {
            if (n <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(n), $"{n} <= 0: " + n);
            }

            long value = NextInt64(random);
            long range = n - 1;
            if ((n & range) == 0L)
            {
                value &= range;
            }
            else
            {
                for (long u = value.TripleShift(1); u + range - (value = u % n) < 0L;)
                {
                    u = NextInt64(random).TripleShift(1);
                }
            }
            return value;
        }

        /// <summary>
        /// Generates a random <see cref="float"/>.
        /// </summary>
        /// <param name="random">This <see cref="Random"/>.</param>
        /// <returns>A random <see cref="float"/>.</returns>
        public static float NextSingle(this Random random)
        {
            return (float)random.NextDouble();
        }

        /// <summary>
        /// Pick a random object from the <paramref name="collection"/>.
        /// </summary>
        public static T NextFrom<T>(this Random random, ICollection<T> collection)
        {
            return RandomPicks.RandomFrom(random, collection);
        }

        /// <summary>
        /// Returns a random string consisting only of lowercase characters 'a' through 'z'.
        /// </summary>
        public static string NextSimpleString(this Random random, int maxLength)
        {
            return TestUtil.RandomSimpleString(random, maxLength);
        }

        /// <summary>
        /// Returns a random string consisting only of lowercase characters 'a' through 'z'.
        /// </summary>
        public static string NextSimpleString(this Random random, int minLength, int maxLength)
        {
            return TestUtil.RandomSimpleString(random, minLength, maxLength);
        }

        /// <summary>
        /// Returns a random string consisting only of characters between <paramref name="minChar"/> and <paramref name="maxChar"/>.
        /// </summary>
        public static string NextSimpleStringRange(this Random random, char minChar, char maxChar, int maxLength)
        {
            return TestUtil.RandomSimpleStringRange(random, minChar, maxChar, maxLength);
        }

        /// <summary>
        /// Returns a random string consisting only of lowercase characters 'a' through 'z',
        /// between 0 and 20 characters in length.
        /// </summary>
        public static string NextSimpleString(this Random random)
        {
            return TestUtil.RandomSimpleString(random);
        }

        /// <summary>
        /// Returns random string, including full unicode range. </summary>
        public static string NextUnicodeString(this Random random)
        {
            return TestUtil.RandomUnicodeString(random);
        }

        /// <summary>
        /// Returns a random string up to a certain length.
        /// </summary>
        public static string NextUnicodeString(this Random random, int maxLength)
        {
            return TestUtil.RandomUnicodeString(random, maxLength);
        }

        /// <summary>
        /// Fills provided <see cref="T:char[]"/> with valid random unicode code
        /// unit sequence.
        /// </summary>
        public static void NextFixedLengthUnicodeString(this Random random, char[] chars, int offset, int length)
        {
            TestUtil.RandomFixedLengthUnicodeString(random, chars, offset, length);
        }

        /// <summary>
        /// Returns a <see cref="string"/> thats "regexish" (contains lots of operators typically found in regular expressions)
        /// If you call this enough times, you might get a valid regex!
        /// </summary>
        public static string NextRegexishString(this Random random)
        {
            return TestUtil.RandomRegexpishString(random);
        }

        /// <summary>
        /// Returns a <see cref="string"/> thats "regexish" (contains lots of operators typically found in regular expressions)
        /// If you call this enough times, you might get a valid regex!
        ///
        /// <para/>Note: to avoid practically endless backtracking patterns we replace asterisk and plus
        /// operators with bounded repetitions. See LUCENE-4111 for more info.
        /// </summary>
        /// <param name="maxLength"> A hint about maximum length of the regexpish string. It may be exceeded by a few characters. </param>
        public static string NextRegexishString(this Random random, int maxLength)
        {
            return TestUtil.RandomRegexpishString(random, maxLength);
        }

        /// <summary>
        /// Returns a random HTML-like string.
        /// </summary>
        public static string NextHtmlishString(this Random random, int numElements)
        {
            return TestUtil.RandomHtmlishString(random, numElements);
        }

        /// <summary>
        /// Randomly upcases, downcases, or leaves intact each code point in the given string.
        /// </summary>
        public static string NextRecasedString(this Random random, string str)
        {
            return TestUtil.RandomlyRecaseString(random, str);
        }

        /// <summary>
        /// Returns random string of length between 0-20 codepoints, all codepoints within the same unicode block. </summary>
        public static string NextRealisticUnicodeString(this Random random)
        {
            return TestUtil.RandomRealisticUnicodeString(random);
        }

        /// <summary>
        /// Returns random string of length up to maxLength codepoints, all codepoints within the same unicode block. </summary>
        public static string NextRealisticUnicodeString(this Random random, int maxLength)
        {
            return TestUtil.RandomRealisticUnicodeString(random, maxLength);
        }

        /// <summary>
        /// Returns random string of length between min and max codepoints, all codepoints within the same unicode block. </summary>
        public static string NextRealisticUnicodeString(this Random random, int minLength, int maxLength)
        {
            return TestUtil.RandomRealisticUnicodeString(random, minLength, maxLength);
        }

        /// <summary>
        /// Returns random string, with a given UTF-8 byte <paramref name="length"/>. </summary>
        public static string NextFixedByteLengthUnicodeString(this Random random, int length)
        {
            return TestUtil.RandomFixedByteLengthUnicodeString(random, length);
        }

        /// <summary>
        /// Returns a valid (compiling) <see cref="Regex"/> instance with random stuff inside. Be careful
        /// when applying random patterns to longer strings as certain types of patterns
        /// may explode into exponential times in backtracking implementations (such as Java's).
        /// </summary>        
        public static Regex NextRegex(this Random random)
        {
            return TestUtil.RandomRegex(random);
        }

        public static FilteredQuery.FilterStrategy NextFilterStrategy(this Random random)
        {
            return TestUtil.RandomFilterStrategy(random);
        }

        /// <summary>
        /// Returns a random string in the specified length range consisting
        /// entirely of whitespace characters.
        /// </summary>
        /// <seealso cref="TestUtil.WHITESPACE_CHARACTERS"/>
        public static string NextWhitespace(this Random random, int minLength, int maxLength)
        {
            return TestUtil.RandomWhitespace(random, minLength, maxLength);
        }

        public static string NextAnalysisString(this Random random, int maxLength, bool simple)
        {
            return TestUtil.RandomAnalysisString(random, maxLength, simple);
        }

        public static string NextSubString(this Random random, int wordLength, bool simple)
        {
            return TestUtil.RandomSubString(random, wordLength, simple);
        }
    }
}
