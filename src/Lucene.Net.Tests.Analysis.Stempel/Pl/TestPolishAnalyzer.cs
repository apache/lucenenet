using Lucene.Net.Analysis.Util;
using NUnit.Framework;
using System;
using System.IO;

namespace Lucene.Net.Analysis.Pl
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

    public class TestPolishAnalyzer : BaseTokenStreamTestCase
    {
        /// <summary>
        /// This test fails with NPE when the 
        /// stopwords file is missing in classpath
        /// </summary>
        [Test]
        public void TestResourcesAvailable()
        {
            new PolishAnalyzer(TEST_VERSION_CURRENT);
        }

        /// <summary>
        /// test stopwords and stemming
        /// </summary>
        [Test]
        public void TestBasics()
        {
            Analyzer a = new PolishAnalyzer(TEST_VERSION_CURRENT);
            // stemming
            CheckOneTerm(a, "studenta", "student");
            CheckOneTerm(a, "studenci", "student");
            // stopword
            AssertAnalyzesTo(a, "był", new String[] { });
        }

        /// <summary>
        /// test use of exclusion set
        /// </summary>
        [Test]
        public void TestExclude()
        {
            CharArraySet exclusionSet = new CharArraySet(TEST_VERSION_CURRENT, AsSet("studenta"), false); ;
            Analyzer a = new PolishAnalyzer(TEST_VERSION_CURRENT,
                PolishAnalyzer.DefaultStopSet, exclusionSet);
            CheckOneTerm(a, "studenta", "studenta");
            CheckOneTerm(a, "studenci", "student");
        }

        /// <summary>
        /// blast some random strings through the analyzer
        /// </summary>
        [Test]
        public void TestRandomStrings()
        {
            CheckRandomData(Random, new PolishAnalyzer(TEST_VERSION_CURRENT), 1000 * RandomMultiplier);
        }

        /// <summary>
        /// LUCENENET specific. The original Java implementation relied on String.subSequence(int, int) to throw an IndexOutOfBoundsException 
        /// (in .NET, it would be string.SubString(int, int) and an ArgumentOutOfRangeException). 
        /// However, the logic was corrected for .NET to test when the argument is negative and not 
        /// throw an exception, since exceptions are expensive and not meant for "normal"
        /// behavior in .NET. This test case was made trying to figure out that issue (since initially an IndexOutOfRangeException,
        /// rather than ArgumentOutOfRangeException, was in the catch block which made the TestRandomStrings test fail). 
        /// It will trigger the behavior that cause the second substring argument to be negative 
        /// (although that behavior no longer throws an exception).
        /// </summary>
        [Test]
        public void TestOutOfRange()
        {
            var a = new PolishAnalyzer(TEST_VERSION_CURRENT);
            var text = "zyaolz 96619727 p";
            var reader = new StringReader(text);
            int remainder = 2;
            using var ts = a.GetTokenStream("dummy", (TextReader)new MockCharFilter(reader, remainder));
            ts.Reset();

            while (ts.IncrementToken())
            {
            }

            ts.End();
        }
    }
}
