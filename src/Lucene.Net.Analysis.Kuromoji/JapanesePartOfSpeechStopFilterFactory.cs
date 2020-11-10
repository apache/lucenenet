using Lucene.Net.Analysis.Util;
using System;
using System.Collections.Generic;
using JCG = J2N.Collections.Generic;

namespace Lucene.Net.Analysis.Ja
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
    /// Factory for <see cref="JapanesePartOfSpeechStopFilter"/>.
    /// <code>
    /// &lt;fieldType name="text_ja" class="solr.TextField"&gt;
    ///   &lt;analyzer&gt;
    ///     &lt;tokenizer class="solr.JapaneseTokenizerFactory"/&gt;
    ///     &lt;filter class="solr.JapanesePartOfSpeechStopFilterFactory"
    ///             tags="stopTags.txt" 
    ///             enablePositionIncrements="true"/&gt;
    ///   &lt;/analyzer&gt;
    /// &lt;/fieldType&gt;
    /// </code>
    /// </summary>
    public class JapanesePartOfSpeechStopFilterFactory : TokenFilterFactory, IResourceLoaderAware
    {
        private readonly string stopTagFiles;
        private readonly bool enablePositionIncrements;
        private ISet<string> stopTags;

        /// <summary>Creates a new JapanesePartOfSpeechStopFilterFactory</summary>
        public JapanesePartOfSpeechStopFilterFactory(IDictionary<string, string> args)
            : base(args)
        {
            stopTagFiles = Get(args, "tags");
            enablePositionIncrements = GetBoolean(args, "enablePositionIncrements", true);
            if (args.Count > 0)
            {
                throw new ArgumentException(string.Format(J2N.Text.StringFormatter.CurrentCulture, "Unknown parameters: {0}", args));
            }
        }

        public virtual void Inform(IResourceLoader loader)
        {
            stopTags = null;
            CharArraySet cas = GetWordSet(loader, stopTagFiles, false);
            if (cas != null)
            {
                stopTags = new JCG.HashSet<string>();
                foreach (string element in cas) 
                {
                    stopTags.Add(element);
                }
            }
        }

        public override TokenStream Create(TokenStream stream)
        {
            // if stoptags is null, it means the file is empty
            if (stopTags != null)
            {
#pragma warning disable 612, 618
                TokenStream filter = new JapanesePartOfSpeechStopFilter(m_luceneMatchVersion, enablePositionIncrements, stream, stopTags);
#pragma warning restore 612, 618
                return filter;
            }
            else
            {
                return stream;
            }
        }
    }
}
