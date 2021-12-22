using Lucene.Net.Attributes;
using Lucene.Net.Documents;
using NUnit.Framework;
using System;
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

    using DateTools = DateTools;
    using Directory = Lucene.Net.Store.Directory;
    using Document = Documents.Document;
    using Field = Field;
    using IndexReader = Lucene.Net.Index.IndexReader;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;
    using Term = Lucene.Net.Index.Term;

    /// <summary>
    /// DateFilter JUnit tests.
    ///
    ///
    /// </summary>
    [TestFixture]
    public class TestDateFilter : LuceneTestCase
    {
        ///
        [OneTimeSetUp]
        public override void BeforeClass() // LUCENENET specific - renamed from TestBefore() to ensure calling order vs base class
        {
            base.BeforeClass();

            // create an index
            Directory indexStore = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random, indexStore);

            long now = J2N.Time.CurrentTimeMilliseconds();

            Document doc = new Document();
            // add time that is in the past
            doc.Add(NewStringField("datefield", DateTools.TimeToString(now - 1000, DateResolution.MILLISECOND), Field.Store.YES));
            doc.Add(NewTextField("body", "Today is a very sunny day in New York City", Field.Store.YES));
            writer.AddDocument(doc);

            IndexReader reader = writer.GetReader();
            writer.Dispose();
            IndexSearcher searcher = NewSearcher(reader);

            // filter that should preserve matches
            // DateFilter df1 = DateFilter.Before("datefield", now);
            TermRangeFilter df1 = TermRangeFilter.NewStringRange("datefield", DateTools.TimeToString(now - 2000, DateResolution.MILLISECOND), DateTools.TimeToString(now, DateResolution.MILLISECOND), false, true);
            // filter that should discard matches
            // DateFilter df2 = DateFilter.Before("datefield", now - 999999);
            TermRangeFilter df2 = TermRangeFilter.NewStringRange("datefield", DateTools.TimeToString(0, DateResolution.MILLISECOND), DateTools.TimeToString(now - 2000, DateResolution.MILLISECOND), true, false);

            // search something that doesn't exist with DateFilter
            Query query1 = new TermQuery(new Term("body", "NoMatchForthis"));

            // search for something that does exists
            Query query2 = new TermQuery(new Term("body", "sunny"));

            ScoreDoc[] result;

            // ensure that queries return expected results without DateFilter first
            result = searcher.Search(query1, null, 1000).ScoreDocs;
            Assert.AreEqual(0, result.Length);

            result = searcher.Search(query2, null, 1000).ScoreDocs;
            Assert.AreEqual(1, result.Length);

            // run queries with DateFilter
            result = searcher.Search(query1, df1, 1000).ScoreDocs;
            Assert.AreEqual(0, result.Length);

            result = searcher.Search(query1, df2, 1000).ScoreDocs;
            Assert.AreEqual(0, result.Length);

            result = searcher.Search(query2, df1, 1000).ScoreDocs;
            Assert.AreEqual(1, result.Length);

            result = searcher.Search(query2, df2, 1000).ScoreDocs;
            Assert.AreEqual(0, result.Length);
            reader.Dispose();
            indexStore.Dispose();
        }

        [Test, LuceneNetSpecific]
        public void Test()
        {
            // noop, required for the before and after tests to run
        }

        ///
        [OneTimeTearDown]
        public override void AfterClass() // LUCENENET specific - renamed from TestAfter() to ensure calling order vs base class
        {
            // create an index
            Directory indexStore = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random, indexStore);

            long now = J2N.Time.CurrentTimeMilliseconds();

            Document doc = new Document();
            // add time that is in the future
            doc.Add(NewStringField("datefield", DateTools.TimeToString(now + 888888, DateResolution.MILLISECOND), Field.Store.YES));
            doc.Add(NewTextField("body", "Today is a very sunny day in New York City", Field.Store.YES));
            writer.AddDocument(doc);

            IndexReader reader = writer.GetReader();
            writer.Dispose();
            IndexSearcher searcher = NewSearcher(reader);

            // filter that should preserve matches
            // DateFilter df1 = DateFilter.After("datefield", now);
            TermRangeFilter df1 = TermRangeFilter.NewStringRange("datefield", DateTools.TimeToString(now, DateResolution.MILLISECOND), DateTools.TimeToString(now + 999999, DateResolution.MILLISECOND), true, false);
            // filter that should discard matches
            // DateFilter df2 = DateFilter.After("datefield", now + 999999);
            TermRangeFilter df2 = TermRangeFilter.NewStringRange("datefield", DateTools.TimeToString(now + 999999, DateResolution.MILLISECOND), DateTools.TimeToString(now + 999999999, DateResolution.MILLISECOND), false, true);

            // search something that doesn't exist with DateFilter
            Query query1 = new TermQuery(new Term("body", "NoMatchForthis"));

            // search for something that does exists
            Query query2 = new TermQuery(new Term("body", "sunny"));

            ScoreDoc[] result;

            // ensure that queries return expected results without DateFilter first
            result = searcher.Search(query1, null, 1000).ScoreDocs;
            Assert.AreEqual(0, result.Length);

            result = searcher.Search(query2, null, 1000).ScoreDocs;
            Assert.AreEqual(1, result.Length);

            // run queries with DateFilter
            result = searcher.Search(query1, df1, 1000).ScoreDocs;
            Assert.AreEqual(0, result.Length);

            result = searcher.Search(query1, df2, 1000).ScoreDocs;
            Assert.AreEqual(0, result.Length);

            result = searcher.Search(query2, df1, 1000).ScoreDocs;
            Assert.AreEqual(1, result.Length);

            result = searcher.Search(query2, df2, 1000).ScoreDocs;
            Assert.AreEqual(0, result.Length);
            reader.Dispose();
            indexStore.Dispose();

            base.AfterClass();
        }
    }
}