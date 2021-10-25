using Lucene.Net.Util;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Globalization;
using JCG = J2N.Collections.Generic;
using TermInfo = Lucene.Net.Search.VectorHighlight.FieldTermStack.TermInfo;
using Toffs = Lucene.Net.Search.VectorHighlight.FieldPhraseList.WeightedPhraseInfo.Toffs;
using WeightedPhraseInfo = Lucene.Net.Search.VectorHighlight.FieldPhraseList.WeightedPhraseInfo;

namespace Lucene.Net.Search.VectorHighlight
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

    public class FieldPhraseListTest : AbstractTestCase
    {
        [Test]
        public void Test1TermIndex()
        {
            make1d1fIndex("a");

            FieldQuery fq = new FieldQuery(tq("a"), true, true);
            FieldTermStack stack = new FieldTermStack(reader, 0, F, fq);
            FieldPhraseList fpl = new FieldPhraseList(stack, fq);
            assertEquals(1, fpl.PhraseList.size());
            assertEquals("a(1.0)((0,1))", fpl.PhraseList[0].ToString(CultureInfo.InvariantCulture)); // LUCENENET specific: use invariant culture, since we are culture-aware

            fq = new FieldQuery(tq("b"), true, true);
            stack = new FieldTermStack(reader, 0, F, fq);
            fpl = new FieldPhraseList(stack, fq);
            assertEquals(0, fpl.PhraseList.size());
        }

        [Test]
        public void Test2TermsIndex()
        {
            make1d1fIndex("a a");

            FieldQuery fq = new FieldQuery(tq("a"), true, true);
            FieldTermStack stack = new FieldTermStack(reader, 0, F, fq);
            FieldPhraseList fpl = new FieldPhraseList(stack, fq);
            assertEquals(2, fpl.PhraseList.size());
            assertEquals("a(1.0)((0,1))", fpl.PhraseList[0].ToString(CultureInfo.InvariantCulture)); // LUCENENET specific: use invariant culture, since we are culture-aware
            assertEquals("a(1.0)((2,3))", fpl.PhraseList[1].ToString(CultureInfo.InvariantCulture)); // LUCENENET specific: use invariant culture, since we are culture-aware
        }

        [Test]
        public void Test1PhraseIndex()
        {
            make1d1fIndex("a b");

            FieldQuery fq = new FieldQuery(pqF("a", "b"), true, true);
            FieldTermStack stack = new FieldTermStack(reader, 0, F, fq);
            FieldPhraseList fpl = new FieldPhraseList(stack, fq);
            assertEquals(1, fpl.PhraseList.size());
            assertEquals("ab(1.0)((0,3))", fpl.PhraseList[0].ToString(CultureInfo.InvariantCulture)); // LUCENENET specific: use invariant culture, since we are culture-aware

            fq = new FieldQuery(tq("b"), true, true);
            stack = new FieldTermStack(reader, 0, F, fq);
            fpl = new FieldPhraseList(stack, fq);
            assertEquals(1, fpl.PhraseList.size());
            assertEquals("b(1.0)((2,3))", fpl.PhraseList[0].ToString(CultureInfo.InvariantCulture)); // LUCENENET specific: use invariant culture, since we are culture-aware
        }

        [Test]
        public void Test1PhraseIndexB()
        {
            // 01 12 23 34 45 56 67 78 (offsets)
            // bb|bb|ba|ac|cb|ba|ab|bc
            //  0  1  2  3  4  5  6  7 (positions)
            make1d1fIndexB("bbbacbabc");

            FieldQuery fq = new FieldQuery(pqF("ba", "ac"), true, true);
            FieldTermStack stack = new FieldTermStack(reader, 0, F, fq);
            FieldPhraseList fpl = new FieldPhraseList(stack, fq);
            assertEquals(1, fpl.PhraseList.size());
            assertEquals("baac(1.0)((2,5))", fpl.PhraseList[0].ToString(CultureInfo.InvariantCulture)); // LUCENENET specific: use invariant culture, since we are culture-aware
        }

        [Test]
        public void Test2ConcatTermsIndexB()
        {
            // 01 12 23 (offsets)
            // ab|ba|ab
            //  0  1  2 (positions)
            make1d1fIndexB("abab");

            FieldQuery fq = new FieldQuery(tq("ab"), true, true);
            FieldTermStack stack = new FieldTermStack(reader, 0, F, fq);
            FieldPhraseList fpl = new FieldPhraseList(stack, fq);
            assertEquals(2, fpl.PhraseList.size());
            assertEquals("ab(1.0)((0,2))", fpl.PhraseList[0].ToString(CultureInfo.InvariantCulture)); // LUCENENET specific: use invariant culture, since we are culture-aware
            assertEquals("ab(1.0)((2,4))", fpl.PhraseList[1].ToString(CultureInfo.InvariantCulture)); // LUCENENET specific: use invariant culture, since we are culture-aware
        }

        [Test]
        public void Test2Terms1PhraseIndex()
        {
            make1d1fIndex("c a a b");

            // phraseHighlight = true
            FieldQuery fq = new FieldQuery(pqF("a", "b"), true, true);
            FieldTermStack stack = new FieldTermStack(reader, 0, F, fq);
            FieldPhraseList fpl = new FieldPhraseList(stack, fq);
            assertEquals(1, fpl.PhraseList.size());
            assertEquals("ab(1.0)((4,7))", fpl.PhraseList[0].ToString(CultureInfo.InvariantCulture)); // LUCENENET specific: use invariant culture, since we are culture-aware

            // phraseHighlight = false
            fq = new FieldQuery(pqF("a", "b"), false, true);
            stack = new FieldTermStack(reader, 0, F, fq);
            fpl = new FieldPhraseList(stack, fq);
            assertEquals(2, fpl.PhraseList.size());
            assertEquals("a(1.0)((2,3))", fpl.PhraseList[0].ToString(CultureInfo.InvariantCulture)); // LUCENENET specific: use invariant culture, since we are culture-aware
            assertEquals("ab(1.0)((4,7))", fpl.PhraseList[1].ToString(CultureInfo.InvariantCulture)); // LUCENENET specific: use invariant culture, since we are culture-aware
        }

        [Test]
        public void TestPhraseSlop()
        {
            make1d1fIndex("c a a b c");

            FieldQuery fq = new FieldQuery(pqF(2F, 1, "a", "c"), true, true);
            FieldTermStack stack = new FieldTermStack(reader, 0, F, fq);
            FieldPhraseList fpl = new FieldPhraseList(stack, fq);
            assertEquals(1, fpl.PhraseList.size());
            assertEquals("ac(2.0)((4,5)(8,9))", fpl.PhraseList[0].ToString(CultureInfo.InvariantCulture)); // LUCENENET specific: use invariant culture, since we are culture-aware
            assertEquals(4, fpl.PhraseList[0].StartOffset);
            assertEquals(9, fpl.PhraseList[0].EndOffset);
        }

        [Test]
        public void Test2PhrasesOverlap()
        {
            make1d1fIndex("d a b c d");

            BooleanQuery query = new BooleanQuery();
            query.Add(pqF("a", "b"), Occur.SHOULD);
            query.Add(pqF("b", "c"), Occur.SHOULD);
            FieldQuery fq = new FieldQuery(query, true, true);
            FieldTermStack stack = new FieldTermStack(reader, 0, F, fq);
            FieldPhraseList fpl = new FieldPhraseList(stack, fq);
            assertEquals(1, fpl.PhraseList.size());
            assertEquals("abc(1.0)((2,7))", fpl.PhraseList[0].ToString(CultureInfo.InvariantCulture)); // LUCENENET specific: use invariant culture, since we are culture-aware
        }

        [Test]
        public void Test3TermsPhrase()
        {
            make1d1fIndex("d a b a b c d");

            FieldQuery fq = new FieldQuery(pqF("a", "b", "c"), true, true);
            FieldTermStack stack = new FieldTermStack(reader, 0, F, fq);
            FieldPhraseList fpl = new FieldPhraseList(stack, fq);
            assertEquals(1, fpl.PhraseList.size());
            assertEquals("abc(1.0)((6,11))", fpl.PhraseList[0].ToString(CultureInfo.InvariantCulture)); // LUCENENET specific: use invariant culture, since we are culture-aware
        }

        [Test]
        public void TestSearchLongestPhrase()
        {
            make1d1fIndex("d a b d c a b c");

            BooleanQuery query = new BooleanQuery();
            query.Add(pqF("a", "b"), Occur.SHOULD);
            query.Add(pqF("a", "b", "c"), Occur.SHOULD);
            FieldQuery fq = new FieldQuery(query, true, true);
            FieldTermStack stack = new FieldTermStack(reader, 0, F, fq);
            FieldPhraseList fpl = new FieldPhraseList(stack, fq);
            assertEquals(2, fpl.PhraseList.size());
            assertEquals("ab(1.0)((2,5))", fpl.PhraseList[0].ToString(CultureInfo.InvariantCulture)); // LUCENENET specific: use invariant culture, since we are culture-aware
            assertEquals("abc(1.0)((10,15))", fpl.PhraseList[1].ToString(CultureInfo.InvariantCulture)); // LUCENENET specific: use invariant culture, since we are culture-aware
        }

        [Test]
        public void Test1PhraseShortMV()
        {
            makeIndexShortMV();

            FieldQuery fq = new FieldQuery(tq("d"), true, true);
            FieldTermStack stack = new FieldTermStack(reader, 0, F, fq);
            FieldPhraseList fpl = new FieldPhraseList(stack, fq);
            assertEquals(1, fpl.PhraseList.size());
            assertEquals("d(1.0)((9,10))", fpl.PhraseList[0].ToString(CultureInfo.InvariantCulture)); // LUCENENET specific: use invariant culture, since we are culture-aware
        }

        [Test]
        public void Test1PhraseLongMV()
        {
            makeIndexLongMV();

            FieldQuery fq = new FieldQuery(pqF("search", "engines"), true, true);
            FieldTermStack stack = new FieldTermStack(reader, 0, F, fq);
            FieldPhraseList fpl = new FieldPhraseList(stack, fq);
            assertEquals(2, fpl.PhraseList.size());
            assertEquals("searchengines(1.0)((102,116))", fpl.PhraseList[0].ToString(CultureInfo.InvariantCulture)); // LUCENENET specific: use invariant culture, since we are culture-aware
            assertEquals("searchengines(1.0)((157,171))", fpl.PhraseList[1].ToString(CultureInfo.InvariantCulture)); // LUCENENET specific: use invariant culture, since we are culture-aware
        }

        [Test]
        public void Test1PhraseLongMVB()
        {
            makeIndexLongMVB();

            FieldQuery fq = new FieldQuery(pqF("sp", "pe", "ee", "ed"), true, true); // "speed" -(2gram)-> "sp","pe","ee","ed"
            FieldTermStack stack = new FieldTermStack(reader, 0, F, fq);
            FieldPhraseList fpl = new FieldPhraseList(stack, fq);
            assertEquals(1, fpl.PhraseList.size());
            assertEquals("sppeeeed(1.0)((88,93))", fpl.PhraseList[0].ToString(CultureInfo.InvariantCulture)); // LUCENENET specific: use invariant culture, since we are culture-aware
        }

        /* This test shows a big speedup from limiting the number of analyzed phrases in 
         * this bad case for FieldPhraseList */
        /* But it is not reliable as a unit test since it is timing-dependent
        public void TestManyRepeatedTerms() throws Exception {
            long t = System.currentTimeMillis();
            testManyTermsWithLimit (-1);
            long t1 = System.currentTimeMillis();
            testManyTermsWithLimit (1);
            long t2 = System.currentTimeMillis();
            assertTrue (t2-t1 * 1000 < t1-t);
        }
        private void TestManyTermsWithLimit (int limit) throws Exception {
            StringBuilder buf = new StringBuilder ();
            for (int i = 0; i < 16000; i++) {
                buf.append("a b c ");
            }
            make1d1fIndex( buf.toString());

            Query query = tq("a");
            FieldQuery fq = new FieldQuery( query, true, true );
            FieldTermStack stack = new FieldTermStack( reader, 0, F, fq );
            FieldPhraseList fpl = new FieldPhraseList( stack, fq, limit);
            if (limit < 0 || limit > 16000)
                assertEquals( 16000, fpl.phraseList.size() );
            else
                assertEquals( limit, fpl.phraseList.size() );
            assertEquals( "a(1.0)((0,1))", fpl.phraseList.Get( 0 ).toString() );      
        }
        */

        [Test]
        public void TestWeightedPhraseInfoComparisonConsistency()
        {
            WeightedPhraseInfo a = newInfo(0, 0, 1);
            WeightedPhraseInfo b = newInfo(1, 2, 1);
            WeightedPhraseInfo c = newInfo(2, 3, 1);
            WeightedPhraseInfo d = newInfo(0, 0, 1);
            WeightedPhraseInfo e = newInfo(0, 0, 2);

            assertConsistentEquals(a, a);
            assertConsistentEquals(b, b);
            assertConsistentEquals(c, c);
            assertConsistentEquals(d, d);
            assertConsistentEquals(e, e);
            assertConsistentEquals(a, d);
            assertConsistentLessThan(a, b);
            assertConsistentLessThan(b, c);
            assertConsistentLessThan(a, c);
            assertConsistentLessThan(a, e);
            assertConsistentLessThan(e, b);
            assertConsistentLessThan(e, c);
            assertConsistentLessThan(d, b);
            assertConsistentLessThan(d, c);
            assertConsistentLessThan(d, e);
        }

        [Test]
        public void TestToffsComparisonConsistency()
        {
            Toffs a = new Toffs(0, 0);
            Toffs b = new Toffs(1, 2);
            Toffs c = new Toffs(2, 3);
            Toffs d = new Toffs(0, 0);

            assertConsistentEquals(a, a);
            assertConsistentEquals(b, b);
            assertConsistentEquals(c, c);
            assertConsistentEquals(d, d);
            assertConsistentEquals(a, d);
            assertConsistentLessThan(a, b);
            assertConsistentLessThan(b, c);
            assertConsistentLessThan(a, c);
            assertConsistentLessThan(d, b);
            assertConsistentLessThan(d, c);
        }

        private WeightedPhraseInfo newInfo(int startOffset, int endOffset, float boost)
        {
            IList<TermInfo> infos = new JCG.List<TermInfo>();
            infos.Add(new TermInfo(TestUtil.RandomUnicodeString(Random), startOffset, endOffset, 0, 0));
            return new WeightedPhraseInfo(infos, boost);
        }

        private void assertConsistentEquals<T>(T a, T b) where T : IComparable<T>
        {
            assertEquals(a, b);
            assertEquals(b, a);
            assertEquals(a.GetHashCode(), b.GetHashCode());
            assertEquals(0, a.CompareTo(b));
            assertEquals(0, b.CompareTo(a));
        }

        private void assertConsistentLessThan<T>(T a, T b) where T : IComparable<T>
        {
            assertFalse(a.equals(b));
            assertFalse(b.equals(a));
            assertFalse(a.GetHashCode() == b.GetHashCode());
            assertTrue(a.CompareTo(b) < 0);
            assertTrue(b.CompareTo(a) > 0);
        }
    }
}
