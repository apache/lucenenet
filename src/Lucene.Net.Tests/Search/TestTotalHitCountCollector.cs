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
    using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;
    using StringField = StringField;

    [TestFixture]
    public class TestTotalHitCountCollector : LuceneTestCase
    {
        [Test]
        public virtual void TestBasics()
        {
            Directory indexStore = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random, indexStore);
            for (int i = 0; i < 5; i++)
            {
                Document doc = new Document();
                doc.Add(new StringField("string", "a" + i, Field.Store.NO));
                doc.Add(new StringField("string", "b" + i, Field.Store.NO));
                writer.AddDocument(doc);
            }
            IndexReader reader = writer.GetReader();
            writer.Dispose();

            IndexSearcher searcher = NewSearcher(reader);
            TotalHitCountCollector c = new TotalHitCountCollector();
            searcher.Search(new MatchAllDocsQuery(), null, c);
            Assert.AreEqual(5, c.TotalHits);
            reader.Dispose();
            indexStore.Dispose();
        }
    }
}