// Lucene version compatibility level 4.8.1
#if FEATURE_BREAKITERATOR
using System;
using Lucene.Net.Util;
using NUnit.Framework;
using ICU4N.Text;
using ICU4N.Support.Text;
using System.Globalization;

namespace Lucene.Net.Analysis.Util
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

    [TestFixture]
    public class TestCharArrayIterator : LuceneTestCase
    {
        [Test]
        public virtual void TestWordInstance()
        {
            DoTests(CharArrayIterator.NewWordInstance());
        }

        [Test]
        public virtual void TestConsumeWordInstance()
        {
            // we use the default locale, as its randomized by LuceneTestCase
            BreakIterator bi = BreakIterator.GetWordInstance(CultureInfo.CurrentCulture);
            var ci = CharArrayIterator.NewWordInstance();
            for (var i = 0; i < 10000; i++)
            {
                var text = TestUtil.RandomUnicodeString(Random).toCharArray();
                ci.SetText(text, 0, text.Length);
                Consume(bi, ci);
            }
        }

        /* run this to test if your JRE is buggy
        public void testWordInstanceJREBUG() {
          // we use the default locale, as its randomized by LuceneTestCase
          BreakIterator bi = BreakIterator.getWordInstance(Locale.getDefault());
          Segment ci = new Segment();
          for (int i = 0; i < 10000; i++) {
            char text[] = TestUtil.randomUnicodeString(random).toCharArray();
            ci.array = text;
            ci.offset = 0;
            ci.count = text.length;
            consume(bi, ci);
          }
        }
        */

        [Test]
        public virtual void TestSentenceInstance()
        {
            DoTests(CharArrayIterator.NewSentenceInstance());
        }

        [Test]
        public virtual void TestConsumeSentenceInstance()
        {
            // we use the default locale, as its randomized by LuceneTestCase
            BreakIterator bi = BreakIterator.GetSentenceInstance(CultureInfo.CurrentCulture);
            var ci = CharArrayIterator.NewSentenceInstance();
            for (var i = 0; i < 10000; i++)
            {
                var text = TestUtil.RandomUnicodeString(Random).toCharArray();
                ci.SetText(text, 0, text.Length);
                Consume(bi, ci);
            }
        }

        /* run this to test if your JRE is buggy
        public void testSentenceInstanceJREBUG() {
          // we use the default locale, as its randomized by LuceneTestCase
          BreakIterator bi = BreakIterator.getSentenceInstance(Locale.getDefault());
          Segment ci = new Segment();
          for (int i = 0; i < 10000; i++) {
            char text[] = TestUtil.randomUnicodeString(random).toCharArray();
            ci.array = text;
            ci.offset = 0;
            ci.count = text.length;
            consume(bi, ci);
          }
        }
        */

        private void DoTests(CharArrayIterator ci)
        {
            // basics
            ci.SetText("testing".ToCharArray(), 0, "testing".Length);
            assertEquals(0, ci.BeginIndex);
            assertEquals(7, ci.EndIndex);
            assertEquals(0, ci.Index);
            assertEquals('t', ci.Current);
            assertEquals('e', ci.Next());
            assertEquals('g', ci.Last());
            assertEquals('n', ci.Previous());
            assertEquals('t', ci.First());
            assertEquals(CharacterIterator.Done, ci.Previous());

            // first()
            ci.SetText("testing".ToCharArray(), 0, "testing".Length);
            ci.Next();
            // Sets the position to getBeginIndex() and returns the character at that position. 
            assertEquals('t', ci.First());
            assertEquals(ci.BeginIndex, ci.Index);
            // or DONE if the text is empty
            ci.SetText(new char[] { }, 0, 0);
            assertEquals(CharacterIterator.Done, ci.First());

            // last()
            ci.SetText("testing".ToCharArray(), 0, "testing".Length);
            // Sets the position to getEndIndex()-1 (getEndIndex() if the text is empty) 
            // and returns the character at that position. 
            assertEquals('g', ci.Last());
            assertEquals(ci.Index, ci.EndIndex - 1);
            // or DONE if the text is empty
            ci.SetText(new char[] { }, 0, 0);
            assertEquals(CharacterIterator.Done, ci.Last());
            assertEquals(ci.EndIndex, ci.Index);

            // current()
            // Gets the character at the current position (as returned by getIndex()). 
            ci.SetText("testing".ToCharArray(), 0, "testing".Length);
            assertEquals('t', ci.Current);
            ci.Last();
            ci.Next();
            // or DONE if the current position is off the end of the text.
            assertEquals(CharacterIterator.Done, ci.Current);

            // next()
            ci.SetText("te".ToCharArray(), 0, 2);
            // Increments the iterator's index by one and returns the character at the new index.
            assertEquals('e', ci.Next());
            assertEquals(1, ci.Index);
            // or DONE if the new position is off the end of the text range.
            assertEquals(CharacterIterator.Done, ci.Next());
            assertEquals(ci.EndIndex, ci.Index);

            // setIndex()
            ci.SetText("test".ToCharArray(), 0, "test".Length);
            try
            {
                ci.SetIndex(5);
                fail();
            }
            catch (Exception e) when (e.IsException())
            {
                assertTrue(e is ArgumentException);
            }

            // clone()
            var text = "testing".ToCharArray();
            ci.SetText(text, 0, text.Length);
            ci.Next();
            var ci2 = ci.Clone() as CharArrayIterator;
            assertEquals(ci.Index, ci2.Index);
            assertEquals(ci.Next(), ci2.Next());
            assertEquals(ci.Last(), ci2.Last());
        }

        private void Consume(BreakIterator bi, CharacterIterator ci)
        {
            bi.SetText(ci);
            while (bi.Next() != BreakIterator.Done)
            {
            }
        }
    }
}
#endif