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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Cz;
using Lucene.Net.Test.Analysis;
using NUnit.Framework;
using Version = Lucene.Net.Util.Version;

namespace Lucene.Net.Analyzers.Cz
{
    /*
     * Test the CzechAnalyzer
     * 
     * CzechAnalyzer is like a StandardAnalyzer with a custom stopword list.
     *
     */
    [TestFixture]
    public class TestCzechAnalyzer : BaseTokenStreamTestCase
    {
        string customStopFile = @"Cz\customStopWordFile.txt";

        [Test]
        public void TestStopWord()
        {
            AssertAnalyzesTo(new CzechAnalyzer(Version.LUCENE_CURRENT), "Pokud mluvime o volnem", new String[] { "mluvime", "volnem" });
        }

        [Test]
        public void TestReusableTokenStream()
        {
            Analyzer analyzer = new CzechAnalyzer(Version.LUCENE_CURRENT);
            AssertAnalyzesToReuse(analyzer, "Pokud mluvime o volnem", new String[] { "mluvime", "volnem" });
            AssertAnalyzesToReuse(analyzer, "Česká Republika", new String[] { "česká", "republika" });
        }

        /*
         * An input stream that always throws IOException for testing.
         */
        private class UnreliableInputStream : MemoryStream
        {
            public override int Read(byte[] buffer, int offset, int count)
            {
                throw new IOException();
            }
        }

        /*
         * The loadStopWords method does not throw IOException on error,
         * instead previously it set the stoptable to null (versus empty)
         * this would cause a NPE when it is time to create the StopFilter.
         */
        [Test]
        public void TestInvalidStopWordFile()
        {
            CzechAnalyzer cz = new CzechAnalyzer(Version.LUCENE_CURRENT);
            cz.LoadStopWords(new UnreliableInputStream(), Encoding.UTF8);
            AssertAnalyzesTo(cz, "Pokud mluvime o volnem",
                new String[] { "pokud", "mluvime", "o", "volnem" });
        }

        /* 
         * Test that changes to the stop table via loadStopWords are applied immediately
         * when using reusable token streams.
         */
        [Test]
        public void TestStopWordFileReuse()
        {
            CzechAnalyzer cz = new CzechAnalyzer(Version.LUCENE_CURRENT);
            AssertAnalyzesToReuse(cz, "Česká Republika",
              new String[] { "česká", "republika" });

            Stream stopwords = new FileStream(customStopFile, FileMode.Open, FileAccess.Read);
            cz.LoadStopWords(stopwords, Encoding.UTF8);

            AssertAnalyzesToReuse(cz, "Česká Republika", new String[] { "česká" });
        }
    }
}
