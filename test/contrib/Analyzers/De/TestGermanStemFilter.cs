/*
 *
 * Licensed to the Apache Software Foundation (ASF) under one
 * or more contributor license agreements.  See the NOTICE file
 * distributed with this work for additional information
 * regarding copyright ownership.  The ASF licenses this file
 * to you under the Apache License, Version 2.0 (the
 * "License"); you may not use this file except in compliance
 * with the License.  You may obtain a copy of the License at
 *
 *   http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing,
 * software distributed under the License is distributed on an
 * "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
 * KIND, either express or implied.  See the License for the
 * specific language governing permissions and limitations
 * under the License.
 *
*/

using System;
using System.IO;
using System.Text;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.De;
using Lucene.Net.Test.Analysis;
using NUnit.Framework;
using Version=Lucene.Net.Util.Version;

namespace Lucene.Net.Analyzers.De
{
    /*
     * Test the German stemmer. The stemming algorithm is known to work less 
     * than perfect, as it doesn't use any word lists with exceptions. We 
     * also check some of the cases where the algorithm is wrong.
     *
     */
    [TestFixture]
    public class TestGermanStemFilter : BaseTokenStreamTestCase
    {
        const string TestFile = @"De\data.txt";
        const string TestFileDin2 = @"De\data_din2.txt";

        [Test]
        public void TestDin1Stemming()
        {
            // read test cases from external file:
            using (var fis = new FileStream(TestFile, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var breader = new StreamReader(fis, Encoding.GetEncoding("iso-8859-1")))
            {
                while (true)
                {
                    String line = breader.ReadLine();
                    if (line == null)
                        break;
                    line = line.Trim();
                    if (line.StartsWith("#") || string.IsNullOrEmpty(line))
                        continue; // ignore comments and empty lines
                    String[] parts = line.Split(';');
                    //System.out.println(parts[0] + " -- " + parts[1]);
                    Check(parts[0], parts[1], false);
                }
            }
        }

        [Test]
        public void TestDin2Stemming()
        {
            // read test cases from external file(s):
            foreach (var file in new[] { TestFile, TestFileDin2 })
            {
                using (var fis = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (var breader = new StreamReader(fis, Encoding.GetEncoding("iso-8859-1")))
                {
                    string line;
                    while ((line = breader.ReadLine()) != null)
                    {
                        line = line.Trim();
                        if (line.StartsWith("#") || string.IsNullOrEmpty(line))
                            continue; // ignore comments and empty lines

                        var parts = line.Split(';');
                        Check(parts[0], parts[1], true);
                    }
                }
            }
        }

        [Test]
        public void TestReusableTokenStream()
        {
            Analyzer a = new GermanAnalyzer(Version.LUCENE_CURRENT);
            CheckReuse(a, "Tisch", "tisch");
            CheckReuse(a, "Tische", "tisch");
            CheckReuse(a, "Tischen", "tisch");
        }

        /*
         * subclass that acts just like whitespace analyzer for testing
         */
        private sealed class GermanSubclassAnalyzer : GermanAnalyzer
        {
            public GermanSubclassAnalyzer(Version matchVersion)
                : base(matchVersion)
            {
            }

            public override TokenStream TokenStream(String fieldName, TextReader reader)
            {
                return new WhitespaceTokenizer(reader);
            }
        }

        [Test]
        public void TestLucene1678BwComp()
        {
            CheckReuse(new GermanSubclassAnalyzer(Version.LUCENE_CURRENT), "Tischen", "Tischen");
        }

        /* 
         * Test that changes to the exclusion table are applied immediately
         * when using reusable token streams.
         */
        [Test]
        public void TestExclusionTableReuse()
        {
            var a = new GermanAnalyzer(Version.LUCENE_CURRENT);
            CheckReuse(a, "tischen", "tisch");
            a.SetStemExclusionTable(new[] { "tischen" });
            CheckReuse(a, "tischen", "tischen");
        }

        private void Check(String input, String expected, bool useDin2)
        {
            CheckOneTerm(new GermanAnalyzer(Version.LUCENE_CURRENT, useDin2), input, expected);
        }

        private void CheckReuse(Analyzer a, String input, String expected)
        {
            CheckOneTermReuse(a, input, expected);
        }
    }
}
