using System;
using Lucene.Net.Documents;
using Lucene.Net.Index.Extensions;
using NUnit.Framework;
using Assert = Lucene.Net.TestFramework.Assert;

namespace Lucene.Net.Index
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

    using BytesRef = Lucene.Net.Util.BytesRef;
    using CollectionStatistics = Lucene.Net.Search.CollectionStatistics;
    using DefaultSimilarity = Lucene.Net.Search.Similarities.DefaultSimilarity;
    using Directory = Lucene.Net.Store.Directory;
    using Document = Documents.Document;
    using Field = Field;
    using LineFileDocs = Lucene.Net.Util.LineFileDocs;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
    using PerFieldSimilarityWrapper = Lucene.Net.Search.Similarities.PerFieldSimilarityWrapper;
    using Similarity = Lucene.Net.Search.Similarities.Similarity;
    using TermStatistics = Lucene.Net.Search.TermStatistics;
    using TestUtil = Lucene.Net.Util.TestUtil;
    using TextField = TextField;
    using TFIDFSimilarity = Lucene.Net.Search.Similarities.TFIDFSimilarity;

    /// <summary>
    /// Test that norms info is preserved during index life - including
    /// separate norms, addDocument, addIndexes, forceMerge.
    /// </summary>
    [SuppressCodecs("Memory", "Direct", "SimpleText")]
    //[Slow] // LUCENENET specific - not slow in .NET
    [TestFixture]
    public class TestNorms : LuceneTestCase
    {
        private readonly string byteTestField = "normsTestByte";

        internal class CustomNormEncodingSimilarity : TFIDFSimilarity
        {
            private readonly TestNorms outerInstance;

            public CustomNormEncodingSimilarity(TestNorms outerInstance)
            {
                this.outerInstance = outerInstance;
            }

            public override long EncodeNormValue(float f)
            {
                return (long)f;
            }

            public override float DecodeNormValue(long norm)
            {
                return norm;
            }

            public override float LengthNorm(FieldInvertState state)
            {
                return state.Length;
            }

            public override float Coord(int overlap, int maxOverlap)
            {
                return 0;
            }

            public override float QueryNorm(float sumOfSquaredWeights)
            {
                return 0;
            }

            public override float Tf(float freq)
            {
                return 0;
            }

            public override float Idf(long docFreq, long numDocs)
            {
                return 0;
            }

            public override float SloppyFreq(int distance)
            {
                return 0;
            }

            public override float ScorePayload(int doc, int start, int end, BytesRef payload)
            {
                return 0;
            }
        }

        // LUCENE-1260
        [Test]
        public virtual void TestCustomEncoder()
        {
            Directory dir = NewDirectory();
            MockAnalyzer analyzer = new MockAnalyzer(Random);

            IndexWriterConfig config = NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
            config.SetSimilarity(new CustomNormEncodingSimilarity(this));
            RandomIndexWriter writer = new RandomIndexWriter(Random, dir, config);
            Document doc = new Document();
            Field foo = NewTextField("foo", "", Field.Store.NO);
            Field bar = NewTextField("bar", "", Field.Store.NO);
            doc.Add(foo);
            doc.Add(bar);

            for (int i = 0; i < 100; i++)
            {
                bar.SetStringValue("singleton");
                writer.AddDocument(doc);
            }

            IndexReader reader = writer.GetReader();
            writer.Dispose();

            NumericDocValues fooNorms = MultiDocValues.GetNormValues(reader, "foo");
            for (int i = 0; i < reader.MaxDoc; i++)
            {
                Assert.AreEqual(0, fooNorms.Get(i));
            }

            NumericDocValues barNorms = MultiDocValues.GetNormValues(reader, "bar");
            for (int i = 0; i < reader.MaxDoc; i++)
            {
                Assert.AreEqual(1, barNorms.Get(i));
            }

            reader.Dispose();
            dir.Dispose();
        }

        [Test]
        public virtual void TestMaxByteNorms()
        {
            Directory dir = NewFSDirectory(CreateTempDir("TestNorms.testMaxByteNorms"));
            BuildIndex(dir);
            AtomicReader open = SlowCompositeReaderWrapper.Wrap(DirectoryReader.Open(dir));
            NumericDocValues normValues = open.GetNormValues(byteTestField);
            Assert.IsNotNull(normValues);
            for (int i = 0; i < open.MaxDoc; i++)
            {
                Document document = open.Document(i);
                int expected = Convert.ToInt32(document.Get(byteTestField));
                Assert.AreEqual(expected, normValues.Get(i) & 0xff);
            }
            open.Dispose();
            dir.Dispose();
        }

        // TODO: create a testNormsNotPresent ourselves by adding/deleting/merging docs

        public virtual void BuildIndex(Directory dir)
        {
            Random random = Random;
            MockAnalyzer analyzer = new MockAnalyzer(LuceneTestCase.Random);
            analyzer.MaxTokenLength = TestUtil.NextInt32(LuceneTestCase.Random, 1, IndexWriter.MAX_TERM_LENGTH);
            IndexWriterConfig config = NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
            Similarity provider = new MySimProvider(this);
            config.SetSimilarity(provider);
            RandomIndexWriter writer = new RandomIndexWriter(random, dir, config);
            LineFileDocs docs = new LineFileDocs(random, DefaultCodecSupportsDocValues);
            int num = AtLeast(100);
            for (int i = 0; i < num; i++)
            {
                Document doc = docs.NextDoc();
                int boost = LuceneTestCase.Random.Next(255);
                Field f = new TextField(byteTestField, "" + boost, Field.Store.YES);
                f.Boost = boost;
                doc.Add(f);
                writer.AddDocument(doc);
                doc.RemoveField(byteTestField);
                if (Rarely())
                {
                    writer.Commit();
                }
            }
            writer.Commit();
            writer.Dispose();
            docs.Dispose();
        }

        public class MySimProvider : PerFieldSimilarityWrapper
        {
            private readonly TestNorms outerInstance;

            public MySimProvider(TestNorms outerInstance)
            {
                this.outerInstance = outerInstance;
            }

            internal Similarity @delegate = new DefaultSimilarity();

            public override float QueryNorm(float sumOfSquaredWeights)
            {
                return @delegate.QueryNorm(sumOfSquaredWeights);
            }

            public override Similarity Get(string field)
            {
                if (outerInstance.byteTestField.Equals(field, StringComparison.Ordinal))
                {
                    return new ByteEncodingBoostSimilarity();
                }
                else
                {
                    return @delegate;
                }
            }

            public override float Coord(int overlap, int maxOverlap)
            {
                return @delegate.Coord(overlap, maxOverlap);
            }
        }

        public class ByteEncodingBoostSimilarity : Similarity
        {
            public override long ComputeNorm(FieldInvertState state)
            {
                int boost = (int)state.Boost;
                return (sbyte)boost;
            }

            public override SimWeight ComputeWeight(float queryBoost, CollectionStatistics collectionStats, params TermStatistics[] termStats)
            {
                throw UnsupportedOperationException.Create();
            }

            public override SimScorer GetSimScorer(SimWeight weight, AtomicReaderContext context)
            {
                throw UnsupportedOperationException.Create();
            }
        }
    }
}