using Lucene.Net.Attributes;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Index.Extensions;
using Lucene.Net.Store;
using Lucene.Net.Util;
using NUnit.Framework;
using System.Collections.Generic;

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

    /// <summary>
    /// Tests that validate the code examples from the package.md documentation.
    /// </summary>
    [LuceneNetSpecific]
    public class GroupingSearchDocExampleTest : LuceneTestCase
    {
        /// <summary>
        /// Tests the "typical usage for the generic two-pass grouping search" example
        /// from the package.md documentation using <see cref="GroupingSearch.ByField(string)"/>.
        /// </summary>
        [Test]
        public void TestFieldGroupingSearchDocExample()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter w = new RandomIndexWriter(
                Random,
                dir,
                NewIndexWriterConfig(TEST_VERSION_CURRENT,
                    new Analysis.MockAnalyzer(Random)).SetMergePolicy(NewLogMergePolicy()));

            // Index some documents with an "author" group field and "content" text field
            Document doc = new Document();
            doc.Add(new SortedDocValuesField("author", new BytesRef("author1")));
            doc.Add(new TextField("author", "author1", Field.Store.YES));
            doc.Add(new TextField("content", "random text", Field.Store.YES));
            w.AddDocument(doc);

            doc = new Document();
            doc.Add(new SortedDocValuesField("author", new BytesRef("author1")));
            doc.Add(new TextField("author", "author1", Field.Store.YES));
            doc.Add(new TextField("content", "more random text", Field.Store.YES));
            w.AddDocument(doc);

            doc = new Document();
            doc.Add(new SortedDocValuesField("author", new BytesRef("author2")));
            doc.Add(new TextField("author", "author2", Field.Store.YES));
            doc.Add(new TextField("content", "random content", Field.Store.YES));
            w.AddDocument(doc);

            IndexSearcher indexSearcher = NewSearcher(w.GetReader());
            w.Dispose();

            // --- Code from package.md documentation ---
            Sort groupSort = Sort.RELEVANCE;
            bool fillFields = true;
            bool useCache = true;
            bool requiredTotalGroupCount = true;
            string searchTerm = "random";
            int groupOffset = 0;
            int groupLimit = 10;

            FieldGroupingSearch groupingSearch = GroupingSearch.ByField("author");
            groupingSearch.SetGroupSort(groupSort);
            groupingSearch.SetFillSortFields(fillFields);

            if (useCache)
            {
                // Sets cache in MB
                groupingSearch.SetCachingInMB(maxCacheRAMMB: 4.0, cacheScores: true);
            }

            if (requiredTotalGroupCount)
            {
                groupingSearch.SetAllGroups(true);
            }

            TermQuery query = new TermQuery(new Index.Term("content", searchTerm));
            TopGroups<BytesRef> result = groupingSearch.Search(indexSearcher, query, groupOffset, groupLimit);

            // Verify results
            Assert.IsNotNull(result);
            assertEquals(3, result.TotalHitCount);
            assertEquals(2, result.Groups.Length); // 2 groups: author1 and author2

            if (requiredTotalGroupCount)
            {
                int? totalGroupCount = result.TotalGroupCount;
                Assert.IsNotNull(totalGroupCount);
                assertEquals(2, totalGroupCount.Value);
            }

            indexSearcher.IndexReader.Dispose();
            dir.Dispose();
        }

        /// <summary>
        /// Tests the "GroupingSearch convenience utility" example for doc block grouping
        /// from the package.md documentation using <see cref="GroupingSearch.ByDocBlock{TGroupValue}(Filter)"/>.
        /// </summary>
        [Test]
        public void TestDocBlockGroupingSearchDocExample()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter w = new RandomIndexWriter(
                Random,
                dir,
                NewIndexWriterConfig(TEST_VERSION_CURRENT,
                    new Analysis.MockAnalyzer(Random)).SetMergePolicy(NewLogMergePolicy()));

            // --- Indexing code from package.md documentation ---
            // Group 1: two documents by author1
            List<Document> oneGroup = new List<Document>();
            Document doc = new Document();
            doc.Add(new TextField("content", "random text", Field.Store.YES));
            oneGroup.Add(doc);

            doc = new Document();
            doc.Add(new TextField("content", "more random text", Field.Store.YES));
            oneGroup.Add(doc);

            Field groupEndField = new StringField("groupEnd", "x", Field.Store.NO);
            oneGroup[oneGroup.Count - 1].Add(groupEndField);
            w.AddDocuments(oneGroup);

            // Group 2: one document by author2
            oneGroup = new List<Document>();
            doc = new Document();
            doc.Add(new TextField("content", "random content", Field.Store.YES));
            oneGroup.Add(doc);

            groupEndField = new StringField("groupEnd", "x", Field.Store.NO);
            oneGroup[oneGroup.Count - 1].Add(groupEndField);
            w.AddDocuments(oneGroup);

            IndexSearcher indexSearcher = NewSearcher(w.GetReader());
            w.Dispose();

            // --- Search code from package.md documentation ---
            Sort groupSort = Sort.RELEVANCE;
            bool needsScores = true;
            string searchTerm = "random";
            int groupOffset = 0;
            int groupLimit = 10;

            // Set this once in your app & save away for reusing across all queries:
            Filter groupEndDocs = new CachingWrapperFilter(new QueryWrapperFilter(new TermQuery(new Index.Term("groupEnd", "x"))));

            // Per search:
            DocBlockGroupingSearch<object> groupingSearch = GroupingSearch.ByDocBlock<object>(groupEndDocs);
            groupingSearch.SetGroupSort(groupSort);
            groupingSearch.SetIncludeScores(needsScores);
            TermQuery query = new TermQuery(new Index.Term("content", searchTerm));
            TopGroups<object> groupsResult = groupingSearch.Search(indexSearcher, query, groupOffset, groupLimit);

            // Verify results
            Assert.IsNotNull(groupsResult);
            assertEquals(3, groupsResult.TotalHitCount);
            assertEquals(2, groupsResult.Groups.Length); // 2 doc block groups

            // Note that the groupValue of each GroupDocs will be null
            foreach (var group in groupsResult.Groups)
            {
                assertNull(group.GroupValue);
            }

            indexSearcher.IndexReader.Dispose();
            dir.Dispose();
        }
    }
}
