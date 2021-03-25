// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.Util;
using System.Collections.Generic;
using System;
using System.IO;
using System.Text.RegularExpressions;

namespace Lucene.Net.Analysis.Pattern
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
    /// Factory for <see cref="PatternReplaceCharFilter"/>. 
    /// <code>
    /// &lt;fieldType name="text_ptnreplace" class="solr.TextField" positionIncrementGap="100"&gt;
    ///   &lt;analyzer&gt;
    ///     &lt;charFilter class="solr.PatternReplaceCharFilterFactory" 
    ///                    pattern="([^a-z])" replacement=""/&gt;
    ///     &lt;tokenizer class="solr.KeywordTokenizerFactory"/&gt;
    ///   &lt;/analyzer&gt;
    /// &lt;/fieldType&gt;</code>
    /// 
    /// @since Solr 3.1
    /// </summary>
    public class PatternReplaceCharFilterFactory : CharFilterFactory
    {
        private readonly Regex pattern;
        private readonly string replacement;
        private readonly int maxBlockChars;
        private readonly string blockDelimiters;

        /// <summary>
        /// Creates a new <see cref="PatternReplaceCharFilterFactory"/> </summary>
        public PatternReplaceCharFilterFactory(IDictionary<string, string> args) : base(args)
        {
            pattern = GetPattern(args, "pattern");
            replacement = Get(args, "replacement", "");
            // TODO: warn if you set maxBlockChars or blockDelimiters ?
            maxBlockChars = GetInt32(args, "maxBlockChars",
#pragma warning disable 612, 618
                PatternReplaceCharFilter.DEFAULT_MAX_BLOCK_CHARS);
#pragma warning restore 612, 618
            if (args.TryGetValue("blockDelimiters", out blockDelimiters))
            {
                args.Remove("blockDelimiters");
            }
            if (args.Count > 0)
            {
                throw new ArgumentException(string.Format(J2N.Text.StringFormatter.CurrentCulture, "Unknown parameters: {0}", args));
            }
        }

        public override TextReader Create(TextReader input)
        {
#pragma warning disable 612, 618
            return new PatternReplaceCharFilter(pattern, replacement, maxBlockChars, blockDelimiters, input);
#pragma warning restore 612, 618
        }
    }
}