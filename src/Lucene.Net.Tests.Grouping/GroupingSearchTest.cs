using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Index.Extensions;
using Lucene.Net.Queries.Function;
using Lucene.Net.Queries.Function.ValueSources;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Lucene.Net.Util.Mutable;
using NUnit.Framework;
using System;
using System.Collections;
using JCG = J2N.Collections.Generic;

namespace Lucene.Net.Search.Grouping
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

    public class GroupingSearchTest : LuceneTestCase
    {
        // Tests some very basic usages...
        [Test]
        public virtual void TestBasic()
        {

            string groupField = "author";

            FieldType customType = new FieldType();
            customType.IsStored = (true);

            Directory dir = NewDirectory();
            RandomIndexWriter w = new RandomIndexWriter(
                Random,
                dir,
                NewIndexWriterConfig(TEST_VERSION_CURRENT,
                    new MockAnalyzer(Random)).SetMergePolicy(NewLogMergePolicy()));
            bool canUseIDV = !"Lucene3x".Equals(w.IndexWriter.Config.Codec.Name, StringComparison.Ordinal);
            JCG.List<Document> documents = new JCG.List<Document>();
            // 0
            Document doc = new Document();
            AddGroupField(doc, groupField, "author1", canUseIDV);
            doc.Add(new TextField("content", "random text", Field.Store.YES));
            doc.Add(new Field("id", "1", customType));
            documents.Add(doc);

            // 1
            doc = new Document();
            AddGroupField(doc, groupField, "author1", canUseIDV);
            doc.Add(new TextField("content", "some more random text", Field.Store.YES));
            doc.Add(new Field("id", "2", customType));
            documents.Add(doc);

            // 2
            doc = new Document();
            AddGroupField(doc, groupField, "author1", canUseIDV);
            doc.Add(new TextField("content", "some more random textual data", Field.Store.YES));
            doc.Add(new Field("id", "3", customType));
            doc.Add(new StringField("groupend", "x", Field.Store.NO));
            documents.Add(doc);
            w.AddDocuments(documents);
            documents.Clear();

            // 3
            doc = new Document();
            AddGroupField(doc, groupField, "author2", canUseIDV);
            doc.Add(new TextField("content", "some random text", Field.Store.YES));
            doc.Add(new Field("id", "4", customType));
            doc.Add(new StringField("groupend", "x", Field.Store.NO));
            w.AddDocument(doc);

            // 4
            doc = new Document();
            AddGroupField(doc, groupField, "author3", canUseIDV);
            doc.Add(new TextField("content", "some more random text", Field.Store.YES));
            doc.Add(new Field("id", "5", customType));
            documents.Add(doc);

            // 5
            doc = new Document();
            AddGroupField(doc, groupField, "author3", canUseIDV);
            doc.Add(new TextField("content", "random", Field.Store.YES));
            doc.Add(new Field("id", "6", customType));
            doc.Add(new StringField("groupend", "x", Field.Store.NO));
            documents.Add(doc);
            w.AddDocuments(documents);
            documents.Clear();

            // 6 -- no author field
            doc = new Document();
            doc.Add(new TextField("content", "random word stuck in alot of other text", Field.Store.YES));
            doc.Add(new Field("id", "6", customType));
            doc.Add(new StringField("groupend", "x", Field.Store.NO));

            w.AddDocument(doc);

            IndexSearcher indexSearcher = NewSearcher(w.GetReader());
            w.Dispose();

            Sort groupSort = Sort.RELEVANCE;
            GroupingSearch groupingSearch = CreateRandomGroupingSearch(groupField, groupSort, 5, canUseIDV);

            ITopGroups<object> groups = groupingSearch.Search(indexSearcher, (Filter)null, new TermQuery(new Index.Term("content", "random")), 0, 10);

            assertEquals(7, groups.TotalHitCount);
            assertEquals(7, groups.TotalGroupedHitCount);
            assertEquals(4, groups.Groups.Length);

            // relevance order: 5, 0, 3, 4, 1, 2, 6

            // the later a document is added the higher this docId
            // value
            IGroupDocs<object> group = groups.Groups[0];
            CompareGroupValue("author3", group);
            assertEquals(2, group.ScoreDocs.Length);
            assertEquals(5, group.ScoreDocs[0].Doc);
            assertEquals(4, group.ScoreDocs[1].Doc);
            assertTrue(group.ScoreDocs[0].Score > group.ScoreDocs[1].Score);

            group = groups.Groups[1];
            CompareGroupValue("author1", group);
            assertEquals(3, group.ScoreDocs.Length);
            assertEquals(0, group.ScoreDocs[0].Doc);
            assertEquals(1, group.ScoreDocs[1].Doc);
            assertEquals(2, group.ScoreDocs[2].Doc);
            assertTrue(group.ScoreDocs[0].Score > group.ScoreDocs[1].Score);
            assertTrue(group.ScoreDocs[1].Score > group.ScoreDocs[2].Score);

            group = groups.Groups[2];
            CompareGroupValue("author2", group);
            assertEquals(1, group.ScoreDocs.Length);
            assertEquals(3, group.ScoreDocs[0].Doc);

            group = groups.Groups[3];
            CompareGroupValue(null, group);
            assertEquals(1, group.ScoreDocs.Length);
            assertEquals(6, group.ScoreDocs[0].Doc);

            Filter lastDocInBlock = new CachingWrapperFilter(new QueryWrapperFilter(new TermQuery(new Index.Term("groupend", "x"))));
            groupingSearch = new GroupingSearch(lastDocInBlock);
            groups = groupingSearch.Search(indexSearcher, null, new TermQuery(new Index.Term("content", "random")), 0, 10);

            assertEquals(7, groups.TotalHitCount);
            assertEquals(7, groups.TotalGroupedHitCount);
            assertEquals(4, groups.TotalGroupCount.GetValueOrDefault());
            assertEquals(4, groups.Groups.Length);

            indexSearcher.IndexReader.Dispose();
            dir.Dispose();
        }

        private void AddGroupField(Document doc, string groupField, string value, bool canUseIDV)
        {
            doc.Add(new TextField(groupField, value, Field.Store.YES));
            if (canUseIDV)
            {
                doc.Add(new SortedDocValuesField(groupField, new BytesRef(value)));
            }
        }

        private void CompareGroupValue(string expected, IGroupDocs<object> group)
        {
            if (expected is null)
            {
                if (group.GroupValue is null)
                {
                    return;
                }
                else if (group.GroupValue.GetType().IsAssignableFrom(typeof(MutableValueStr)))
                {
                    return;
                }
                else if (((BytesRef)group.GroupValue).Length == 0)
                {
                    return;
                }
                fail();
            }

            if (group.GroupValue.GetType().IsAssignableFrom(typeof(BytesRef)))
            {
                assertEquals(new BytesRef(expected), group.GroupValue);
            }
            else if (group.GroupValue.GetType().IsAssignableFrom(typeof(MutableValueStr)))
            {
                MutableValueStr v = new MutableValueStr();
                v.Value = new BytesRef(expected);
                assertEquals(v, group.GroupValue);
            }
            else
            {
                fail();
            }
        }

        private GroupingSearch CreateRandomGroupingSearch(string groupField, Sort groupSort, int docsInGroup, bool canUseIDV)
        {
            GroupingSearch groupingSearch;
            if (Random.nextBoolean())
            {
                ValueSource vs = new BytesRefFieldSource(groupField);
                groupingSearch = new GroupingSearch(vs, new Hashtable());
            }
            else
            {
                groupingSearch = new GroupingSearch(groupField);
            }

            groupingSearch.SetGroupSort(groupSort);
            groupingSearch.SetGroupDocsLimit(docsInGroup);

            if (Random.nextBoolean())
            {
                groupingSearch.SetCachingInMB(4.0, true);
            }

            return groupingSearch;
        }

        [Test]
        public virtual void TestSetAllGroups()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter w = new RandomIndexWriter(
                Random,
                dir,
                NewIndexWriterConfig(TEST_VERSION_CURRENT,
                    new MockAnalyzer(Random)).SetMergePolicy(NewLogMergePolicy()));
            Document doc = new Document();
            doc.Add(NewField("group", "foo", StringField.TYPE_NOT_STORED));
            w.AddDocument(doc);

            IndexSearcher indexSearcher = NewSearcher(w.GetReader());
            w.Dispose();

            GroupingSearch gs = new GroupingSearch("group");
            gs.SetAllGroups(true);
            ITopGroups<object> groups = gs.Search(indexSearcher, null, new TermQuery(new Index.Term("group", "foo")), 0, 10);
            assertEquals(1, groups.TotalHitCount);
            //assertEquals(1, groups.totalGroupCount.intValue());
            assertEquals(1, groups.TotalGroupedHitCount);
            assertEquals(1, gs.GetAllMatchingGroups().Count);
            indexSearcher.IndexReader.Dispose();
            dir.Dispose();
        }
    }
}
