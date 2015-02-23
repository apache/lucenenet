using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Lucene.Net.Analysis.Core;
using Lucene.Net.Analysis.Util;
using Lucene.Net.Util;

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
    internal sealed class FSTSynonymFilterFactory : TokenFilterFactory, ResourceLoaderAware
    {
        private readonly bool ignoreCase;
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
            ignoreCase = getBoolean(args, "ignoreCase", false);
            synonyms = require(args, "synonyms");
            format = get(args, "format");
            expand = getBoolean(args, "expand", true);

            tokenizerFactory = get(args, "tokenizerFactory");
            if (tokenizerFactory != null)
            {
                assureMatchVersion();
                tokArgs["luceneMatchVersion"] = LuceneMatchVersion.ToString();
                for (var itr = args.Keys.GetEnumerator(); itr.MoveNext(); )
                {
                    var key = itr.Current;
                    tokArgs[Regex.Replace(itr.Current, "^tokenizerFactory\\.", string.Empty)] = args[key];
                    itr.Remove();
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

        public void Inform(ResourceLoader loader)
        {
            TokenizerFactory factory = tokenizerFactory == null ? null : LoadTokenizerFactory(loader, tokenizerFactory);

            Analyzer analyzer = new AnalyzerAnonymousInnerClassHelper(this, factory);

            try
            {
                string formatClass = format;
                if (format == null || format.Equals("solr"))
                {
                    formatClass = typeof(SolrSynonymParser).Name;
                }
                else if (format.Equals("wordnet"))
                {
                    formatClass = typeof(WordnetSynonymParser).Name;
                }
                // TODO: expose dedup as a parameter?
                map = LoadSynonyms(loader, formatClass, true, analyzer);
            }
            catch (ParseException e)
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
                Tokenizer tokenizer = factory == null ? new WhitespaceTokenizer(LuceneVersion.LUCENE_CURRENT, reader) : factory.Create(reader);
                TokenStream stream = outerInstance.ignoreCase ? new LowerCaseFilter(LuceneVersion.LUCENE_CURRENT, tokenizer) : tokenizer;
                return new Analyzer.TokenStreamComponents(tokenizer, stream);
            }
        }

        /// <summary>
        /// Load synonyms with the given <seealso cref="SynonymMap.Parser"/> class.
        /// </summary>
        private SynonymMap LoadSynonyms(ResourceLoader loader, string cname, bool dedup, Analyzer analyzer)
        {
            CharsetDecoder decoder = Charset.forName("UTF-8").newDecoder().onMalformedInput(CodingErrorAction.REPORT).onUnmappableCharacter(CodingErrorAction.REPORT);

            SynonymMap.Parser parser;
            Type clazz = loader.findClass(cname, typeof(SynonymMap.Parser));
            try
            {
                parser = clazz.getConstructor(typeof(bool), typeof(bool), typeof(Analyzer)).newInstance(dedup, expand, analyzer);
            }
            catch (Exception e)
            {
                throw new Exception(e);
            }

            if (File.Exists(synonyms))
            {
                decoder.Reset();
                parser.Parse(new InputStreamReader(loader.openResource(synonyms), decoder));
            }
            else
            {
                IList<string> files = splitFileNames(synonyms);
                foreach (string file in files)
                {
                    decoder.reset();
                    parser.Parse(new InputStreamReader(loader.openResource(file), decoder));
                }
            }
            return parser.Build();
        }

        // (there are no tests for this functionality)
        private TokenizerFactory LoadTokenizerFactory(ResourceLoader loader, string cname)
        {
            Type clazz = loader.findClass(cname, typeof(TokenizerFactory));
            try
            {
                TokenizerFactory tokFactory = clazz.getConstructor(typeof(IDictionary)).newInstance(tokArgs);
                if (tokFactory is ResourceLoaderAware)
                {
                    ((ResourceLoaderAware)tokFactory).inform(loader);
                }
                return tokFactory;
            }
            catch (Exception e)
            {
                throw new Exception(e);
            }
        }
    }

}