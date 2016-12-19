using Lucene.Net.Documents;

namespace Lucene.Net.Search
{
    using NUnit.Framework;
    using AttributeSource = Lucene.Net.Util.AttributeSource;
    using BytesRef = Lucene.Net.Util.BytesRef;
    using Codec = Lucene.Net.Codecs.Codec;
    using Directory = Lucene.Net.Store.Directory;
    using Document = Documents.Document;
    using Field = Field;
    using FilteredTermsEnum = Lucene.Net.Index.FilteredTermsEnum;
    using IndexReader = Lucene.Net.Index.IndexReader;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;

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

    using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
    using MockTokenizer = Lucene.Net.Analysis.MockTokenizer;
    using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;
    using StringHelper = Lucene.Net.Util.StringHelper;
    using Term = Lucene.Net.Index.Term;
    using Terms = Lucene.Net.Index.Terms;
    using TermsEnum = Lucene.Net.Index.TermsEnum;
    using TestUtil = Lucene.Net.Util.TestUtil;

    /// <summary>
    /// Create an index with random unicode terms
    /// Generates random prefix queries, and validates against a simple impl.
    /// </summary>
    [TestFixture]
    public class TestPrefixRandom : LuceneTestCase
    {
        private IndexSearcher Searcher;
        private IndexReader Reader;
        private Directory Dir;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            Dir = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random(), Dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random(), MockTokenizer.KEYWORD, false)).SetMaxBufferedDocs(TestUtil.NextInt(Random(), 50, 1000)));

            var doc = new Document();
            Field field = NewStringField("field", "", Field.Store.NO);
            doc.Add(field);

            // we generate aweful prefixes: good for testing.
            // but for preflex codec, the test can be very slow, so use less iterations.
            string codec = Codec.Default.Name;
            int num = codec.Equals("Lucene3x") ? 200 * RANDOM_MULTIPLIER : AtLeast(1000);
            for (int i = 0; i < num; i++)
            {
                field.SetStringValue(TestUtil.RandomUnicodeString(Random(), 10));
                writer.AddDocument(doc);
            }
            Reader = writer.Reader;
            Searcher = NewSearcher(Reader);
            writer.Dispose();
        }

        [TearDown]
        public override void TearDown()
        {
            Reader.Dispose();
            Dir.Dispose();
            base.TearDown();
        }

        /// <summary>
        /// a stupid prefix query that just blasts thru the terms </summary>
        private class DumbPrefixQuery : MultiTermQuery
        {
            private readonly TestPrefixRandom OuterInstance;

            private readonly BytesRef Prefix;

            internal DumbPrefixQuery(TestPrefixRandom outerInstance, Term term)
                : base(term.Field)
            {
                this.OuterInstance = outerInstance;
                Prefix = term.Bytes;
            }

            public override TermsEnum GetTermsEnum(Terms terms, AttributeSource atts)
            {
                return new SimplePrefixTermsEnum(this, terms.Iterator(null), Prefix);
            }

            private class SimplePrefixTermsEnum : FilteredTermsEnum
            {
                private readonly TestPrefixRandom.DumbPrefixQuery OuterInstance;

                internal readonly BytesRef Prefix;

                internal SimplePrefixTermsEnum(TestPrefixRandom.DumbPrefixQuery outerInstance, TermsEnum tenum, BytesRef prefix)
                    : base(tenum)
                {
                    this.OuterInstance = outerInstance;
                    this.Prefix = prefix;
                    InitialSeekTerm = new BytesRef("");
                }

                protected internal override AcceptStatus Accept(BytesRef term)
                {
                    return StringHelper.StartsWith(term, Prefix) ? AcceptStatus.YES : AcceptStatus.NO;
                }
            }

            public override string ToString(string field)
            {
                return field.ToString() + ":" + Prefix.ToString();
            }
        }

        /// <summary>
        /// test a bunch of random prefixes </summary>
        [Test]
        public virtual void TestPrefixes()
        {
            int num = AtLeast(100);
            for (int i = 0; i < num; i++)
            {
                AssertSame(TestUtil.RandomUnicodeString(Random(), 5));
            }
        }

        /// <summary>
        /// check that the # of hits is the same as from a very
        /// simple prefixquery implementation.
        /// </summary>
        private void AssertSame(string prefix)
        {
            PrefixQuery smart = new PrefixQuery(new Term("field", prefix));
            DumbPrefixQuery dumb = new DumbPrefixQuery(this, new Term("field", prefix));

            TopDocs smartDocs = Searcher.Search(smart, 25);
            TopDocs dumbDocs = Searcher.Search(dumb, 25);
            CheckHits.CheckEqual(smart, smartDocs.ScoreDocs, dumbDocs.ScoreDocs);
        }
    }
}