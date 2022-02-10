// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.Util;
using System;
using System.Collections.Generic;

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
    /// Factory for <see cref="KeepWordFilter"/>. 
    /// <code>
    /// &lt;fieldType name="text_keepword" class="solr.TextField" positionIncrementGap="100"&gt;
    ///   &lt;analyzer&gt;
    ///     &lt;tokenizer class="solr.WhitespaceTokenizerFactory"/&gt;
    ///     &lt;filter class="solr.KeepWordFilterFactory" words="keepwords.txt" ignoreCase="false"/&gt;
    ///   &lt;/analyzer&gt;
    /// &lt;/fieldType&gt;</code>
    /// </summary>
    public class KeepWordFilterFactory : TokenFilterFactory, IResourceLoaderAware
    {
        private readonly bool ignoreCase;
        private readonly bool enablePositionIncrements;
        private readonly string wordFiles;
        private CharArraySet words;

        /// <summary>
        /// Creates a new <see cref="KeepWordFilterFactory"/> </summary>
        public KeepWordFilterFactory(IDictionary<string, string> args)
            : base(args)
        {
            AssureMatchVersion();
            wordFiles = Get(args, "words");
            ignoreCase = GetBoolean(args, "ignoreCase", false);
            enablePositionIncrements = GetBoolean(args, "enablePositionIncrements", true);
            if (args.Count > 0)
            {
                throw new ArgumentException(string.Format(J2N.Text.StringFormatter.CurrentCulture, "Unknown parameters: {0}", args));
            }
        }

        public virtual void Inform(IResourceLoader loader)
        {
            if (wordFiles != null)
            {
                words = GetWordSet(loader, wordFiles, ignoreCase);
            }
        }

        public virtual bool EnablePositionIncrements => enablePositionIncrements;

        public virtual bool IgnoreCase => ignoreCase;

        public virtual CharArraySet Words => words;

        public override TokenStream Create(TokenStream input)
        {
            // if the set is null, it means it was empty
            if (words is null)
            {
                return input;
            }
            else
            {
#pragma warning disable 612, 618
                TokenStream filter = new KeepWordFilter(m_luceneMatchVersion, enablePositionIncrements, input, words);
#pragma warning restore 612, 618
                return filter;
            }
        }
    }
}