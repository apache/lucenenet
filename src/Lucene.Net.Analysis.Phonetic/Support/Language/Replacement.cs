using System.Text.RegularExpressions;

namespace Lucene.Net.Analysis.Phonetic.Language
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
    /// Provides an easy means to cache a culture-invariant pre-compiled regular expression
    /// and its replacement value. This class doesn't do any caching itself, it is meant to
    /// be created within an object initializer and stored as a static reference.
    /// </summary>
    internal class Replacement
    {
        private readonly Regex regex;
        private readonly string _replacement;

        public Replacement(string pattern, string replacement)
        {
            regex = new Regex(pattern, RegexOptions.Compiled | RegexOptions.CultureInvariant);
            this._replacement = replacement;
        }

        public string Replace(string input)
        {
            return regex.Replace(input, _replacement);
        }
    }
}
