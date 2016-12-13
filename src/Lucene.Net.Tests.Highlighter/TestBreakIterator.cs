using Lucene.Net.Support;
using Lucene.Net.Util;
using NUnit.Framework;
using System;
using System.Globalization;

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

    public class TestBreakIterator : LuceneTestCase
    {
        static readonly String TEXT =
            "Apache Lucene(TM) is a high-performance, full-featured text search engine library written entirely in Java.";

        static readonly String SENTENCE_TEXT =
            "Apache Lucene(TM) is a high-performance, full-featured text search engine library written entirely in Java. " +
            "It is a technology suitable for nearly any application that requires" +
            "full-text search, especially cross-platform. Apache Lucene is an open source project available for free download. " +
            "Lucene makes finding things easy. Lucene is powerful. Lucene is exciting. Lucene is cool. Where be Lucene now?";

        private BreakIterator GetWordInstance(CultureInfo locale)
        {
            //return new WordBreakIterator(locale);
            return new IcuBreakIterator(Icu.BreakIterator.UBreakIteratorType.WORD, locale);
        }

        private BreakIterator GetSentenceInstance(CultureInfo locale)
        {
            return new IcuBreakIterator(Icu.BreakIterator.UBreakIteratorType.SENTENCE, locale);
        }

        [Test]
        public void TestWordIteration()
        {
            BreakIterator bi = GetWordInstance(CultureInfo.InvariantCulture);
            bi.SetText(TEXT);

            int temp;
            temp = bi.Current;
            Console.WriteLine(temp);
            assertEquals(0, temp);

            temp = bi.Next();
            Console.WriteLine(temp);
            assertEquals(6, temp);

            temp = bi.Current;
            Console.WriteLine(temp);
            assertEquals(6, temp);

            temp = bi.Next();
            Console.WriteLine(temp);
            assertEquals(7, temp);
            temp = bi.Current;
            Console.WriteLine(temp);
            assertEquals(7, temp);
            temp = bi.Next();
            Console.WriteLine(temp);
            assertEquals(13, temp);
            temp = bi.Current;
            Console.WriteLine(temp);
            assertEquals(13, temp);
            temp = bi.Next();
            Console.WriteLine(temp);
            assertEquals(14, temp);
            temp = bi.Next();
            Console.WriteLine(temp);
            assertEquals(16, temp);
            temp = bi.Next();
            Console.WriteLine(temp);
            assertEquals(17, temp);
            temp = bi.Current;
            Console.WriteLine(temp);
            assertEquals(17, temp);

            temp = bi.Previous();
            Console.WriteLine(temp);
            assertEquals(16, temp);
            temp = bi.Current;
            Console.WriteLine(temp);
            assertEquals(16, temp);
            temp = bi.Next();
            Console.WriteLine(temp);
            assertEquals(17, temp);
            temp = bi.Current;
            Console.WriteLine(temp);
            assertEquals(17, temp);
            temp = bi.Next();
            Console.WriteLine(temp);
            assertEquals(18, temp);

            temp = bi.Last();
            Console.WriteLine(temp);
            assertEquals(107, temp);
            temp = bi.Current;
            Console.WriteLine(temp);
            assertEquals(107, temp);
            temp = bi.Next();
            Console.WriteLine(temp);
            assertEquals(-1, temp);
            temp = bi.Current;
            Console.WriteLine(temp);
            assertEquals(107, temp);
            temp = bi.Previous();
            Console.WriteLine(temp);
            assertEquals(106, temp);
            temp = bi.Current;
            Console.WriteLine(temp);
            assertEquals(106, temp);
            temp = bi.Previous();
            Console.WriteLine(temp);
            assertEquals(102, temp);
            temp = bi.Current;
            Console.WriteLine(temp);
            assertEquals(102, temp);
            temp = bi.Previous();
            Console.WriteLine(temp);
            assertEquals(101, temp);
            temp = bi.Current;
            Console.WriteLine(temp);
            assertEquals(101, temp);
        }

        [Test]
        public void TestWordFollowing()
        {
            BreakIterator bi = GetWordInstance(CultureInfo.InvariantCulture);
            bi.SetText(TEXT);

            int temp;
            temp = bi.Current;
            Console.WriteLine(temp);
            assertEquals(0, temp);
            temp = bi.Next();
            Console.WriteLine(temp);
            assertEquals(6, temp);


            temp = bi.Following(70);
            Console.WriteLine(temp);
            assertEquals(73, temp);
            temp = bi.Current;
            Console.WriteLine(temp);
            assertEquals(73, temp);
            temp = bi.Next();
            Console.WriteLine(temp);
            assertEquals(74, temp);
            temp = bi.Current;
            Console.WriteLine(temp);
            assertEquals(74, temp);
            temp = bi.Next();
            Console.WriteLine(temp);
            assertEquals(81, temp);
            temp = bi.Following(107); // Test the final boundary
            Console.WriteLine(temp);
            assertEquals(-1, temp);
            temp = bi.Next();
            Console.WriteLine(temp);
            assertEquals(-1, temp);
            temp = bi.Current;
            Console.WriteLine(temp);
            assertEquals(107, temp);

            temp = bi.Following(66); // Test exactly on a boundary position
            Console.WriteLine(temp);
            assertEquals(67, temp);
            temp = bi.Next();
            Console.WriteLine(temp);
            assertEquals(73, temp);
            temp = bi.Current;
            Console.WriteLine(temp);
            assertEquals(73, temp);

            temp = bi.Following(0); // Test the first boundary
            Console.WriteLine(temp);
            assertEquals(6, temp);
            temp = bi.Next();
            Console.WriteLine(temp);
            assertEquals(7, temp);
            temp = bi.Current;
            Console.WriteLine(temp);
            assertEquals(7, temp);

        }

        [Test]
        public void TestWordPreceding()
        {
            BreakIterator bi = GetWordInstance(CultureInfo.InvariantCulture);
            bi.SetText(TEXT);

            int temp;
            temp = bi.Current;
            Console.WriteLine(temp);
            assertEquals(0, temp);
            temp = bi.Next();
            Console.WriteLine(temp);
            assertEquals(6, temp);


            temp = bi.Preceding(70);
            Console.WriteLine(temp);
            assertEquals(67, temp);
            temp = bi.Current;
            Console.WriteLine(temp);
            assertEquals(67, temp);
            temp = bi.Next();
            Console.WriteLine(temp);
            assertEquals(73, temp);
            temp = bi.Current;
            Console.WriteLine(temp);
            assertEquals(73, temp);
            temp = bi.Next();
            Console.WriteLine(temp);
            assertEquals(74, temp);
            temp = bi.Preceding(107); // Test the final boundary
            Console.WriteLine(temp);
            assertEquals(106, temp);
            temp = bi.Next();
            Console.WriteLine(temp);
            assertEquals(107, temp);
            temp = bi.Current;
            Console.WriteLine(temp);
            assertEquals(107, temp);

            temp = bi.Preceding(66); // Test exactly on a boundary position
            Console.WriteLine(temp);
            assertEquals(60, temp);
            temp = bi.Previous();
            Console.WriteLine(temp);
            assertEquals(59, temp);
            temp = bi.Current;
            Console.WriteLine(temp);
            assertEquals(59, temp);

            temp = bi.Preceding(0); // Test the first boundary
            Console.WriteLine(temp);
            assertEquals(-1, temp);
            temp = bi.Current;
            Console.WriteLine(temp);
            assertEquals(0, temp);
            temp = bi.Next();
            Console.WriteLine(temp);
            assertEquals(6, temp);
            temp = bi.Current;
            Console.WriteLine(temp);
            assertEquals(6, temp);

        }

        [Test]
        public void TestWordNextWithInt()
        {
            BreakIterator bi = GetWordInstance(CultureInfo.InvariantCulture);
            bi.SetText(TEXT);

            int temp;
            temp = bi.Current;
            Console.WriteLine(temp);
            assertEquals(0, temp);
            temp = bi.Next();
            Console.WriteLine(temp);
            assertEquals(6, temp);


            temp = bi.Next(10);
            Console.WriteLine(temp);
            assertEquals(23, temp);
            temp = bi.Current;
            Console.WriteLine(temp);
            assertEquals(23, temp);
            temp = bi.Next();
            Console.WriteLine(temp);
            assertEquals(39, temp);
            temp = bi.Current;
            Console.WriteLine(temp);
            assertEquals(39, temp);
            temp = bi.Next();
            Console.WriteLine(temp);
            assertEquals(40, temp);
            temp = bi.Next(-8); // Test going backward
            Console.WriteLine(temp);
            assertEquals(16, temp); // Magically, this is correct (from position 28 back 8 places) in Java, even though its start position is wrong
            temp = bi.Next();
            Console.WriteLine(temp);
            assertEquals(17, temp);
            temp = bi.Current;
            Console.WriteLine(temp);
            assertEquals(17, temp);


            temp = bi.Next(107); // Go past the last boundary
            Console.WriteLine(temp);
            assertEquals(-1, temp);
            temp = bi.Current;
            Console.WriteLine(temp);
            assertEquals(107, temp);
            temp = bi.Next(-107); // Go past the first boundary
            Console.WriteLine(temp);
            assertEquals(-1, temp);
            temp = bi.Current;
            Console.WriteLine(temp);
            assertEquals(0, temp);

        }

        [Test]
        public void TestSentenceIteration()
        {
            BreakIterator bi = GetSentenceInstance(CultureInfo.InvariantCulture);
            bi.SetText(SENTENCE_TEXT);

            int temp;
            temp = bi.Current;
            Console.WriteLine(temp);
            assertEquals(0, temp);

            temp = bi.Next();
            Console.WriteLine(temp);
            assertEquals(108, temp);

            temp = bi.Current;
            Console.WriteLine(temp);
            assertEquals(108, temp);

            temp = bi.Next();
            Console.WriteLine(temp);
            assertEquals(221, temp);
            temp = bi.Current;
            Console.WriteLine(temp);
            assertEquals(221, temp);
            temp = bi.Next();
            Console.WriteLine(temp);
            assertEquals(290, temp);
            temp = bi.Current;
            Console.WriteLine(temp);
            assertEquals(290, temp);
            temp = bi.Next();
            Console.WriteLine(temp);
            assertEquals(324, temp);
            temp = bi.Next();
            Console.WriteLine(temp);
            assertEquals(344, temp);
            temp = bi.Next();
            Console.WriteLine(temp);
            assertEquals(364, temp);
            temp = bi.Current;
            Console.WriteLine(temp);
            assertEquals(364, temp);

            temp = bi.Previous();
            Console.WriteLine(temp);
            assertEquals(344, temp);
            temp = bi.Current;
            Console.WriteLine(temp);
            assertEquals(344, temp);
            temp = bi.Next();
            Console.WriteLine(temp);
            assertEquals(364, temp);
            temp = bi.Current;
            Console.WriteLine(temp);
            assertEquals(364, temp);
            temp = bi.Next();
            Console.WriteLine(temp);
            assertEquals(380, temp);

            temp = bi.First();
            Console.WriteLine(temp);
            assertEquals(0, temp);

            temp = bi.Last();
            Console.WriteLine(temp);
            assertEquals(400, temp);
            temp = bi.Current;
            Console.WriteLine(temp);
            assertEquals(400, temp);
            temp = bi.Next();
            Console.WriteLine(temp);
            assertEquals(-1, temp);
            temp = bi.Current;
            Console.WriteLine(temp);
            assertEquals(400, temp);
            temp = bi.Previous();
            Console.WriteLine(temp);
            assertEquals(380, temp);
            temp = bi.Current;
            Console.WriteLine(temp);
            assertEquals(380, temp);
            temp = bi.Previous();
            Console.WriteLine(temp);
            assertEquals(364, temp);
            temp = bi.Current;
            Console.WriteLine(temp);
            assertEquals(364, temp);
            temp = bi.Previous();
            Console.WriteLine(temp);
            assertEquals(344, temp);
            temp = bi.Current;
            Console.WriteLine(temp);
            assertEquals(344, temp);
        }
    }
}
