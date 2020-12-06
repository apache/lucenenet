using ICU4N.Text;
using Lucene.Net.Attributes;
using Lucene.Net.Support;
using NUnit.Framework;
using System;
using Assert = Lucene.Net.TestFramework.Assert;

namespace Lucene.Net.Tests.ICU.Support
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

    /// <summary>
    /// Tests to ensure compatibility with JDK 7
    /// </summary>
    [TestFixture]
    //[Ignore("Ignore tests until a JavaBreakIterator using RuleBasedBreakIterator is written. The rules for JavaBreakIterator can be found at:\n"
    //	+ "http://grepcode.com/file/repository.grepcode.com/java/root/jdk/openjdk/7u40-b43/sun/text/resources/BreakIteratorRules.java/ \n"
    //	+ "and http://grepcode.com/file/repository.grepcode.com/java/root/jdk/openjdk/7u40-b43/sun/text/resources/BreakIteratorRules_th.java#BreakIteratorRules_th")]
    public class TestJdkBreakIterator
    {
        const String TEXT =
            "Apache Lucene(TM) is a high-performance, full-featured text search engine library written entirely in Java.";

        private BreakIterator GetWordInstance(System.Globalization.CultureInfo locale)
        {
            return JdkBreakIterator.GetWordInstance(locale);
        }

        [Test]
        [LuceneNetSpecific]
        public void TestWordIteration()
        {
            BreakIterator bi = GetWordInstance(System.Globalization.CultureInfo.InvariantCulture);
            bi.SetText("");

            // Test empty
            Assert.AreEqual(0, bi.Current);
            Assert.AreEqual(BreakIterator.Done, bi.Next());
            Assert.AreEqual(0, bi.Current);

            bi.SetText(TEXT);

            // Ensure position starts at 0 when initialized
            Assert.AreEqual(0, bi.Current);

            // Check first boundary (Apache^)
            Assert.AreEqual(6, bi.Next());

            // Ensure Current returns the last boundary iterated to
            Assert.AreEqual(6, bi.Current);

            // Check second boundary (^Lucene)
            Assert.AreEqual(7, bi.Next());

            // Ensure Current returns the last boundary iterated to
            Assert.AreEqual(7, bi.Current);

            // Check third boundary (Lucene^)
            Assert.AreEqual(13, bi.Next());

            // Ensure Current returns the last boundary iterated to
            Assert.AreEqual(13, bi.Current);

            // Check fourth boundary (^TM)
            Assert.AreEqual(14, bi.Next());

            // Check fifth boundary (TM^)
            Assert.AreEqual(16, bi.Next());

            // Check sixth boundary (TM)^
            Assert.AreEqual(17, bi.Next());

            // Check seventh boundary (^is)
            Assert.AreEqual(18, bi.Next());

            // Move to (^high-performance)
            bi.Next();
            bi.Next();
            bi.Next();

            // Check next boundary (^high-performance)
            Assert.AreEqual(23, bi.Next());

            // Ensure we don't break on hyphen (high-performance^)
            Assert.AreEqual(39, bi.Next());


            // Check MoveLast()
            Assert.AreEqual(107, bi.Last());

            // Check going past last boundary
            Assert.AreEqual(BreakIterator.Done, bi.Next());

            // Check we are still at last boundary
            Assert.AreEqual(107, bi.Current);


            // Check MoveFirst()
            Assert.AreEqual(0, bi.First());

            // Check going past first boundary
            Assert.AreEqual(BreakIterator.Done, bi.Previous());

            // Check we are still at first boundary
            Assert.AreEqual(0, bi.Current);
        }

        [Ignore("We did not make a dictionary based break iterator with custom Thai rules, so this has the default behavior of ICU rather than the JDK")]
        [Test]
        [LuceneNetSpecific]
        public void TestWordIterationThai()
        {
            BreakIterator bi = GetWordInstance(new System.Globalization.CultureInfo("th"));
            bi.SetText("");
            

            // Test empty
            Assert.AreEqual(0, bi.Current);
            Assert.AreEqual(BreakIterator.Done, bi.Next());
            Assert.AreEqual(0, bi.Current);

            bi.SetText("บริษัทMicrosoftบริการดีที่สุด");

            // Ensure position starts at 0 when initialized
            Assert.AreEqual(0, bi.Current);

            // Check first boundary (บริษัท^Microsoft)
            Assert.AreEqual(6, bi.Next());

            // Ensure Current returns the last boundary iterated to
            Assert.AreEqual(6, bi.Current);

            // Check second boundary (Microsoft^บริการ)
            Assert.AreEqual(15, bi.Next());

            // Ensure Current returns the last boundary iterated to
            Assert.AreEqual(15, bi.Current);

            // Check third boundary (บริการ^ดี)
            Assert.AreEqual(21, bi.Next());

            // Ensure Current returns the last boundary iterated to
            Assert.AreEqual(21, bi.Current);

            // Check fourth boundary (ดี^ที่สุด)
            Assert.AreEqual(23, bi.Next());

            // Check fifth boundary (ดีที่สุด^)
            Assert.AreEqual(29, bi.Next());

            // Check beyond last boundary (ดีที่สุด)^
            Assert.AreEqual(BreakIterator.Done, bi.Next());

            // Check we are still at last boundary
            Assert.AreEqual(29, bi.Current);

            // Check MovePrevious() (ดี^ที่สุด)
            Assert.AreEqual(23, bi.Previous());


            // Check MoveFirst()
            Assert.AreEqual(0, bi.First());

            // Check going past first boundary
            Assert.AreEqual(BreakIterator.Done, bi.Previous());

            // Check we are still at first boundary
            Assert.AreEqual(0, bi.Current);


            // Check Numerals
            bi.SetText("๑23๔๕๖7");

            // Ensure position starts at 0 when initialized
            Assert.AreEqual(0, bi.Current);

            // Ensure Hindu and Thai numerals stay in one group
            Assert.AreEqual(7, bi.Next());
        }


        const String SENTENCE_TEXT =
            "Apache Lucene(TM) is a high-performance, full-featured text\nsearch engine library written entirely in Java. " +
            "It is a technology suitable for nearly any application that requires" +
            "full-text search, especially cross-platform. Apache Lucene is an open source project available for free download.\n" +
            "Lucene makes finding things easy. Lucene is powerful. Lucene is exciting. Lucene is cool. Where be Lucene now?";

        private BreakIterator GetSentenceInstance(System.Globalization.CultureInfo locale)
        {
            return JdkBreakIterator.GetSentenceInstance(locale);
        }

        [Test]
        [LuceneNetSpecific]
        public void TestSentenceIteration()
        {
            BreakIterator bi = GetSentenceInstance(System.Globalization.CultureInfo.InvariantCulture);
            bi.SetText("");

            // Test empty
            Assert.AreEqual(0, bi.Current);
            Assert.AreEqual(BreakIterator.Done, bi.Next());
            Assert.AreEqual(0, bi.Current);

            bi.SetText(SENTENCE_TEXT);

            // Ensure position starts at 0 when initialized
            Assert.AreEqual(0, bi.Current);

            // Check first boundary (in Java.^) - Ensure we don't break on \n
            Assert.AreEqual(108, bi.Next());

            // Ensure Current returns the most recent boundary
            Assert.AreEqual(108, bi.Current);

            // Check next boundary (especially cross-platform.^)
            Assert.AreEqual(221, bi.Next());

            // Check next boundary (free download.^)
            Assert.AreEqual(290, bi.Next());

            // Check next boundary (things easy.^)
            Assert.AreEqual(324, bi.Next());

            // Check next boundary (is powerful.^)
            Assert.AreEqual(344, bi.Next());

            // Check next boundary (is exciting.^)
            Assert.AreEqual(364, bi.Next());

            // Check next boundary (is cool.^)
            Assert.AreEqual(380, bi.Next());

            // Check last boundary (Lucene now?^)
            Assert.AreEqual(400, bi.Next());

            // Check move past last boundary
            Assert.AreEqual(BreakIterator.Done, bi.Next());

            // Ensure we are still at last boundary
            Assert.AreEqual(400, bi.Current);


            // Check MovePrevious
            Assert.AreEqual(380, bi.Previous());

            // Ensure we get the same value for Current as the last move
            Assert.AreEqual(380, bi.Current);


            // Check MoveFirst
            Assert.AreEqual(0, bi.First());

            // Ensure we get the same value for Current as the last move
            Assert.AreEqual(0, bi.Current);

            // Check moving beyond first boundary
            Assert.AreEqual(BreakIterator.Done, bi.Previous());

            // Ensure we are still at first boundary
            Assert.AreEqual(0, bi.Current);


            // Check MoveLast()
            Assert.AreEqual(400, bi.Last());
        }

        // NOTE: This test doesn't pass. We need to customize line iteration in order to get it to. However,
        // none of the defaults set in lucene use line iteration, so this is low priority. Leaving in place
        // in case we need to make JDK style line breaks in the future.
        const String LINE_TEXT =
            "Apache\tLucene(TM) is a high-\nperformance, full-featured text search engine library written entirely in Java.";

        private BreakIterator GetLineInstance(System.Globalization.CultureInfo locale)
        {
            return BreakIterator.GetLineInstance(locale);
        }

        [Test]
        [Ignore("Not required to confirm compatibility with Java, as this is not required by Lucene's tests.")]
        public void TestLineIteration()
        {
            BreakIterator bi = GetLineInstance(System.Globalization.CultureInfo.InvariantCulture);

            // Test empty
            Assert.AreEqual(0, bi.Current);
            Assert.AreEqual(BreakIterator.Done, bi.Next());
            Assert.AreEqual(0, bi.Current);

            bi.SetText(LINE_TEXT);

            // Ensure position starts at 0 when initialized
            Assert.AreEqual(0, bi.Current);

            // Check first boundary (Apache\t^Lucene) - Ensure we break on \t
            Assert.AreEqual(7, bi.Next());

            // Ensure Current returns the most recent boundary
            Assert.AreEqual(7, bi.Current);

            // Check next boundary (Lucene^(TM))
            Assert.AreEqual(13, bi.Next());

            // Ensure Current returns the most recent boundary
            Assert.AreEqual(13, bi.Current);

            // Check next boundary (Lucene(TM) ^is a)
            Assert.AreEqual(18, bi.Next());

            // Ensure Current returns the most recent boundary
            Assert.AreEqual(18, bi.Current);

            // Move to start of high-performance
            bi.Next();
            bi.Next();

            // Check next boundary (high-\n^performance)
            Assert.AreEqual(29, bi.Next());


            // Check last boundary (in Java.^)
            Assert.AreEqual(108, bi.Last());


            // Check move past last boundary
            Assert.AreEqual(BreakIterator.Done, bi.Next());

            // Ensure we are still at last boundary
            Assert.AreEqual(108, bi.Current);


            // Check MovePrevious
            Assert.AreEqual(103, bi.Previous());

            // Ensure we get the same value for Current as the last move
            Assert.AreEqual(103, bi.Current);


            // Check MoveFirst
            Assert.AreEqual(0, bi.First());

            // Ensure we get the same value for Current as the last move
            Assert.AreEqual(0, bi.Current);


            // Check moving beyond first boundary
            Assert.AreEqual(BreakIterator.Done, bi.Previous());

            // Ensure we are still at first boundary
            Assert.AreEqual(0, bi.Current);


            // Check MoveLast()
            Assert.AreEqual(108, bi.Last());
        }
    }
}
