#if FEATURE_BREAKITERATOR
using ICU4N.Support.Text;
using ICU4N.Text;
using Lucene.Net.Support;
using Lucene.Net.Util;
using NUnit.Framework;
using System;
using System.Globalization;

namespace Lucene.Net.Search.PostingsHighlight
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

    public class TestWholeBreakIterator : LuceneTestCase
    {
        /** For single sentences, we know WholeBreakIterator should break the same as a sentence iterator */
        [Test]
        public void TestSingleSentences()
        {
            BreakIterator expected = BreakIterator.GetSentenceInstance(CultureInfo.InvariantCulture);
            BreakIterator actual = new WholeBreakIterator();
            assertSameBreaks("a", expected, actual);
            assertSameBreaks("ab", expected, actual);
            assertSameBreaks("abc", expected, actual);
            assertSameBreaks("", expected, actual);
        }

        [Test]
        public void TestSliceEnd()
        {
            BreakIterator expected = BreakIterator.GetSentenceInstance(CultureInfo.InvariantCulture);
            BreakIterator actual = new WholeBreakIterator();
            assertSameBreaks("a000", 0, 1, expected, actual);
            assertSameBreaks("ab000", 0, 1, expected, actual);
            assertSameBreaks("abc000", 0, 1, expected, actual);
            assertSameBreaks("000", 0, 0, expected, actual);
        }

        [Test]
        public void TestSliceStart()
        {
            BreakIterator expected = BreakIterator.GetSentenceInstance(CultureInfo.InvariantCulture);
            BreakIterator actual = new WholeBreakIterator();
            assertSameBreaks("000a", 3, 1, expected, actual);
            assertSameBreaks("000ab", 3, 2, expected, actual);
            assertSameBreaks("000abc", 3, 3, expected, actual);
            assertSameBreaks("000", 3, 0, expected, actual);
        }

        [Test]
        public void TestSliceMiddle()
        {
            BreakIterator expected = BreakIterator.GetSentenceInstance(CultureInfo.InvariantCulture);
            BreakIterator actual = new WholeBreakIterator();
            assertSameBreaks("000a000", 3, 1, expected, actual);
            assertSameBreaks("000ab000", 3, 2, expected, actual);
            assertSameBreaks("000abc000", 3, 3, expected, actual);
            assertSameBreaks("000000", 3, 0, expected, actual);
        }

        /** the current position must be ignored, initial position is always first() */
        [Test]
        public void TestFirstPosition()
        {
            BreakIterator expected = BreakIterator.GetSentenceInstance(CultureInfo.InvariantCulture);
            BreakIterator actual = new WholeBreakIterator();
            assertSameBreaks("000ab000", 3, 2, 4, expected, actual);
        }

        public void assertSameBreaks(String text, BreakIterator expected, BreakIterator actual)
        {
            assertSameBreaks(new StringCharacterIterator(text),
                             new StringCharacterIterator(text),
                             expected,
                             actual);
        }

        public void assertSameBreaks(String text, int offset, int length, BreakIterator expected, BreakIterator actual)
        {
            assertSameBreaks(text, offset, length, offset, expected, actual);
        }

        public void assertSameBreaks(String text, int offset, int length, int current, BreakIterator expected, BreakIterator actual)
        {
            assertSameBreaks(new StringCharacterIterator(text, offset, offset + length, current),
                             new StringCharacterIterator(text, offset, offset + length, current),
                             expected,
                             actual);
        }

        /** Asserts that two breakiterators break the text the same way */
        public void assertSameBreaks(CharacterIterator one, CharacterIterator two, BreakIterator expected, BreakIterator actual)
        {
            expected.SetText(one);
            actual.SetText(two);

            assertEquals(expected.Current, actual.Current);

            // next()
            int v = expected.Current;
            while (v != BreakIterator.Done)
            {
                assertEquals(v = expected.Next(), actual.Next());
                assertEquals(expected.Current, actual.Current);
            }

            // first()
            assertEquals(expected.First(), actual.First());
            assertEquals(expected.Current, actual.Current);
            // last()
            assertEquals(expected.Last(), actual.Last());
            assertEquals(expected.Current, actual.Current);

            // previous()
            v = expected.Current;
            while (v != BreakIterator.Done)
            {
                assertEquals(v = expected.Previous(), actual.Previous());
                assertEquals(expected.Current, actual.Current);
            }

            // following()
            for (int i = one.BeginIndex; i <= one.EndIndex; i++)
            {
                expected.First();
                actual.First();
                assertEquals(expected.Following(i), actual.Following(i));
                assertEquals(expected.Current, actual.Current);
            }

            // preceding()
            for (int i = one.BeginIndex; i <= one.EndIndex; i++)
            {
                expected.Last();
                actual.Last();
                assertEquals(expected.Preceding(i), actual.Preceding(i));
                assertEquals(expected.Current, actual.Current);
            }
        }
    }
}
#endif