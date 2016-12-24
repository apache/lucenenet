using System;
using System.Collections.Generic;
using System.Text;
using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Join;
using Lucene.Net.Randomized.Generators;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Support;
using Lucene.Net.Util;
using NUnit.Framework;

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

    public class TestBlockJoinValidation : LuceneTestCase
    {

        public const int AMOUNT_OF_SEGMENTS = 5;
        public const int AMOUNT_OF_PARENT_DOCS = 10;
        public const int AMOUNT_OF_CHILD_DOCS = 5;
        public static readonly int AMOUNT_OF_DOCS_IN_SEGMENT = AMOUNT_OF_PARENT_DOCS + AMOUNT_OF_PARENT_DOCS * AMOUNT_OF_CHILD_DOCS;

        private Directory Directory;
        private IndexReader IndexReader;
        private IndexSearcher IndexSearcher;
        private Filter ParentsFilter;
        
        [SetUp]
        public override void SetUp()
        {
            Directory = NewDirectory();
            IndexWriterConfig config = new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random()));
            IndexWriter indexWriter = new IndexWriter(Directory, config);
            for (int i = 0; i < AMOUNT_OF_SEGMENTS; i++)
            {
                IList<Document> segmentDocs = CreateDocsForSegment(i);
                indexWriter.AddDocuments(segmentDocs);
                indexWriter.Commit();
            }
            IndexReader = DirectoryReader.Open(indexWriter, Random().NextBoolean());
            indexWriter.Dispose();
            IndexSearcher = new IndexSearcher(IndexReader);
            ParentsFilter = new FixedBitSetCachingWrapperFilter(new QueryWrapperFilter(new WildcardQuery(new Term("parent", "*"))));
        }

        [TearDown]
        public override void TearDown()
        {
            IndexReader.Dispose();
            Directory.Dispose();
        }

        [Test]
        public void TestNextDocValidationForToParentBjq()
        {
            Query parentQueryWithRandomChild = CreateChildrenQueryWithOneParent(GetRandomChildNumber(0));
            var blockJoinQuery = new ToParentBlockJoinQuery(parentQueryWithRandomChild, ParentsFilter, ScoreMode.None);

            var ex = Throws<InvalidOperationException>(() => IndexSearcher.Search(blockJoinQuery, 1));
            StringAssert.Contains("child query must only match non-parent docs", ex.Message);

        }

        [Test]
        public void TestAdvanceValidationForToParentBjq()
        {
            int randomChildNumber = GetRandomChildNumber(0);
            // we need to make advance method meet wrong document, so random child number
            // in BJQ must be greater than child number in Boolean clause
            int nextRandomChildNumber = GetRandomChildNumber(randomChildNumber);
            Query parentQueryWithRandomChild = CreateChildrenQueryWithOneParent(nextRandomChildNumber);
            ToParentBlockJoinQuery blockJoinQuery = new ToParentBlockJoinQuery(parentQueryWithRandomChild, ParentsFilter, ScoreMode.None);
            // advance() method is used by ConjunctionScorer, so we need to create Boolean conjunction query
            BooleanQuery conjunctionQuery = new BooleanQuery();
            WildcardQuery childQuery = new WildcardQuery(new Term("child", CreateFieldValue(randomChildNumber)));
            conjunctionQuery.Add(new BooleanClause(childQuery, Occur.MUST));
            conjunctionQuery.Add(new BooleanClause(blockJoinQuery, Occur.MUST));

            var ex = Throws<InvalidOperationException>(() => IndexSearcher.Search(conjunctionQuery, 1));
            StringAssert.Contains("child query must only match non-parent docs", ex.Message);
        }

        [Test]
        public void TestNextDocValidationForToChildBjq()
        {
            Query parentQueryWithRandomChild = CreateParentsQueryWithOneChild(GetRandomChildNumber(0));
            var blockJoinQuery = new ToChildBlockJoinQuery(parentQueryWithRandomChild, ParentsFilter, false);

            var ex = Throws<InvalidOperationException>(() => IndexSearcher.Search(blockJoinQuery, 1));
            StringAssert.Contains(ToChildBlockJoinQuery.InvalidQueryMessage, ex.Message);
        }
        
        [Test]
        public void TestAdvanceValidationForToChildBjq()
        {
            int randomChildNumber = GetRandomChildNumber(0);
            // we need to make advance method meet wrong document, so random child number
            // in BJQ must be greater than child number in Boolean clause
            int nextRandomChildNumber = GetRandomChildNumber(randomChildNumber);
            Query parentQueryWithRandomChild = CreateParentsQueryWithOneChild(nextRandomChildNumber);
            var blockJoinQuery = new ToChildBlockJoinQuery(parentQueryWithRandomChild, ParentsFilter, false);
            // advance() method is used by ConjunctionScorer, so we need to create Boolean conjunction query
            var conjunctionQuery = new BooleanQuery();
            var childQuery = new WildcardQuery(new Term("child", CreateFieldValue(randomChildNumber)));
            conjunctionQuery.Add(new BooleanClause(childQuery, Occur.MUST));
            conjunctionQuery.Add(new BooleanClause(blockJoinQuery, Occur.MUST));
            
            var ex = Throws<InvalidOperationException>(() => IndexSearcher.Search(conjunctionQuery, 1));
            StringAssert.Contains(ToChildBlockJoinQuery.InvalidQueryMessage, ex.Message);
        }

        private IList<Document> CreateDocsForSegment(int segmentNumber)
        {
            IList<IList<Document>> blocks = new List<IList<Document>>(AMOUNT_OF_PARENT_DOCS);
            for (int i = 0; i < AMOUNT_OF_PARENT_DOCS; i++)
            {
                blocks.Add(CreateParentDocWithChildren(segmentNumber, i));
            }
            IList<Document> result = new List<Document>(AMOUNT_OF_DOCS_IN_SEGMENT);
            foreach (IList<Document> block in blocks)
            {
                result.AddRange(block);
            }
            return result;
        }

        private IList<Document> CreateParentDocWithChildren(int segmentNumber, int parentNumber)
        {
            IList<Document> result = new List<Document>(AMOUNT_OF_CHILD_DOCS + 1);
            for (int i = 0; i < AMOUNT_OF_CHILD_DOCS; i++)
            {
                result.Add(CreateChildDoc(segmentNumber, parentNumber, i));
            }
            result.Add(CreateParentDoc(segmentNumber, parentNumber));
            return result;
        }

        private Document CreateParentDoc(int segmentNumber, int parentNumber)
        {
            Document result = new Document();
            result.Add(NewStringField("id", CreateFieldValue(segmentNumber * AMOUNT_OF_PARENT_DOCS + parentNumber), Field.Store.YES));
            result.Add(NewStringField("parent", CreateFieldValue(parentNumber), Field.Store.NO));
            return result;
        }

        private Document CreateChildDoc(int segmentNumber, int parentNumber, int childNumber)
        {
            Document result = new Document();
            result.Add(NewStringField("id", CreateFieldValue(segmentNumber * AMOUNT_OF_PARENT_DOCS + parentNumber, childNumber), Field.Store.YES));
            result.Add(NewStringField("child", CreateFieldValue(childNumber), Field.Store.NO));
            return result;
        }

        private static string CreateFieldValue(params int[] documentNumbers)
        {
            StringBuilder stringBuilder = new StringBuilder();
            foreach (int documentNumber in documentNumbers)
            {
                if (stringBuilder.Length > 0)
                {
                    stringBuilder.Append("_");
                }
                stringBuilder.Append(documentNumber);
            }
            return stringBuilder.ToString();
        }

        private static Query CreateChildrenQueryWithOneParent(int childNumber)
        {
            TermQuery childQuery = new TermQuery(new Term("child", CreateFieldValue(childNumber)));
            Query randomParentQuery = new TermQuery(new Term("id", CreateFieldValue(RandomParentId)));
            BooleanQuery childrenQueryWithRandomParent = new BooleanQuery();
            childrenQueryWithRandomParent.Add(new BooleanClause(childQuery, Occur.SHOULD));
            childrenQueryWithRandomParent.Add(new BooleanClause(randomParentQuery, Occur.SHOULD));
            return childrenQueryWithRandomParent;
        }

        private static Query CreateParentsQueryWithOneChild(int randomChildNumber)
        {
            BooleanQuery childQueryWithRandomParent = new BooleanQuery();
            Query parentsQuery = new TermQuery(new Term("parent", CreateFieldValue(RandomParentNumber)));
            childQueryWithRandomParent.Add(new BooleanClause(parentsQuery, Occur.SHOULD));
            childQueryWithRandomParent.Add(new BooleanClause(RandomChildQuery(randomChildNumber), Occur.SHOULD));
            return childQueryWithRandomParent;
        }

        private static int RandomParentId
        {
            get { return Random().Next(AMOUNT_OF_PARENT_DOCS*AMOUNT_OF_SEGMENTS); }
        }

        private static int RandomParentNumber
        {
            get { return Random().Next(AMOUNT_OF_PARENT_DOCS); }
        }

        private static Query RandomChildQuery(int randomChildNumber)
        {
            return new TermQuery(new Term("id", CreateFieldValue(RandomParentId, randomChildNumber)));
        }

        private static int GetRandomChildNumber(int notLessThan)
        {
            return notLessThan + Random().Next(AMOUNT_OF_CHILD_DOCS - notLessThan);
        }
    }
}