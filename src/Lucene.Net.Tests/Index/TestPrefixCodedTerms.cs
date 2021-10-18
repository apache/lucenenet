using Lucene.Net.Util;
using NUnit.Framework;
using System.Collections.Generic;
using Assert = Lucene.Net.TestFramework.Assert;
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

    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using TestUtil = Lucene.Net.Util.TestUtil;

    [TestFixture]
    public class TestPrefixCodedTerms : LuceneTestCase
    {
        [Test]
        public virtual void TestEmpty()
        {
            PrefixCodedTerms.Builder b = new PrefixCodedTerms.Builder();
            PrefixCodedTerms pb = b.Finish();
            Assert.IsFalse(pb.GetEnumerator().MoveNext());
        }

        [Test]
        public virtual void TestOne()
        {
            Term term = new Term("foo", "bogus");
            PrefixCodedTerms.Builder b = new PrefixCodedTerms.Builder();
            b.Add(term);
            PrefixCodedTerms pb = b.Finish();
            IEnumerator<Term> iterator = pb.GetEnumerator();
            Assert.IsTrue(iterator.MoveNext());
            Assert.AreEqual(term, iterator.Current);
        }

        [Test]
        public virtual void TestRandom()
        {
            JCG.SortedSet<Term> terms = new JCG.SortedSet<Term>();
            int nterms = AtLeast(10000);
            for (int i = 0; i < nterms; i++)
            {
                Term term = new Term(TestUtil.RandomUnicodeString(Random, 2), TestUtil.RandomUnicodeString(Random));
                terms.Add(term);
            }

            PrefixCodedTerms.Builder b = new PrefixCodedTerms.Builder();
            foreach (Term @ref in terms)
            {
                b.Add(@ref);
            }
            PrefixCodedTerms pb = b.Finish();

            IEnumerator<Term> expected = terms.GetEnumerator();
            foreach (Term t in pb)
            {
                Assert.IsTrue(expected.MoveNext());
                Assert.AreEqual(expected.Current, t);
            }
            Assert.IsFalse(expected.MoveNext());
        }

        [Test]
        public virtual void TestMergeOne()
        {
            Term t1 = new Term("foo", "a");
            PrefixCodedTerms.Builder b1 = new PrefixCodedTerms.Builder();
            b1.Add(t1);
            PrefixCodedTerms pb1 = b1.Finish();

            Term t2 = new Term("foo", "b");
            PrefixCodedTerms.Builder b2 = new PrefixCodedTerms.Builder();
            b2.Add(t2);
            PrefixCodedTerms pb2 = b2.Finish();

            IEnumerator<Term> merged = new MergedEnumerator<Term>(pb1.GetEnumerator(), pb2.GetEnumerator());
            Assert.IsTrue(merged.MoveNext());
            Assert.AreEqual(t1, merged.Current);
            Assert.IsTrue(merged.MoveNext());
            Assert.AreEqual(t2, merged.Current);
        }

        [Test]
        public virtual void TestMergeRandom()
        {
            PrefixCodedTerms[] pb = new PrefixCodedTerms[TestUtil.NextInt32(Random, 2, 10)];
            JCG.SortedSet<Term> superSet = new JCG.SortedSet<Term>();

            for (int i = 0; i < pb.Length; i++)
            {
                JCG.SortedSet<Term> terms = new JCG.SortedSet<Term>();
                int nterms = TestUtil.NextInt32(Random, 0, 10000);
                for (int j = 0; j < nterms; j++)
                {
                    Term term = new Term(TestUtil.RandomUnicodeString(Random, 2), TestUtil.RandomUnicodeString(Random, 4));
                    terms.Add(term);
                }
                superSet.UnionWith(terms);

                PrefixCodedTerms.Builder b = new PrefixCodedTerms.Builder();
                foreach (Term @ref in terms)
                {
                    b.Add(@ref);
                }
                pb[i] = b.Finish();
            }

            JCG.List<IEnumerator<Term>> subs = new JCG.List<IEnumerator<Term>>();
            for (int i = 0; i < pb.Length; i++)
            {
                subs.Add(pb[i].GetEnumerator());
            }

            IEnumerator<Term> expected = superSet.GetEnumerator();
            IEnumerator<Term> actual = new MergedEnumerator<Term>(subs.ToArray());
            while (actual.MoveNext())
            {
                Assert.IsTrue(expected.MoveNext());
                Assert.AreEqual(expected.Current, actual.Current);
            }
            Assert.IsFalse(expected.MoveNext());
        }
    }
}