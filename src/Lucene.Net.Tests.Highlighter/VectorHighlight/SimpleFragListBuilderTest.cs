using Lucene.Net.Index;
using NUnit.Framework;
using System;
using System.Globalization;
using Console = Lucene.Net.Util.SystemConsole;

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

    public class SimpleFragListBuilderTest : AbstractTestCase
    {
        [Test]
        public void TestNullFieldFragList()
        {
            SimpleFragListBuilder sflb = new SimpleFragListBuilder();
            FieldFragList ffl = sflb.CreateFieldFragList(fpl(new TermQuery(new Term(F, "a")), "b c d"), 100);
            assertEquals(0, ffl.FragInfos.size());
        }

        [Test]
        public void TestTooSmallFragSize()
        {
            try
            {
                SimpleFragListBuilder sflb = new SimpleFragListBuilder();
                sflb.CreateFieldFragList(fpl(new TermQuery(new Term(F, "a")), "b c d"), sflb.minFragCharSize - 1);
                fail("IllegalArgumentException must be thrown");
            }
            catch (ArgumentOutOfRangeException) // LUCENENET specific - changed from IllegalArgumentException to ArgumentOutOfRangeException (.NET convention)
            {
            }
        }

        [Test]
        public void TestSmallerFragSizeThanTermQuery()
        {
            SimpleFragListBuilder sflb = new SimpleFragListBuilder();
            FieldFragList ffl = sflb.CreateFieldFragList(fpl(new TermQuery(new Term(F, "abcdefghijklmnopqrs")), "abcdefghijklmnopqrs"), sflb.minFragCharSize);
            assertEquals(1, ffl.FragInfos.size());
            assertEquals("subInfos=(abcdefghijklmnopqrs((0,19)))/1.0(0,19)", ffl.FragInfos[0].ToString(CultureInfo.InvariantCulture)); // LUCENENET specific: use invariant culture, since we are culture-aware
        }

        [Test]
        public void TestSmallerFragSizeThanPhraseQuery()
        {
            SimpleFragListBuilder sflb = new SimpleFragListBuilder();

            PhraseQuery phraseQuery = new PhraseQuery();
            phraseQuery.Add(new Term(F, "abcdefgh"));
            phraseQuery.Add(new Term(F, "jklmnopqrs"));

            FieldFragList ffl = sflb.CreateFieldFragList(fpl(phraseQuery, "abcdefgh   jklmnopqrs"), sflb.minFragCharSize);
            assertEquals(1, ffl.FragInfos.size());
            if (Verbose) Console.WriteLine(ffl.FragInfos[0].ToString(CultureInfo.InvariantCulture)); // LUCENENET specific: use invariant culture, since we are culture-aware
            assertEquals("subInfos=(abcdefghjklmnopqrs((0,21)))/1.0(0,21)", ffl.FragInfos[0].ToString(CultureInfo.InvariantCulture)); // LUCENENET specific: use invariant culture, since we are culture-aware
        }

        [Test]
        public void Test1TermIndex()
        {
            SimpleFragListBuilder sflb = new SimpleFragListBuilder();
            FieldFragList ffl = sflb.CreateFieldFragList(fpl(new TermQuery(new Term(F, "a")), "a"), 100);
            assertEquals(1, ffl.FragInfos.size());
            assertEquals("subInfos=(a((0,1)))/1.0(0,100)", ffl.FragInfos[0].ToString(CultureInfo.InvariantCulture)); // LUCENENET specific: use invariant culture, since we are culture-aware
        }

        [Test]
        public void Test2TermsIndex1Frag()
        {
            SimpleFragListBuilder sflb = new SimpleFragListBuilder();
            FieldFragList ffl = sflb.CreateFieldFragList(fpl(new TermQuery(new Term(F, "a")), "a a"), 100);
            assertEquals(1, ffl.FragInfos.size());
            assertEquals("subInfos=(a((0,1))a((2,3)))/2.0(0,100)", ffl.FragInfos[0].ToString(CultureInfo.InvariantCulture)); // LUCENENET specific: use invariant culture, since we are culture-aware

            ffl = sflb.CreateFieldFragList(fpl(new TermQuery(new Term(F, "a")), "a b b b b b b b b a"), 20);
            assertEquals(1, ffl.FragInfos.size());
            assertEquals("subInfos=(a((0,1))a((18,19)))/2.0(0,20)", ffl.FragInfos[0].ToString(CultureInfo.InvariantCulture)); // LUCENENET specific: use invariant culture, since we are culture-aware

            ffl = sflb.CreateFieldFragList(fpl(new TermQuery(new Term(F, "a")), "b b b b a b b b b a"), 20);
            assertEquals(1, ffl.FragInfos.size());
            assertEquals("subInfos=(a((8,9))a((18,19)))/2.0(4,24)", ffl.FragInfos[0].ToString(CultureInfo.InvariantCulture)); // LUCENENET specific: use invariant culture, since we are culture-aware
        }

        [Test]
        public void Test2TermsIndex2Frags()
        {
            SimpleFragListBuilder sflb = new SimpleFragListBuilder();
            FieldFragList ffl = sflb.CreateFieldFragList(fpl(new TermQuery(new Term(F, "a")), "a b b b b b b b b b b b b b a"), 20);
            assertEquals(2, ffl.FragInfos.size());
            assertEquals("subInfos=(a((0,1)))/1.0(0,20)", ffl.FragInfos[0].ToString(CultureInfo.InvariantCulture)); // LUCENENET specific: use invariant culture, since we are culture-aware
            assertEquals("subInfos=(a((28,29)))/1.0(20,40)", ffl.FragInfos[1].ToString(CultureInfo.InvariantCulture)); // LUCENENET specific: use invariant culture, since we are culture-aware

            ffl = sflb.CreateFieldFragList(fpl(new TermQuery(new Term(F, "a")), "a b b b b b b b b b b b b a"), 20);
            assertEquals(2, ffl.FragInfos.size());
            assertEquals("subInfos=(a((0,1)))/1.0(0,20)", ffl.FragInfos[0].ToString(CultureInfo.InvariantCulture)); // LUCENENET specific: use invariant culture, since we are culture-aware
            assertEquals("subInfos=(a((26,27)))/1.0(20,40)", ffl.FragInfos[1].ToString(CultureInfo.InvariantCulture)); // LUCENENET specific: use invariant culture, since we are culture-aware

            ffl = sflb.CreateFieldFragList(fpl(new TermQuery(new Term(F, "a")), "a b b b b b b b b b a"), 20);
            assertEquals(2, ffl.FragInfos.size());
            assertEquals("subInfos=(a((0,1)))/1.0(0,20)", ffl.FragInfos[0].ToString(CultureInfo.InvariantCulture)); // LUCENENET specific: use invariant culture, since we are culture-aware
            assertEquals("subInfos=(a((20,21)))/1.0(20,40)", ffl.FragInfos[1].ToString(CultureInfo.InvariantCulture)); // LUCENENET specific: use invariant culture, since we are culture-aware
        }

        [Test]
        public void Test2TermsQuery()
        {
            SimpleFragListBuilder sflb = new SimpleFragListBuilder();

            BooleanQuery booleanQuery = new BooleanQuery();
            booleanQuery.Add(new TermQuery(new Term(F, "a")), Occur.SHOULD);
            booleanQuery.Add(new TermQuery(new Term(F, "b")), Occur.SHOULD);

            FieldFragList ffl = sflb.CreateFieldFragList(fpl(booleanQuery, "c d e"), 20);
            assertEquals(0, ffl.FragInfos.size());

            ffl = sflb.CreateFieldFragList(fpl(booleanQuery, "d b c"), 20);
            assertEquals(1, ffl.FragInfos.size());
            assertEquals("subInfos=(b((2,3)))/1.0(0,20)", ffl.FragInfos[0].ToString(CultureInfo.InvariantCulture)); // LUCENENET specific: use invariant culture, since we are culture-aware

            ffl = sflb.CreateFieldFragList(fpl(booleanQuery, "a b c"), 20);
            assertEquals(1, ffl.FragInfos.size());
            assertEquals("subInfos=(a((0,1))b((2,3)))/2.0(0,20)", ffl.FragInfos[0].ToString(CultureInfo.InvariantCulture)); // LUCENENET specific: use invariant culture, since we are culture-aware
        }

        [Test]
        public void TestPhraseQuery()
        {
            SimpleFragListBuilder sflb = new SimpleFragListBuilder();

            PhraseQuery phraseQuery = new PhraseQuery();
            phraseQuery.Add(new Term(F, "a"));
            phraseQuery.Add(new Term(F, "b"));

            FieldFragList ffl = sflb.CreateFieldFragList(fpl(phraseQuery, "c d e"), 20);
            assertEquals(0, ffl.FragInfos.size());

            ffl = sflb.CreateFieldFragList(fpl(phraseQuery, "a c b"), 20);
            assertEquals(0, ffl.FragInfos.size());

            ffl = sflb.CreateFieldFragList(fpl(phraseQuery, "a b c"), 20);
            assertEquals(1, ffl.FragInfos.size());
            assertEquals("subInfos=(ab((0,3)))/1.0(0,20)", ffl.FragInfos[0].ToString(CultureInfo.InvariantCulture)); // LUCENENET specific: use invariant culture, since we are culture-aware
        }

        [Test]
        public void TestPhraseQuerySlop()
        {
            SimpleFragListBuilder sflb = new SimpleFragListBuilder();

            PhraseQuery phraseQuery = new PhraseQuery();
            phraseQuery.Slop = (1);
            phraseQuery.Add(new Term(F, "a"));
            phraseQuery.Add(new Term(F, "b"));

            FieldFragList ffl = sflb.CreateFieldFragList(fpl(phraseQuery, "a c b"), 20);
            assertEquals(1, ffl.FragInfos.size());
            assertEquals("subInfos=(ab((0,1)(4,5)))/1.0(0,20)", ffl.FragInfos[0].ToString(CultureInfo.InvariantCulture)); // LUCENENET specific: use invariant culture, since we are culture-aware
        }

        private FieldPhraseList fpl(Query query, String indexValue)
        {
            make1d1fIndex(indexValue);
            FieldQuery fq = new FieldQuery(query, true, true);
            FieldTermStack stack = new FieldTermStack(reader, 0, F, fq);
            return new FieldPhraseList(stack, fq);
        }

        [Test]
        public void Test1PhraseShortMV()
        {
            makeIndexShortMV();

            FieldQuery fq = new FieldQuery(tq("d"), true, true);
            FieldTermStack stack = new FieldTermStack(reader, 0, F, fq);
            FieldPhraseList fpl = new FieldPhraseList(stack, fq);
            SimpleFragListBuilder sflb = new SimpleFragListBuilder();
            FieldFragList ffl = sflb.CreateFieldFragList(fpl, 100);
            assertEquals(1, ffl.FragInfos.size());
            assertEquals("subInfos=(d((9,10)))/1.0(0,100)", ffl.FragInfos[0].ToString(CultureInfo.InvariantCulture)); // LUCENENET specific: use invariant culture, since we are culture-aware
        }

        [Test]
        public void Test1PhraseLongMV()
        {
            makeIndexLongMV();

            FieldQuery fq = new FieldQuery(pqF("search", "engines"), true, true);
            FieldTermStack stack = new FieldTermStack(reader, 0, F, fq);
            FieldPhraseList fpl = new FieldPhraseList(stack, fq);
            SimpleFragListBuilder sflb = new SimpleFragListBuilder();
            FieldFragList ffl = sflb.CreateFieldFragList(fpl, 100);
            assertEquals(1, ffl.FragInfos.size());
            assertEquals("subInfos=(searchengines((102,116))searchengines((157,171)))/2.0(87,187)", ffl.FragInfos[0].ToString(CultureInfo.InvariantCulture)); // LUCENENET specific: use invariant culture, since we are culture-aware
        }

        [Test]
        public void Test1PhraseLongMVB()
        {
            makeIndexLongMVB();

            FieldQuery fq = new FieldQuery(pqF("sp", "pe", "ee", "ed"), true, true); // "speed" -(2gram)-> "sp","pe","ee","ed"
            FieldTermStack stack = new FieldTermStack(reader, 0, F, fq);
            FieldPhraseList fpl = new FieldPhraseList(stack, fq);
            SimpleFragListBuilder sflb = new SimpleFragListBuilder();
            FieldFragList ffl = sflb.CreateFieldFragList(fpl, 100);
            assertEquals(1, ffl.FragInfos.size());
            assertEquals("subInfos=(sppeeeed((88,93)))/1.0(41,141)", ffl.FragInfos[0].ToString(CultureInfo.InvariantCulture)); // LUCENENET specific: use invariant culture, since we are culture-aware
        }
    }
}
