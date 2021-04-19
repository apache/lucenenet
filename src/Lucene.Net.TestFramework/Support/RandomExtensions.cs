using Lucene.Net.Search;
using Lucene.Net.Util;
using System;

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
    /// custom extensions to Lucene.NET.
    /// </summary>
    public static class RandomExtensions
    {
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
