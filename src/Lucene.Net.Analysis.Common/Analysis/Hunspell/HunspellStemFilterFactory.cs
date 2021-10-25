// Lucene version compatibility level 4.10.4
using J2N.Text;
using Lucene.Net.Analysis.Util;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.IO;
using JCG = J2N.Collections.Generic;

namespace Lucene.Net.Analysis.Hunspell
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
    /// <see cref="TokenFilterFactory"/> that creates instances of <see cref="HunspellStemFilter"/>.
    /// Example config for British English:
    /// <code>
    /// &lt;filter class=&quot;solr.HunspellStemFilterFactory&quot;
    ///         dictionary=&quot;en_GB.dic,my_custom.dic&quot;
    ///         affix=&quot;en_GB.aff&quot; 
    ///         ignoreCase=&quot;false&quot;
    ///         longestOnly=&quot;false&quot; /&gt;</code>
    /// Both parameters dictionary and affix are mandatory.
    /// Dictionaries for many languages are available through the OpenOffice project.
    /// 
    /// See <a href="http://wiki.apache.org/solr/Hunspell">http://wiki.apache.org/solr/Hunspell</a>
    /// @lucene.experimental
    /// </summary>
    public class HunspellStemFilterFactory : TokenFilterFactory, IResourceLoaderAware
    {
        private const string PARAM_DICTIONARY = "dictionary";
        private const string PARAM_AFFIX = "affix";
        private const string PARAM_RECURSION_CAP = "recursionCap";
        private const string PARAM_IGNORE_CASE = "ignoreCase";
        private const string PARAM_LONGEST_ONLY = "longestOnly";

        private readonly string dictionaryFiles;
        private readonly string affixFile;
        private readonly bool ignoreCase;
        private readonly bool longestOnly;
        private Dictionary dictionary;

        /// <summary>
        /// Creates a new <see cref="HunspellStemFilterFactory"/> </summary>
        public HunspellStemFilterFactory(IDictionary<string, string> args) 
            : base(args)
        {
            dictionaryFiles = Require(args, PARAM_DICTIONARY);
            affixFile = Get(args, PARAM_AFFIX);
            ignoreCase = GetBoolean(args, PARAM_IGNORE_CASE, false);
            longestOnly = GetBoolean(args, PARAM_LONGEST_ONLY, false);
            // this isnt necessary: we properly load all dictionaries.
            // but recognize and ignore for back compat
            GetBoolean(args, "strictAffixParsing", true);
            // this isn't necessary: multi-stage stripping is fixed and 
            // flags like COMPLEXPREFIXES in the data itself control this.
            // but recognize and ignore for back compat
            GetInt32(args, "recursionCap", 0);
            if (args.Count > 0)
            {
                throw new ArgumentException(string.Format(J2N.Text.StringFormatter.CurrentCulture, "Unknown parameters: {0}", args));
            }
        }

        public virtual void Inform(IResourceLoader loader)
        {
            string[] dicts = dictionaryFiles.Split(',').TrimEnd();

            Stream affix = null;
            IList<Stream> dictionaries = new JCG.List<Stream>();

            try
            {
                dictionaries = new JCG.List<Stream>();
                foreach (string file in dicts)
                {
                    dictionaries.Add(loader.OpenResource(file));
                }
                affix = loader.OpenResource(affixFile);

                this.dictionary = new Dictionary(affix, dictionaries, ignoreCase);
            }
            catch (Exception e) when (e.IsParseException())
            {
                throw new IOException("Unable to load hunspell data! [dictionary=" + dictionaries + ",affix=" + affixFile + "]", e);
            }
            finally
            {
                IOUtils.DisposeWhileHandlingException(affix);
                IOUtils.DisposeWhileHandlingException(dictionaries);
            }
        }

        public override TokenStream Create(TokenStream tokenStream)
        {
            return new HunspellStemFilter(tokenStream, dictionary, true, longestOnly);
        }
    }
}