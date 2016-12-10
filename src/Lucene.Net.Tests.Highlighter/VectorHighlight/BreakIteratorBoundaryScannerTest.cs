//LUCENENET TODO: BreakIterator

//using Lucene.Net.Support;
//using Lucene.Net.Util;
//using NUnit.Framework;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace Lucene.Net.Search.VectorHighlight
//{
//    /*
//	 * Licensed to the Apache Software Foundation (ASF) under one or more
//	 * contributor license agreements.  See the NOTICE file distributed with
//	 * this work for additional information regarding copyright ownership.
//	 * The ASF licenses this file to You under the Apache License, Version 2.0
//	 * (the "License"); you may not use this file except in compliance with
//	 * the License.  You may obtain a copy of the License at
//	 *
//	 *     http://www.apache.org/licenses/LICENSE-2.0
//	 *
//	 * Unless required by applicable law or agreed to in writing, software
//	 * distributed under the License is distributed on an "AS IS" BASIS,
//	 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//	 * See the License for the specific language governing permissions and
//	 * limitations under the License.
//	 */

//    public class BreakIteratorBoundaryScannerTest : LuceneTestCase
//    {
//        static readonly String TEXT =
//            "Apache Lucene(TM) is a high-performance, full-featured text search engine library written entirely in Java." +
//            "\nIt is a technology suitable for nearly any application that requires\n" +
//            "full-text search, especially cross-platform. \nApache Lucene is an open source project available for free download.";

//        [Test]
//        public void TestOutOfRange()
//        {
//            StringBuilder text = new StringBuilder(TEXT);
//            BreakIterator bi = BreakIterator.getWordInstance(Locale.ROOT);
//            IBoundaryScanner scanner = new BreakIteratorBoundaryScanner(bi);

//            int start = TEXT.Length + 1;
//            assertEquals(start, scanner.FindStartOffset(text, start));
//            assertEquals(start, scanner.FindEndOffset(text, start));
//            start = 0;
//            assertEquals(start, scanner.FindStartOffset(text, start));
//            start = -1;
//            assertEquals(start, scanner.FindEndOffset(text, start));
//        }

//        [Test]
//        public void TestWordBoundary()
//        {
//            StringBuilder text = new StringBuilder(TEXT);
//            BreakIterator bi = BreakIterator.getWordInstance(Locale.ROOT);
//            IBoundaryScanner scanner = new BreakIteratorBoundaryScanner(bi);

//            int start = TEXT.IndexOf("formance");
//            int expected = TEXT.IndexOf("high-performance");
//            testFindStartOffset(text, start, expected, scanner);

//            expected = TEXT.IndexOf(", full");
//            testFindEndOffset(text, start, expected, scanner);
//        }

//        [Test]
//        public void TestSentenceBoundary()
//        {
//            StringBuilder text = new StringBuilder(TEXT);
//            // we test this with default locale, its randomized by LuceneTestCase
//            BreakIterator bi = BreakIterator.getSentenceInstance(Locale.getDefault());
//            IBoundaryScanner scanner = new BreakIteratorBoundaryScanner(bi);

//            int start = TEXT.IndexOf("any application");
//            int expected = TEXT.IndexOf("It is a");
//            testFindStartOffset(text, start, expected, scanner);

//            expected = TEXT.IndexOf("Apache Lucene is an open source");
//            testFindEndOffset(text, start, expected, scanner);
//        }

//        [Test]
//        public void TestLineBoundary()
//        {
//            StringBuilder text = new StringBuilder(TEXT);
//            // we test this with default locale, its randomized by LuceneTestCase
//            BreakIterator bi = BreakIterator.getLineInstance(Locale.getDefault());
//            IBoundaryScanner scanner = new BreakIteratorBoundaryScanner(bi);

//            int start = TEXT.IndexOf("any application");
//            int expected = TEXT.IndexOf("nearly");
//            testFindStartOffset(text, start, expected, scanner);

//            expected = TEXT.IndexOf("application that requires");
//            testFindEndOffset(text, start, expected, scanner);
//        }

//        [Test]
//        private void TestFindStartOffset(StringBuilder text, int start, int expected, IBoundaryScanner scanner)
//        {
//            assertEquals(expected, scanner.FindStartOffset(text, start));
//        }

//        [Test]
//        private void TestFindEndOffset(StringBuilder text, int start, int expected, IBoundaryScanner scanner)
//        {
//            assertEquals(expected, scanner.FindEndOffset(text, start));
//        }
//    }
//}
