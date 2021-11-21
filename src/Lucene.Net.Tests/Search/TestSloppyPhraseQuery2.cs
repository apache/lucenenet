using NUnit.Framework;
using RandomizedTesting.Generators;
using System;

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
    using TestUtil = Lucene.Net.Util.TestUtil;

    /// <summary>
    /// random sloppy phrase query tests
    /// </summary>
    [TestFixture]
    public class TestSloppyPhraseQuery2 : SearchEquivalenceTestBase
    {
        /// <summary>
        /// "A B"~N ⊆ "A B"~N+1 </summary>
        [Test]
        public virtual void TestIncreasingSloppiness()
        {
            Term t1 = RandomTerm();
            Term t2 = RandomTerm();
            PhraseQuery q1 = new PhraseQuery();
            q1.Add(t1);
            q1.Add(t2);
            PhraseQuery q2 = new PhraseQuery();
            q2.Add(t1);
            q2.Add(t2);
            for (int i = 0; i < 10; i++)
            {
                q1.Slop = i;
                q2.Slop = i + 1;
                AssertSubsetOf(q1, q2);
            }
        }

        /// <summary>
        /// same as the above with posincr </summary>
        [Test]
        public virtual void TestIncreasingSloppinessWithHoles()
        {
            Term t1 = RandomTerm();
            Term t2 = RandomTerm();
            PhraseQuery q1 = new PhraseQuery();
            q1.Add(t1);
            q1.Add(t2, 2);
            PhraseQuery q2 = new PhraseQuery();
            q2.Add(t1);
            q2.Add(t2, 2);
            for (int i = 0; i < 10; i++)
            {
                q1.Slop = i;
                q2.Slop = i + 1;
                AssertSubsetOf(q1, q2);
            }
        }

        /// <summary>
        /// "A B C"~N ⊆ "A B C"~N+1 </summary>
        [Test]
        public virtual void TestIncreasingSloppiness3()
        {
            Term t1 = RandomTerm();
            Term t2 = RandomTerm();
            Term t3 = RandomTerm();
            PhraseQuery q1 = new PhraseQuery();
            q1.Add(t1);
            q1.Add(t2);
            q1.Add(t3);
            PhraseQuery q2 = new PhraseQuery();
            q2.Add(t1);
            q2.Add(t2);
            q2.Add(t3);
            for (int i = 0; i < 10; i++)
            {
                q1.Slop = i;
                q2.Slop = i + 1;
                AssertSubsetOf(q1, q2);
            }
        }

        /// <summary>
        /// same as the above with posincr </summary>
        [Test]
        public virtual void TestIncreasingSloppiness3WithHoles()
        {
            Term t1 = RandomTerm();
            Term t2 = RandomTerm();
            Term t3 = RandomTerm();
            int pos1 = 1 + Random.Next(3);
            int pos2 = pos1 + 1 + Random.Next(3);
            PhraseQuery q1 = new PhraseQuery();
            q1.Add(t1);
            q1.Add(t2, pos1);
            q1.Add(t3, pos2);
            PhraseQuery q2 = new PhraseQuery();
            q2.Add(t1);
            q2.Add(t2, pos1);
            q2.Add(t3, pos2);
            for (int i = 0; i < 10; i++)
            {
                q1.Slop = i;
                q2.Slop = i + 1;
                AssertSubsetOf(q1, q2);
            }
        }

        /// <summary>
        /// "A A"~N ⊆ "A A"~N+1 </summary>
        [Test]
        public virtual void TestRepetitiveIncreasingSloppiness()
        {
            Term t = RandomTerm();
            PhraseQuery q1 = new PhraseQuery();
            q1.Add(t);
            q1.Add(t);
            PhraseQuery q2 = new PhraseQuery();
            q2.Add(t);
            q2.Add(t);
            for (int i = 0; i < 10; i++)
            {
                q1.Slop = i;
                q2.Slop = i + 1;
                AssertSubsetOf(q1, q2);
            }
        }

        /// <summary>
        /// same as the above with posincr </summary>
        [Test]
        public virtual void TestRepetitiveIncreasingSloppinessWithHoles()
        {
            Term t = RandomTerm();
            PhraseQuery q1 = new PhraseQuery();
            q1.Add(t);
            q1.Add(t, 2);
            PhraseQuery q2 = new PhraseQuery();
            q2.Add(t);
            q2.Add(t, 2);
            for (int i = 0; i < 10; i++)
            {
                q1.Slop = i;
                q2.Slop = i + 1;
                AssertSubsetOf(q1, q2);
            }
        }

        /// <summary>
        /// "A A A"~N ⊆ "A A A"~N+1 </summary>
        [Test]
        public virtual void TestRepetitiveIncreasingSloppiness3()
        {
            Term t = RandomTerm();
            PhraseQuery q1 = new PhraseQuery();
            q1.Add(t);
            q1.Add(t);
            q1.Add(t);
            PhraseQuery q2 = new PhraseQuery();
            q2.Add(t);
            q2.Add(t);
            q2.Add(t);
            for (int i = 0; i < 10; i++)
            {
                q1.Slop = i;
                q2.Slop = i + 1;
                AssertSubsetOf(q1, q2);
            }
        }

        /// <summary>
        /// same as the above with posincr </summary>
        [Test]
        public virtual void TestRepetitiveIncreasingSloppiness3WithHoles()
        {
            Term t = RandomTerm();
            int pos1 = 1 + Random.Next(3);
            int pos2 = pos1 + 1 + Random.Next(3);
            PhraseQuery q1 = new PhraseQuery();
            q1.Add(t);
            q1.Add(t, pos1);
            q1.Add(t, pos2);
            PhraseQuery q2 = new PhraseQuery();
            q2.Add(t);
            q2.Add(t, pos1);
            q2.Add(t, pos2);
            for (int i = 0; i < 10; i++)
            {
                q1.Slop = i;
                q2.Slop = i + 1;
                AssertSubsetOf(q1, q2);
            }
        }

        /// <summary>
        /// MultiPhraseQuery~N ⊆ MultiPhraseQuery~N+1 </summary>
        [Test]
        public virtual void TestRandomIncreasingSloppiness()
        {
            long seed = Random.NextInt64();
            MultiPhraseQuery q1 = RandomPhraseQuery(seed);
            MultiPhraseQuery q2 = RandomPhraseQuery(seed);
            for (int i = 0; i < 10; i++)
            {
                q1.Slop = i;
                q2.Slop = i + 1;
                AssertSubsetOf(q1, q2);
            }
        }

        private MultiPhraseQuery RandomPhraseQuery(long seed)
        {
            Random random = new J2N.Randomizer(seed);
            int length = TestUtil.NextInt32(random, 2, 5);
            MultiPhraseQuery pq = new MultiPhraseQuery();
            int position = 0;
            for (int i = 0; i < length; i++)
            {
                int depth = TestUtil.NextInt32(random, 1, 3);
                Term[] terms = new Term[depth];
                for (int j = 0; j < depth; j++)
                {
                    terms[j] = new Term("field", "" + (char)TestUtil.NextInt32(random, 'a', 'z'));
                }
                pq.Add(terms, position);
                position += TestUtil.NextInt32(random, 1, 3);
            }
            return pq;
        }
    }
}