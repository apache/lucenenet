using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using NUnit.Framework;
using System;

namespace Lucene.Net.Sandbox.Queries
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

    /// <summary>Simple tests for SortedSetSortField</summary>
    public class TestSortedSetSortField : LuceneTestCase
    {
        [Test]
        public void TestForward()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random, dir);
            Document doc = new Document();
            doc.Add(NewStringField("value", "baz", Field.Store.NO));
            doc.Add(NewStringField("id", "2", Field.Store.YES));
            writer.AddDocument(doc);
            doc = new Document();
            doc.Add(NewStringField("value", "foo", Field.Store.NO));
            doc.Add(NewStringField("value", "bar", Field.Store.NO));
            doc.Add(NewStringField("id", "1", Field.Store.YES));
            writer.AddDocument(doc);
            IndexReader ir = writer.GetReader();
            writer.Dispose();

            IndexSearcher searcher = NewSearcher(ir);
            Sort sort = new Sort(new SortedSetSortField("value", false));

            TopDocs td = searcher.Search(new MatchAllDocsQuery(), 10, sort);
            assertEquals(2, td.TotalHits);
            // 'bar' comes before 'baz'
            assertEquals("1", searcher.Doc(td.ScoreDocs[0].Doc).Get("id"));
            assertEquals("2", searcher.Doc(td.ScoreDocs[1].Doc).Get("id"));

            ir.Dispose();
            dir.Dispose();
        }

        [Test]
        public void TestReverse()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random, dir);
            Document doc = new Document();
            doc.Add(NewStringField("value", "foo", Field.Store.NO));
            doc.Add(NewStringField("value", "bar", Field.Store.NO));
            doc.Add(NewStringField("id", "1", Field.Store.YES));
            writer.AddDocument(doc);
            doc = new Document();
            doc.Add(NewStringField("value", "baz", Field.Store.NO));
            doc.Add(NewStringField("id", "2", Field.Store.YES));
            writer.AddDocument(doc);

            IndexReader ir = writer.GetReader();
            writer.Dispose();

            IndexSearcher searcher = NewSearcher(ir);
            Sort sort = new Sort(new SortedSetSortField("value", true));

            TopDocs td = searcher.Search(new MatchAllDocsQuery(), 10, sort);
            assertEquals(2, td.TotalHits);
            // 'bar' comes before 'baz'
            assertEquals("2", searcher.Doc(td.ScoreDocs[0].Doc).Get("id"));
            assertEquals("1", searcher.Doc(td.ScoreDocs[1].Doc).Get("id"));

            ir.Dispose();
            dir.Dispose();
        }

        [Test]
        public void TestMissingFirst()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random, dir);
            Document doc = new Document();
            doc.Add(NewStringField("value", "baz", Field.Store.NO));
            doc.Add(NewStringField("id", "2", Field.Store.YES));
            writer.AddDocument(doc);
            doc = new Document();
            doc.Add(NewStringField("value", "foo", Field.Store.NO));
            doc.Add(NewStringField("value", "bar", Field.Store.NO));
            doc.Add(NewStringField("id", "1", Field.Store.YES));
            writer.AddDocument(doc);
            doc = new Document();
            doc.Add(NewStringField("id", "3", Field.Store.YES));
            writer.AddDocument(doc);
            IndexReader ir = writer.GetReader();
            writer.Dispose();

            IndexSearcher searcher = NewSearcher(ir);
            SortField sortField = new SortedSetSortField("value", false);
            sortField.SetMissingValue(SortField.STRING_FIRST);
            Sort sort = new Sort(sortField);

            TopDocs td = searcher.Search(new MatchAllDocsQuery(), 10, sort);
            assertEquals(3, td.TotalHits);
            // 'bar' comes before 'baz'
            // null comes first
            assertEquals("3", searcher.Doc(td.ScoreDocs[0].Doc).Get("id"));
            assertEquals("1", searcher.Doc(td.ScoreDocs[1].Doc).Get("id"));
            assertEquals("2", searcher.Doc(td.ScoreDocs[2].Doc).Get("id"));

            ir.Dispose();
            dir.Dispose();
        }

        [Test]
        public void TestMissingLast()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random, dir);
            Document doc = new Document();
            doc.Add(NewStringField("value", "baz", Field.Store.NO));
            doc.Add(NewStringField("id", "2", Field.Store.YES));
            writer.AddDocument(doc);
            doc = new Document();
            doc.Add(NewStringField("value", "foo", Field.Store.NO));
            doc.Add(NewStringField("value", "bar", Field.Store.NO));
            doc.Add(NewStringField("id", "1", Field.Store.YES));
            writer.AddDocument(doc);
            doc = new Document();
            doc.Add(NewStringField("id", "3", Field.Store.YES));
            writer.AddDocument(doc);
            IndexReader ir = writer.GetReader();
            writer.Dispose();

            IndexSearcher searcher = NewSearcher(ir);
            SortField sortField = new SortedSetSortField("value", false);
            sortField.SetMissingValue(SortField.STRING_LAST);
            Sort sort = new Sort(sortField);

            TopDocs td = searcher.Search(new MatchAllDocsQuery(), 10, sort);
            assertEquals(3, td.TotalHits);
            // 'bar' comes before 'baz'
            assertEquals("1", searcher.Doc(td.ScoreDocs[0].Doc).Get("id"));
            assertEquals("2", searcher.Doc(td.ScoreDocs[1].Doc).Get("id"));
            // null comes last
            assertEquals("3", searcher.Doc(td.ScoreDocs[2].Doc).Get("id"));

            ir.Dispose();
            dir.Dispose();
        }

        [Test]
        public void TestSingleton()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random, dir);
            Document doc = new Document();
            doc.Add(NewStringField("value", "baz", Field.Store.NO));
            doc.Add(NewStringField("id", "2", Field.Store.YES));
            writer.AddDocument(doc);
            doc = new Document();
            doc.Add(NewStringField("value", "bar", Field.Store.NO));
            doc.Add(NewStringField("id", "1", Field.Store.YES));
            writer.AddDocument(doc);
            IndexReader ir = writer.GetReader();
            writer.Dispose();

            IndexSearcher searcher = NewSearcher(ir);
            Sort sort = new Sort(new SortedSetSortField("value", false));

            TopDocs td = searcher.Search(new MatchAllDocsQuery(), 10, sort);
            assertEquals(2, td.TotalHits);
            // 'bar' comes before 'baz'
            assertEquals("1", searcher.Doc(td.ScoreDocs[0].Doc).Get("id"));
            assertEquals("2", searcher.Doc(td.ScoreDocs[1].Doc).Get("id"));

            ir.Dispose();
            dir.Dispose();
        }

        [Test]
        public void TestEmptyIndex()
        {
            IndexSearcher empty = NewSearcher(new MultiReader());
            Query query = new TermQuery(new Term("contents", "foo"));

            Sort sort = new Sort();
            sort.SetSort(new SortedSetSortField("sortedset", false));
            TopDocs td = empty.Search(query, null, 10, sort, true, true);
            assertEquals(0, td.TotalHits);

            // for an empty index, any selector should work
            foreach (Selector v in Enum.GetValues(typeof(Selector)))
            {
                sort.SetSort(new SortedSetSortField("sortedset", false, v));
                td = empty.Search(query, null, 10, sort, true, true);
                assertEquals(0, td.TotalHits);
            }
        }

        [Test]
        public void TestEquals()
        {
            SortField sf = new SortedSetSortField("a", false);
            assertFalse(sf.equals(null));


            assertEquals(sf, sf);

            SortField sf2 = new SortedSetSortField("a", false);
            assertEquals(sf, sf2);
            assertEquals(sf.GetHashCode(), sf2.GetHashCode());


            assertFalse(sf.equals(new SortedSetSortField("a", true)));
            assertFalse(sf.equals(new SortedSetSortField("b", false)));
            assertFalse(sf.equals(new SortedSetSortField("a", false, Selector.MAX)));
            assertFalse(sf.equals("foo"));
        }
    }
}
