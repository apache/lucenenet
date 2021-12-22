using Lucene.Net.Analysis;
using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace Lucene.Net.QueryParsers.Analyzing
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

    [SuppressCodecs("Lucene3x")] // binary terms
    [TestFixture]
    public class TestAnalyzingQueryParser : LuceneTestCase
    {
        private readonly static string FIELD = "field";

        private Analyzer a;

        private string[] wildcardInput;
        private string[] wildcardExpected;
        private string[] prefixInput;
        private string[] prefixExpected;
        private string[] rangeInput;
        private string[] rangeExpected;
        private string[] fuzzyInput;
        private string[] fuzzyExpected;

        private IDictionary<string, string> wildcardEscapeHits = new Dictionary<string, string>();
        private IDictionary<string, string> wildcardEscapeMisses = new Dictionary<string, string>();

        public override void SetUp()
        {
            base.SetUp();
            wildcardInput = new string[] { "*bersetzung über*ung",
                "Mötley Cr\u00fce Mötl?* Crü?", "Renée Zellweger Ren?? Zellw?ger" };
            wildcardExpected = new string[] { "*bersetzung uber*ung", "motley crue motl?* cru?",
                "renee zellweger ren?? zellw?ger" };

            prefixInput = new string[] { "übersetzung übersetz*",
                "Mötley Crüe Mötl* crü*", "René? Zellw*" };
            prefixExpected = new string[] { "ubersetzung ubersetz*", "motley crue motl* cru*",
                "rene? zellw*" };

            rangeInput = new string[] { "[aa TO bb]", "{Anaïs TO Zoé}" };
            rangeExpected = new string[] { "[aa TO bb]", "{anais TO zoe}" };

            fuzzyInput = new string[] { "Übersetzung Übersetzung~0.9",
                "Mötley Crüe Mötley~0.75 Crüe~0.5",
                "Renée Zellweger Renée~0.9 Zellweger~" };
            fuzzyExpected = new string[] { "ubersetzung ubersetzung~1",
                "motley crue motley~1 crue~2", "renee zellweger renee~0 zellweger~2" };

            wildcardEscapeHits["mö*tley"] = "moatley";

            // need to have at least one genuine wildcard to trigger the wildcard analysis
            // hence the * before the y
            wildcardEscapeHits["mö\\*tl*y"] = "mo*tley";

            // escaped backslash then true wildcard
            wildcardEscapeHits["mö\\\\*tley"] = "mo\\atley";

            // escaped wildcard then true wildcard
            wildcardEscapeHits["mö\\??ley"] = "mo?tley";

            // the first is an escaped * which should yield a miss
            wildcardEscapeMisses["mö\\*tl*y"] = "moatley";

            a = new ASCIIAnalyzer();
        }

        [Test]
        public virtual void TestSingleChunkExceptions()
        {
            bool ex = false;
            string termStr = "the*tre";

            Analyzer stopsAnalyzer = new MockAnalyzer
                (Random, MockTokenizer.WHITESPACE, true, MockTokenFilter.ENGLISH_STOPSET);
            try
            {
                string q = ParseWithAnalyzingQueryParser(termStr, stopsAnalyzer, true);
            }
            catch (Lucene.Net.QueryParsers.Classic.ParseException e)
            {
                if (e.Message.Contains("returned nothing"))
                {
                    ex = true;
                }
            }
            assertEquals("Should have returned nothing", true, ex);
            ex = false;

            AnalyzingQueryParser qp = new AnalyzingQueryParser(TEST_VERSION_CURRENT, FIELD, a);
            try
            {
                qp.AnalyzeSingleChunk(FIELD, "", "not a single chunk");
            }
            catch (Lucene.Net.QueryParsers.Classic.ParseException e)
            {
                if (e.Message.Contains("multiple terms"))
                {
                    ex = true;
                }
            }
            assertEquals("Should have produced multiple terms", true, ex);
        }

        [Test]
        public virtual void TestWildcardAlone()
        {
            //seems like crazy edge case, but can be useful in concordance 
            bool pex = false;
            try
            {
                Query q = GetAnalyzedQuery("*", a, false);
            }
            catch (Lucene.Net.QueryParsers.Classic.ParseException /*e*/)
            {
                pex = true;
            }
            assertEquals("Wildcard alone with allowWildcard=false", true, pex);

            pex = false;
            try
            {
                String qString = ParseWithAnalyzingQueryParser("*", a, true);
                assertEquals("Every word", "*", qString);
            }
            catch (Lucene.Net.QueryParsers.Classic.ParseException /*e*/)
            {
                pex = true;
            }

            assertEquals("Wildcard alone with allowWildcard=true", false, pex);
        }

        [Test]
        public virtual void TestWildCardEscapes()
        {
            foreach (var entry in wildcardEscapeHits)
            {
                Query q = GetAnalyzedQuery(entry.Key, a, false);
                assertEquals("WildcardEscapeHits: " + entry.Key, true, IsAHit(q, entry.Value, a));
            }
            foreach (var entry in wildcardEscapeMisses)
            {
                Query q = GetAnalyzedQuery(entry.Key, a, false);
                assertEquals("WildcardEscapeMisses: " + entry.Key, false, IsAHit(q, entry.Value, a));
            }
        }

        [Test]
        public virtual void TestWildCardQueryNoLeadingAllowed()
        {
            bool ex = false;
            try
            {
                string q = ParseWithAnalyzingQueryParser(wildcardInput[0], a, false);

            }
            catch (Lucene.Net.QueryParsers.Classic.ParseException /*e*/)
            {
                ex = true;
            }
            assertEquals("Testing initial wildcard not allowed",
                true, ex);
        }

        [Test]
        public virtual void TestWildCardQuery()
        {
            for (int i = 0; i < wildcardInput.Length; i++)
            {
                assertEquals("Testing wildcards with analyzer " + a.GetType() + ", input string: "
                    + wildcardInput[i], wildcardExpected[i], ParseWithAnalyzingQueryParser(wildcardInput[i], a, true));
            }
        }

        [Test]
        public virtual void TestPrefixQuery()
        {
            for (int i = 0; i < prefixInput.Length; i++)
            {
                assertEquals("Testing prefixes with analyzer " + a.GetType() + ", input string: "
                    + prefixInput[i], prefixExpected[i], ParseWithAnalyzingQueryParser(prefixInput[i], a, false));
            }
        }

        [Test]
        public virtual void TestRangeQuery()
        {
            for (int i = 0; i < rangeInput.Length; i++)
            {
                assertEquals("Testing ranges with analyzer " + a.GetType() + ", input string: "
                    + rangeInput[i], rangeExpected[i], ParseWithAnalyzingQueryParser(rangeInput[i], a, false));
            }
        }

        [Test]
        public virtual void TestFuzzyQuery()
        {
            for (int i = 0; i < fuzzyInput.Length; i++)
            {
                assertEquals("Testing fuzzys with analyzer " + a.GetType() + ", input string: "
                  + fuzzyInput[i], fuzzyExpected[i], ParseWithAnalyzingQueryParser(fuzzyInput[i], a, false));
            }
        }


        private string ParseWithAnalyzingQueryParser(string s, Analyzer a, bool allowLeadingWildcard)
        {
            Query q = GetAnalyzedQuery(s, a, allowLeadingWildcard);
            return q.ToString(FIELD);
        }

        private Query GetAnalyzedQuery(string s, Analyzer a, bool allowLeadingWildcard)
        {
            AnalyzingQueryParser qp = new AnalyzingQueryParser(TEST_VERSION_CURRENT, FIELD, a);
            qp.AllowLeadingWildcard = allowLeadingWildcard;
            Query q = qp.Parse(s);
            return q;
        }

        internal sealed class FoldingFilter : TokenFilter
        {
            private readonly ICharTermAttribute termAtt;

            public FoldingFilter(TokenStream input)
                : base(input)
            {
                termAtt = AddAttribute<ICharTermAttribute>();
            }

            public sealed override bool IncrementToken()
            {
                if (m_input.IncrementToken())
                {
                    char[] term = termAtt.Buffer;
                    for (int i = 0; i < term.Length; i++)
                        switch (term[i])
                        {
                            case 'ü':
                                term[i] = 'u';
                                break;
                            case 'ö':
                                term[i] = 'o';
                                break;
                            case 'é':
                                term[i] = 'e';
                                break;
                            case 'ï':
                                term[i] = 'i';
                                break;
                        }
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        internal sealed class ASCIIAnalyzer : Analyzer
        {

            protected internal override TokenStreamComponents CreateComponents(string fieldName, System.IO.TextReader reader)
            {
                Tokenizer result = new MockTokenizer(reader, MockTokenizer.WHITESPACE, true);
                return new TokenStreamComponents(result, new FoldingFilter(result));
            }
        }

        // LUCENE-4176
        [Test]
        public virtual void TestByteTerms()
        {
            string s = "เข";
            Analyzer analyzer = new MockBytesAnalyzer();
            Classic.QueryParser qp = new AnalyzingQueryParser(TEST_VERSION_CURRENT, FIELD, analyzer);
            Query q = qp.Parse("[เข TO เข]");
            assertEquals(true, IsAHit(q, s, analyzer));
        }

        private bool IsAHit(Query q, string content, Analyzer analyzer)
        {
            int hits;
            using (Directory ramDir = NewDirectory())
            {
                using (RandomIndexWriter writer = new RandomIndexWriter(Random, ramDir, analyzer))
                {
                    Document doc = new Document();
                    FieldType fieldType = new FieldType();
                    fieldType.IsIndexed = (true);
                    fieldType.IsTokenized = (true);
                    fieldType.IsStored = (true);
                    Field field = new Field(FIELD, content, fieldType);
                    doc.Add(field);
                    writer.AddDocument(doc);
                }
                using DirectoryReader ir = DirectoryReader.Open(ramDir);
                IndexSearcher @is = new IndexSearcher(ir);

                hits = @is.Search(q, 10).TotalHits;
            }
            if (hits == 1)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
