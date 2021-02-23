// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace Lucene.Net.Analysis.CharFilters
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
    /// Factory for <see cref="HTMLStripCharFilter"/>. 
    /// <code>
    /// &lt;fieldType name="text_html" class="solr.TextField" positionIncrementGap="100"&gt;
    ///   &lt;analyzer&gt;
    ///     &lt;charFilter class="solr.HTMLStripCharFilterFactory" escapedTags="a, title" /&gt;
    ///     &lt;tokenizer class="solr.WhitespaceTokenizerFactory"/&gt;
    ///   &lt;/analyzer&gt;
    /// &lt;/fieldType&gt;
    /// </code>
    /// </summary>
    public class HTMLStripCharFilterFactory : CharFilterFactory
    {
        private readonly ICollection<string> escapedTags;
        //private static readonly Regex TAG_NAME_PATTERN = new Regex(@"[^\\s,]+", RegexOptions.Compiled); // LUCENENET: Never read

        /// <summary>
        /// Creates a new <see cref="HTMLStripCharFilterFactory"/> </summary>
        public HTMLStripCharFilterFactory(IDictionary<string, string> args) : base(args)
        {
            escapedTags = GetSet(args, "escapedTags");
            if (args.Count > 0)
            {
                throw new ArgumentException(string.Format(J2N.Text.StringFormatter.CurrentCulture, "Unknown parameters: {0}", args));
            }
        }

        public override TextReader Create(TextReader input)
        {
            HTMLStripCharFilter charFilter;
            if (null == escapedTags)
            {
                charFilter = new HTMLStripCharFilter(input);
            }
            else
            {
                charFilter = new HTMLStripCharFilter(input, escapedTags);
            }
            return charFilter;
        }
    }
}