using Lucene.Net.Documents;
using NUnit.Framework;
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
    using MultiFields = Lucene.Net.Index.MultiFields;
    using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;
    using Term = Lucene.Net.Index.Term;
    using Terms = Lucene.Net.Index.Terms;

    /// <summary>
    /// Tests <seealso cref="PrefixQuery"/> class.
    ///
    /// </summary>
    [TestFixture]
    public class TestPrefixQuery : LuceneTestCase
    {
        [Test]
        public virtual void TestPrefixQuery_Mem()
        {
            Directory directory = NewDirectory();

            string[] categories = new string[] { "/Computers", "/Computers/Mac", "/Computers/Windows" };
            RandomIndexWriter writer = new RandomIndexWriter(Random, directory);
            for (int i = 0; i < categories.Length; i++)
            {
                Document doc = new Document();
                doc.Add(NewStringField("category", categories[i], Field.Store.YES));
                writer.AddDocument(doc);
            }
            IndexReader reader = writer.GetReader();

            PrefixQuery query = new PrefixQuery(new Term("category", "/Computers"));
            IndexSearcher searcher = NewSearcher(reader);
            ScoreDoc[] hits = searcher.Search(query, null, 1000).ScoreDocs;
            Assert.AreEqual(3, hits.Length, "All documents in /Computers category and below");

            query = new PrefixQuery(new Term("category", "/Computers/Mac"));
            hits = searcher.Search(query, null, 1000).ScoreDocs;
            Assert.AreEqual(1, hits.Length, "One in /Computers/Mac");

            query = new PrefixQuery(new Term("category", ""));
            Terms terms = MultiFields.GetTerms(searcher.IndexReader, "category");
            Assert.IsFalse(query.GetTermsEnum(terms) is PrefixTermsEnum);
            hits = searcher.Search(query, null, 1000).ScoreDocs;
            Assert.AreEqual(3, hits.Length, "everything");
            writer.Dispose();
            reader.Dispose();
            directory.Dispose();
        }
    }
}