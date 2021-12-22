using Lucene.Net.Documents;
using NUnit.Framework;
using JCG = J2N.Collections.Generic;
using Assert = Lucene.Net.TestFramework.Assert;

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

    using Directory = Lucene.Net.Store.Directory;
    using Document = Documents.Document;
    using Field = Field;
    using IndexReader = Lucene.Net.Index.IndexReader;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;

    /// <summary>
    /// A basic unit test for FieldCacheTermsFilter
    /// </summary>
    /// <seealso cref= Lucene.Net.Search.FieldCacheTermsFilter </seealso>
    [TestFixture]
    public class TestFieldCacheTermsFilter : LuceneTestCase
    {
        [Test]
        public virtual void TestMissingTerms()
        {
            string fieldName = "field1";
            Directory rd = NewDirectory();
            RandomIndexWriter w = new RandomIndexWriter(Random, rd);
            for (int i = 0; i < 100; i++)
            {
                Document doc = new Document();
                int term = i * 10; //terms are units of 10;
                doc.Add(NewStringField(fieldName, "" + term, Field.Store.YES));
                w.AddDocument(doc);
            }
            IndexReader reader = w.GetReader();
            w.Dispose();

            IndexSearcher searcher = NewSearcher(reader);
            int numDocs = reader.NumDocs;
            ScoreDoc[] results;
            MatchAllDocsQuery q = new MatchAllDocsQuery();

            JCG.List<string> terms = new JCG.List<string>();
            terms.Add("5");
            results = searcher.Search(q, new FieldCacheTermsFilter(fieldName, terms.ToArray()), numDocs).ScoreDocs;
            Assert.AreEqual(0, results.Length, "Must match nothing");

            terms = new JCG.List<string>();
            terms.Add("10");
            results = searcher.Search(q, new FieldCacheTermsFilter(fieldName, terms.ToArray()), numDocs).ScoreDocs;
            Assert.AreEqual(1, results.Length, "Must match 1");

            terms = new JCG.List<string>();
            terms.Add("10");
            terms.Add("20");
            results = searcher.Search(q, new FieldCacheTermsFilter(fieldName, terms.ToArray()), numDocs).ScoreDocs;
            Assert.AreEqual(2, results.Length, "Must match 2");

            reader.Dispose();
            rd.Dispose();
        }
    }
}