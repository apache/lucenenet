// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Index.Extensions;
using Lucene.Net.Queries.Function;
using Lucene.Net.Queries.Function.ValueSources;
using Lucene.Net.Search;
using Lucene.Net.Search.Similarities;
using Lucene.Net.Store;
using Lucene.Net.Util;
using NUnit.Framework;
using System;

namespace Lucene.Net.Tests.Queries.Function
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

    [SuppressCodecs("Lucene3x")]
    public class TestLongNormValueSource : LuceneTestCase
    {
        internal static Directory dir;
        internal static IndexReader reader;
        internal static IndexSearcher searcher;
        private static Similarity sim = new PreciseDefaultSimilarity();
        
        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            dir = NewDirectory();
            IndexWriterConfig iwConfig = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random));
            iwConfig.SetMergePolicy(NewLogMergePolicy());
            iwConfig.SetSimilarity(sim);
            RandomIndexWriter iw = new RandomIndexWriter(Random, dir, iwConfig);

            Document doc = new Document();
            doc.Add(new TextField("text", "this is a test test test", Field.Store.NO));
            iw.AddDocument(doc);

            doc = new Document();
            doc.Add(new TextField("text", "second test", Field.Store.NO));
            iw.AddDocument(doc);

            reader = iw.GetReader();
            searcher = NewSearcher(reader);
            iw.Dispose();
        }
        
        [TearDown]
        public override void TearDown()
        {
            searcher = null;
            reader.Dispose();
            reader = null;
            dir.Dispose();
            dir = null;
            base.TearDown();
        }
        
        [Test]
        public void TestNorm()
        {
            Similarity saved = searcher.Similarity;
            try
            {
                // no norm field (so agnostic to indexed similarity)
                searcher.Similarity = sim;
                AssertHits(new FunctionQuery(
                    new NormValueSource("text")),
                    new float[] { 0f, 0f });
            }
            finally
            {
                searcher.Similarity = saved;
            }
        }
        
        internal void AssertHits(Query q, float[] scores)
        {
            ScoreDoc[] expected = new ScoreDoc[scores.Length];
            int[] expectedDocs = new int[scores.Length];
            for (int i = 0; i < expected.Length; i++)
            {
                expectedDocs[i] = i;
                expected[i] = new ScoreDoc(i, scores[i]);
            }
            TopDocs docs = searcher.Search(q, 2, new Sort(new SortField("id", SortFieldType.STRING)));

            /*
            for (int i=0;i<docs.scoreDocs.length;i++) {
              System.out.println(searcher.explain(q, docs.scoreDocs[i].doc));
            }
            */

            CheckHits.DoCheckHits(Random, q, "", searcher, expectedDocs);
            CheckHits.CheckHitsQuery(q, expected, docs.ScoreDocs, expectedDocs);
            CheckHits.CheckExplanations(q, "", searcher);
        }
    }

    /// <summary>
    /// Encodes norm as 4-byte float. </summary>
    internal class PreciseDefaultSimilarity : TFIDFSimilarity
    {
        /// <summary>
        /// Sole constructor: parameter-free </summary>
        public PreciseDefaultSimilarity()
        {
        }

        /// <summary>
        /// Implemented as <code>overlap / maxOverlap</code>. </summary>
        public override float Coord(int overlap, int maxOverlap)
        {
            return overlap / (float)maxOverlap;
        }

        /// <summary>
        /// Implemented as <code>1/sqrt(sumOfSquaredWeights)</code>. </summary>
        public override float QueryNorm(float sumOfSquaredWeights)
        {
            return (float)(1.0 / Math.Sqrt(sumOfSquaredWeights));
        }

        /// <summary>
        /// Encodes a normalization factor for storage in an index.
        /// <p>
        /// The encoding uses a three-bit mantissa, a five-bit exponent, and the
        /// zero-exponent point at 15, thus representing values from around 7x10^9 to
        /// 2x10^-9 with about one significant decimal digit of accuracy. Zero is also
        /// represented. Negative numbers are rounded up to zero. Values too large to
        /// represent are rounded down to the largest representable value. Positive
        /// values too small to represent are rounded up to the smallest positive
        /// representable value.
        /// </summary>
        /// <seealso cref= org.apache.lucene.document.Field#setBoost(float) </seealso>
        /// <seealso cref= org.apache.lucene.util.SmallFloat </seealso>
        public override long EncodeNormValue(float f)
        {
            return J2N.BitConversion.SingleToInt32Bits(f);
        }

        /// <summary>
        /// Decodes the norm value, assuming it is a single byte.
        /// </summary>
        /// <seealso cref= #encodeNormValue(float) </seealso>
        public override float DecodeNormValue(long norm)
        {
            return J2N.BitConversion.Int32BitsToSingle((int)norm);
        }

        /// <summary>
        /// Implemented as
        /// <c>state.Boost*LengthNorm(numTerms)</c>, where
        /// <c>numTerms</c> is <see cref="FieldInvertState.Length"/> if 
        /// <see cref="DiscountOverlaps"/># is false, else it's 
        /// <see cref="FieldInvertState.Length"/> - 
        /// <see cref="FieldInvertState.NumOverlap"/>.
        /// <para/>
        /// @lucene.experimental 
        /// </summary>
        public override float LengthNorm(FieldInvertState state)
        {
            int numTerms;
            if (discountOverlaps)
            {
                numTerms = state.Length - state.NumOverlap;
            }
            else
            {
                numTerms = state.Length;
            }
            return state.Boost * ((float)(1.0 / Math.Sqrt(numTerms)));
        }

        /// <summary>
        /// Implemented as <code>sqrt(freq)</code>. </summary>
        public override float Tf(float freq)
        {
            return (float)Math.Sqrt(freq);
        }

        /// <summary>
        /// Implemented as <code>1 / (distance + 1)</code>. 
        /// </summary>
        public override float SloppyFreq(int distance)
        {
            return 1.0f / (distance + 1);
        }

        /// <summary>
        /// The default implementation returns <code>1</code>
        /// </summary>
        public override float ScorePayload(int doc, int start, int end, BytesRef payload)
        {
            return 1;
        }

        /// <summary>
        /// Implemented as <code>log(numDocs/(docFreq+1)) + 1</code>. 
        /// </summary>
        public override float Idf(long docFreq, long numDocs)
        {
            return (float)(Math.Log(numDocs / (double)(docFreq + 1)) + 1.0);
        }

        /// <summary>
        /// True if overlap tokens (tokens with a position of increment of zero) are
        /// discounted from the document's length.
        /// </summary>
        protected internal bool discountOverlaps = true;

        /// <summary>
        /// Determines whether overlap tokens (Tokens with
        ///  0 position increment) are ignored when computing
        ///  norm.  By default this is true, meaning overlap
        ///  tokens do not count when computing norms.
        /// 
        ///  @lucene.experimental
        /// </summary>
        ///  <seealso cref= #computeNorm </seealso>
        public virtual bool DiscountOverlaps
        {
            set => discountOverlaps = value;
            get => discountOverlaps;
        }

        public override string ToString()
        {
            return "DefaultSimilarity";
        }
    }
}
