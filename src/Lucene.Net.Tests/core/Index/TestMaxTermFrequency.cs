using System;
using System.Collections.Generic;
using System.Linq;
using Lucene.Net.Documents;

namespace Lucene.Net.Index
{
    using Lucene.Net.Support;
    using NUnit.Framework;
    using BytesRef = Lucene.Net.Util.BytesRef;
    using Directory = Lucene.Net.Store.Directory;
    using Document = Documents.Document;
    using Field = Field;
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
    using TestUtil = Lucene.Net.Util.TestUtil;
    using TFIDFSimilarity = Lucene.Net.Search.Similarities.TFIDFSimilarity;

    /// <summary>
    /// Tests the maxTermFrequency statistic in FieldInvertState
    /// </summary>
    [TestFixture]
    public class TestMaxTermFrequency : LuceneTestCase
    {
        internal Directory Dir;
        internal IndexReader Reader;
        /* expected maxTermFrequency values for our documents */
        internal List<int?> Expected = new List<int?>();

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            Dir = NewDirectory();
            IndexWriterConfig config = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random(), MockTokenizer.SIMPLE, true)).SetMergePolicy(NewLogMergePolicy());
            config.SetSimilarity(new TestSimilarity(this));
            RandomIndexWriter writer = new RandomIndexWriter(Random(), Dir, config);
            Document doc = new Document();
            Field foo = NewTextField("foo", "", Field.Store.NO);
            doc.Add(foo);
            for (int i = 0; i < 100; i++)
            {
                foo.SetStringValue(AddValue());
                writer.AddDocument(doc);
            }
            Reader = writer.Reader;
            writer.Dispose();
        }

        [TearDown]
        public override void TearDown()
        {
            Reader.Dispose();
            Dir.Dispose();
            base.TearDown();
        }

        [Test]
        public virtual void Test()
        {
            NumericDocValues fooNorms = MultiDocValues.GetNormValues(Reader, "foo");
            for (int i = 0; i < Reader.MaxDoc; i++)
            {
                Assert.AreEqual((int)Expected[i], fooNorms.Get(i) & 0xff);
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
            IList<string> terms = new List<string>();
            int maxCeiling = TestUtil.NextInt(Random(), 0, 255);
            int max = 0;
            for (char ch = 'a'; ch <= 'z'; ch++)
            {
                int num = TestUtil.NextInt(Random(), 0, maxCeiling);
                for (int i = 0; i < num; i++)
                {
                    terms.Add(char.ToString(ch));
                }
                max = Math.Max(max, num);
            }
            Expected.Add(max);

            terms = CollectionsHelper.Shuffle(terms);
            return Arrays.ToString(terms.ToArray());
        }

        /// <summary>
        /// Simple similarity that encodes maxTermFrequency directly as a byte
        /// </summary>
        internal class TestSimilarity : TFIDFSimilarity
        {
            private readonly TestMaxTermFrequency OuterInstance;

            public TestSimilarity(TestMaxTermFrequency outerInstance)
            {
                this.OuterInstance = outerInstance;
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