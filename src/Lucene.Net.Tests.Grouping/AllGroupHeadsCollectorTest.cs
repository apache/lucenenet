using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Queries.Function;
using Lucene.Net.Queries.Function.ValueSources;
using Lucene.Net.Search.Grouping.Function;
using Lucene.Net.Search.Grouping.Terms;
using Lucene.Net.Store;
using Lucene.Net.Support;
using Lucene.Net.Util;
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

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

    public class AllGroupHeadsCollectorTest : LuceneTestCase
    {
        private static readonly FieldInfo.DocValuesType_e[] vts = new FieldInfo.DocValuesType_e[]{
            FieldInfo.DocValuesType_e.BINARY, FieldInfo.DocValuesType_e.SORTED
        };

        [Test]
        public void TestBasic()
        {
            string groupField = "author";
            Directory dir = NewDirectory();
            RandomIndexWriter w = new RandomIndexWriter(
                Random(),
                dir,
                NewIndexWriterConfig(TEST_VERSION_CURRENT,
                    new MockAnalyzer(Random())).SetMergePolicy(NewLogMergePolicy()));
            bool canUseIDV = !"Lucene3x".Equals(w.w.Config.Codec.Name, StringComparison.Ordinal);
            FieldInfo.DocValuesType_e valueType = vts[Random().nextInt(vts.Length)];

            // 0
            Document doc = new Document();
            AddGroupField(doc, groupField, "author1", canUseIDV, valueType);
            doc.Add(NewTextField("content", "random text", Field.Store.NO));
            doc.Add(NewStringField("id_1", "1", Field.Store.NO));
            doc.Add(NewStringField("id_2", "1", Field.Store.NO));
            w.AddDocument(doc);

            // 1
            doc = new Document();
            AddGroupField(doc, groupField, "author1", canUseIDV, valueType);
            doc.Add(NewTextField("content", "some more random text blob", Field.Store.NO));
            doc.Add(NewStringField("id_1", "2", Field.Store.NO));
            doc.Add(NewStringField("id_2", "2", Field.Store.NO));
            w.AddDocument(doc);

            // 2
            doc = new Document();
            AddGroupField(doc, groupField, "author1", canUseIDV, valueType);
            doc.Add(NewTextField("content", "some more random textual data", Field.Store.NO));
            doc.Add(NewStringField("id_1", "3", Field.Store.NO));
            doc.Add(NewStringField("id_2", "3", Field.Store.NO));
            w.AddDocument(doc);
            w.Commit(); // To ensure a second segment

            // 3
            doc = new Document();
            AddGroupField(doc, groupField, "author2", canUseIDV, valueType);
            doc.Add(NewTextField("content", "some random text", Field.Store.NO));
            doc.Add(NewStringField("id_1", "4", Field.Store.NO));
            doc.Add(NewStringField("id_2", "4", Field.Store.NO));
            w.AddDocument(doc);

            // 4
            doc = new Document();
            AddGroupField(doc, groupField, "author3", canUseIDV, valueType);
            doc.Add(NewTextField("content", "some more random text", Field.Store.NO));
            doc.Add(NewStringField("id_1", "5", Field.Store.NO));
            doc.Add(NewStringField("id_2", "5", Field.Store.NO));
            w.AddDocument(doc);

            // 5
            doc = new Document();
            AddGroupField(doc, groupField, "author3", canUseIDV, valueType);
            doc.Add(NewTextField("content", "random blob", Field.Store.NO));
            doc.Add(NewStringField("id_1", "6", Field.Store.NO));
            doc.Add(NewStringField("id_2", "6", Field.Store.NO));
            w.AddDocument(doc);

            // 6 -- no author field
            doc = new Document();
            doc.Add(NewTextField("content", "random word stuck in alot of other text", Field.Store.NO));
            doc.Add(NewStringField("id_1", "6", Field.Store.NO));
            doc.Add(NewStringField("id_2", "6", Field.Store.NO));
            w.AddDocument(doc);

            // 7 -- no author field
            doc = new Document();
            doc.Add(NewTextField("content", "random word stuck in alot of other text", Field.Store.NO));
            doc.Add(NewStringField("id_1", "7", Field.Store.NO));
            doc.Add(NewStringField("id_2", "7", Field.Store.NO));
            w.AddDocument(doc);

            IndexReader reader = w.Reader;
            IndexSearcher indexSearcher = NewSearcher(reader);

            w.Dispose();
            int maxDoc = reader.MaxDoc;

            Sort sortWithinGroup = new Sort(new SortField("id_1", SortField.Type_e.INT, true));
            var allGroupHeadsCollector = CreateRandomCollector(groupField, sortWithinGroup, canUseIDV, valueType);
            indexSearcher.Search(new TermQuery(new Term("content", "random")), allGroupHeadsCollector);
            assertTrue(ArrayContains(new int[] { 2, 3, 5, 7 }, allGroupHeadsCollector.RetrieveGroupHeads()));
            assertTrue(OpenBitSetContains(new int[] { 2, 3, 5, 7 }, allGroupHeadsCollector.RetrieveGroupHeads(maxDoc), maxDoc));

            allGroupHeadsCollector = CreateRandomCollector(groupField, sortWithinGroup, canUseIDV, valueType);
            indexSearcher.Search(new TermQuery(new Term("content", "some")), allGroupHeadsCollector);
            assertTrue(ArrayContains(new int[] { 2, 3, 4 }, allGroupHeadsCollector.RetrieveGroupHeads()));
            assertTrue(OpenBitSetContains(new int[] { 2, 3, 4 }, allGroupHeadsCollector.RetrieveGroupHeads(maxDoc), maxDoc));

            allGroupHeadsCollector = CreateRandomCollector(groupField, sortWithinGroup, canUseIDV, valueType);
            indexSearcher.Search(new TermQuery(new Term("content", "blob")), allGroupHeadsCollector);
            assertTrue(ArrayContains(new int[] { 1, 5 }, allGroupHeadsCollector.RetrieveGroupHeads()));
            assertTrue(OpenBitSetContains(new int[] { 1, 5 }, allGroupHeadsCollector.RetrieveGroupHeads(maxDoc), maxDoc));

            // STRING sort type triggers different implementation
            Sort sortWithinGroup2 = new Sort(new SortField("id_2", SortField.Type_e.STRING, true));
            allGroupHeadsCollector = CreateRandomCollector(groupField, sortWithinGroup2, canUseIDV, valueType);
            indexSearcher.Search(new TermQuery(new Term("content", "random")), allGroupHeadsCollector);
            assertTrue(ArrayContains(new int[] { 2, 3, 5, 7 }, allGroupHeadsCollector.RetrieveGroupHeads()));
            assertTrue(OpenBitSetContains(new int[] { 2, 3, 5, 7 }, allGroupHeadsCollector.RetrieveGroupHeads(maxDoc), maxDoc));

            Sort sortWithinGroup3 = new Sort(new SortField("id_2", SortField.Type_e.STRING, false));
            allGroupHeadsCollector = CreateRandomCollector(groupField, sortWithinGroup3, canUseIDV, valueType);
            indexSearcher.Search(new TermQuery(new Term("content", "random")), allGroupHeadsCollector);
            // 7 b/c higher doc id wins, even if order of field is in not in reverse.
            assertTrue(ArrayContains(new int[] { 0, 3, 4, 6 }, allGroupHeadsCollector.RetrieveGroupHeads()));
            assertTrue(OpenBitSetContains(new int[] { 0, 3, 4, 6 }, allGroupHeadsCollector.RetrieveGroupHeads(maxDoc), maxDoc));

            indexSearcher.IndexReader.Dispose();
            dir.Dispose();
        }

        [Test]
        public void TestRandom()
        {
            int numberOfRuns = TestUtil.NextInt(Random(), 3, 6);
            for (int iter = 0; iter < numberOfRuns; iter++)
            {
                if (VERBOSE)
                {
                    Console.WriteLine(string.Format("TEST: iter={0} total={1}", iter, numberOfRuns));
                }

                int numDocs = TestUtil.NextInt(Random(), 100, 1000) * RANDOM_MULTIPLIER;
                int numGroups = TestUtil.NextInt(Random(), 1, numDocs);

                if (VERBOSE)
                {
                    Console.WriteLine("TEST: numDocs=" + numDocs + " numGroups=" + numGroups);
                }

                List<BytesRef> groups = new List<BytesRef>();
                for (int i = 0; i < numGroups; i++)
                {
                    string randomValue;
                    do
                    {
                        // B/c of DV based impl we can't see the difference between an empty string and a null value.
                        // For that reason we don't generate empty string groups.
                        randomValue = TestUtil.RandomRealisticUnicodeString(Random());
                    } while ("".Equals(randomValue, StringComparison.Ordinal));
                    groups.Add(new BytesRef(randomValue));
                }
                string[] contentStrings = new string[TestUtil.NextInt(Random(), 2, 20)];
                if (VERBOSE)
                {
                    Console.WriteLine("TEST: create fake content");
                }
                for (int contentIDX = 0; contentIDX < contentStrings.Length; contentIDX++)
                {
                    StringBuilder sb = new StringBuilder();
                    sb.append("real").append(Random().nextInt(3)).append(' ');
                    int fakeCount = Random().nextInt(10);
                    for (int fakeIDX = 0; fakeIDX < fakeCount; fakeIDX++)
                    {
                        sb.append("fake ");
                    }
                    contentStrings[contentIDX] = sb.toString();
                    if (VERBOSE)
                    {
                        Console.WriteLine("  content=" + sb.toString());
                    }
                }

                Directory dir = NewDirectory();
                RandomIndexWriter w = new RandomIndexWriter(
                    Random(),
                    dir,
                    NewIndexWriterConfig(TEST_VERSION_CURRENT,
                        new MockAnalyzer(Random())));
                bool preFlex = "Lucene3x".Equals(w.w.Config.Codec.Name, StringComparison.Ordinal);
                bool canUseIDV = !preFlex;
                FieldInfo.DocValuesType_e valueType = vts[Random().nextInt(vts.Length)];

                Document doc = new Document();
                Document docNoGroup = new Document();
                Field group = NewStringField("group", "", Field.Store.NO);
                doc.Add(group);
                Field valuesField = null;
                if (canUseIDV)
                {
                    switch (valueType)
                    {
                        case FieldInfo.DocValuesType_e.BINARY:
                            valuesField = new BinaryDocValuesField("group_dv", new BytesRef());
                            break;
                        case FieldInfo.DocValuesType_e.SORTED:
                            valuesField = new SortedDocValuesField("group_dv", new BytesRef());
                            break;
                        default:
                            fail("unhandled type");
                            break;
                    }
                    doc.Add(valuesField);
                }
                Field sort1 = NewStringField("sort1", "", Field.Store.NO);
                doc.Add(sort1);
                docNoGroup.Add(sort1);
                Field sort2 = NewStringField("sort2", "", Field.Store.NO);
                doc.Add(sort2);
                docNoGroup.Add(sort2);
                Field sort3 = NewStringField("sort3", "", Field.Store.NO);
                doc.Add(sort3);
                docNoGroup.Add(sort3);
                Field content = NewTextField("content", "", Field.Store.NO);
                doc.Add(content);
                docNoGroup.Add(content);
                IntField id = new IntField("id", 0, Field.Store.NO);
                doc.Add(id);
                docNoGroup.Add(id);
                GroupDoc[] groupDocs = new GroupDoc[numDocs];
                for (int i = 0; i < numDocs; i++)
                {
                    BytesRef groupValue;
                    if (Random().nextInt(24) == 17)
                    {
                        // So we test the "doc doesn't have the group'd
                        // field" case:
                        groupValue = null;
                    }
                    else
                    {
                        groupValue = groups[Random().nextInt(groups.size())];
                    }

                    GroupDoc groupDoc = new GroupDoc(
                        i,
                        groupValue,
                        groups[Random().nextInt(groups.size())],
                        groups[Random().nextInt(groups.size())],
                        new BytesRef(string.Format(CultureInfo.InvariantCulture, "{0:D5}", i)),
                        contentStrings[Random().nextInt(contentStrings.Length)]
                    );

                    if (VERBOSE)
                    {
                        Console.WriteLine("  doc content=" + groupDoc.content + " id=" + i + " group=" + (groupDoc.group == null ? "null" : groupDoc.group.Utf8ToString()) + " sort1=" + groupDoc.sort1.Utf8ToString() + " sort2=" + groupDoc.sort2.Utf8ToString() + " sort3=" + groupDoc.sort3.Utf8ToString());
                    }

                    groupDocs[i] = groupDoc;
                    if (groupDoc.group != null)
                    {
                        group.StringValue = (groupDoc.group.Utf8ToString());
                        if (canUseIDV)
                        {
                            valuesField.BytesValue = (new BytesRef(groupDoc.group.Utf8ToString()));
                        }
                    }
                    sort1.StringValue = (groupDoc.sort1.Utf8ToString());
                    sort2.StringValue = (groupDoc.sort2.Utf8ToString());
                    sort3.StringValue = (groupDoc.sort3.Utf8ToString());
                    content.StringValue = (groupDoc.content);
                    id.IntValue = (groupDoc.id);
                    if (groupDoc.group == null)
                    {
                        w.AddDocument(docNoGroup);
                    }
                    else
                    {
                        w.AddDocument(doc);
                    }
                }

                DirectoryReader r = w.Reader;
                w.Dispose();

                // NOTE: intentional but temporary field cache insanity!
                FieldCache.Ints docIdToFieldId = FieldCache.DEFAULT.GetInts(SlowCompositeReaderWrapper.Wrap(r), "id", false);
                int[] fieldIdToDocID = new int[numDocs];
                for (int i = 0; i < numDocs; i++)
                {
                    int fieldId = docIdToFieldId.Get(i);
                    fieldIdToDocID[fieldId] = i;
                }

                try
                {
                    IndexSearcher s = NewSearcher(r);
                    if (typeof(SlowCompositeReaderWrapper).IsAssignableFrom(s.IndexReader.GetType()))
                    {
                        canUseIDV = false;
                    }
                    else
                    {
                        canUseIDV = !preFlex;
                    }

                    for (int contentID = 0; contentID < 3; contentID++)
                    {
                        ScoreDoc[] hits = s.Search(new TermQuery(new Term("content", "real" + contentID)), numDocs).ScoreDocs;
                        foreach (ScoreDoc hit in hits)
                        {
                            GroupDoc gd = groupDocs[docIdToFieldId.Get(hit.Doc)];
                            assertTrue(gd.score == 0.0);
                            gd.score = hit.Score;
                            int docId = gd.id;
                            assertEquals(docId, docIdToFieldId.Get(hit.Doc));
                        }
                    }

                    foreach (GroupDoc gd in groupDocs)
                    {
                        assertTrue(gd.score != 0.0);
                    }

                    for (int searchIter = 0; searchIter < 100; searchIter++)
                    {

                        if (VERBOSE)
                        {
                            Console.WriteLine("TEST: searchIter=" + searchIter);
                        }

                        string searchTerm = "real" + Random().nextInt(3);
                        bool sortByScoreOnly = Random().nextBoolean();
                        Sort sortWithinGroup = GetRandomSort(sortByScoreOnly);
                        AbstractAllGroupHeadsCollector allGroupHeadsCollector = CreateRandomCollector("group", sortWithinGroup, canUseIDV, valueType);
                        s.Search(new TermQuery(new Term("content", searchTerm)), allGroupHeadsCollector);
                        int[] expectedGroupHeads = CreateExpectedGroupHeads(searchTerm, groupDocs, sortWithinGroup, sortByScoreOnly, fieldIdToDocID);
                        int[] actualGroupHeads = allGroupHeadsCollector.RetrieveGroupHeads();
                        // The actual group heads contains Lucene ids. Need to change them into our id value.
                        for (int i = 0; i < actualGroupHeads.Length; i++)
                        {
                            actualGroupHeads[i] = docIdToFieldId.Get(actualGroupHeads[i]);
                        }
                        // Allows us the easily iterate and assert the actual and expected results.
                        Array.Sort(expectedGroupHeads);
                        Array.Sort(actualGroupHeads);

                        if (VERBOSE)
                        {
                            Console.WriteLine("Collector: " + allGroupHeadsCollector.GetType().Name);
                            Console.WriteLine("Sort within group: " + sortWithinGroup);
                            Console.WriteLine("Num group: " + numGroups);
                            Console.WriteLine("Num doc: " + numDocs);
                            Console.WriteLine("\n=== Expected: \n");
                            foreach (int expectedDocId in expectedGroupHeads)
                            {
                                GroupDoc expectedGroupDoc = groupDocs[expectedDocId];
                                string expectedGroup = expectedGroupDoc.group == null ? null : expectedGroupDoc.group.Utf8ToString();
                                Console.WriteLine(
                                    string.Format(CultureInfo.InvariantCulture,
                                    "Group:{0,10} score{1:0.0#######,5} Sort1:{2,10} Sort2:{3,10} Sort3:{4,10} doc:{5,10}",
                                    expectedGroup, expectedGroupDoc.score, expectedGroupDoc.sort1.Utf8ToString(),
                                    expectedGroupDoc.sort2.Utf8ToString(), expectedGroupDoc.sort3.Utf8ToString(), expectedDocId)
                                );
                            }
                            Console.WriteLine("\n=== Actual: \n");
                            foreach (int actualDocId in actualGroupHeads)
                            {
                                GroupDoc actualGroupDoc = groupDocs[actualDocId];
                                string actualGroup = actualGroupDoc.group == null ? null : actualGroupDoc.group.Utf8ToString();
                                Console.WriteLine(
                                    string.Format(CultureInfo.InvariantCulture,
                                    "Group:{0,10} score{1:0.0#######,5} Sort1:{2,10} Sort2:{3,10} Sort3:{4,10} doc:{5,10}",
                                    actualGroup, actualGroupDoc.score, actualGroupDoc.sort1.Utf8ToString(),
                                    actualGroupDoc.sort2.Utf8ToString(), actualGroupDoc.sort3.Utf8ToString(), actualDocId)
                                );
                            }
                            Console.WriteLine("\n===================================================================================");
                        }

                        assertArrayEquals(expectedGroupHeads, actualGroupHeads);
                    }
                }
                finally
                {
                    QueryUtils.PurgeFieldCache(r);
                }

                r.Dispose();
                dir.Dispose();
            }
        }


        private bool ArrayContains(int[] expected, int[] actual)
        {
            Array.Sort(actual); // in some cases the actual docs aren't sorted by docid. This method expects that.
            if (expected.Length != actual.Length)
            {
                return false;
            }

            foreach (int e in expected)
            {
                bool found = false;
                foreach (int a in actual)
                {
                    if (e == a)
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    return false;
                }
            }

            return true;
        }

        private bool OpenBitSetContains(int[] expectedDocs, FixedBitSet actual, int maxDoc)
        {
            if (expectedDocs.Length != actual.Cardinality())
            {
                return false;
            }

            FixedBitSet expected = new FixedBitSet(maxDoc);
            foreach (int expectedDoc in expectedDocs)
            {
                expected.Set(expectedDoc);
            }

            int docId;
            DocIdSetIterator iterator = expected.GetIterator();
            while ((docId = iterator.NextDoc()) != DocIdSetIterator.NO_MORE_DOCS)
            {
                if (!actual.Get(docId))
                {
                    return false;
                }
            }

            return true;
        }

        private int[] CreateExpectedGroupHeads(string searchTerm, GroupDoc[] groupDocs, Sort docSort, bool sortByScoreOnly, int[] fieldIdToDocID)
        {
            IDictionary<BytesRef, List<GroupDoc>> groupHeads = new HashMap<BytesRef, List<GroupDoc>>();
            foreach (GroupDoc groupDoc in groupDocs)
            {
                if (!groupDoc.content.StartsWith(searchTerm, StringComparison.Ordinal))
                {
                    continue;
                }

                if (!groupHeads.ContainsKey(groupDoc.group))
                {
                    List<GroupDoc> list = new List<GroupDoc>();
                    list.Add(groupDoc);
                    groupHeads[groupDoc.group] = list;
                    continue;
                }
                groupHeads[groupDoc.group].Add(groupDoc);
            }

            int[] allGroupHeads = new int[groupHeads.Count];
            int i = 0;
            foreach (BytesRef groupValue in groupHeads.Keys)
            {
                List<GroupDoc> docs = groupHeads[groupValue];
                // LUCENENET TODO: The original API Collections.Sort does not currently exist.
                // This call ultimately results in calling TimSort, which is why this line was replaced
                // with CollectionUtil.TimSort(IList<T>, IComparer<T>).
                //
                // NOTE: List.Sort(comparer) won't work in this case because it calls the comparer when the
                // values are the same, which results in this test failing. TimSort only calls the comparer
                // when the values differ.
                //Collections.Sort(docs, GetComparator(docSort, sortByScoreOnly, fieldIdToDocID));
                CollectionUtil.TimSort(docs, GetComparator(docSort, sortByScoreOnly, fieldIdToDocID));
                allGroupHeads[i++] = docs[0].id;
            }

            return allGroupHeads;
        }

        private Sort GetRandomSort(bool scoreOnly)
        {
            List<SortField> sortFields = new List<SortField>();
            if (Random().nextInt(7) == 2 || scoreOnly)
            {
                sortFields.Add(SortField.FIELD_SCORE);
            }
            else
            {
                if (Random().nextBoolean())
                {
                    if (Random().nextBoolean())
                    {
                        sortFields.Add(new SortField("sort1", SortField.Type_e.STRING, Random().nextBoolean()));
                    }
                    else
                    {
                        sortFields.Add(new SortField("sort2", SortField.Type_e.STRING, Random().nextBoolean()));
                    }
                }
                else if (Random().nextBoolean())
                {
                    sortFields.Add(new SortField("sort1", SortField.Type_e.STRING, Random().nextBoolean()));
                    sortFields.Add(new SortField("sort2", SortField.Type_e.STRING, Random().nextBoolean()));
                }
            }
            // Break ties:
            if (Random().nextBoolean() && !scoreOnly)
            {
                sortFields.Add(new SortField("sort3", SortField.Type_e.STRING));
            }
            else if (!scoreOnly)
            {
                sortFields.Add(new SortField("id", SortField.Type_e.INT));
            }
            return new Sort(sortFields.ToArray(/*new SortField[sortFields.size()]*/));
        }

        internal class ComparatorAnonymousHelper : IComparer<GroupDoc>
        {
            private readonly AllGroupHeadsCollectorTest outerInstance;
            private readonly SortField[] sortFields;
            private readonly bool sortByScoreOnly;
            private readonly int[] fieldIdToDocID;

            public ComparatorAnonymousHelper(AllGroupHeadsCollectorTest outerInstance, SortField[] sortFields, bool sortByScoreOnly, int[] fieldIdToDocID)
            {
                this.outerInstance = outerInstance;
                this.sortFields = sortFields;
                this.sortByScoreOnly = sortByScoreOnly;
                this.fieldIdToDocID = fieldIdToDocID;
            }

            public int Compare(GroupDoc d1, GroupDoc d2)
            {
                foreach (SortField sf in sortFields)
                {
                    int cmp;
                    if (sf.Type == SortField.Type_e.SCORE)
                    {
                        if (d1.score > d2.score)
                        {
                            cmp = -1;
                        }
                        else if (d1.score < d2.score)
                        {
                            cmp = 1;
                        }
                        else
                        {
                            cmp = sortByScoreOnly ? fieldIdToDocID[d1.id] - fieldIdToDocID[d2.id] : 0;
                        }
                    }
                    else if (sf.Field.Equals("sort1", StringComparison.Ordinal))
                    {
                        cmp = d1.sort1.CompareTo(d2.sort1);
                    }
                    else if (sf.Field.Equals("sort2", StringComparison.Ordinal))
                    {
                        cmp = d1.sort2.CompareTo(d2.sort2);
                    }
                    else if (sf.Field.Equals("sort3", StringComparison.Ordinal))
                    {
                        cmp = d1.sort3.CompareTo(d2.sort3);
                    }
                    else
                    {
                        assertEquals(sf.Field, "id");
                        cmp = d1.id - d2.id;
                    }
                    if (cmp != 0)
                    {
                        return sf.Reverse ? -cmp : cmp;
                    }
                }
                // Our sort always fully tie breaks:
                fail();
                return 0;
            }
        }

        private IComparer<GroupDoc> GetComparator(Sort sort, bool sortByScoreOnly, int[] fieldIdToDocID)
        {
            SortField[] sortFields = sort.GetSort();
            return new ComparatorAnonymousHelper(this, sortFields, sortByScoreOnly, fieldIdToDocID);
        }

        private AbstractAllGroupHeadsCollector CreateRandomCollector(string groupField, Sort sortWithinGroup, bool canUseIDV, FieldInfo.DocValuesType_e valueType)
        {
            AbstractAllGroupHeadsCollector collector;
            if (Random().nextBoolean())
            {
                ValueSource vs = new BytesRefFieldSource(groupField);
                collector = new FunctionAllGroupHeadsCollector(vs, new Hashtable(), sortWithinGroup);
            }
            else
            {
                collector = TermAllGroupHeadsCollector.Create(groupField, sortWithinGroup);
            }

            if (VERBOSE)
            {
                Console.WriteLine("Selected implementation: " + collector.GetType().Name);
            }

            return collector;
        }

        private void AddGroupField(Document doc, string groupField, string value, bool canUseIDV, FieldInfo.DocValuesType_e valueType)
        {
            doc.Add(new TextField(groupField, value, Field.Store.YES));
            if (canUseIDV)
            {
                Field valuesField = null;
                switch (valueType)
                {
                    case FieldInfo.DocValuesType_e.BINARY:
                        valuesField = new BinaryDocValuesField(groupField + "_dv", new BytesRef(value));
                        break;
                    case FieldInfo.DocValuesType_e.SORTED:
                        valuesField = new SortedDocValuesField(groupField + "_dv", new BytesRef(value));
                        break;
                    default:
                        fail("unhandled type");
                        break;
                }
                doc.Add(valuesField);
            }
        }

        internal class GroupDoc
        {
            internal readonly int id;
            internal readonly BytesRef group;
            internal readonly BytesRef sort1;
            internal readonly BytesRef sort2;
            internal readonly BytesRef sort3;
            // content must be "realN ..."
            internal readonly string content;
            internal float score;

            public GroupDoc(int id, BytesRef group, BytesRef sort1, BytesRef sort2, BytesRef sort3, String content)
            {
                this.id = id;
                this.group = group;
                this.sort1 = sort1;
                this.sort2 = sort2;
                this.sort3 = sort3;
                this.content = content;
            }
        }
    }
}
