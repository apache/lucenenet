using Lucene.Net.Diagnostics;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Index.Extensions;
using Lucene.Net.Support;
using NUnit.Framework;
using RandomizedTesting.Generators;
using System;
using Assert = Lucene.Net.TestFramework.Assert;

namespace Lucene.Net.Codecs.PerField
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

    using Analyzer = Lucene.Net.Analysis.Analyzer;
    using BaseDocValuesFormatTestCase = Lucene.Net.Index.BaseDocValuesFormatTestCase;
    using BinaryDocValues = Lucene.Net.Index.BinaryDocValues;
    using BinaryDocValuesField = BinaryDocValuesField;
    using BytesRef = Lucene.Net.Util.BytesRef;
    using Directory = Lucene.Net.Store.Directory;
    using DirectoryReader = Lucene.Net.Index.DirectoryReader;
    using Document = Documents.Document;
    using Field = Field;
    using IndexReader = Lucene.Net.Index.IndexReader;
    using IndexSearcher = Lucene.Net.Search.IndexSearcher;
    using IndexWriter = Lucene.Net.Index.IndexWriter;
    using IndexWriterConfig = Lucene.Net.Index.IndexWriterConfig;
    using Lucene46Codec = Lucene.Net.Codecs.Lucene46.Lucene46Codec;
    using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
    using NumericDocValues = Lucene.Net.Index.NumericDocValues;
    using NumericDocValuesField = NumericDocValuesField;
    using Query = Lucene.Net.Search.Query;
    using RandomCodec = Lucene.Net.Index.RandomCodec;
    using Term = Lucene.Net.Index.Term;
    using TermQuery = Lucene.Net.Search.TermQuery;
    using TestUtil = Lucene.Net.Util.TestUtil;
    using TopDocs = Lucene.Net.Search.TopDocs;

    /// <summary>
    /// Basic tests of PerFieldDocValuesFormat
    /// </summary>
    [TestFixture]
    public class TestPerFieldDocValuesFormat : BaseDocValuesFormatTestCase
    {
        private Codec codec;

        [SetUp]
        public override void SetUp()
        {
            codec = new RandomCodec(new J2N.Randomizer(Random.NextInt64()), Collections.EmptySet<string>());
            base.SetUp();
        }

        protected override Codec GetCodec()
        {
            return codec;
        }

        protected override bool CodecAcceptsHugeBinaryValues(string field)
        {
            return TestUtil.FieldSupportsHugeBinaryDocValues(field);
        }

        // just a simple trivial test
        // TODO: we should come up with a test that somehow checks that segment suffix
        // is respected by all codec apis (not just docvalues and postings)
        [Test]
        public virtual void TestTwoFieldsTwoFormats()
        {
            Analyzer analyzer = new MockAnalyzer(Random);

            Directory directory = NewDirectory();
            // we don't use RandomIndexWriter because it might add more docvalues than we expect !!!!1
            IndexWriterConfig iwc = NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
            DocValuesFormat fast = DocValuesFormat.ForName("Lucene45");
            DocValuesFormat slow = DocValuesFormat.ForName("SimpleText");
            iwc.SetCodec(new Lucene46CodecAnonymousClass(this, fast, slow));
            IndexWriter iwriter = new IndexWriter(directory, iwc);
            Document doc = new Document();
            string longTerm = "longtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongterm";
            string text = "this is the text to be indexed. " + longTerm;
            doc.Add(NewTextField("fieldname", text, Field.Store.YES));
            doc.Add(new NumericDocValuesField("dv1", 5));
            doc.Add(new BinaryDocValuesField("dv2", new BytesRef("hello world")));
            iwriter.AddDocument(doc);
            iwriter.Dispose();

            // Now search the index:
            IndexReader ireader = DirectoryReader.Open(directory); // read-only=true
            IndexSearcher isearcher = NewSearcher(ireader);

            Assert.AreEqual(1, isearcher.Search(new TermQuery(new Term("fieldname", longTerm)), 1).TotalHits);
            Query query = new TermQuery(new Term("fieldname", "text"));
            TopDocs hits = isearcher.Search(query, null, 1);
            Assert.AreEqual(1, hits.TotalHits);
            BytesRef scratch = new BytesRef();
            // Iterate through the results:
            for (int i = 0; i < hits.ScoreDocs.Length; i++)
            {
                Document hitDoc = isearcher.Doc(hits.ScoreDocs[i].Doc);
                Assert.AreEqual(text, hitDoc.Get("fieldname"));
                if (Debugging.AssertsEnabled) Debugging.Assert(ireader.Leaves.Count == 1);
                NumericDocValues dv = ((AtomicReader)ireader.Leaves[0].Reader).GetNumericDocValues("dv1");
                Assert.AreEqual(5, dv.Get(hits.ScoreDocs[i].Doc));
                BinaryDocValues dv2 = ((AtomicReader)ireader.Leaves[0].Reader).GetBinaryDocValues("dv2");
                dv2.Get(hits.ScoreDocs[i].Doc, scratch);
                Assert.AreEqual(new BytesRef("hello world"), scratch);
            }

            ireader.Dispose();
            directory.Dispose();
        }

        private sealed class Lucene46CodecAnonymousClass : Lucene46Codec
        {
            private readonly TestPerFieldDocValuesFormat outerInstance;

            private readonly DocValuesFormat fast;
            private readonly DocValuesFormat slow;

            public Lucene46CodecAnonymousClass(TestPerFieldDocValuesFormat outerInstance, DocValuesFormat fast, DocValuesFormat slow)
            {
                this.outerInstance = outerInstance;
                this.fast = fast;
                this.slow = slow;
            }

            public override DocValuesFormat GetDocValuesFormatForField(string field)
            {
                if ("dv1".Equals(field, StringComparison.Ordinal))
                {
                    return fast;
                }
                else
                {
                    return slow;
                }
            }
        }
    }
}