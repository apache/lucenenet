using NUnit.Framework;

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

    using Term = Lucene.Net.Index.Term;

    /// <summary>
    /// Basic equivalence tests for core queries
    /// </summary>
    [TestFixture]
    public class TestSimpleSearchEquivalence : SearchEquivalenceTestBase
    {
        // TODO: we could go a little crazy for a lot of these,
        // but these are just simple minimal cases in case something
        // goes horribly wrong. Put more intense tests elsewhere.

        /// <summary>
        /// A ⊆ (A B) </summary>
        [Test]
        public virtual void TestTermVersusBooleanOr()
        {
            Term t1 = RandomTerm();
            Term t2 = RandomTerm();
            TermQuery q1 = new TermQuery(t1);
            BooleanQuery q2 = new BooleanQuery();
            q2.Add(new TermQuery(t1), Occur.SHOULD);
            q2.Add(new TermQuery(t2), Occur.SHOULD);
            AssertSubsetOf(q1, q2);
        }

        /// <summary>
        /// A ⊆ (+A B) </summary>
        [Test]
        public virtual void TestTermVersusBooleanReqOpt()
        {
            Term t1 = RandomTerm();
            Term t2 = RandomTerm();
            TermQuery q1 = new TermQuery(t1);
            BooleanQuery q2 = new BooleanQuery();
            q2.Add(new TermQuery(t1), Occur.MUST);
            q2.Add(new TermQuery(t2), Occur.SHOULD);
            AssertSubsetOf(q1, q2);
        }

        /// <summary>
        /// (A -B) ⊆ A </summary>
        [Test]
        public virtual void TestBooleanReqExclVersusTerm()
        {
            Term t1 = RandomTerm();
            Term t2 = RandomTerm();
            BooleanQuery q1 = new BooleanQuery();
            q1.Add(new TermQuery(t1), Occur.MUST);
            q1.Add(new TermQuery(t2), Occur.MUST_NOT);
            TermQuery q2 = new TermQuery(t1);
            AssertSubsetOf(q1, q2);
        }

        /// <summary>
        /// (+A +B) ⊆ (A B) </summary>
        [Test]
        public virtual void TestBooleanAndVersusBooleanOr()
        {
            Term t1 = RandomTerm();
            Term t2 = RandomTerm();
            BooleanQuery q1 = new BooleanQuery();
            q1.Add(new TermQuery(t1), Occur.SHOULD);
            q1.Add(new TermQuery(t2), Occur.SHOULD);
            BooleanQuery q2 = new BooleanQuery();
            q2.Add(new TermQuery(t1), Occur.SHOULD);
            q2.Add(new TermQuery(t2), Occur.SHOULD);
            AssertSubsetOf(q1, q2);
        }

        /// <summary>
        /// (A B) = (A | B) </summary>
        [Test]
        public virtual void TestDisjunctionSumVersusDisjunctionMax()
        {
            Term t1 = RandomTerm();
            Term t2 = RandomTerm();
            BooleanQuery q1 = new BooleanQuery();
            q1.Add(new TermQuery(t1), Occur.SHOULD);
            q1.Add(new TermQuery(t2), Occur.SHOULD);
            DisjunctionMaxQuery q2 = new DisjunctionMaxQuery(0.5f);
            q2.Add(new TermQuery(t1));
            q2.Add(new TermQuery(t2));
            AssertSameSet(q1, q2);
        }

        /// <summary>
        /// "A B" ⊆ (+A +B) </summary>
        [Test]
        public virtual void TestExactPhraseVersusBooleanAnd()
        {
            Term t1 = RandomTerm();
            Term t2 = RandomTerm();
            PhraseQuery q1 = new PhraseQuery();
            q1.Add(t1);
            q1.Add(t2);
            BooleanQuery q2 = new BooleanQuery();
            q2.Add(new TermQuery(t1), Occur.MUST);
            q2.Add(new TermQuery(t2), Occur.MUST);
            AssertSubsetOf(q1, q2);
        }

        /// <summary>
        /// same as above, with posincs </summary>
        [Test]
        public virtual void TestExactPhraseVersusBooleanAndWithHoles()
        {
            Term t1 = RandomTerm();
            Term t2 = RandomTerm();
            PhraseQuery q1 = new PhraseQuery();
            q1.Add(t1);
            q1.Add(t2, 2);
            BooleanQuery q2 = new BooleanQuery();
            q2.Add(new TermQuery(t1), Occur.MUST);
            q2.Add(new TermQuery(t2), Occur.MUST);
            AssertSubsetOf(q1, q2);
        }

        /// <summary>
        /// "A B" ⊆ "A B"~1 </summary>
        [Test]
        public virtual void TestPhraseVersusSloppyPhrase()
        {
            Term t1 = RandomTerm();
            Term t2 = RandomTerm();
            PhraseQuery q1 = new PhraseQuery();
            q1.Add(t1);
            q1.Add(t2);
            PhraseQuery q2 = new PhraseQuery();
            q2.Add(t1);
            q2.Add(t2);
            q2.Slop = 1;
            AssertSubsetOf(q1, q2);
        }

        /// <summary>
        /// same as above, with posincs </summary>
        [Test]
        public virtual void TestPhraseVersusSloppyPhraseWithHoles()
        {
            Term t1 = RandomTerm();
            Term t2 = RandomTerm();
            PhraseQuery q1 = new PhraseQuery();
            q1.Add(t1);
            q1.Add(t2, 2);
            PhraseQuery q2 = new PhraseQuery();
            q2.Add(t1);
            q2.Add(t2, 2);
            q2.Slop = 1;
            AssertSubsetOf(q1, q2);
        }

        /// <summary>
        /// "A B" ⊆ "A (B C)" </summary>
        [Test]
        public virtual void TestExactPhraseVersusMultiPhrase()
        {
            Term t1 = RandomTerm();
            Term t2 = RandomTerm();
            PhraseQuery q1 = new PhraseQuery();
            q1.Add(t1);
            q1.Add(t2);
            Term t3 = RandomTerm();
            MultiPhraseQuery q2 = new MultiPhraseQuery();
            q2.Add(t1);
            q2.Add(new Term[] { t2, t3 });
            AssertSubsetOf(q1, q2);
        }

        /// <summary>
        /// same as above, with posincs </summary>
        [Test]
        public virtual void TestExactPhraseVersusMultiPhraseWithHoles()
        {
            Term t1 = RandomTerm();
            Term t2 = RandomTerm();
            PhraseQuery q1 = new PhraseQuery();
            q1.Add(t1);
            q1.Add(t2, 2);
            Term t3 = RandomTerm();
            MultiPhraseQuery q2 = new MultiPhraseQuery();
            q2.Add(t1);
            q2.Add(new Term[] { t2, t3 }, 2);
            AssertSubsetOf(q1, q2);
        }

        /// <summary>
        /// "A B"~∞ = +A +B if A != B </summary>
        [Test]
        public virtual void TestSloppyPhraseVersusBooleanAnd()
        {
            Term t1 = RandomTerm();
            Term t2 = null;
            // semantics differ from SpanNear: SloppyPhrase handles repeats,
            // so we must ensure t1 != t2
            do
            {
                t2 = RandomTerm();
            } while (t1.Equals(t2));
            PhraseQuery q1 = new PhraseQuery();
            q1.Add(t1);
            q1.Add(t2);
            q1.Slop = int.MaxValue;
            BooleanQuery q2 = new BooleanQuery();
            q2.Add(new TermQuery(t1), Occur.MUST);
            q2.Add(new TermQuery(t2), Occur.MUST);
            AssertSameSet(q1, q2);
        }
    }
}