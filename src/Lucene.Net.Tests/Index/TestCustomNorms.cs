using Lucene.Net.Documents;
using Lucene.Net.Index.Extensions;
using NUnit.Framework;
using System;
using System.Globalization;
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

    [SuppressCodecs("Lucene3x")]
    [TestFixture]
    public class TestCustomNorms : LuceneTestCase
    {
        internal readonly string floatTestField = "normsTestFloat";
        internal readonly string exceptionTestField = "normsTestExcp";

        [Test]
        public virtual void TestFloatNorms()
        {
            Directory dir = NewDirectory();
            MockAnalyzer analyzer = new MockAnalyzer(Random);
            analyzer.MaxTokenLength = TestUtil.NextInt32(Random, 1, IndexWriter.MAX_TERM_LENGTH);

            IndexWriterConfig config = NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
            Similarity provider = new MySimProvider(this);
            config.SetSimilarity(provider);
            RandomIndexWriter writer = new RandomIndexWriter(Random, dir, config);
            LineFileDocs docs = new LineFileDocs(Random);
            int num = AtLeast(100);
            for (int i = 0; i < num; i++)
            {
                Document doc = docs.NextDoc();
                float nextFloat = Random.nextFloat();
                // Cast to a double to get more precision output to the string.
                Field f = new TextField(floatTestField, "" + ((double)nextFloat).ToString(CultureInfo.InvariantCulture), Field.Store.YES);
                f.Boost = nextFloat;

                doc.Add(f);
                writer.AddDocument(doc);
                doc.RemoveField(floatTestField);
                if (Rarely())
                {
                    writer.Commit();
                }
            }
            writer.Commit();
            writer.Dispose();
            AtomicReader open = SlowCompositeReaderWrapper.Wrap(DirectoryReader.Open(dir));
            NumericDocValues norms = open.GetNormValues(floatTestField);
            Assert.IsNotNull(norms);
            for (int i = 0; i < open.MaxDoc; i++)
            {
                Document document = open.Document(i);
                float expected = Convert.ToSingle(document.Get(floatTestField), CultureInfo.InvariantCulture);
                Assert.AreEqual(expected, J2N.BitConversion.Int32BitsToSingle((int)norms.Get(i)), 0.0f);
            }
            open.Dispose();
            dir.Dispose();
            docs.Dispose();
        }

        public class MySimProvider : PerFieldSimilarityWrapper
        {
            private readonly TestCustomNorms outerInstance;

            public MySimProvider(TestCustomNorms outerInstance)
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
                if (outerInstance.floatTestField.Equals(field, StringComparison.Ordinal))
                {
                    return new FloatEncodingBoostSimilarity();
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

        public class FloatEncodingBoostSimilarity : Similarity
        {
            public override long ComputeNorm(FieldInvertState state)
            {
                return J2N.BitConversion.SingleToInt32Bits(state.Boost);
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