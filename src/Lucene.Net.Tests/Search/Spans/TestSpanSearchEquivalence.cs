using NUnit.Framework;

namespace Lucene.Net.Search.Spans
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

    using Occur = Lucene.Net.Search.Occur;
    using Term = Lucene.Net.Index.Term;

    /// <summary>
    /// Basic equivalence tests for span queries
    /// </summary>
    [TestFixture]
    public class TestSpanSearchEquivalence : SearchEquivalenceTestBase
    {
        // TODO: we could go a little crazy for a lot of these,
        // but these are just simple minimal cases in case something
        // goes horribly wrong. Put more intense tests elsewhere.

        /// <summary>
        /// SpanTermQuery(A) = TermQuery(A) </summary>
        [Test]
        public virtual void TestSpanTermVersusTerm()
        {
            Term t1 = RandomTerm();
            AssertSameSet(new TermQuery(t1), new SpanTermQuery(t1));
        }

        /// <summary>
        /// SpanOrQuery(A, B) = (A B) </summary>
        [Test]
        public virtual void TestSpanOrVersusBoolean()
        {
            Term t1 = RandomTerm();
            Term t2 = RandomTerm();
            BooleanQuery q1 = new BooleanQuery();
            q1.Add(new TermQuery(t1), Occur.SHOULD);
            q1.Add(new TermQuery(t2), Occur.SHOULD);
            SpanOrQuery q2 = new SpanOrQuery(new SpanTermQuery(t1), new SpanTermQuery(t2));
            AssertSameSet(q1, q2);
        }

        /// <summary>
        /// SpanNotQuery(A, B) ⊆ SpanTermQuery(A) </summary>
        [Test]
        public virtual void TestSpanNotVersusSpanTerm()
        {
            Term t1 = RandomTerm();
            Term t2 = RandomTerm();
            AssertSubsetOf(new SpanNotQuery(new SpanTermQuery(t1), new SpanTermQuery(t2)), new SpanTermQuery(t1));
        }

        /// <summary>
        /// SpanFirstQuery(A, 10) ⊆ SpanTermQuery(A) </summary>
        [Test]
        public virtual void TestSpanFirstVersusSpanTerm()
        {
            Term t1 = RandomTerm();
            AssertSubsetOf(new SpanFirstQuery(new SpanTermQuery(t1), 10), new SpanTermQuery(t1));
        }

        /// <summary>
        /// SpanNearQuery([A, B], 0, true) = "A B" </summary>
        [Test]
        public virtual void TestSpanNearVersusPhrase()
        {
            Term t1 = RandomTerm();
            Term t2 = RandomTerm();
            SpanQuery[] subquery = new SpanQuery[] { new SpanTermQuery(t1), new SpanTermQuery(t2) };
            SpanNearQuery q1 = new SpanNearQuery(subquery, 0, true);
            PhraseQuery q2 = new PhraseQuery();
            q2.Add(t1);
            q2.Add(t2);
            AssertSameSet(q1, q2);
        }

        /// <summary>
        /// SpanNearQuery([A, B], ∞, false) = +A +B </summary>
        [Test]
        public virtual void TestSpanNearVersusBooleanAnd()
        {
            Term t1 = RandomTerm();
            Term t2 = RandomTerm();
            SpanQuery[] subquery = new SpanQuery[] { new SpanTermQuery(t1), new SpanTermQuery(t2) };
            SpanNearQuery q1 = new SpanNearQuery(subquery, int.MaxValue, false);
            BooleanQuery q2 = new BooleanQuery();
            q2.Add(new TermQuery(t1), Occur.MUST);
            q2.Add(new TermQuery(t2), Occur.MUST);
            AssertSameSet(q1, q2);
        }

        /// <summary>
        /// SpanNearQuery([A B], 0, false) ⊆ SpanNearQuery([A B], 1, false) </summary>
        [Test]
        public virtual void TestSpanNearVersusSloppySpanNear()
        {
            Term t1 = RandomTerm();
            Term t2 = RandomTerm();
            SpanQuery[] subquery = new SpanQuery[] { new SpanTermQuery(t1), new SpanTermQuery(t2) };
            SpanNearQuery q1 = new SpanNearQuery(subquery, 0, false);
            SpanNearQuery q2 = new SpanNearQuery(subquery, 1, false);
            AssertSubsetOf(q1, q2);
        }

        /// <summary>
        /// SpanNearQuery([A B], 3, true) ⊆ SpanNearQuery([A B], 3, false) </summary>
        [Test]
        public virtual void TestSpanNearInOrderVersusOutOfOrder()
        {
            Term t1 = RandomTerm();
            Term t2 = RandomTerm();
            SpanQuery[] subquery = new SpanQuery[] { new SpanTermQuery(t1), new SpanTermQuery(t2) };
            SpanNearQuery q1 = new SpanNearQuery(subquery, 3, true);
            SpanNearQuery q2 = new SpanNearQuery(subquery, 3, false);
            AssertSubsetOf(q1, q2);
        }
    }
}