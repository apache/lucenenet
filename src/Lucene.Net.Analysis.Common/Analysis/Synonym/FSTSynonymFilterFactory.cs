// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.Core;
using Lucene.Net.Analysis.Util;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using JCG = J2N.Collections.Generic;

namespace Lucene.Net.Analysis.Synonym
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
    internal sealed class FSTSynonymFilterFactory : TokenFilterFactory, IResourceLoaderAware
    {
        private readonly bool ignoreCase;
        private readonly string tokenizerFactory;
        private readonly string synonyms;
        private readonly string format;
        private readonly bool expand;
        private readonly IDictionary<string, string> tokArgs = new Dictionary<string, string>();

        private SynonymMap map;

        // LUCENENET: Optimized by pre-comiling regex and lazy-loading
        private class Holder
        {
            public static readonly Regex TOKENIZER_FACTORY_REPLACEMENT_PATTERN = new Regex("^tokenizerFactory\\.", RegexOptions.Compiled);
        }

        [Obsolete(@"(3.4) use SynonymFilterFactory instead. this is only a backwards compatibility")]
        public FSTSynonymFilterFactory(IDictionary<string, string> args)
            : base(args)
        {
            ignoreCase = GetBoolean(args, "ignoreCase", false);
            synonyms = Require(args, "synonyms");
            format = Get(args, "format");
            expand = GetBoolean(args, "expand", true);

            tokenizerFactory = Get(args, "tokenizerFactory");
            if (tokenizerFactory != null)
            {
                AssureMatchVersion();
                tokArgs["luceneMatchVersion"] = LuceneMatchVersion.ToString();

                var keys = new JCG.List<string>(args.Keys);
                foreach (string key in keys)
                {
                    tokArgs[Holder.TOKENIZER_FACTORY_REPLACEMENT_PATTERN.Replace(key, "")] = args[key];
                    args.Remove(key);
                }
            }
            if (args.Count > 0)
            {
                throw new ArgumentException(string.Format(J2N.Text.StringFormatter.CurrentCulture, "Unknown parameters: {0}", args));
            }
        }

        public override TokenStream Create(TokenStream input)
        {
            // if the fst is null, it means there's actually no synonyms... just return the original stream
            // as there is nothing to do here.
            return map.Fst is null ? input : new SynonymFilter(input, map, ignoreCase);
        }

        public void Inform(IResourceLoader loader)
        {
            TokenizerFactory factory = tokenizerFactory is null ? null : LoadTokenizerFactory(loader, tokenizerFactory);

            Analyzer analyzer = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
            {
#pragma warning disable 612, 618
                Tokenizer tokenizer = factory is null ? new WhitespaceTokenizer(LuceneVersion.LUCENE_CURRENT, reader) : factory.Create(reader);
                TokenStream stream = ignoreCase ? (TokenStream)new LowerCaseFilter(LuceneVersion.LUCENE_CURRENT, tokenizer) : tokenizer;
#pragma warning restore 612, 618
                return new TokenStreamComponents(tokenizer, stream);
            });

            try
            {
                string formatClass = format;
                if (format is null || format.Equals("solr", StringComparison.Ordinal))
                {
                    formatClass = typeof(SolrSynonymParser).AssemblyQualifiedName;
                }
                else if (format.Equals("wordnet", StringComparison.Ordinal))
                {
                    formatClass = typeof(WordnetSynonymParser).AssemblyQualifiedName;
                }
                // TODO: expose dedup as a parameter?
                map = LoadSynonyms(loader, formatClass, true, analyzer);
            }
            catch (Exception e) when (e.IsParseException())
            {
                throw new IOException("Error parsing synonyms file:", e);
            }
        }

        /// <summary>
        /// Load synonyms with the given <see cref="SynonymMap.Parser"/> class.
        /// </summary>
        private SynonymMap LoadSynonyms(IResourceLoader loader, string cname, bool dedup, Analyzer analyzer)
        {
            Encoding decoder = Encoding.UTF8;

            SynonymMap.Parser parser;
            Type clazz = loader.FindType(cname /*, typeof(SynonymMap.Parser) */);
            try
            {
                parser = (SynonymMap.Parser)Activator.CreateInstance(clazz, new object[] { dedup, expand, analyzer });
            }
            catch (Exception e) when (e.IsException())
            {
                throw; // LUCENENET: CA2200: Rethrow to preserve stack details (https://docs.microsoft.com/en-us/visualstudio/code-quality/ca2200-rethrow-to-preserve-stack-details)
            }

            if (File.Exists(synonyms))
            {
                parser.Parse(new StreamReader(loader.OpenResource(synonyms), decoder));
            }
            else
            {
                IList<string> files = SplitFileNames(synonyms);
                foreach (string file in files)
                {
                    parser.Parse(new StreamReader(loader.OpenResource(synonyms), decoder));
                }
            }
            return parser.Build();
        }

        // (there are no tests for this functionality)
        private TokenizerFactory LoadTokenizerFactory(IResourceLoader loader, string cname)
        {
            Type clazz = loader.FindType(cname /*, typeof(TokenizerFactory) */);
            try
            {
                TokenizerFactory tokFactory = (TokenizerFactory)Activator.CreateInstance(clazz, new object[] { tokArgs });

                if (tokFactory is IResourceLoaderAware resourceLoaderAware)
                {
                    resourceLoaderAware.Inform(loader);
                }
                return tokFactory;
            }
            catch (Exception e) when (e.IsException())
            {
                throw; // LUCENENET: CA2200: Rethrow to preserve stack details (https://docs.microsoft.com/en-us/visualstudio/code-quality/ca2200-rethrow-to-preserve-stack-details)
            }
        }
    }
}