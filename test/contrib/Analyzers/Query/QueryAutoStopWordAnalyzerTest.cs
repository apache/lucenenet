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
using Lucene.Net.Analysis.Query;
using Lucene.Net.Analysis.Tokenattributes;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Test.Analysis;
using NUnit.Framework;
using Version=Lucene.Net.Util.Version;

namespace Lucene.Net.Analyzers.Query
{
    [TestFixture]
    public class QueryAutoStopWordAnalyzerTest : BaseTokenStreamTestCase
    {
        String[] variedFieldValues = { "the", "quick", "brown", "fox", "jumped", "over", "the", "lazy", "boring", "dog" };
        String[] repetitiveFieldValues = { "boring", "boring", "vaguelyboring" };
        RAMDirectory dir;
        Analyzer appAnalyzer;
        IndexReader reader;
        QueryAutoStopWordAnalyzer protectedAnalyzer;

        public override void SetUp()
        {
            dir = new RAMDirectory();
            appAnalyzer = new WhitespaceAnalyzer();
            IndexWriter writer = new IndexWriter(dir, appAnalyzer, true, IndexWriter.MaxFieldLength.UNLIMITED);
            int numDocs = 200;
            for (int i = 0; i < numDocs; i++)
            {
                Document doc = new Document();
                String variedFieldValue = variedFieldValues[i % variedFieldValues.Length];
                String repetitiveFieldValue = repetitiveFieldValues[i % repetitiveFieldValues.Length];
                doc.Add(new Field("variedField", variedFieldValue, Field.Store.YES, Field.Index.ANALYZED));
                doc.Add(new Field("repetitiveField", repetitiveFieldValue, Field.Store.YES, Field.Index.ANALYZED));
                writer.AddDocument(doc);
            }
            writer.Close();
            reader = IndexReader.Open(dir, true);
            protectedAnalyzer = new QueryAutoStopWordAnalyzer(Version.LUCENE_CURRENT, appAnalyzer);
            base.SetUp();
        }

        public override void TearDown()
        {
            reader.Close();
            base.TearDown();
        }

        //Helper method to query
        private int Search(Analyzer a, String queryString)
        {
            QueryParser qp = new QueryParser(Version.LUCENE_CURRENT, "repetitiveField", a);
            var q = qp.Parse(queryString);
            return new IndexSearcher(reader).Search(q, null, 1000).TotalHits;
        }

        [Test]
        public void TestUninitializedAnalyzer()
        {
            //Note: no calls to "addStopWord"
            String query = "variedField:quick repetitiveField:boring";
            int numHits1 = Search(protectedAnalyzer, query);
            int numHits2 = Search(appAnalyzer, query);
            Assert.AreEqual(numHits1, numHits2, "No filtering test");
        }

        /*
          * Test method for 'org.apache.lucene.analysis.QueryAutoStopWordAnalyzer.AddStopWords(IndexReader)'
          */
        [Test]
        public void TestDefaultAddStopWordsIndexReader()
        {
            protectedAnalyzer.AddStopWords(reader);
            int numHits = Search(protectedAnalyzer, "repetitiveField:boring");
            Assert.AreEqual(0, numHits, "Default filter should remove all docs");
        }


        /*
          * Test method for 'org.apache.lucene.analysis.QueryAutoStopWordAnalyzer.AddStopWords(IndexReader, int)'
          */
        [Test]
        public void TestAddStopWordsIndexReaderInt()
        {
            protectedAnalyzer.AddStopWords(reader, 1f / 2f);
            int numHits = Search(protectedAnalyzer, "repetitiveField:boring");
            Assert.AreEqual(0, numHits, "A filter on terms in > one half of docs remove boring docs");

            numHits = Search(protectedAnalyzer, "repetitiveField:vaguelyboring");
            Assert.True(numHits > 1, "A filter on terms in > half of docs should not remove vaguelyBoring docs");

            protectedAnalyzer.AddStopWords(reader, 1f / 4f);
            numHits = Search(protectedAnalyzer, "repetitiveField:vaguelyboring");
            Assert.AreEqual(0, numHits, "A filter on terms in > quarter of docs should remove vaguelyBoring docs");
        }


        [Test]
        public void TestAddStopWordsIndexReaderStringFloat()
        {
            protectedAnalyzer.AddStopWords(reader, "variedField", 1f / 2f);
            int numHits = Search(protectedAnalyzer, "repetitiveField:boring");
            Assert.True(numHits > 0, "A filter on one Field should not affect queris on another");

            protectedAnalyzer.AddStopWords(reader, "repetitiveField", 1f / 2f);
            numHits = Search(protectedAnalyzer, "repetitiveField:boring");
            Assert.AreEqual(numHits, 0, "A filter on the right Field should affect queries on it");
        }

        [Test]
        public void TestAddStopWordsIndexReaderStringInt()
        {
            int numStopWords = protectedAnalyzer.AddStopWords(reader, "repetitiveField", 10);
            Assert.True(numStopWords > 0, "Should have identified stop words");

            Term[] t = protectedAnalyzer.GetStopWords();
            Assert.AreEqual(t.Length, numStopWords, "num terms should = num stopwords returned");

            int numNewStopWords = protectedAnalyzer.AddStopWords(reader, "variedField", 10);
            Assert.True(numNewStopWords > 0, "Should have identified more stop words");
            t = protectedAnalyzer.GetStopWords();
            Assert.AreEqual(t.Length, numStopWords + numNewStopWords, "num terms should = num stopwords returned");
        }

        [Test]
        public void TestNoFieldNamePollution()
        {
            protectedAnalyzer.AddStopWords(reader, "repetitiveField", 10);
            int numHits = Search(protectedAnalyzer, "repetitiveField:boring");
            Assert.AreEqual(0, numHits, "Check filter set up OK");

            numHits = Search(protectedAnalyzer, "variedField:boring");
            Assert.True(numHits > 0, "Filter should not prevent stopwords in one field being used in another ");

        }

        /*
         * subclass that acts just like whitespace analyzer for testing
         */
        private class QueryAutoStopWordSubclassAnalyzer : QueryAutoStopWordAnalyzer
        {
            public QueryAutoStopWordSubclassAnalyzer(Version matchVersion)
                : base(matchVersion, new WhitespaceAnalyzer())
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
            QueryAutoStopWordAnalyzer a = new QueryAutoStopWordSubclassAnalyzer(Version.LUCENE_CURRENT);
            a.AddStopWords(reader, "repetitiveField", 10);
            int numHits = Search(a, "repetitiveField:boring");
            Assert.False(numHits == 0);
        }

        /*
         * analyzer that does not support reuse
         * it is LetterTokenizer on odd invocations, WhitespaceTokenizer on even.
         */
        private class NonreusableAnalyzer : Analyzer
        {
            int invocationCount = 0;

            public override TokenStream TokenStream(String fieldName, TextReader reader)
            {
                if (++invocationCount % 2 == 0)
                    return new WhitespaceTokenizer(reader);
                else
                    return new LetterTokenizer(reader);
            }
        }

        [Test]
        public void TestWrappingNonReusableAnalyzer()
        {
            QueryAutoStopWordAnalyzer a = new QueryAutoStopWordAnalyzer(Version.LUCENE_CURRENT, new NonreusableAnalyzer());
            a.AddStopWords(reader, 10);
            int numHits = Search(a, "repetitiveField:boring");
            Assert.True(numHits == 0);
            numHits = Search(a, "repetitiveField:vaguelyboring");
            Assert.True(numHits == 0);
        }

        [Test]
        public void TestTokenStream()
        {
            QueryAutoStopWordAnalyzer a = new QueryAutoStopWordAnalyzer(Version.LUCENE_CURRENT, new WhitespaceAnalyzer());
            a.AddStopWords(reader, 10);
            TokenStream ts = a.TokenStream("repetitiveField", new StringReader("this boring"));
            ITermAttribute termAtt = ts.GetAttribute<ITermAttribute>();
            Assert.True(ts.IncrementToken());
            Assert.AreEqual("this", termAtt.Term);
            Assert.False(ts.IncrementToken());
        }
    }
}
