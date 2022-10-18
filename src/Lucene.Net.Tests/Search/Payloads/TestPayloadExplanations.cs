using NUnit.Framework;

namespace Lucene.Net.Search.Payloads
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
    using DefaultSimilarity = Lucene.Net.Search.Similarities.DefaultSimilarity;
    using SpanQuery = Lucene.Net.Search.Spans.SpanQuery;
    using Term = Lucene.Net.Index.Term;

    /// <summary>
    /// TestExplanations subclass focusing on payload queries
    /// </summary>
    [TestFixture]
    public class TestPayloadExplanations : TestExplanations
    {
        private readonly PayloadFunction[] functions = new PayloadFunction[]
        {
            new AveragePayloadFunction(),
            new MinPayloadFunction(),
            new MaxPayloadFunction()
        };

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            searcher.Similarity = new DefaultSimilarityAnonymousClass(this);
        }

        private sealed class DefaultSimilarityAnonymousClass : DefaultSimilarity
        {
            private readonly TestPayloadExplanations outerInstance;

            public DefaultSimilarityAnonymousClass(TestPayloadExplanations outerInstance)
            {
                this.outerInstance = outerInstance;
            }

            public override float ScorePayload(int doc, int start, int end, BytesRef payload)
            {
                return 1 + (payload.GetHashCode() % 10);
            }
        }

        /// <summary>
        /// macro for payloadtermquery </summary>
        private SpanQuery Pt(string s, PayloadFunction fn, bool includeSpanScore)
        {
            return new PayloadTermQuery(new Term(FIELD, s), fn, includeSpanScore);
        }

        /* simple PayloadTermQueries */

        [Test]
        public virtual void TestPT1()
        {
            foreach (PayloadFunction fn in functions)
            {
                Qtest(Pt("w1", fn, false), new int[] { 0, 1, 2, 3 });
                Qtest(Pt("w1", fn, true), new int[] { 0, 1, 2, 3 });
            }
        }

        [Test]
        public virtual void TestPT2()
        {
            foreach (PayloadFunction fn in functions)
            {
                SpanQuery q = Pt("w1", fn, false);
                q.Boost = 1000;
                Qtest(q, new int[] { 0, 1, 2, 3 });
                q = Pt("w1", fn, true);
                q.Boost = 1000;
                Qtest(q, new int[] { 0, 1, 2, 3 });
            }
        }

        [Test]
        public virtual void TestPT4()
        {
            foreach (PayloadFunction fn in functions)
            {
                Qtest(Pt("xx", fn, false), new int[] { 2, 3 });
                Qtest(Pt("xx", fn, true), new int[] { 2, 3 });
            }
        }

        [Test]
        public virtual void TestPT5()
        {
            foreach (PayloadFunction fn in functions)
            {
                SpanQuery q = Pt("xx", fn, false);
                q.Boost = 1000;
                Qtest(q, new int[] { 2, 3 });
                q = Pt("xx", fn, true);
                q.Boost = 1000;
                Qtest(q, new int[] { 2, 3 });
            }
        }

        // TODO: test the payloadnear query too!
    }
}