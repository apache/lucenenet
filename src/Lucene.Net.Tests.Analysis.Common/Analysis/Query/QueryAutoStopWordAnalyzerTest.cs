// Lucene version compatibility level 4.8.1
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Support;
using NUnit.Framework;

namespace Lucene.Net.Analysis.Query
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

    public class QueryAutoStopWordAnalyzerTest : BaseTokenStreamTestCase
    {
        internal string[] variedFieldValues = new string[] { "the", "quick", "brown", "fox", "jumped", "over", "the", "lazy", "boring", "dog" };
        internal string[] repetitiveFieldValues = new string[] { "boring", "boring", "vaguelyboring" };
        internal RAMDirectory dir;
        internal Analyzer appAnalyzer;
        internal IndexReader reader;
        internal QueryAutoStopWordAnalyzer protectedAnalyzer;

        public override void SetUp()
        {
            base.SetUp();
            dir = new RAMDirectory();
            appAnalyzer = new MockAnalyzer(Random, MockTokenizer.WHITESPACE, false);
            IndexWriter writer = new IndexWriter(dir, new IndexWriterConfig(TEST_VERSION_CURRENT, appAnalyzer));
            int numDocs = 200;
            for (int i = 0; i < numDocs; i++)
            {
                Document doc = new Document();
                string variedFieldValue = variedFieldValues[i % variedFieldValues.Length];
                string repetitiveFieldValue = repetitiveFieldValues[i % repetitiveFieldValues.Length];
                doc.Add(new TextField("variedField", variedFieldValue, Field.Store.YES));
                doc.Add(new TextField("repetitiveField", repetitiveFieldValue, Field.Store.YES));
                writer.AddDocument(doc);
            }
            writer.Dispose();
            reader = DirectoryReader.Open(dir);
        }

        public override void TearDown()
        {
            reader.Dispose();
            base.TearDown();
        }

        [Test]
        public virtual void TestNoStopwords()
        {
            // Note: an empty list of fields passed in
            protectedAnalyzer = new QueryAutoStopWordAnalyzer(TEST_VERSION_CURRENT, appAnalyzer, reader, Collections.EmptyList<string>(), 1);
            TokenStream protectedTokenStream = protectedAnalyzer.GetTokenStream("variedField", "quick");
            AssertTokenStreamContents(protectedTokenStream, new string[] { "quick" });

            protectedTokenStream = protectedAnalyzer.GetTokenStream("repetitiveField", "boring");
            AssertTokenStreamContents(protectedTokenStream, new string[] { "boring" });
        }

        [Test]
        public virtual void TestDefaultStopwordsAllFields()
        {
            protectedAnalyzer = new QueryAutoStopWordAnalyzer(TEST_VERSION_CURRENT, appAnalyzer, reader);
            TokenStream protectedTokenStream = protectedAnalyzer.GetTokenStream("repetitiveField", "boring");
            AssertTokenStreamContents(protectedTokenStream, new string[0]); // Default stop word filtering will remove boring
        }

        [Test]
        public virtual void TestStopwordsAllFieldsMaxPercentDocs()
        {
            protectedAnalyzer = new QueryAutoStopWordAnalyzer(TEST_VERSION_CURRENT, appAnalyzer, reader, 1f / 2f);

            TokenStream protectedTokenStream = protectedAnalyzer.GetTokenStream("repetitiveField", "boring");
            // A filter on terms in > one half of docs remove boring
            AssertTokenStreamContents(protectedTokenStream, new string[0]);

            protectedTokenStream = protectedAnalyzer.GetTokenStream("repetitiveField", "vaguelyboring");
            // A filter on terms in > half of docs should not remove vaguelyBoring
            AssertTokenStreamContents(protectedTokenStream, new string[] { "vaguelyboring" });

            protectedAnalyzer = new QueryAutoStopWordAnalyzer(TEST_VERSION_CURRENT, appAnalyzer, reader, 1f / 4f);
            protectedTokenStream = protectedAnalyzer.GetTokenStream("repetitiveField", "vaguelyboring");
            // A filter on terms in > quarter of docs should remove vaguelyBoring
            AssertTokenStreamContents(protectedTokenStream, new string[0]);
        }

        [Test]
        public virtual void TestStopwordsPerFieldMaxPercentDocs()
        {
            protectedAnalyzer = new QueryAutoStopWordAnalyzer(TEST_VERSION_CURRENT, appAnalyzer, reader, new string[] { "variedField" }, 1f / 2f);
            TokenStream protectedTokenStream = protectedAnalyzer.GetTokenStream("repetitiveField", "boring");
            // A filter on one Field should not affect queries on another
            AssertTokenStreamContents(protectedTokenStream, new string[] { "boring" });

            protectedAnalyzer = new QueryAutoStopWordAnalyzer(TEST_VERSION_CURRENT, appAnalyzer, reader, new string[] { "variedField", "repetitiveField" }, 1f / 2f);
            protectedTokenStream = protectedAnalyzer.GetTokenStream("repetitiveField", "boring");
            // A filter on the right Field should affect queries on it
            AssertTokenStreamContents(protectedTokenStream, new string[0]);
        }

        [Test]
        public virtual void TestStopwordsPerFieldMaxDocFreq()
        {
            protectedAnalyzer = new QueryAutoStopWordAnalyzer(TEST_VERSION_CURRENT, appAnalyzer, reader, new string[] { "repetitiveField" }, 10);
            int numStopWords = protectedAnalyzer.GetStopWords("repetitiveField").Length;
            assertTrue("Should have identified stop words", numStopWords > 0);

            protectedAnalyzer = new QueryAutoStopWordAnalyzer(TEST_VERSION_CURRENT, appAnalyzer, reader, new string[] { "repetitiveField", "variedField" }, 10);
            int numNewStopWords = protectedAnalyzer.GetStopWords("repetitiveField").Length + protectedAnalyzer.GetStopWords("variedField").Length;
            assertTrue("Should have identified more stop words", numNewStopWords > numStopWords);
        }

        [Test]
        public virtual void TestNoFieldNamePollution()
        {
            protectedAnalyzer = new QueryAutoStopWordAnalyzer(TEST_VERSION_CURRENT, appAnalyzer, reader, new string[] { "repetitiveField" }, 10);

            TokenStream protectedTokenStream = protectedAnalyzer.GetTokenStream("repetitiveField", "boring");
            // Check filter set up OK
            AssertTokenStreamContents(protectedTokenStream, new string[0]);

            protectedTokenStream = protectedAnalyzer.GetTokenStream("variedField", "boring");
            // Filter should not prevent stopwords in one field being used in another
            AssertTokenStreamContents(protectedTokenStream, new string[] { "boring" });
        }

        [Test]
        public virtual void TestTokenStream()
        {
            QueryAutoStopWordAnalyzer a = new QueryAutoStopWordAnalyzer(TEST_VERSION_CURRENT, new MockAnalyzer(Random, MockTokenizer.WHITESPACE, false), reader, 10);
            TokenStream ts = a.GetTokenStream("repetitiveField", "this boring");
            AssertTokenStreamContents(ts, new string[] { "this" });
        }
    }
}