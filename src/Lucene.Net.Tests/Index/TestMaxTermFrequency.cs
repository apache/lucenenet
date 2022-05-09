using J2N.Collections.Generic.Extensions;
using Lucene.Net.Documents;
using Lucene.Net.Index.Extensions;
using Lucene.Net.Support;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using JCG = J2N.Collections.Generic;
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
    using Directory = Lucene.Net.Store.Directory;
    using Document = Documents.Document;
    using Field = Field;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
    using MockTokenizer = Lucene.Net.Analysis.MockTokenizer;
    using TestUtil = Lucene.Net.Util.TestUtil;
    using TFIDFSimilarity = Lucene.Net.Search.Similarities.TFIDFSimilarity;

    /// <summary>
    /// Tests the maxTermFrequency statistic in FieldInvertState
    /// </summary>
    [TestFixture]
    public class TestMaxTermFrequency : LuceneTestCase
    {
        private Directory dir;
        private IndexReader reader;
        /* expected maxTermFrequency values for our documents */
        private readonly IList<int> expected = new JCG.List<int>();

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            dir = NewDirectory();
            IndexWriterConfig config = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random, MockTokenizer.SIMPLE, true)).SetMergePolicy(NewLogMergePolicy());
            config.SetSimilarity(new TestSimilarity(this));
            RandomIndexWriter writer = new RandomIndexWriter(Random, dir, config);
            Document doc = new Document();
            Field foo = NewTextField("foo", "", Field.Store.NO);
            doc.Add(foo);
            for (int i = 0; i < 100; i++)
            {
                foo.SetStringValue(AddValue());
                writer.AddDocument(doc);
            }
            reader = writer.GetReader();
            writer.Dispose();
        }

        [TearDown]
        public override void TearDown()
        {
            reader.Dispose();
            dir.Dispose();
            base.TearDown();
        }

        [Test]
        public virtual void Test()
        {
            NumericDocValues fooNorms = MultiDocValues.GetNormValues(reader, "foo");
            for (int i = 0; i < reader.MaxDoc; i++)
            {
                Assert.AreEqual(expected[i], fooNorms.Get(i) & 0xff);
            }
        }

        /// <summary>
        /// Makes a bunch of single-char tokens (the max freq will at most be 255).
        /// shuffles them around, and returns the whole list with Arrays.toString().
        /// this works fine because we use lettertokenizer.
        /// puts the max-frequency term into expected, to be checked against the norm.
        /// </summary>
        private string AddValue()
        {
            IList<string> terms = new JCG.List<string>();
            int maxCeiling = TestUtil.NextInt32(Random, 0, 255);
            int max = 0;
            for (char ch = 'a'; ch <= 'z'; ch++)
            {
                int num = TestUtil.NextInt32(Random, 0, maxCeiling);
                for (int i = 0; i < num; i++)
                {
                    terms.Add(char.ToString(ch));
                }
                max = Math.Max(max, num);
            }
            expected.Add(max);

            terms.Shuffle(Random);
            return Collections.ToString(terms);
        }

        /// <summary>
        /// Simple similarity that encodes maxTermFrequency directly as a byte
        /// </summary>
        internal class TestSimilarity : TFIDFSimilarity
        {
            private readonly TestMaxTermFrequency outerInstance;

            public TestSimilarity(TestMaxTermFrequency outerInstance)
            {
                this.outerInstance = outerInstance;
            }

            public override float LengthNorm(FieldInvertState state)
            {
                return state.MaxTermFrequency;
            }

            public override long EncodeNormValue(float f)
            {
                return (sbyte)f;
            }

            public override float DecodeNormValue(long norm)
            {
                return norm;
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
    }
}