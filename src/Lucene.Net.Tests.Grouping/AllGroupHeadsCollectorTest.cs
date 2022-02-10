using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Index.Extensions;
using Lucene.Net.Queries.Function;
using Lucene.Net.Queries.Function.ValueSources;
using Lucene.Net.Search.Grouping.Function;
using Lucene.Net.Search.Grouping.Terms;
using Lucene.Net.Store;
using Lucene.Net.Util;
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Console = Lucene.Net.Util.SystemConsole;
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

    public class AllGroupHeadsCollectorTest : LuceneTestCase
    {
        private static readonly DocValuesType[] vts = new DocValuesType[]
        {
            DocValuesType.BINARY, DocValuesType.SORTED
        };

        [Test]
        public void TestBasic()
        {
            string groupField = "author";
            Directory dir = NewDirectory();
            RandomIndexWriter w = new RandomIndexWriter(
                Random,
                dir,
                NewIndexWriterConfig(TEST_VERSION_CURRENT,
                    new MockAnalyzer(Random)).SetMergePolicy(NewLogMergePolicy()));
            bool canUseIDV = !"Lucene3x".Equals(w.IndexWriter.Config.Codec.Name, StringComparison.Ordinal);
            DocValuesType valueType = vts[Random.nextInt(vts.Length)];

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

            IndexReader reader = w.GetReader();
            IndexSearcher indexSearcher = NewSearcher(reader);

            w.Dispose();
            int maxDoc = reader.MaxDoc;

            Sort sortWithinGroup = new Sort(new SortField("id_1", SortFieldType.INT32, true));
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
            Sort sortWithinGroup2 = new Sort(new SortField("id_2", SortFieldType.STRING, true));
            allGroupHeadsCollector = CreateRandomCollector(groupField, sortWithinGroup2, canUseIDV, valueType);
            indexSearcher.Search(new TermQuery(new Term("content", "random")), allGroupHeadsCollector);
            assertTrue(ArrayContains(new int[] { 2, 3, 5, 7 }, allGroupHeadsCollector.RetrieveGroupHeads()));
            assertTrue(OpenBitSetContains(new int[] { 2, 3, 5, 7 }, allGroupHeadsCollector.RetrieveGroupHeads(maxDoc), maxDoc));

            Sort sortWithinGroup3 = new Sort(new SortField("id_2", SortFieldType.STRING, false));
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
            int numberOfRuns = TestUtil.NextInt32(Random, 3, 6);
            for (int iter = 0; iter < numberOfRuns; iter++)
            {
                if (Verbose)
                {
                    Console.WriteLine(string.Format("TEST: iter={0} total={1}", iter, numberOfRuns));
                }

                int numDocs = TestUtil.NextInt32(Random, 100, 1000) * RandomMultiplier;
                int numGroups = TestUtil.NextInt32(Random, 1, numDocs);

                if (Verbose)
                {
                    Console.WriteLine("TEST: numDocs=" + numDocs + " numGroups=" + numGroups);
                }

                JCG.List<BytesRef> groups = new JCG.List<BytesRef>();
                for (int i = 0; i < numGroups; i++)
                {
                    string randomValue;
                    do
                    {
                        // B/c of DV based impl we can't see the difference between an empty string and a null value.
                        // For that reason we don't generate empty string groups.
                        randomValue = TestUtil.RandomRealisticUnicodeString(Random);
                    } while ("".Equals(randomValue, StringComparison.Ordinal));
                    groups.Add(new BytesRef(randomValue));
                }
                string[] contentStrings = new string[TestUtil.NextInt32(Random, 2, 20)];
                if (Verbose)
                {
                    Console.WriteLine("TEST: create fake content");
                }
                for (int contentIDX = 0; contentIDX < contentStrings.Length; contentIDX++)
                {
                    StringBuilder sb = new StringBuilder();
                    sb.append("real").append(Random.nextInt(3)).append(' ');
                    int fakeCount = Random.nextInt(10);
                    for (int fakeIDX = 0; fakeIDX < fakeCount; fakeIDX++)
                    {
                        sb.append("fake ");
                    }
                    contentStrings[contentIDX] = sb.toString();
                    if (Verbose)
                    {
                        Console.WriteLine("  content=" + sb.toString());
                    }
                }

                Directory dir = NewDirectory();
                RandomIndexWriter w = new RandomIndexWriter(
                    Random,
                    dir,
                    NewIndexWriterConfig(TEST_VERSION_CURRENT,
                        new MockAnalyzer(Random)));
                bool preFlex = "Lucene3x".Equals(w.IndexWriter.Config.Codec.Name, StringComparison.Ordinal);
                bool canUseIDV = !preFlex;
                DocValuesType valueType = vts[Random.nextInt(vts.Length)];

                Document doc = new Document();
                Document docNoGroup = new Document();
                Field group = NewStringField("group", "", Field.Store.NO);
                doc.Add(group);
                Field valuesField = null;
                if (canUseIDV)
                {
                    switch (valueType)
                    {
                        case DocValuesType.BINARY:
                            valuesField = new BinaryDocValuesField("group_dv", new BytesRef());
                            break;
                        case DocValuesType.SORTED:
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
                Int32Field id = new Int32Field("id", 0, Field.Store.NO);
                doc.Add(id);
                docNoGroup.Add(id);
                GroupDoc[] groupDocs = new GroupDoc[numDocs];
                for (int i = 0; i < numDocs; i++)
                {
                    BytesRef groupValue;
                    if (Random.nextInt(24) == 17)
                    {
                        // So we test the "doc doesn't have the group'd
                        // field" case:
                        groupValue = null;
                    }
                    else
                    {
                        groupValue = groups[Random.nextInt(groups.size())];
                    }

                    GroupDoc groupDoc = new GroupDoc(
                        i,
                        groupValue,
                        groups[Random.nextInt(groups.size())],
                        groups[Random.nextInt(groups.size())],
                        new BytesRef(string.Format(CultureInfo.InvariantCulture, "{0:D5}", i)),
                        contentStrings[Random.nextInt(contentStrings.Length)]
                    );

                    if (Verbose)
                    {
                        Console.WriteLine("  doc content=" + groupDoc.content + " id=" + i + " group=" + (groupDoc.group is null ? "null" : groupDoc.group.Utf8ToString()) + " sort1=" + groupDoc.sort1.Utf8ToString() + " sort2=" + groupDoc.sort2.Utf8ToString() + " sort3=" + groupDoc.sort3.Utf8ToString());
                    }

                    groupDocs[i] = groupDoc;
                    if (groupDoc.group != null)
                    {
                        group.SetStringValue(groupDoc.group.Utf8ToString());
                        if (canUseIDV)
                        {
                            valuesField.SetBytesValue(new BytesRef(groupDoc.group.Utf8ToString()));
                        }
                    }
                    sort1.SetStringValue(groupDoc.sort1.Utf8ToString());
                    sort2.SetStringValue(groupDoc.sort2.Utf8ToString());
                    sort3.SetStringValue(groupDoc.sort3.Utf8ToString());
                    content.SetStringValue(groupDoc.content);
                    id.SetInt32Value(groupDoc.id);
                    if (groupDoc.group is null)
                    {
                        w.AddDocument(docNoGroup);
                    }
                    else
                    {
                        w.AddDocument(doc);
                    }
                }

                DirectoryReader r = w.GetReader();
                w.Dispose();

                // NOTE: intentional but temporary field cache insanity!
                FieldCache.Int32s docIdToFieldId = FieldCache.DEFAULT.GetInt32s(SlowCompositeReaderWrapper.Wrap(r), "id", false);
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

                        if (Verbose)
                        {
                            Console.WriteLine("TEST: searchIter=" + searchIter);
                        }

                        string searchTerm = "real" + Random.nextInt(3);
                        bool sortByScoreOnly = Random.nextBoolean();
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

                        if (Verbose)
                        {
                            Console.WriteLine("Collector: " + allGroupHeadsCollector.GetType().Name);
                            Console.WriteLine("Sort within group: " + sortWithinGroup);
                            Console.WriteLine("Num group: " + numGroups);
                            Console.WriteLine("Num doc: " + numDocs);
                            Console.WriteLine("\n=== Expected: \n");
                            foreach (int expectedDocId in expectedGroupHeads)
                            {
                                GroupDoc expectedGroupDoc = groupDocs[expectedDocId];
                                string expectedGroup = expectedGroupDoc.group is null ? null : expectedGroupDoc.group.Utf8ToString();
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
                                string actualGroup = actualGroupDoc.group is null ? null : actualGroupDoc.group.Utf8ToString();
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
            if (expectedDocs.Length != actual.Cardinality)
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
            IDictionary<BytesRef, JCG.List<GroupDoc>> groupHeads = new JCG.Dictionary<BytesRef, JCG.List<GroupDoc>>();
            foreach (GroupDoc groupDoc in groupDocs)
            {
                if (!groupDoc.content.StartsWith(searchTerm, StringComparison.Ordinal))
                {
                    continue;
                }

                if (!groupHeads.TryGetValue(groupDoc.group, out JCG.List<GroupDoc> grouphead))
                {
                    JCG.List<GroupDoc> list = new JCG.List<GroupDoc>();
                    list.Add(groupDoc);
                    groupHeads[groupDoc.group] = list;
                    continue;
                }
                grouphead.Add(groupDoc);
            }

            int[] allGroupHeads = new int[groupHeads.Count];
            int i = 0;
            foreach (BytesRef groupValue in groupHeads.Keys)
            {
                JCG.List<GroupDoc> docs = groupHeads[groupValue];
                // LUCENENET TODO: The original API Collections.Sort does not currently exist.
                // This call ultimately results in calling TimSort, which is why this line was replaced
                // with CollectionUtil.TimSort(IList<T>, IComparer<T>).
                //
                // NOTE: List.Sort(comparer) won't work in this case because it calls the comparer when the
                // values are the same, which results in this test failing. TimSort only calls the comparer
                // when the values differ.
                //Collections.Sort(docs, GetComparer(docSort, sortByScoreOnly, fieldIdToDocID));
                CollectionUtil.TimSort(docs, GetComparer(docSort, sortByScoreOnly, fieldIdToDocID));
                allGroupHeads[i++] = docs[0].id;
            }

            return allGroupHeads;
        }

        private Sort GetRandomSort(bool scoreOnly)
        {
            JCG.List<SortField> sortFields = new JCG.List<SortField>();
            if (Random.nextInt(7) == 2 || scoreOnly)
            {
                sortFields.Add(SortField.FIELD_SCORE);
            }
            else
            {
                if (Random.nextBoolean())
                {
                    if (Random.nextBoolean())
                    {
                        sortFields.Add(new SortField("sort1", SortFieldType.STRING, Random.nextBoolean()));
                    }
                    else
                    {
                        sortFields.Add(new SortField("sort2", SortFieldType.STRING, Random.nextBoolean()));
                    }
                }
                else if (Random.nextBoolean())
                {
                    sortFields.Add(new SortField("sort1", SortFieldType.STRING, Random.nextBoolean()));
                    sortFields.Add(new SortField("sort2", SortFieldType.STRING, Random.nextBoolean()));
                }
            }
            // Break ties:
            if (Random.nextBoolean() && !scoreOnly)
            {
                sortFields.Add(new SortField("sort3", SortFieldType.STRING));
            }
            else if (!scoreOnly)
            {
                sortFields.Add(new SortField("id", SortFieldType.INT32));
            }
            return new Sort(sortFields.ToArray(/*new SortField[sortFields.size()]*/));
        }

        private IComparer<GroupDoc> GetComparer(Sort sort, bool sortByScoreOnly, int[] fieldIdToDocID)
        {
            SortField[] sortFields = sort.GetSort();
            return Comparer<GroupDoc>.Create((d1,d2)=> {
                foreach (SortField sf in sortFields)
                {
                    int cmp;
                    if (sf.Type == SortFieldType.SCORE)
                    {
                        // LUCENENET specific - compare bits rather than using equality operators to prevent these comparisons from failing in x86 in .NET Framework with optimizations enabled
                        if (NumericUtils.SingleToSortableInt32(d1.score) > NumericUtils.SingleToSortableInt32(d2.score))
                        {
                            cmp = -1;
                        }
                        // LUCENENET specific - compare bits rather than using equality operators to prevent these comparisons from failing in x86 in .NET Framework with optimizations enabled
                        else if (NumericUtils.SingleToSortableInt32(d1.score) < NumericUtils.SingleToSortableInt32(d2.score))
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
                        return sf.IsReverse ? -cmp : cmp;
                    }
                }
                // Our sort always fully tie breaks:
                fail();
                return 0;
            });
        }

        private AbstractAllGroupHeadsCollector CreateRandomCollector(string groupField, Sort sortWithinGroup, bool canUseIDV, DocValuesType valueType)
        {
            AbstractAllGroupHeadsCollector collector;
            if (Random.nextBoolean())
            {
                ValueSource vs = new BytesRefFieldSource(groupField);
                collector = new FunctionAllGroupHeadsCollector(vs, new Hashtable(), sortWithinGroup);
            }
            else
            {
                collector = TermAllGroupHeadsCollector.Create(groupField, sortWithinGroup);
            }

            if (Verbose)
            {
                Console.WriteLine("Selected implementation: " + collector.GetType().Name);
            }

            return collector;
        }

        private void AddGroupField(Document doc, string groupField, string value, bool canUseIDV, DocValuesType valueType)
        {
            doc.Add(new TextField(groupField, value, Field.Store.YES));
            if (canUseIDV)
            {
                Field valuesField = null;
                switch (valueType)
                {
                    case DocValuesType.BINARY:
                        valuesField = new BinaryDocValuesField(groupField + "_dv", new BytesRef(value));
                        break;
                    case DocValuesType.SORTED:
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
