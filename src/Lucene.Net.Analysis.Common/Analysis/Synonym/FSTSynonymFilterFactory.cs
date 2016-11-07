using Lucene.Net.Analysis.Core;
using Lucene.Net.Analysis.Util;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

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
        internal readonly bool ignoreCase;
        private readonly string tokenizerFactory;
        private readonly string synonyms;
        private readonly string format;
        private readonly bool expand;
        private readonly IDictionary<string, string> tokArgs = new Dictionary<string, string>();

        private SynonymMap map;

        [Obsolete(@"(3.4) use <seealso cref=""SynonymFilterFactory"" instead. this is only a backwards compatibility")]
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

                var keys = new List<string>(args.Keys);
                foreach (string key in keys)
                {
                    tokArgs[Regex.Replace(key, "^tokenizerFactory\\.", "")] = args[key];
                    args.Remove(key);
                }
            }
            if (args.Count > 0)
            {
                throw new System.ArgumentException("Unknown parameters: " + args);
            }
        }

        public override TokenStream Create(TokenStream input)
        {
            // if the fst is null, it means there's actually no synonyms... just return the original stream
            // as there is nothing to do here.
            return map.fst == null ? input : new SynonymFilter(input, map, ignoreCase);
        }

        public void Inform(IResourceLoader loader)
        {
            TokenizerFactory factory = tokenizerFactory == null ? null : LoadTokenizerFactory(loader, tokenizerFactory);

            Analyzer analyzer = new AnalyzerAnonymousInnerClassHelper(this, factory);

            try
            {
                string formatClass = format;
                if (format == null || format.Equals("solr"))
                {
                    formatClass = typeof(SolrSynonymParser).AssemblyQualifiedName;
                }
                else if (format.Equals("wordnet"))
                {
                    formatClass = typeof(WordnetSynonymParser).AssemblyQualifiedName;
                }
                // TODO: expose dedup as a parameter?
                map = LoadSynonyms(loader, formatClass, true, analyzer);
            }
            catch (Exception e)
            {
                throw new IOException("Error parsing synonyms file:", e);
            }
        }

        private class AnalyzerAnonymousInnerClassHelper : Analyzer
        {
            private readonly FSTSynonymFilterFactory outerInstance;

            private readonly TokenizerFactory factory;

            public AnalyzerAnonymousInnerClassHelper(FSTSynonymFilterFactory outerInstance, TokenizerFactory factory)
            {
                this.outerInstance = outerInstance;
                this.factory = factory;
            }

            public override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
            {
#pragma warning disable 612, 618
                Tokenizer tokenizer = factory == null ? new WhitespaceTokenizer(LuceneVersion.LUCENE_CURRENT, reader) : factory.Create(reader);
                TokenStream stream = outerInstance.ignoreCase ? (TokenStream)new LowerCaseFilter(LuceneVersion.LUCENE_CURRENT, tokenizer) : tokenizer;
#pragma warning restore 612, 618
                return new Analyzer.TokenStreamComponents(tokenizer, stream);
            }
        }

        /// <summary>
        /// Load synonyms with the given <seealso cref="SynonymMap.Parser"/> class.
        /// </summary>
        private SynonymMap LoadSynonyms(IResourceLoader loader, string cname, bool dedup, Analyzer analyzer)
        {
            Encoding decoder = Encoding.UTF8;

            SynonymMap.Parser parser;
            Type clazz = loader.FindClass(cname /*, typeof(SynonymMap.Parser) */);
            try
            {
                parser = (SynonymMap.Parser)Activator.CreateInstance(clazz, new object[] { dedup, expand, analyzer });
            }
            catch (Exception e)
            {
                throw e;
            }

            if (File.Exists(synonyms))
            {
                parser.Parse(new StreamReader(loader.OpenResource(synonyms), decoder));
            }
            else
            {
                IEnumerable<string> files = SplitFileNames(synonyms);
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
            Type clazz = loader.FindClass(cname /*, typeof(TokenizerFactory) */);
            try
            {
                TokenizerFactory tokFactory = (TokenizerFactory)Activator.CreateInstance(clazz, new object[] { tokArgs });

                if (tokFactory is IResourceLoaderAware)
                {
                    ((IResourceLoaderAware)tokFactory).Inform(loader);
                }
                return tokFactory;
            }
            catch (Exception e)
            {
                throw e;
            }
        }
    }
}