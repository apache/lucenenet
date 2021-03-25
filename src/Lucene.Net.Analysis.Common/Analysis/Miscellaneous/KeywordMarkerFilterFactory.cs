// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.Util;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

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
    /// Factory for <see cref="KeywordMarkerFilter"/>.
    /// <code>
    /// &lt;fieldType name="text_keyword" class="solr.TextField" positionIncrementGap="100"&gt;
    ///   &lt;analyzer&gt;
    ///     &lt;tokenizer class="solr.WhitespaceTokenizerFactory"/&gt;
    ///     &lt;filter class="solr.KeywordMarkerFilterFactory" protected="protectedkeyword.txt" pattern="^.+er$" ignoreCase="false"/&gt;
    ///   &lt;/analyzer&gt;
    /// &lt;/fieldType&gt;</code>
    /// </summary>
    public class KeywordMarkerFilterFactory : TokenFilterFactory, IResourceLoaderAware
    {
        public const string PROTECTED_TOKENS = "protected";
        public const string PATTERN = "pattern";
        private readonly string wordFiles;
        private readonly string stringPattern;
        private readonly bool ignoreCase;
        private Regex pattern;
        private CharArraySet protectedWords;

        /// <summary>
        /// Creates a new <see cref="KeywordMarkerFilterFactory"/> </summary>
        public KeywordMarkerFilterFactory(IDictionary<string, string> args)
            : base(args)
        {
            wordFiles = Get(args, PROTECTED_TOKENS);
            stringPattern = Get(args, PATTERN);
            ignoreCase = GetBoolean(args, "ignoreCase", false);
            if (args.Count > 0)
            {
                throw new ArgumentException(string.Format(J2N.Text.StringFormatter.CurrentCulture, "Unknown parameters: {0}", args));
            }
        }

        public virtual void Inform(IResourceLoader loader)
        {
            if (wordFiles != null)
            {
                protectedWords = GetWordSet(loader, wordFiles, ignoreCase);
            }
            if (stringPattern != null)
            {
                pattern = ignoreCase ?
                    new Regex(stringPattern, RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant) :
                    new Regex(stringPattern, RegexOptions.Compiled | RegexOptions.CultureInvariant);
            }
        }

        public virtual bool IgnoreCase => ignoreCase;

        public override TokenStream Create(TokenStream input)
        {
            if (pattern != null)
            {
                input = new PatternKeywordMarkerFilter(input, pattern);
            }
            if (protectedWords != null)
            {
                input = new SetKeywordMarkerFilter(input, protectedWords);
            }
            return input;
        }
    }
}