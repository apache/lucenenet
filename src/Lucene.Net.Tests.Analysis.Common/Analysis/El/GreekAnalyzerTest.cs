// Lucene version compatibility level 4.8.1
using Lucene.Net.Util;
using NUnit.Framework;
using System;

namespace Lucene.Net.Analysis.El
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
    /// A unit test class for verifying the correct operation of the GreekAnalyzer.
    /// 
    /// </summary>
    public class GreekAnalyzerTest : BaseTokenStreamTestCase
    {

        /// <summary>
        /// Test the analysis of various greek strings.
        /// </summary>
        /// <exception cref="Exception"> in case an error occurs </exception>
        [Test]
        public virtual void TestAnalyzer()
        {
            Analyzer a = new GreekAnalyzer(TEST_VERSION_CURRENT);
            // Verify the correct analysis of capitals and small accented letters, and
            // stemming
            AssertAnalyzesTo(a, "Μία εξαιρετικά καλή και πλούσια σειρά χαρακτήρων της Ελληνικής γλώσσας", new string[] { "μια", "εξαιρετ", "καλ", "πλουσ", "σειρ", "χαρακτηρ", "ελληνικ", "γλωσσ" });
            // Verify the correct analysis of small letters with diaeresis and the elimination
            // of punctuation marks
            AssertAnalyzesTo(a, "Προϊόντα (και)     [πολλαπλές] - ΑΝΑΓΚΕΣ", new string[] { "προιοντ", "πολλαπλ", "αναγκ" });
            // Verify the correct analysis of capital accented letters and capital letters with diaeresis,
            // as well as the elimination of stop words
            AssertAnalyzesTo(a, "ΠΡΟΫΠΟΘΕΣΕΙΣ  Άψογος, ο μεστός και οι άλλοι", new string[] { "προυποθεσ", "αψογ", "μεστ", "αλλ" });
        }

        /// <summary>
        /// Test the analysis of various greek strings.
        /// </summary>
        /// <exception cref="Exception"> in case an error occurs </exception>
        /// @deprecated (3.1) Remove this test when support for 3.0 is no longer needed 
        [Test]
        [Obsolete("(3.1) Remove this test when support for 3.0 is no longer needed")]
        public virtual void TestAnalyzerBWCompat()
        {
            Analyzer a = new GreekAnalyzer(LuceneVersion.LUCENE_30);
            // Verify the correct analysis of capitals and small accented letters
            AssertAnalyzesTo(a, "Μία εξαιρετικά καλή και πλούσια σειρά χαρακτήρων της Ελληνικής γλώσσας", new string[] { "μια", "εξαιρετικα", "καλη", "πλουσια", "σειρα", "χαρακτηρων", "ελληνικησ", "γλωσσασ" });
            // Verify the correct analysis of small letters with diaeresis and the elimination
            // of punctuation marks
            AssertAnalyzesTo(a, "Προϊόντα (και)     [πολλαπλές] - ΑΝΑΓΚΕΣ", new string[] { "προιοντα", "πολλαπλεσ", "αναγκεσ" });
            // Verify the correct analysis of capital accented letters and capital letters with diaeresis,
            // as well as the elimination of stop words
            AssertAnalyzesTo(a, "ΠΡΟΫΠΟΘΕΣΕΙΣ  Άψογος, ο μεστός και οι άλλοι", new string[] { "προυποθεσεισ", "αψογοσ", "μεστοσ", "αλλοι" });
        }

        [Test]
        public virtual void TestReusableTokenStream()
        {
            Analyzer a = new GreekAnalyzer(TEST_VERSION_CURRENT);
            // Verify the correct analysis of capitals and small accented letters, and
            // stemming
            AssertAnalyzesTo(a, "Μία εξαιρετικά καλή και πλούσια σειρά χαρακτήρων της Ελληνικής γλώσσας", new string[] { "μια", "εξαιρετ", "καλ", "πλουσ", "σειρ", "χαρακτηρ", "ελληνικ", "γλωσσ" });
            // Verify the correct analysis of small letters with diaeresis and the elimination
            // of punctuation marks
            AssertAnalyzesTo(a, "Προϊόντα (και)     [πολλαπλές] - ΑΝΑΓΚΕΣ", new string[] { "προιοντ", "πολλαπλ", "αναγκ" });
            // Verify the correct analysis of capital accented letters and capital letters with diaeresis,
            // as well as the elimination of stop words
            AssertAnalyzesTo(a, "ΠΡΟΫΠΟΘΕΣΕΙΣ  Άψογος, ο μεστός και οι άλλοι", new string[] { "προυποθεσ", "αψογ", "μεστ", "αλλ" });
        }

        /// <summary>
        /// blast some random strings through the analyzer </summary>
        [Test]
        public virtual void TestRandomStrings()
        {
            CheckRandomData(Random, new GreekAnalyzer(TEST_VERSION_CURRENT), 1000 * RandomMultiplier);
        }
    }
}