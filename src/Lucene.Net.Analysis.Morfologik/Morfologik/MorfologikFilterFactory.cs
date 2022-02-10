// Lucene version compatibility level 8.2.0
using Lucene.Net.Analysis.Util;
using Morfologik.Stemming;
using Morfologik.Stemming.Polish;
using System;
using System.Collections.Generic;
using System.IO;

namespace Lucene.Net.Analysis.Morfologik
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
    /// Filter factory for <see cref="MorfologikFilter"/>.
    /// <para/>
    /// An explicit resource name of the dictionary (<c>".dict"</c>) can be 
    /// provided via the <code>dictionary</code> attribute, as the example below demonstrates:
    /// <code>
    /// &lt;fieldType name="text_mylang" class="solr.TextField" positionIncrementGap="100"&gt;
    ///   &lt;analyzer&gt;
    ///     &lt;tokenizer class="solr.WhitespaceTokenizerFactory"/&gt;
    ///     &lt;filter class="solr.MorfologikFilterFactory" dictionary="mylang.dict" /&gt;
    ///   &lt;/analyzer&gt;
    /// &lt;/fieldType&gt;
    /// </code>
    /// <para/>
    /// If the dictionary attribute is not provided, the Polish dictionary is loaded
    /// and used by default.
    /// <para/>
    /// See: <a href="http://morfologik.blogspot.com/">Morfologik web site</a>
    /// </summary>
    /// <since>4.0.0</since>
    public class MorfologikFilterFactory : TokenFilterFactory, IResourceLoaderAware
    {
        /// <summary>Dictionary resource attribute (should have <c>".dict"</c> suffix), loaded from <see cref="IResourceLoader"/>.</summary>
        public const string DICTIONARY_ATTRIBUTE = "dictionary";

        /// <summary><see cref="DICTIONARY_ATTRIBUTE"/> value passed to <see cref="Inform(IResourceLoader)"/>.</summary>
        private readonly string resourceName;

        /// <summary>Loaded <see cref="Dictionary"/>, initialized on <see cref="Inform(IResourceLoader)"/>.</summary>
        private Dictionary dictionary;

        /// <summary>Creates a new <see cref="MorfologikFilterFactory"/></summary>
        public MorfologikFilterFactory(IDictionary<string, string> args)
            : base(args)
        {
            // Be specific about no-longer-supported dictionary attribute.
            string DICTIONARY_RESOURCE_ATTRIBUTE = "dictionary-resource";
            string dictionaryResource = Get(args, DICTIONARY_RESOURCE_ATTRIBUTE);
            if (!string.IsNullOrEmpty(dictionaryResource))
            {
                throw new ArgumentException("The " + DICTIONARY_RESOURCE_ATTRIBUTE + " attribute is no "
                    + "longer supported. Use the '" + DICTIONARY_ATTRIBUTE + "' attribute instead (see LUCENE-6833).");
            }

            resourceName = Get(args, DICTIONARY_ATTRIBUTE);

            if (args.Count > 0)
            {
                throw new ArgumentException(string.Format(J2N.Text.StringFormatter.CurrentCulture, "Unknown parameters: {0}", args));
            }
        }

        public virtual void Inform(IResourceLoader loader)
        {
            if (resourceName is null)
            {
                // Get the dictionary lazily, does not hold up memory.
                this.dictionary = new PolishStemmer().Dictionary;
            }
            else
            {
                using Stream dict = loader.OpenResource(resourceName);
                using Stream meta = loader.OpenResource(DictionaryMetadata.GetExpectedMetadataFileName(resourceName));
                this.dictionary = Dictionary.Read(dict, meta);
            }
        }

        public override TokenStream Create(TokenStream ts)
        {
            if (this.dictionary is null)
                throw new ArgumentException("MorfologikFilterFactory was not fully initialized.");

            return new MorfologikFilter(ts, dictionary);
        }
    }
}
