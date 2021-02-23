// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.Core;
using Lucene.Net.Analysis.Util;
using System;
using System.Collections.Generic;

namespace Lucene.Net.Analysis.CommonGrams
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
    /// Constructs a <see cref="CommonGramsFilter"/>.
    /// <code>
    /// &lt;fieldType name="text_cmmngrms" class="solr.TextField" positionIncrementGap="100"&gt;
    ///   &lt;analyzer&gt;
    ///     &lt;tokenizer class="solr.WhitespaceTokenizerFactory"/&gt;
    ///     &lt;filter class="solr.CommonGramsFilterFactory" words="commongramsstopwords.txt" ignoreCase="false"/&gt;
    ///   &lt;/analyzer&gt;
    /// &lt;/fieldType&gt;
    /// </code>
    /// </summary>
    public class CommonGramsFilterFactory : TokenFilterFactory, IResourceLoaderAware
    {
        // TODO: shared base class for Stop/Keep/CommonGrams? 
        private CharArraySet commonWords;
        private readonly string commonWordFiles;
        private readonly string format;
        private readonly bool ignoreCase;

        /// <summary>
        /// Creates a new <see cref="CommonGramsFilterFactory"/> </summary>
        public CommonGramsFilterFactory(IDictionary<string, string> args)
            : base(args)
        {
            commonWordFiles = Get(args, "words");
            format = Get(args, "format");
            ignoreCase = GetBoolean(args, "ignoreCase", false);
            if (args.Count > 0)
            {
                throw new ArgumentException(string.Format(J2N.Text.StringFormatter.CurrentCulture, "Unknown parameters: {0}", args));
            }
        }

        public virtual void Inform(IResourceLoader loader)
        {
            if (commonWordFiles != null)
            {
                if ("snowball".Equals(format, StringComparison.OrdinalIgnoreCase))
                {
                    commonWords = GetSnowballWordSet(loader, commonWordFiles, ignoreCase);
                }
                else
                {
                    commonWords = GetWordSet(loader, commonWordFiles, ignoreCase);
                }
            }
            else
            {
                commonWords = StopAnalyzer.ENGLISH_STOP_WORDS_SET;
            }
        }

        public virtual bool IgnoreCase => ignoreCase;

        public virtual CharArraySet CommonWords => commonWords;

        public override TokenStream Create(TokenStream input)
        {
            var commonGrams = new CommonGramsFilter(m_luceneMatchVersion, input, commonWords);
            return commonGrams;
        }
    }
}