using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Index.Extensions;
using Lucene.Net.Search;
using Lucene.Net.Search.Similarities;
using Lucene.Net.Store;
using Lucene.Net.Util;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using JCG = J2N.Collections.Generic;

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

    /// <summary>
    /// Tests the uniqueTermCount statistic in FieldInvertState
    /// </summary>
    public class TestUniqueTermCount : LuceneTestCase
    {
        Directory dir;
        IndexReader reader;
        /* expected uniqueTermCount values for our documents */
        IList<int> expected = new JCG.List<int>();

        public override void SetUp()
        {
            base.SetUp();
            dir = NewDirectory();
            MockAnalyzer analyzer = new MockAnalyzer(Random, MockTokenizer.SIMPLE, true);
            IndexWriterConfig config = NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
            config.SetMergePolicy(NewLogMergePolicy());
            config.SetSimilarity(new TestSimilarity());
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

        public override void TearDown()
        {
            reader.Dispose();
            dir.Dispose();
            base.TearDown();
        }

        [Test]
        public void Test()
        {
            NumericDocValues fooNorms = MultiDocValues.GetNormValues(reader, "foo");
            assertNotNull(fooNorms);
            for (int i = 0; i < reader.MaxDoc; i++)
            {
                assertEquals(expected[i], fooNorms.Get(i));
            }
        }

        /**
         * Makes a bunch of single-char tokens (the max # unique terms will at most be 26).
         * puts the # unique terms into expected, to be checked against the norm.
         */
        private string AddValue()
        {
            StringBuilder sb = new StringBuilder();
            ISet<string> terms = new JCG.HashSet<string>();
            int num = TestUtil.NextInt32(Random, 0, 255);
            for (int i = 0; i < num; i++)
            {
                sb.append(' ');
                char term = (char)TestUtil.NextInt32(Random, 'a', 'z');
                sb.append(term);
                terms.add("" + term);
            }
            expected.Add(terms.size());
            return sb.toString();
        }

        /**
         * Simple similarity that encodes maxTermFrequency directly
         */
        internal class TestSimilarity : Similarity
        {

            public override long ComputeNorm(FieldInvertState state)
            {
                return state.UniqueTermCount;
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
