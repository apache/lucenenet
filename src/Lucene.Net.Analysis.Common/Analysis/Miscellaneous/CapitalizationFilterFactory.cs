// Lucene version compatibility level 4.8.1
using Lucene.Net.Support;
using Lucene.Net.Analysis.Util;
using System;
using System.Collections.Generic;
using System.Globalization;
using JCG = J2N.Collections.Generic;

namespace Lucene.Net.Analysis.Miscellaneous
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
    /// Factory for <see cref="CapitalizationFilter"/>.
    /// <para/>
    /// The factory takes parameters:<para/>
    /// "onlyFirstWord" - should each word be capitalized or all of the words?<para/>
    /// "keep" - a keep word list.  Each word that should be kept separated by whitespace.<para/>
    /// "keepIgnoreCase - true or false.  If true, the keep list will be considered case-insensitive.<para/>
    /// "forceFirstLetter" - Force the first letter to be capitalized even if it is in the keep list<para/>
    /// "okPrefix" - do not change word capitalization if a word begins with something in this list.
    /// for example if "McK" is on the okPrefix list, the word "McKinley" should not be changed to
    /// "Mckinley"<para/>
    /// "minWordLength" - how long the word needs to be to get capitalization applied.  If the
    /// minWordLength is 3, "and" > "And" but "or" stays "or"<para/>
    /// "maxWordCount" - if the token contains more then maxWordCount words, the capitalization is
    /// assumed to be correct.<para/>
    /// "culture" - the culture to use to apply the capitalization rules. If not supplied or the string
    /// "invariant" is supplied, the invariant culture is used.<para/>
    /// 
    /// <code>
    /// &lt;fieldType name="text_cptlztn" class="solr.TextField" positionIncrementGap="100"&gt;
    ///   &lt;analyzer&gt;
    ///     &lt;tokenizer class="solr.WhitespaceTokenizerFactory"/&gt;
    ///     &lt;filter class="solr.CapitalizationFilterFactory" onlyFirstWord="true"
    ///           keep="java solr lucene" keepIgnoreCase="false"
    ///           okPrefix="McK McD McA"/&gt;   
    ///   &lt;/analyzer&gt;
    /// &lt;/fieldType&gt;</code>
    /// 
    /// @since solr 1.3
    /// </summary>
    public class CapitalizationFilterFactory : TokenFilterFactory
    {
        public const string KEEP = "keep";
        public const string KEEP_IGNORE_CASE = "keepIgnoreCase";
        public const string OK_PREFIX = "okPrefix";
        public const string MIN_WORD_LENGTH = "minWordLength";
        public const string MAX_WORD_COUNT = "maxWordCount";
        public const string MAX_TOKEN_LENGTH = "maxTokenLength";
        public const string ONLY_FIRST_WORD = "onlyFirstWord";
        public const string FORCE_FIRST_LETTER = "forceFirstLetter";
        public const string CULTURE = "culture"; // LUCENENET specific

        internal CharArraySet keep;

        internal ICollection<char[]> okPrefix = Collections.EmptyList<char[]>(); // for Example: McK

        internal readonly int minWordLength; // don't modify capitalization for words shorter then this
        internal readonly int maxWordCount;
        internal readonly int maxTokenLength;
        internal readonly bool onlyFirstWord;
        internal readonly bool forceFirstLetter; // make sure the first letter is capital even if it is in the keep list
        private readonly CultureInfo culture; // LUCENENET specific

        /// <summary>
        /// Creates a new <see cref="CapitalizationFilterFactory"/> </summary>
        public CapitalizationFilterFactory(IDictionary<string, string> args)
            : base(args)
        {
            AssureMatchVersion();
            bool ignoreCase = GetBoolean(args, KEEP_IGNORE_CASE, false);
            ICollection<string> k = GetSet(args, KEEP);
            if (k != null)
            {
                keep = new CharArraySet(m_luceneMatchVersion, 10, ignoreCase);
                keep.UnionWith(k);
            }

            k = GetSet(args, OK_PREFIX);
            if (k != null)
            {
                okPrefix = new JCG.List<char[]>();
                foreach (string item in k)
                {
                    okPrefix.Add(item.ToCharArray());
                }
            }

            minWordLength = GetInt32(args, MIN_WORD_LENGTH, 0);
            maxWordCount = GetInt32(args, MAX_WORD_COUNT, CapitalizationFilter.DEFAULT_MAX_WORD_COUNT);
            maxTokenLength = GetInt32(args, MAX_TOKEN_LENGTH, CapitalizationFilter.DEFAULT_MAX_TOKEN_LENGTH);
            onlyFirstWord = GetBoolean(args, ONLY_FIRST_WORD, true);
            forceFirstLetter = GetBoolean(args, FORCE_FIRST_LETTER, true);
            culture = GetCulture(args, CULTURE, null);
            if (args.Count > 0)
            {
                throw new ArgumentException(string.Format(J2N.Text.StringFormatter.CurrentCulture, "Unknown parameters: {0}", args));
            }
        }

        public override TokenStream Create(TokenStream input)
        {
            return new CapitalizationFilter(input, onlyFirstWord, keep, forceFirstLetter, okPrefix, minWordLength, maxWordCount, maxTokenLength, culture);
        }
    }
}