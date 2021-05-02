// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Index.Extensions;
using Lucene.Net.Join;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using NUnit.Framework;
using RandomizedTesting.Generators;
using System;
using System.Collections.Generic;

namespace Lucene.Net.Tests.Join
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

    [Obsolete("Production tests are in Lucene.Net.Search.Join. This class will be removed in 4.8.0 release candidate."), System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public class TestBlockJoinSorting : LuceneTestCase
    {
        [Test]
        public void TestNestedSorting()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter w = new RandomIndexWriter(Random, dir, NewIndexWriterConfig(TEST_VERSION_CURRENT,
                new MockAnalyzer(Random)).SetMergePolicy(NoMergePolicy.COMPOUND_FILES));

            IList<Document> docs = new List<Document>();
            Document document = new Document();
            document.Add(new StringField("field2", "a", Field.Store.NO));
            document.Add(new StringField("filter_1", "T", Field.Store.NO));
            docs.Add(document);
            document = new Document();
            document.Add(new StringField("field2", "b", Field.Store.NO));
            document.Add(new StringField("filter_1", "T", Field.Store.NO));
            docs.Add(document);
            document = new Document();
            document.Add(new StringField("field2", "c", Field.Store.NO));
            document.Add(new StringField("filter_1", "T", Field.Store.NO));
            docs.Add(document);
            document = new Document();
            document.Add(new StringField("__type", "parent", Field.Store.NO));
            document.Add(new StringField("field1", "a", Field.Store.NO));
            docs.Add(document);
            w.AddDocuments(docs);
            w.Commit();

            docs.Clear();
            document = new Document();
            document.Add(new StringField("field2", "c", Field.Store.NO));
            document.Add(new StringField("filter_1", "T", Field.Store.NO));
            docs.Add(document);
            document = new Document();
            document.Add(new StringField("field2", "d", Field.Store.NO));
            document.Add(new StringField("filter_1", "T", Field.Store.NO));
            docs.Add(document);
            document = new Document();
            document.Add(new StringField("field2", "e", Field.Store.NO));
            document.Add(new StringField("filter_1", "T", Field.Store.NO));
            docs.Add(document);
            document = new Document();
            document.Add(new StringField("__type", "parent", Field.Store.NO));
            document.Add(new StringField("field1", "b", Field.Store.NO));
            docs.Add(document);
            w.AddDocuments(docs);

            docs.Clear();
            document = new Document();
            document.Add(new StringField("field2", "e", Field.Store.NO));
            document.Add(new StringField("filter_1", "T", Field.Store.NO));
            docs.Add(document);
            document = new Document();
            document.Add(new StringField("field2", "f", Field.Store.NO));
            document.Add(new StringField("filter_1", "T", Field.Store.NO));
            docs.Add(document);
            document = new Document();
            document.Add(new StringField("field2", "g", Field.Store.NO));
            document.Add(new StringField("filter_1", "T", Field.Store.NO));
            docs.Add(document);
            document = new Document();
            document.Add(new StringField("__type", "parent", Field.Store.NO));
            document.Add(new StringField("field1", "c", Field.Store.NO));
            docs.Add(document);
            w.AddDocuments(docs);

            docs.Clear();
            document = new Document();
            document.Add(new StringField("field2", "g", Field.Store.NO));
            document.Add(new StringField("filter_1", "T", Field.Store.NO));
            docs.Add(document);
            document = new Document();
            document.Add(new StringField("field2", "h", Field.Store.NO));
            document.Add(new StringField("filter_1", "F", Field.Store.NO));
            docs.Add(document);
            document = new Document();
            document.Add(new StringField("field2", "i", Field.Store.NO));
            document.Add(new StringField("filter_1", "F", Field.Store.NO));
            docs.Add(document);
            document = new Document();
            document.Add(new StringField("__type", "parent", Field.Store.NO));
            document.Add(new StringField("field1", "d", Field.Store.NO));
            docs.Add(document);
            w.AddDocuments(docs);
            w.Commit();

            docs.Clear();
            document = new Document();
            document.Add(new StringField("field2", "i", Field.Store.NO));
            document.Add(new StringField("filter_1", "F", Field.Store.NO));
            docs.Add(document);
            document = new Document();
            document.Add(new StringField("field2", "j", Field.Store.NO));
            document.Add(new StringField("filter_1", "F", Field.Store.NO));
            docs.Add(document);
            document = new Document();
            document.Add(new StringField("field2", "k", Field.Store.NO));
            document.Add(new StringField("filter_1", "F", Field.Store.NO));
            docs.Add(document);
            document = new Document();
            document.Add(new StringField("__type", "parent", Field.Store.NO));
            document.Add(new StringField("field1", "f", Field.Store.NO));
            docs.Add(document);
            w.AddDocuments(docs);

            docs.Clear();
            document = new Document();
            document.Add(new StringField("field2", "k", Field.Store.NO));
            document.Add(new StringField("filter_1", "T", Field.Store.NO));
            docs.Add(document);
            document = new Document();
            document.Add(new StringField("field2", "l", Field.Store.NO));
            document.Add(new StringField("filter_1", "T", Field.Store.NO));
            docs.Add(document);
            document = new Document();
            document.Add(new StringField("field2", "m", Field.Store.NO));
            document.Add(new StringField("filter_1", "T", Field.Store.NO));
            docs.Add(document);
            document = new Document();
            document.Add(new StringField("__type", "parent", Field.Store.NO));
            document.Add(new StringField("field1", "g", Field.Store.NO));
            docs.Add(document);
            w.AddDocuments(docs);

            // This doc will not be included, because it doesn't have nested docs
            document = new Document();
            document.Add(new StringField("__type", "parent", Field.Store.NO));
            document.Add(new StringField("field1", "h", Field.Store.NO));
            w.AddDocument(document);

            docs.Clear();
            document = new Document();
            document.Add(new StringField("field2", "m", Field.Store.NO));
            document.Add(new StringField("filter_1", "T", Field.Store.NO));
            docs.Add(document);
            document = new Document();
            document.Add(new StringField("field2", "n", Field.Store.NO));
            document.Add(new StringField("filter_1", "F", Field.Store.NO));
            docs.Add(document);
            document = new Document();
            document.Add(new StringField("field2", "o", Field.Store.NO));
            document.Add(new StringField("filter_1", "F", Field.Store.NO));
            docs.Add(document);
            document = new Document();
            document.Add(new StringField("__type", "parent", Field.Store.NO));
            document.Add(new StringField("field1", "i", Field.Store.NO));
            docs.Add(document);
            w.AddDocuments(docs);
            w.Commit();

            // Some garbage docs, just to check if the NestedFieldComparer can deal with this.
            document = new Document();
            document.Add(new StringField("fieldXXX", "x", Field.Store.NO));
            w.AddDocument(document);
            document = new Document();
            document.Add(new StringField("fieldXXX", "x", Field.Store.NO));
            w.AddDocument(document);
            document = new Document();
            document.Add(new StringField("fieldXXX", "x", Field.Store.NO));
            w.AddDocument(document);

            IndexSearcher searcher = new IndexSearcher(DirectoryReader.Open(w.IndexWriter, false));
            w.Dispose();
            Filter parentFilter = new QueryWrapperFilter(new TermQuery(new Term("__type", "parent")));
            Filter childFilter = new QueryWrapperFilter(new PrefixQuery(new Term("field2")));
            ToParentBlockJoinQuery query = new ToParentBlockJoinQuery(new FilteredQuery(new MatchAllDocsQuery(), childFilter), new FixedBitSetCachingWrapperFilter(parentFilter), ScoreMode.None);

            // Sort by field ascending, order first
            ToParentBlockJoinSortField sortField = new ToParentBlockJoinSortField("field2", SortFieldType.STRING, false, Wrap(parentFilter), Wrap(childFilter));
            Sort sort = new Sort(sortField);
            TopFieldDocs topDocs = searcher.Search(query, 5, sort);
            assertEquals(7, topDocs.TotalHits);
            assertEquals(5, topDocs.ScoreDocs.Length);
            assertEquals(3, topDocs.ScoreDocs[0].Doc);
            assertEquals("a", ((BytesRef)((FieldDoc)topDocs.ScoreDocs[0]).Fields[0]).Utf8ToString());
            assertEquals(7, topDocs.ScoreDocs[1].Doc);
            assertEquals("c", ((BytesRef)((FieldDoc)topDocs.ScoreDocs[1]).Fields[0]).Utf8ToString());
            assertEquals(11, topDocs.ScoreDocs[2].Doc);
            assertEquals("e", ((BytesRef)((FieldDoc)topDocs.ScoreDocs[2]).Fields[0]).Utf8ToString());
            assertEquals(15, topDocs.ScoreDocs[3].Doc);
            assertEquals("g", ((BytesRef)((FieldDoc)topDocs.ScoreDocs[3]).Fields[0]).Utf8ToString());
            assertEquals(19, topDocs.ScoreDocs[4].Doc);
            assertEquals("i", ((BytesRef)((FieldDoc)topDocs.ScoreDocs[4]).Fields[0]).Utf8ToString());

            // Sort by field ascending, order last
            sortField = new ToParentBlockJoinSortField("field2", SortFieldType.STRING, false, true, Wrap(parentFilter), Wrap(childFilter));
            sort = new Sort(sortField);
            topDocs = searcher.Search(query, 5, sort);
            assertEquals(7, topDocs.TotalHits);
            assertEquals(5, topDocs.ScoreDocs.Length);
            assertEquals(3, topDocs.ScoreDocs[0].Doc);
            assertEquals("c", ((BytesRef)((FieldDoc)topDocs.ScoreDocs[0]).Fields[0]).Utf8ToString());
            assertEquals(7, topDocs.ScoreDocs[1].Doc);
            assertEquals("e", ((BytesRef)((FieldDoc)topDocs.ScoreDocs[1]).Fields[0]).Utf8ToString());
            assertEquals(11, topDocs.ScoreDocs[2].Doc);
            assertEquals("g", ((BytesRef)((FieldDoc)topDocs.ScoreDocs[2]).Fields[0]).Utf8ToString());
            assertEquals(15, topDocs.ScoreDocs[3].Doc);
            assertEquals("i", ((BytesRef)((FieldDoc)topDocs.ScoreDocs[3]).Fields[0]).Utf8ToString());
            assertEquals(19, topDocs.ScoreDocs[4].Doc);
            assertEquals("k", ((BytesRef)((FieldDoc)topDocs.ScoreDocs[4]).Fields[0]).Utf8ToString());

            // Sort by field descending, order last
            sortField = new ToParentBlockJoinSortField("field2", SortFieldType.STRING, true, Wrap(parentFilter), Wrap(childFilter));
            sort = new Sort(sortField);
            topDocs = searcher.Search(query, 5, sort);
            assertEquals(topDocs.TotalHits, 7);
            assertEquals(5, topDocs.ScoreDocs.Length);
            assertEquals(28, topDocs.ScoreDocs[0].Doc);
            assertEquals("o", ((BytesRef)((FieldDoc)topDocs.ScoreDocs[0]).Fields[0]).Utf8ToString());
            assertEquals(23, topDocs.ScoreDocs[1].Doc);
            assertEquals("m", ((BytesRef)((FieldDoc)topDocs.ScoreDocs[1]).Fields[0]).Utf8ToString());
            assertEquals(19, topDocs.ScoreDocs[2].Doc);
            assertEquals("k", ((BytesRef)((FieldDoc)topDocs.ScoreDocs[2]).Fields[0]).Utf8ToString());
            assertEquals(15, topDocs.ScoreDocs[3].Doc);
            assertEquals("i", ((BytesRef)((FieldDoc)topDocs.ScoreDocs[3]).Fields[0]).Utf8ToString());
            assertEquals(11, topDocs.ScoreDocs[4].Doc);
            assertEquals("g", ((BytesRef)((FieldDoc)topDocs.ScoreDocs[4]).Fields[0]).Utf8ToString());

            // Sort by field descending, order last, sort filter (filter_1:T)
            childFilter = new QueryWrapperFilter(new TermQuery((new Term("filter_1", "T"))));
            query = new ToParentBlockJoinQuery(
                new FilteredQuery(new MatchAllDocsQuery(), childFilter),
                new FixedBitSetCachingWrapperFilter(parentFilter),
                ScoreMode.None);
            sortField = new ToParentBlockJoinSortField("field2", SortFieldType.STRING, true, Wrap(parentFilter), Wrap(childFilter));
            sort = new Sort(sortField);
            topDocs = searcher.Search(query, 5, sort);
            assertEquals(6, topDocs.TotalHits);
            assertEquals(5, topDocs.ScoreDocs.Length);
            assertEquals(23, topDocs.ScoreDocs[0].Doc);
            assertEquals("m", ((BytesRef)((FieldDoc)topDocs.ScoreDocs[0]).Fields[0]).Utf8ToString());
            assertEquals(28, topDocs.ScoreDocs[1].Doc);
            assertEquals("m", ((BytesRef)((FieldDoc)topDocs.ScoreDocs[1]).Fields[0]).Utf8ToString());
            assertEquals(11, topDocs.ScoreDocs[2].Doc);
            assertEquals("g", ((BytesRef)((FieldDoc)topDocs.ScoreDocs[2]).Fields[0]).Utf8ToString());
            assertEquals(15, topDocs.ScoreDocs[3].Doc);
            assertEquals("g", ((BytesRef)((FieldDoc)topDocs.ScoreDocs[3]).Fields[0]).Utf8ToString());
            assertEquals(7, topDocs.ScoreDocs[4].Doc);
            assertEquals("e", ((BytesRef)((FieldDoc)topDocs.ScoreDocs[4]).Fields[0]).Utf8ToString());

            searcher.IndexReader.Dispose();
            dir.Dispose();
        }

        private Filter Wrap(Filter filter)
        {
            return Random.NextBoolean() ? new FixedBitSetCachingWrapperFilter(filter) : filter;
        }
    }
}