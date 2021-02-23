// Lucene version compatibility level 4.8.1
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Util;
using NUnit.Framework;
using System.Text;

namespace Lucene.Net.Analysis.Miscellaneous
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

    public class TestLimitTokenCountAnalyzer_ : BaseTokenStreamTestCase
    {

        [Test]
        public virtual void TestLimitTokenCountAnalyzer()
        {
            foreach (bool consumeAll in new bool[] { true, false })
            {
                MockAnalyzer mock = new MockAnalyzer(Random);

                // if we are consuming all tokens, we can use the checks, 
                // otherwise we can't
                mock.EnableChecks = consumeAll;
                Analyzer a = new LimitTokenCountAnalyzer(mock, 2, consumeAll);

                // dont use assertAnalyzesTo here, as the end offset is not the end of the string (unless consumeAll is true, in which case its correct)!
                AssertTokenStreamContents(a.GetTokenStream("dummy", "1  2     3  4  5"), new string[] { "1", "2" }, new int[] { 0, 3 }, new int[] { 1, 4 }, consumeAll ? (int?)16 : null);
                AssertTokenStreamContents(a.GetTokenStream("dummy", "1 2 3 4 5"), new string[] { "1", "2" }, new int[] { 0, 2 }, new int[] { 1, 3 }, consumeAll ? (int?)9 : null);

                // less than the limit, ensure we behave correctly
                AssertTokenStreamContents(a.GetTokenStream("dummy", "1  "), new string[] { "1" }, new int[] { 0 }, new int[] { 1 }, (consumeAll ? (int?)3 : null));

                // equal to limit
                AssertTokenStreamContents(a.GetTokenStream("dummy", "1  2  "), new string[] { "1", "2" }, new int[] { 0, 3 }, new int[] { 1, 4 }, consumeAll ? (int?)6 : null);
            }
        }

        [Test]
        public virtual void TestLimitTokenCountIndexWriter()
        {

            foreach (bool consumeAll in new bool[] { true, false })
            {
                Store.Directory dir = NewDirectory();
                int limit = TestUtil.NextInt32(Random, 50, 101000);
                MockAnalyzer mock = new MockAnalyzer(Random);

                // if we are consuming all tokens, we can use the checks, 
                // otherwise we can't
                mock.EnableChecks = consumeAll;
                Analyzer a = new LimitTokenCountAnalyzer(mock, limit, consumeAll);

                IndexWriter writer = new IndexWriter(dir, new IndexWriterConfig(TEST_VERSION_CURRENT, a));

                Document doc = new Document();
                StringBuilder b = new StringBuilder();
                for (int i = 1; i < limit; i++)
                {
                    b.Append(" a");
                }
                b.Append(" x");
                b.Append(" z");
                doc.Add(NewTextField("field", b.ToString(), Field.Store.NO));
                writer.AddDocument(doc);
                writer.Dispose();

                IndexReader reader = DirectoryReader.Open(dir);
                Term t = new Term("field", "x");
                assertEquals(1, reader.DocFreq(t));
                t = new Term("field", "z");
                assertEquals(0, reader.DocFreq(t));
                reader.Dispose();
                dir.Dispose();
            }
        }
    }
}