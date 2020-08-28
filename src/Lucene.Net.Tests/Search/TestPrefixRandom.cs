using Lucene.Net.Documents;
using Lucene.Net.Index.Extensions;
using NUnit.Framework;
using System;

namespace Lucene.Net.Search
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

    using AttributeSource = Lucene.Net.Util.AttributeSource;
    using BytesRef = Lucene.Net.Util.BytesRef;
    using Codec = Lucene.Net.Codecs.Codec;
    using Directory = Lucene.Net.Store.Directory;
    using Document = Documents.Document;
    using Field = Field;
    using FilteredTermsEnum = Lucene.Net.Index.FilteredTermsEnum;
    using IndexReader = Lucene.Net.Index.IndexReader;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
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
        private IndexSearcher searcher;
        private IndexReader reader;
        private Directory dir;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            dir = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random, dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random, MockTokenizer.KEYWORD, false)).SetMaxBufferedDocs(TestUtil.NextInt32(Random, 50, 1000)));

            var doc = new Document();
            Field field = NewStringField("field", "", Field.Store.NO);
            doc.Add(field);

            // we generate aweful prefixes: good for testing.
            // but for preflex codec, the test can be very slow, so use less iterations.
            string codec = Codec.Default.Name;
            int num = codec.Equals("Lucene3x", StringComparison.Ordinal) ? 200 * RandomMultiplier : AtLeast(1000);
            for (int i = 0; i < num; i++)
            {
                field.SetStringValue(TestUtil.RandomUnicodeString(Random, 10));
                writer.AddDocument(doc);
            }
            reader = writer.GetReader();
            searcher = NewSearcher(reader);
            writer.Dispose();
        }

        [TearDown]
        public override void TearDown()
        {
            reader.Dispose();
            dir.Dispose();
            base.TearDown();
        }

        /// <summary>
        /// a stupid prefix query that just blasts thru the terms </summary>
        private class DumbPrefixQuery : MultiTermQuery
        {
            private readonly TestPrefixRandom outerInstance;

            private readonly BytesRef prefix;

            internal DumbPrefixQuery(TestPrefixRandom outerInstance, Term term)
                : base(term.Field)
            {
                this.outerInstance = outerInstance;
                prefix = term.Bytes;
            }

            protected override TermsEnum GetTermsEnum(Terms terms, AttributeSource atts)
            {
                return new SimplePrefixTermsEnum(this, terms.GetEnumerator(), prefix);
            }

            private class SimplePrefixTermsEnum : FilteredTermsEnum
            {
                private readonly TestPrefixRandom.DumbPrefixQuery outerInstance;

                private readonly BytesRef prefix;

                internal SimplePrefixTermsEnum(TestPrefixRandom.DumbPrefixQuery outerInstance, TermsEnum tenum, BytesRef prefix)
                    : base(tenum)
                {
                    this.outerInstance = outerInstance;
                    this.prefix = prefix;
                    SetInitialSeekTerm(new BytesRef(""));
                }

                protected override AcceptStatus Accept(BytesRef term)
                {
                    return StringHelper.StartsWith(term, prefix) ? AcceptStatus.YES : AcceptStatus.NO;
                }
            }

            public override string ToString(string field)
            {
                return field.ToString() + ":" + prefix.ToString();
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
                AssertSame(TestUtil.RandomUnicodeString(Random, 5));
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

            TopDocs smartDocs = searcher.Search(smart, 25);
            TopDocs dumbDocs = searcher.Search(dumb, 25);
            CheckHits.CheckEqual(smart, smartDocs.ScoreDocs, dumbDocs.ScoreDocs);
        }
    }
}