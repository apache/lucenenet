using Lucene.Net.Analysis;
using Lucene.Net.Index;
using Lucene.Net.Util;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using Console = Lucene.Net.Util.SystemConsole;

namespace Lucene.Net.Search.Highlight.Custom
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
    /// Tests the extensibility of <see cref="WeightedSpanTermExtractor"/> and
    /// <see cref="QueryScorer"/> in a user defined package
    /// </summary>
    public class HighlightCustomQueryTest : LuceneTestCase
    {
        private static readonly String FIELD_NAME = "contents";

        [Test]
        public void TestHighlightCustomQuery()
        {
            String s1 = "I call our world Flatland, not because we call it so,";

            // Verify that a query against the default field results in text being
            // highlighted
            // regardless of the field name.

            CustomQuery q = new CustomQuery(new Term(FIELD_NAME, "world"));

            String expected = "I call our <B>world</B> Flatland, not because we call it so,";
            String observed = highlightField(q, "SOME_FIELD_NAME", s1);
            if (Verbose)
                Console.WriteLine("Expected: \"" + expected + "\n" + "Observed: \""
                    + observed);
            assertEquals(
                "Query in the default field results in text for *ANY* field being highlighted",
                expected, observed);

            // Verify that a query against a named field does not result in any
            // highlighting
            // when the query field name differs from the name of the field being
            // highlighted,
            // which in this example happens to be the default field name.
            q = new CustomQuery(new Term("text", "world"));

            expected = s1;
            observed = highlightField(q, FIELD_NAME, s1);
            if (Verbose)
                Console.WriteLine("Expected: \"" + expected + "\n" + "Observed: \""
          + observed);
            assertEquals(
                "Query in a named field does not result in highlighting when that field isn't in the query",
                s1, highlightField(q, FIELD_NAME, s1));

        }

        /**
         * This method intended for use with
         * <tt>testHighlightingWithDefaultField()</tt>
         */
        private String highlightField(Query query, String fieldName,
            String text)
        {
            TokenStream tokenStream = new MockAnalyzer(Random, MockTokenizer.SIMPLE,
                true, MockTokenFilter.ENGLISH_STOPSET).GetTokenStream(fieldName, text);
            // Assuming "<B>", "</B>" used to highlight
            SimpleHTMLFormatter formatter = new SimpleHTMLFormatter();
            MyQueryScorer scorer = new MyQueryScorer(query, fieldName, FIELD_NAME);
            Highlighter highlighter = new Highlighter(formatter, scorer);
            highlighter.TextFragmenter = (new SimpleFragmenter(int.MaxValue));

            String rv = highlighter.GetBestFragments(tokenStream, text, 1,
                "(FIELD TEXT TRUNCATED)");
            return rv.Length == 0 ? text : rv;
        }

        public class MyWeightedSpanTermExtractor : WeightedSpanTermExtractor
        {

            public MyWeightedSpanTermExtractor()
                        : base()
            {
            }

            public MyWeightedSpanTermExtractor(String defaultField)
                            : base(defaultField)
            {
            }


            protected override void ExtractUnknownQuery(Query query,
                IDictionary<String, WeightedSpanTerm> terms)
            {
                if (query is CustomQuery)
                {
                    ExtractWeightedTerms(terms, new TermQuery(((CustomQuery)query).term));
                }
            }

        }

        public class MyQueryScorer : QueryScorer
        {

            public MyQueryScorer(Query query, String field, String defaultField)
                        : base(query, field, defaultField)
            {
            }


            protected override WeightedSpanTermExtractor NewTermExtractor(String defaultField)
            {
                return defaultField is null ? new MyWeightedSpanTermExtractor()
                    : new MyWeightedSpanTermExtractor(defaultField);
            }

        }

        public class CustomQuery : Query
        {
            internal readonly Term term;

            public CustomQuery(Term term)
                        : base()
            {
                this.term = term;
            }

            public override String ToString(String field)
            {
                return new TermQuery(term).ToString(field);
            }

            public override Query Rewrite(IndexReader reader)
            {
                return new TermQuery(term);
            }


            public override int GetHashCode()
            {
                int prime = 31;
                int result = base.GetHashCode();
                result = prime * result + ((term is null) ? 0 : term.GetHashCode());
                return result;
            }

            public override bool Equals(Object obj)
            {
                if (this == obj)
                    return true;
                if (!base.Equals(obj))
                    return false;
                if (GetType() != obj.GetType())
                    return false;
                CustomQuery other = (CustomQuery)obj;
                if (term is null)
                {
                    if (other.term != null)
                        return false;
                }
                else if (!term.equals(other.term))
                    return false;
                return true;
            }
        }
    }
}
