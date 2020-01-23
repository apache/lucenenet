#if FEATURE_BREAKITERATOR
using ICU4N.Text;
using Lucene.Net.Attributes;
using Lucene.Net.Support;
using Lucene.Net.Util;
using NUnit.Framework;
using System;
using System.Globalization;
using System.Text;

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

    public class BreakIteratorBoundaryScannerTest : LuceneTestCase
    {
        static readonly String TEXT =
            "Apache Lucene(TM) is a high-performance, full-featured text search engine library written entirely in Java." +
            "\nIt is a technology suitable for nearly any application that requires\n" +
            "full-text search, especially cross-platform. \nApache Lucene is an open source project available for free download.";

        [Test]
        public void TestOutOfRange()
        {
            StringBuilder text = new StringBuilder(TEXT);
            BreakIterator bi = BreakIterator.GetWordInstance(CultureInfo.InvariantCulture);
            IBoundaryScanner scanner = new BreakIteratorBoundaryScanner(bi);

            int start = TEXT.Length + 1;
            assertEquals(start, scanner.FindStartOffset(text, start));
            assertEquals(start, scanner.FindEndOffset(text, start));
            start = 0;
            assertEquals(start, scanner.FindStartOffset(text, start));
            start = -1;
            assertEquals(start, scanner.FindEndOffset(text, start));
        }

        // LUCENENET specific - Confirmed that ICU4J 60.1 behaves like this by default...
        [Test, LuceneNetSpecific]
        public void TestICUWordBoundary()
        {
            StringBuilder text = new StringBuilder(TEXT);
            BreakIterator bi = BreakIterator.GetWordInstance(CultureInfo.InvariantCulture);
            IBoundaryScanner scanner = new BreakIteratorBoundaryScanner(bi);

            int start = TEXT.IndexOf("formance", StringComparison.Ordinal);
            int expected = TEXT.IndexOf("performance", StringComparison.Ordinal);
            TestFindStartOffset(text, start, expected, scanner);

            expected = TEXT.IndexOf(", full", StringComparison.Ordinal);
            TestFindEndOffset(text, start, expected, scanner);
        }

        // LUCENENET specific - this is the original Lucene test with a mock BreakIterator that
        // is intended to act (sort of) like the JDK
        [Test]
        public void TestWordBoundary()
        {
            StringBuilder text = new StringBuilder(TEXT);
            // LUCENENET specific - using a mock of the JDK BreakIterator class, which is just
            // an ICU BreakIterator with custom rules applied.
            BreakIterator bi = JdkBreakIterator.GetWordInstance(CultureInfo.InvariantCulture);
            IBoundaryScanner scanner = new BreakIteratorBoundaryScanner(bi);

            int start = TEXT.IndexOf("formance", StringComparison.Ordinal);
            int expected = TEXT.IndexOf("high-performance", StringComparison.Ordinal);
            TestFindStartOffset(text, start, expected, scanner);

            expected = TEXT.IndexOf(", full", StringComparison.Ordinal);
            TestFindEndOffset(text, start, expected, scanner);
        }

        // LUCENENET specific - Confirmed that ICU4J 60.1 behaves like this by default...
        [Test, LuceneNetSpecific]
        public void TestICUSentenceBoundary()
        {
            StringBuilder text = new StringBuilder(TEXT);
            // we test this with default locale, its randomized by LuceneTestCase
            BreakIterator bi = BreakIterator.GetSentenceInstance(CultureInfo.CurrentCulture);
            IBoundaryScanner scanner = new BreakIteratorBoundaryScanner(bi);

            int start = TEXT.IndexOf("any application");
            int expected = TEXT.IndexOf("It is a");
            TestFindStartOffset(text, start, expected, scanner);

            expected = TEXT.IndexOf("application that requires") + "application that requires\n".Length;
            TestFindEndOffset(text, start, expected, scanner);
        }

        // LUCENENET specific - this is the original Lucene test with a mock BreakIterator that
        // is intended to act (sort of) like the JDK
        [Test]
        public void TestSentenceBoundary()
        {
            // LUCENENET specific - using a mock of the JDK BreakIterator class, which is just
            // an ICU BreakIterator with custom rules applied. East Asian
            // languages are skipped because the DictionaryBasedBreakIterator is not overridden by the rules.
            switch (CultureInfo.CurrentCulture.TwoLetterISOLanguageName)
            {
                case "th": // Thai
                case "lo": // Lao
                case "my": // Burmese
                case "km": // Khmer
                case "ja": // Japanese
                case "ko": // Korean
                case "zh": // Chinese
                    Assume.That(false, "This test does not apply to East Asian languages.");
                    break;
            }

            StringBuilder text = new StringBuilder(TEXT);
            // we test this with default locale, its randomized by LuceneTestCase

            // LUCENENET specific - using a mock of the JDK BreakIterator class, which is just
            // an ICU BreakIterator with custom rules applied.
            BreakIterator bi = JdkBreakIterator.GetSentenceInstance(CultureInfo.CurrentCulture);
            IBoundaryScanner scanner = new BreakIteratorBoundaryScanner(bi);

            int start = TEXT.IndexOf("any application", StringComparison.Ordinal);
            int expected = TEXT.IndexOf("It is a", StringComparison.Ordinal);
            TestFindStartOffset(text, start, expected, scanner);

            expected = TEXT.IndexOf("Apache Lucene is an open source", StringComparison.Ordinal);
            TestFindEndOffset(text, start, expected, scanner);
        }

        [Test]
        public void TestLineBoundary()
        {
            StringBuilder text = new StringBuilder(TEXT);
            // we test this with default locale, its randomized by LuceneTestCase
            BreakIterator bi = BreakIterator.GetLineInstance(CultureInfo.CurrentCulture);
            IBoundaryScanner scanner = new BreakIteratorBoundaryScanner(bi);

            int start = TEXT.IndexOf("any application", StringComparison.Ordinal);
            int expected = TEXT.IndexOf("nearly", StringComparison.Ordinal);
            TestFindStartOffset(text, start, expected, scanner);

            expected = TEXT.IndexOf("application that requires", StringComparison.Ordinal);
            TestFindEndOffset(text, start, expected, scanner);
        }

        private void TestFindStartOffset(StringBuilder text, int start, int expected, IBoundaryScanner scanner)
        {
            assertEquals(expected, scanner.FindStartOffset(text, start));
        }

        private void TestFindEndOffset(StringBuilder text, int start, int expected, IBoundaryScanner scanner)
        {
            assertEquals(expected, scanner.FindEndOffset(text, start));
        }
    }
}
#endif