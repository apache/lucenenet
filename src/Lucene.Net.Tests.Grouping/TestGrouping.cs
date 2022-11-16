/*
 *
 * Licensed to the Apache Software Foundation (ASF) under one
 * or more contributor license agreements.  See the NOTICE file
 * distributed with this work for additional information
 * regarding copyright ownership.  The ASF licenses this file
 * to you under the Apache License, Version 2.0 (the
 * "License"); you may not use this file except in compliance
 * with the License.  You may obtain a copy of the License at
 *
 *   http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing,
 * software distributed under the License is distributed on an
 * "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
 * KIND, either express or implied.  See the License for the
 * specific language governing permissions and limitations
 * under the License.
 *
*/

using J2N.Collections.Generic.Extensions;
using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Index.Extensions;
using Lucene.Net.Queries.Function;
using Lucene.Net.Queries.Function.ValueSources;
using Lucene.Net.Search.Grouping.Function;
using Lucene.Net.Search.Grouping.Terms;
using Lucene.Net.Store;
using Lucene.Net.Support;
using Lucene.Net.Util;
using Lucene.Net.Util.Mutable;
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Console = Lucene.Net.Util.SystemConsole;
using JCG = J2N.Collections.Generic;

namespace Lucene.Net.Search.Grouping
{
    // TODO
    //   - should test relevance sort too
    //   - test null
    //   - test ties
    //   - test compound sort
    public class TestGrouping : LuceneTestCase
    {
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
            // 0
            Document doc = new Document();
            AddGroupField(doc, groupField, "author1", canUseIDV);
            doc.Add(new TextField("content", "random text", Field.Store.YES));
            doc.Add(new Field("id", "1", customType));
            w.AddDocument(doc);

            // 1
            doc = new Document();
            AddGroupField(doc, groupField, "author1", canUseIDV);
            doc.Add(new TextField("content", "some more random text", Field.Store.YES));
            doc.Add(new Field("id", "2", customType));
            w.AddDocument(doc);

            // 2
            doc = new Document();
            AddGroupField(doc, groupField, "author1", canUseIDV);
            doc.Add(new TextField("content", "some more random textual data", Field.Store.YES));
            doc.Add(new Field("id", "3", customType));
            w.AddDocument(doc);

            // 3
            doc = new Document();
            AddGroupField(doc, groupField, "author2", canUseIDV);
            doc.Add(new TextField("content", "some random text", Field.Store.YES));
            doc.Add(new Field("id", "4", customType));
            w.AddDocument(doc);

            // 4
            doc = new Document();
            AddGroupField(doc, groupField, "author3", canUseIDV);
            doc.Add(new TextField("content", "some more random text", Field.Store.YES));
            doc.Add(new Field("id", "5", customType));
            w.AddDocument(doc);

            // 5
            doc = new Document();
            AddGroupField(doc, groupField, "author3", canUseIDV);
            doc.Add(new TextField("content", "random", Field.Store.YES));
            doc.Add(new Field("id", "6", customType));
            w.AddDocument(doc);

            // 6 -- no author field
            doc = new Document();
            doc.Add(new TextField("content", "random word stuck in alot of other text", Field.Store.YES));
            doc.Add(new Field("id", "6", customType));
            w.AddDocument(doc);

            IndexSearcher indexSearcher = NewSearcher(w.GetReader());
            w.Dispose();

            Sort groupSort = Sort.RELEVANCE;

            if (canUseIDV && Random.nextBoolean())
            {
                groupField += "_dv";
            }

            IAbstractFirstPassGroupingCollector<object> c1 = CreateRandomFirstPassCollector(groupField, groupSort, 10);
            indexSearcher.Search(new TermQuery(new Index.Term("content", "random")), c1);

            IAbstractSecondPassGroupingCollector<object> c2 = CreateSecondPassCollector(c1, groupField, groupSort, null, 0, 5, true, true, true);
            indexSearcher.Search(new TermQuery(new Index.Term("content", "random")), c2);

            ITopGroups<object> groups = c2.GetTopGroups(0);
            assertFalse(float.IsNaN(groups.MaxScore));

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

            indexSearcher.IndexReader.Dispose();
            dir.Dispose();
        }

        private void AddGroupField(Document doc, string groupField, string value, bool canUseIDV)
        {
            doc.Add(new TextField(groupField, value, Field.Store.YES));
            if (canUseIDV)
            {
                doc.Add(new SortedDocValuesField(groupField + "_dv", new BytesRef(value)));
            }
        }

        private IAbstractFirstPassGroupingCollector<object> CreateRandomFirstPassCollector(string groupField, Sort groupSort, int topDocs)
        {
            IAbstractFirstPassGroupingCollector<object> selected;
            if (Random.nextBoolean())
            {
                ValueSource vs = new BytesRefFieldSource(groupField);
                selected = new FunctionFirstPassGroupingCollector<MutableValue>(vs, new Hashtable(), groupSort, topDocs);        // LUCENENET Specific type for generic must be specified.
            }
            else
            {
                selected = new TermFirstPassGroupingCollector(groupField, groupSort, topDocs);
            }
            if (Verbose)
            {
                Console.WriteLine("Selected implementation: " + selected.GetType().Name);
            }
            return selected;
        }

        private IAbstractFirstPassGroupingCollector<T> CreateFirstPassCollector<T>(string groupField, Sort groupSort, int topDocs, IAbstractFirstPassGroupingCollector<T> firstPassGroupingCollector)
        {
            if (typeof(TermFirstPassGroupingCollector).IsAssignableFrom(firstPassGroupingCollector.GetType()))
            {
                ValueSource vs = new BytesRefFieldSource(groupField);
                return new FunctionFirstPassGroupingCollector<MutableValue>(vs, new Hashtable(), groupSort, topDocs)            // LUCENENET Specific type for generic must be specified.
                    as IAbstractFirstPassGroupingCollector<T>;
            }
            else
            {
                return new TermFirstPassGroupingCollector(groupField, groupSort, topDocs)
                    as IAbstractFirstPassGroupingCollector<T>;
            }
        }

        private IAbstractSecondPassGroupingCollector<T> CreateSecondPassCollector<T>(IAbstractFirstPassGroupingCollector<T> firstPassGroupingCollector,
                                                                              string groupField,
                                                                              Sort groupSort,
                                                                              Sort sortWithinGroup,
                                                                              int groupOffset,
                                                                              int maxDocsPerGroup,
                                                                              bool getScores,
                                                                              bool getMaxScores,
                                                                              bool fillSortFields)
        {

            if (typeof(TermFirstPassGroupingCollector).IsAssignableFrom(firstPassGroupingCollector.GetType()))
            {
                var searchGroups = firstPassGroupingCollector.GetTopGroups(groupOffset, fillSortFields);
                return (IAbstractSecondPassGroupingCollector<T>)new TermSecondPassGroupingCollector(groupField, searchGroups as IEnumerable<ISearchGroup<BytesRef>>, groupSort, sortWithinGroup, maxDocsPerGroup, getScores, getMaxScores, fillSortFields);
            }
            else
            {
                ValueSource vs = new BytesRefFieldSource(groupField);
                var searchGroups = firstPassGroupingCollector.GetTopGroups(groupOffset, fillSortFields);
                return (IAbstractSecondPassGroupingCollector<T>)new FunctionSecondPassGroupingCollector<MutableValue>(searchGroups as IEnumerable<ISearchGroup<MutableValue>>, groupSort, sortWithinGroup, maxDocsPerGroup, getScores, getMaxScores, fillSortFields, vs, new Hashtable());        // LUCENENET Specific type for generic must be specified.
            }
        }

        // Basically converts searchGroups from MutableValue to BytesRef if grouping by ValueSource
        private IAbstractSecondPassGroupingCollector<T> CreateSecondPassCollector<T>(IAbstractFirstPassGroupingCollector<T> firstPassGroupingCollector,
                                                                              string groupField,
                                                                              ICollection<SearchGroup<BytesRef>> searchGroups,
                                                                              Sort groupSort,
                                                                              Sort sortWithinGroup,
                                                                              int maxDocsPerGroup,
                                                                              bool getScores,
                                                                              bool getMaxScores,
                                                                              bool fillSortFields)
        {
            if (firstPassGroupingCollector.GetType().IsAssignableFrom(typeof(TermFirstPassGroupingCollector)))
            {
                return new TermSecondPassGroupingCollector(groupField, searchGroups, groupSort, sortWithinGroup, maxDocsPerGroup, getScores, getMaxScores, fillSortFields) 
                    as IAbstractSecondPassGroupingCollector<T>;
            }
            else
            {
                ValueSource vs = new BytesRefFieldSource(groupField);
                JCG.List<SearchGroup<MutableValue>> mvalSearchGroups = new JCG.List<SearchGroup<MutableValue>>(searchGroups.size());
                foreach (SearchGroup<BytesRef> mergedTopGroup in searchGroups)
                {
                    SearchGroup<MutableValue> sg = new SearchGroup<MutableValue>();
                    MutableValueStr groupValue = new MutableValueStr();
                    if (mergedTopGroup.GroupValue != null)
                    {
                        groupValue.Value = mergedTopGroup.GroupValue;
                    }
                    else
                    {
                        groupValue.Value = new BytesRef();
                        groupValue.Exists = false;
                    }
                    sg.GroupValue = groupValue;
                    sg.SortValues = mergedTopGroup.SortValues;
                    mvalSearchGroups.Add(sg);
                }

                return new FunctionSecondPassGroupingCollector<MutableValue>(mvalSearchGroups, groupSort, sortWithinGroup, maxDocsPerGroup, getScores, getMaxScores, fillSortFields, vs, new Hashtable())        // LUCENENET Specific type for generic must be specified.
                    as IAbstractSecondPassGroupingCollector<T>;
            }
        }

        private IAbstractAllGroupsCollector<T> CreateAllGroupsCollector<T>(IAbstractFirstPassGroupingCollector<T> firstPassGroupingCollector,
                                                                    string groupField)
        {
            if (firstPassGroupingCollector.GetType().IsAssignableFrom(typeof(TermFirstPassGroupingCollector)))
            {
                return new TermAllGroupsCollector(groupField) as IAbstractAllGroupsCollector<T>;
            }
            else
            {
                ValueSource vs = new BytesRefFieldSource(groupField);
                return new FunctionAllGroupsCollector<MutableValue>(vs, new Hashtable()) as IAbstractAllGroupsCollector<T>;        // LUCENENET Specific type for generic must be specified.
            }
        }

        private void CompareGroupValue<T>(string expected, IGroupDocs<T> group)
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
                else if ((group.GroupValue as BytesRef).Length == 0)
                {
                    return;
                }
                fail();
            }

            if (group.GroupValue.GetType().IsAssignableFrom(typeof(BytesRef)))
            {
                assertEquals<object>(new BytesRef(expected), group.GroupValue);
            }
            else if (group.GroupValue.GetType().IsAssignableFrom(typeof(MutableValueStr)))
            {
                MutableValueStr v = new MutableValueStr();
                v.Value = new BytesRef(expected);
                assertEquals<object>(v, group.GroupValue);
            }
            else
            {
                fail();
            }
        }

        private IEnumerable<ISearchGroup<BytesRef>> GetSearchGroups<T>(IAbstractFirstPassGroupingCollector<T> c, int groupOffset, bool fillFields)
        {
            if (typeof(TermFirstPassGroupingCollector).IsAssignableFrom(c.GetType()))
            {

                return (IEnumerable<ISearchGroup<BytesRef>>)c.GetTopGroups(groupOffset, fillFields);
            }
            else if (typeof(FunctionFirstPassGroupingCollector<MutableValue>).IsAssignableFrom(c.GetType()))        // LUCENENET Specific type for generic must be specified.
            {
                // LUCENENET NOTE: This is IEnumerable instead of ICollection because it 
                // needs to be covariant to mimic the wildcard generics in Java
                IEnumerable<ISearchGroup<MutableValue>> mutableValueGroups = ((FunctionFirstPassGroupingCollector<MutableValue>)c).GetTopGroups(groupOffset, fillFields);        // LUCENENET Specific type for generic must be specified.
                if (mutableValueGroups is null)
                {
                    return null;
                }

                JCG.List<SearchGroup<BytesRef>> groups = new JCG.List<SearchGroup<BytesRef>>(mutableValueGroups.Count());
                foreach (var mutableValueGroup in mutableValueGroups)
                {
                    SearchGroup<BytesRef> sg = new SearchGroup<BytesRef>();
                    sg.GroupValue = mutableValueGroup.GroupValue.Exists ? ((MutableValueStr)mutableValueGroup.GroupValue).Value : null;
                    sg.SortValues = mutableValueGroup.SortValues;
                    groups.Add(sg);
                }
                return groups;
            }
            fail();
            return null;
        }

        private ITopGroups<BytesRef> GetTopGroups<T>(IAbstractSecondPassGroupingCollector<T> c, int withinGroupOffset)
        {
            if (c.GetType().IsAssignableFrom(typeof(TermSecondPassGroupingCollector)))
            {
                return ((TermSecondPassGroupingCollector)c).GetTopGroups(withinGroupOffset);
            }
            else if (c.GetType().IsAssignableFrom(typeof(FunctionSecondPassGroupingCollector<MutableValue>)))        // LUCENENET Specific type for generic must be specified.
            {
                ITopGroups<MutableValue> mvalTopGroups = ((FunctionSecondPassGroupingCollector<MutableValue>)c).GetTopGroups(withinGroupOffset);        // LUCENENET Specific type for generic must be specified.
                JCG.List<GroupDocs<BytesRef>> groups = new JCG.List<GroupDocs<BytesRef>>(mvalTopGroups.Groups.Length);
                foreach (GroupDocs<MutableValue> mvalGd in mvalTopGroups.Groups)
                {
                    BytesRef groupValue = mvalGd.GroupValue.Exists ? ((MutableValueStr)mvalGd.GroupValue).Value : null;
                    groups.Add(new GroupDocs<BytesRef>(float.NaN, mvalGd.MaxScore, mvalGd.TotalHits, mvalGd.ScoreDocs, groupValue, mvalGd.GroupSortValues));
                }
                return new TopGroups<BytesRef>(mvalTopGroups.GroupSort, mvalTopGroups.WithinGroupSort, mvalTopGroups.TotalHitCount, mvalTopGroups.TotalGroupedHitCount, groups.ToArray(/*new GroupDocs[groups.size()]*/), float.NaN);
            }
            fail();
            return null;
        }

        internal class GroupDoc
        {
            internal readonly int id;
            internal readonly BytesRef group;
            internal readonly BytesRef sort1;
            internal readonly BytesRef sort2;
            // content must be "realN ..."
            internal readonly string content;
            internal float score;
            internal float score2;

            public GroupDoc(int id, BytesRef group, BytesRef sort1, BytesRef sort2, string content)
            {
                this.id = id;
                this.group = group;
                this.sort1 = sort1;
                this.sort2 = sort2;
                this.content = content;
            }
        }

        private Sort GetRandomSort()
        {
            JCG.List<SortField> sortFields = new JCG.List<SortField>();
            if (Random.nextInt(7) == 2)
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
            sortFields.Add(new SortField("id", SortFieldType.INT32));
            return new Sort(sortFields.ToArray(/*new SortField[sortFields.size()]*/));
        }

        private IComparer<GroupDoc> GetComparer(Sort sort)
        {
            SortField[] sortFields = sort.GetSort();
            return Comparer<GroupDoc>.Create((d1,d2) =>
            {
                foreach (SortField sf in sortFields)
                {
                    int cmp;
                    if (sf.Type == SortFieldType.SCORE)
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
                            cmp = 0;
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

        // LUCENENET NOTE: IComparable because in Java it was IComparable<?>, but note that
        // there is no reason it cannot be object here because nothing actually calls IComparable.Compare().
        private IComparable[] FillFields(GroupDoc d, Sort sort)
        {
            SortField[] sortFields = sort.GetSort();
            IComparable[] fields = new IComparable[sortFields.Length];
            for (int fieldIDX = 0; fieldIDX < sortFields.Length; fieldIDX++)
            {
                IComparable c;
                SortField sf = sortFields[fieldIDX];
                if (sf.Type == SortFieldType.SCORE)
                {
                    c = J2N.Numerics.Single.GetInstance(d.score);
                }
                else if (sf.Field.Equals("sort1", StringComparison.Ordinal))
                {
                    c = d.sort1;
                }
                else if (sf.Field.Equals("sort2", StringComparison.Ordinal))
                {
                    c = d.sort2;
                }
                else
                {
                    assertEquals("id", sf.Field);
                    c = J2N.Numerics.Int32.GetInstance(d.id);
                }
                fields[fieldIDX] = c;
            }
            return fields;
        }

        private string GroupToString(BytesRef b)
        {
            if (b is null)
            {
                return "null";
            }
            else
            {
                return b.Utf8ToString();
            }
        }

        private TopGroups<BytesRef> SlowGrouping(GroupDoc[] groupDocs,
                                                 string searchTerm,
                                                 bool fillFields,
                                                 bool getScores,
                                                 bool getMaxScores,
                                                 bool doAllGroups,
                                                 Sort groupSort,
                                                 Sort docSort,
                                                 int topNGroups,
                                                 int docsPerGroup,
                                                 int groupOffset,
                                                 int docOffset)
        {

            IComparer<GroupDoc> groupSortComp = GetComparer(groupSort);

            // LUCENENET specific: The original Java API Arrays.Sort does not currently exist.
            // This call ultimately results in calling TimSort, which is why this line was replaced
            // with ArrayUtil.TimSort(T[], IComparer<T>).
            //
            // NOTE: Array.Sort(comparer) won't work in this case because it calls the comparer when the
            // values are the same, which results in this test failing. TimSort only calls the comparer
            // when the values differ.
            //Arrays.Sort(groupDocs, groupSortComp);
            ArrayUtil.TimSort(groupDocs, groupSortComp);
            
            IDictionary<BytesRef, IList<GroupDoc>> groups = new JCG.Dictionary<BytesRef, IList<GroupDoc>>();
            IList<BytesRef> sortedGroups = new JCG.List<BytesRef>();
            IList<IComparable[]> sortedGroupFields = new JCG.List<IComparable[]>();

            int totalHitCount = 0;
            ISet<BytesRef> knownGroups = new JCG.HashSet<BytesRef>();

            //Console.WriteLine("TEST: slowGrouping");
            foreach (GroupDoc d in groupDocs)
            {
                // TODO: would be better to filter by searchTerm before sorting!
                if (!d.content.StartsWith(searchTerm, StringComparison.Ordinal))
                {
                    continue;
                }
                totalHitCount++;
                //Console.WriteLine("  match id=" + d.id + " score=" + d.score);

                if (doAllGroups)
                {
                    if (!knownGroups.contains(d.group))
                    {
                        knownGroups.add(d.group);
                        //Console.WriteLine("    add group=" + groupToString(d.group));
                    }
                }

                if (!groups.TryGetValue(d.group, out IList<GroupDoc> l) || l is null)
                {
                    //Console.WriteLine("    add sortedGroup=" + groupToString(d.group));
                    sortedGroups.Add(d.group);
                    if (fillFields)
                    {
                        sortedGroupFields.Add(FillFields(d, groupSort));
                    }
                    l = new JCG.List<GroupDoc>();
                    groups[d.group] = l;
                }
                l.Add(d);
            }

            if (groupOffset >= sortedGroups.size())
            {
                // slice is out of bounds
                return null;
            }

            int limit = Math.Min(groupOffset + topNGroups, groups.size());

            IComparer<GroupDoc> docSortComp = GetComparer(docSort);

            GroupDocs<BytesRef>[] result = new GroupDocs<BytesRef>[limit - groupOffset];
            int totalGroupedHitCount = 0;
            for (int idx = groupOffset; idx < limit; idx++)
            {
                BytesRef group = sortedGroups[idx];
                IList<GroupDoc> docs = groups[group];
                totalGroupedHitCount += docs.size();

                // LUCENENET specific: The original API Collections.Sort does not currently exist.
                // This call ultimately results in calling TimSort, which is why this line was replaced
                // with CollectionUtil.TimSort(IList<T>, IComparer<T>).
                //
                // NOTE: List.Sort(comparer) won't work in this case because it calls the comparer when the
                // values are the same, which results in this test failing. TimSort only calls the comparer
                // when the values differ.
                //Collections.Sort(docs, docSortComp);
                CollectionUtil.TimSort(docs, docSortComp);
                ScoreDoc[] hits;
                if (docs.size() > docOffset)
                {
                    int docIDXLimit = Math.Min(docOffset + docsPerGroup, docs.size());
                    hits = new ScoreDoc[docIDXLimit - docOffset];
                    for (int docIDX = docOffset; docIDX < docIDXLimit; docIDX++)
                    {
                        GroupDoc d = docs[docIDX];
                        FieldDoc fd;
                        if (fillFields)
                        {
                            fd = new FieldDoc(d.id, getScores ? d.score : float.NaN, FillFields(d, docSort));
                        }
                        else
                        {
                            fd = new FieldDoc(d.id, getScores ? d.score : float.NaN);
                        }
                        hits[docIDX - docOffset] = fd;
                    }
                }
                else
                {
                    hits = new ScoreDoc[0];
                }

                result[idx - groupOffset] = new GroupDocs<BytesRef>(float.NaN,
                                                                  0.0f,
                                                                  docs.size(),
                                                                  hits,
                                                                  group,
                                                                  fillFields ? sortedGroupFields[idx] : null);
            }

            if (doAllGroups)
            {
                return new TopGroups<BytesRef>(
                                               new TopGroups<BytesRef>(groupSort.GetSort(), docSort.GetSort(), totalHitCount, totalGroupedHitCount, result, float.NaN),
                                               knownGroups.size()
                );
            }
            else
            {
                return new TopGroups<BytesRef>(groupSort.GetSort(), docSort.GetSort(), totalHitCount, totalGroupedHitCount, result, float.NaN);
            }
        }

        private DirectoryReader GetDocBlockReader(Directory dir, GroupDoc[] groupDocs)
        {
            // Coalesce by group, but in random order:
            groupDocs.Shuffle(Random);
            IDictionary<BytesRef, IList<GroupDoc>> groupMap = new JCG.Dictionary<BytesRef, IList<GroupDoc>>();
            IList<BytesRef> groupValues = new JCG.List<BytesRef>();

            foreach (GroupDoc groupDoc in groupDocs)
            {
                if (!groupMap.TryGetValue(groupDoc.group, out IList<GroupDoc> docs))
                {
                    groupValues.Add(groupDoc.group);
                    groupMap[groupDoc.group] = docs = new JCG.List<GroupDoc>();
                }
                docs.Add(groupDoc);
            }

            RandomIndexWriter w = new RandomIndexWriter(
                                                        Random,
                                                        dir,
                                                        NewIndexWriterConfig(TEST_VERSION_CURRENT,
                                                                             new MockAnalyzer(Random)));

            IList<IList<Document>> updateDocs = new JCG.List<IList<Document>>();

            FieldType groupEndType = new FieldType(StringField.TYPE_NOT_STORED);
            groupEndType.IndexOptions = (IndexOptions.DOCS_ONLY);
            groupEndType.OmitNorms = (true);

            //Console.WriteLine("TEST: index groups");
            foreach (BytesRef group in groupValues)
            {
                IList<Document> docs = new JCG.List<Document>();
                //Console.WriteLine("TEST:   group=" + (group is null ? "null" : group.utf8ToString()));
                foreach (GroupDoc groupValue in groupMap[group])
                {
                    Document doc = new Document();
                    docs.Add(doc);
                    if (groupValue.group != null)
                    {
                        doc.Add(NewStringField("group", groupValue.group.Utf8ToString(), Field.Store.NO));
                    }
                    doc.Add(NewStringField("sort1", groupValue.sort1.Utf8ToString(), Field.Store.NO));
                    doc.Add(NewStringField("sort2", groupValue.sort2.Utf8ToString(), Field.Store.NO));
                    doc.Add(new Int32Field("id", groupValue.id, Field.Store.NO));
                    doc.Add(NewTextField("content", groupValue.content, Field.Store.NO));
                    //Console.WriteLine("TEST:     doc content=" + groupValue.content + " group=" + (groupValue.group is null ? "null" : groupValue.group.utf8ToString()) + " sort1=" + groupValue.sort1.utf8ToString() + " id=" + groupValue.id);
                }
                // So we can pull filter marking last doc in block:
                Field groupEnd = NewField("groupend", "x", groupEndType);
                docs[docs.size() - 1].Add(groupEnd);
                // Add as a doc block:
                w.AddDocuments(docs);
                if (group != null && Random.nextInt(7) == 4)
                {
                    updateDocs.Add(docs);
                }
            }

            foreach (IList<Document> docs in updateDocs)
            {
                // Just replaces docs w/ same docs:
                w.UpdateDocuments(new Index.Term("group", docs[0].Get("group")), docs);
            }

            DirectoryReader r = w.GetReader();
            w.Dispose();

            return r;
        }

        internal class ShardState
        {

            public readonly ShardSearcher[] subSearchers;
            public readonly int[] docStarts;

            public ShardState(IndexSearcher s)
            {
                IndexReaderContext ctx = s.TopReaderContext;
                IList<AtomicReaderContext> leaves = ctx.Leaves;
                subSearchers = new ShardSearcher[leaves.size()];
                for (int searcherIDX = 0; searcherIDX < subSearchers.Length; searcherIDX++)
                {
                    subSearchers[searcherIDX] = new ShardSearcher(leaves[searcherIDX], ctx);
                }

                docStarts = new int[subSearchers.Length];
                for (int subIDX = 0; subIDX < docStarts.Length; subIDX++)
                {
                    docStarts[subIDX] = leaves[subIDX].DocBase;
                    //Console.WriteLine("docStarts[" + subIDX + "]=" + docStarts[subIDX]);
                }
            }
        }

        [Test]
        public virtual void TestRandom()
        {
            int numberOfRuns = TestUtil.NextInt32(Random, 3, 6);
            for (int iter = 0; iter < numberOfRuns; iter++)
            {
                if (Verbose)
                {
                    Console.WriteLine("TEST: iter=" + iter);
                }

                int numDocs = TestUtil.NextInt32(Random, 100, 1000) * RandomMultiplier;
                //final int numDocs = TestUtil.nextInt(random, 5, 20);

                int numGroups = TestUtil.NextInt32(Random, 1, numDocs);


                if (Verbose)
                {
                    Console.WriteLine("TEST: numDocs=" + numDocs + " numGroups=" + numGroups);
                }

                IList<BytesRef> groups = new JCG.List<BytesRef>();
                for (int i = 0; i < numGroups; i++)
                {
                    string randomValue;
                    do
                    {
                        // B/c of DV based impl we can't see the difference between an empty string and a null value.
                        // For that reason we don't generate empty string
                        // groups.
                        randomValue = TestUtil.RandomRealisticUnicodeString(Random);
                        //randomValue = TestUtil.RandomSimpleString(Random());
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

                Document doc = new Document();
                Document docNoGroup = new Document();
                Field idvGroupField = new SortedDocValuesField("group_dv", new BytesRef());
                if (canUseIDV)
                {
                    doc.Add(idvGroupField);
                    docNoGroup.Add(idvGroupField);
                }

                Field group = NewStringField("group", "", Field.Store.NO);
                doc.Add(group);
                Field sort1 = NewStringField("sort1", "", Field.Store.NO);
                doc.Add(sort1);
                docNoGroup.Add(sort1);
                Field sort2 = NewStringField("sort2", "", Field.Store.NO);
                doc.Add(sort2);
                docNoGroup.Add(sort2);
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
                    GroupDoc groupDoc = new GroupDoc(i,
                                                           groupValue,
                                                           groups[Random.nextInt(groups.size())],
                                                           groups[Random.nextInt(groups.size())],
                                                           contentStrings[Random.nextInt(contentStrings.Length)]);
                    if (Verbose)
                    {
                        Console.WriteLine("  doc content=" + groupDoc.content + " id=" + i + " group=" + (groupDoc.group is null ? "null" : groupDoc.group.Utf8ToString()) + " sort1=" + groupDoc.sort1.Utf8ToString() + " sort2=" + groupDoc.sort2.Utf8ToString());
                    }

                    groupDocs[i] = groupDoc;
                    if (groupDoc.group != null)
                    {
                        group.SetStringValue(groupDoc.group.Utf8ToString());
                        if (canUseIDV)
                        {
                            idvGroupField.SetBytesValue(BytesRef.DeepCopyOf(groupDoc.group));
                        }
                    }
                    else if (canUseIDV)
                    {
                        // Must explicitly set empty string, else eg if
                        // the segment has all docs missing the field then
                        // we get null back instead of empty BytesRef:
                        idvGroupField.SetBytesValue(new BytesRef());
                    }
                    sort1.SetStringValue(groupDoc.sort1.Utf8ToString());
                    sort2.SetStringValue(groupDoc.sort2.Utf8ToString());
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

                GroupDoc[] groupDocsByID = new GroupDoc[groupDocs.Length];
                Arrays.Copy(groupDocs, 0, groupDocsByID, 0, groupDocs.Length);

                DirectoryReader r = w.GetReader();
                w.Dispose();

                // NOTE: intentional but temporary field cache insanity!
                FieldCache.Int32s docIDToID = FieldCache.DEFAULT.GetInt32s(SlowCompositeReaderWrapper.Wrap(r), "id", false);
                DirectoryReader rBlocks = null;
                Directory dirBlocks = null;

                try
                {
                    IndexSearcher s = NewSearcher(r);
                    if (Verbose)
                    {
                        Console.WriteLine("\nTEST: searcher=" + s);
                    }

                    if (typeof(SlowCompositeReaderWrapper).IsAssignableFrom(s.IndexReader.GetType()))
                    {
                        canUseIDV = false;
                    }
                    else
                    {
                        canUseIDV = !preFlex;
                    }
                    ShardState shards = new ShardState(s);

                    for (int contentID = 0; contentID < 3; contentID++)
                    {
                        ScoreDoc[] hits = s.Search(new TermQuery(new Index.Term("content", "real" + contentID)), numDocs).ScoreDocs;
                        foreach (ScoreDoc hit in hits)
                        {
                            GroupDoc gd = groupDocs[docIDToID.Get(hit.Doc)];
                            assertTrue(gd.score == 0.0);
                            gd.score = hit.Score;
                            assertEquals(gd.id, docIDToID.Get(hit.Doc));
                        }
                    }

                    foreach (GroupDoc gd in groupDocs)
                    {
                        assertTrue(gd.score != 0.0);
                    }

                    // Build 2nd index, where docs are added in blocks by
                    // group, so we can use single pass collector
                    dirBlocks = NewDirectory();
                    rBlocks = GetDocBlockReader(dirBlocks, groupDocs);
                    Filter lastDocInBlock = new CachingWrapperFilter(new QueryWrapperFilter(new TermQuery(new Term("groupend", "x"))));
                    FieldCache.Int32s docIDToIDBlocks = FieldCache.DEFAULT.GetInt32s(SlowCompositeReaderWrapper.Wrap(rBlocks), "id", false);

                    IndexSearcher sBlocks = NewSearcher(rBlocks);
                    ShardState shardsBlocks = new ShardState(sBlocks);

                    // ReaderBlocks only increases maxDoc() vs reader, which
                    // means a monotonic shift in scores, so we can
                    // reliably remap them w/ Map:
                    IDictionary<string, IDictionary<float, J2N.Numerics.Single>> scoreMap = new Dictionary<string, IDictionary<float, J2N.Numerics.Single>>();

                    // Tricky: must separately set .score2, because the doc
                    // block index was created with possible deletions!
                    //Console.WriteLine("fixup score2");
                    for (int contentID = 0; contentID < 3; contentID++)
                    {
                        //Console.WriteLine("  term=real" + contentID);
                        IDictionary<float, J2N.Numerics.Single> termScoreMap = new Dictionary<float, J2N.Numerics.Single>();
                        scoreMap["real" + contentID] = termScoreMap;
                        //Console.WriteLine("term=real" + contentID + " dfold=" + s.docFreq(new Term("content", "real"+contentID)) +
                        //" dfnew=" + sBlocks.docFreq(new Term("content", "real"+contentID)));
                        ScoreDoc[] hits = sBlocks.Search(new TermQuery(new Term("content", "real" + contentID)), numDocs).ScoreDocs;
                        foreach (ScoreDoc hit in hits)
                        {
                            GroupDoc gd = groupDocsByID[docIDToIDBlocks.Get(hit.Doc)];
                            assertTrue(gd.score2 == 0.0);
                            gd.score2 = hit.Score;
                            assertEquals(gd.id, docIDToIDBlocks.Get(hit.Doc));
                            //Console.WriteLine("    score=" + gd.score + " score2=" + hit.score + " id=" + docIDToIDBlocks.get(hit.doc));
                            termScoreMap[gd.score] = gd.score2;
                        }
                    }

                    for (int searchIter = 0; searchIter < 100; searchIter++)
                    {

                        if (Verbose)
                        {
                            Console.WriteLine("\nTEST: searchIter=" + searchIter);
                        }

                        string searchTerm = "real" + Random.nextInt(3);
                        bool fillFields = Random.nextBoolean();
                        bool getScores = Random.nextBoolean();
                        bool getMaxScores = Random.nextBoolean();
                        Sort groupSort = GetRandomSort();
                        //Sort groupSort = new Sort(new SortField[] {new SortField("sort1", SortField.Type_e.STRING), new SortField("id", SortField.Type_e.INT)});
                        // TODO: also test null (= sort by relevance)
                        Sort docSort = GetRandomSort();

                        foreach (SortField sf in docSort.GetSort())
                        {
                            if (sf.Type == SortFieldType.SCORE)
                            {
                                getScores = true;
                                break;
                            }
                        }

                        foreach (SortField sf in groupSort.GetSort())
                        {
                            if (sf.Type == SortFieldType.SCORE)
                            {
                                getScores = true;
                                break;
                            }
                        }

                        int topNGroups = TestUtil.NextInt32(Random, 1, 30);
                        // int topNGroups = 10;
                        int docsPerGroup = TestUtil.NextInt32(Random, 1, 50);

                        int groupOffset = TestUtil.NextInt32(Random, 0, (topNGroups - 1) / 2);
                        // int groupOffset = 0;

                        int docOffset = TestUtil.NextInt32(Random, 0, docsPerGroup - 1);
                        // int docOffset = 0;

                        bool doCache = Random.nextBoolean();
                        bool doAllGroups = Random.nextBoolean();
                        if (Verbose)
                        {
                            Console.WriteLine("TEST: groupSort=" + groupSort + " docSort=" + docSort + " searchTerm=" + searchTerm + " dF=" + r.DocFreq(new Term("content", searchTerm)) + " dFBlock=" + rBlocks.DocFreq(new Term("content", searchTerm)) + " topNGroups=" + topNGroups + " groupOffset=" + groupOffset + " docOffset=" + docOffset + " doCache=" + doCache + " docsPerGroup=" + docsPerGroup + " doAllGroups=" + doAllGroups + " getScores=" + getScores + " getMaxScores=" + getMaxScores);
                        }

                        string groupField = "group";
                        if (canUseIDV && Random.nextBoolean())
                        {
                            groupField += "_dv";
                        }
                        if (Verbose)
                        {
                            Console.WriteLine("  groupField=" + groupField);
                        }
                        IAbstractFirstPassGroupingCollector<object> c1 = CreateRandomFirstPassCollector(groupField, groupSort, groupOffset + topNGroups);
                        CachingCollector cCache;
                        ICollector c;

                        IAbstractAllGroupsCollector<object> allGroupsCollector;
                        if (doAllGroups)
                        {
                            allGroupsCollector = CreateAllGroupsCollector(c1, groupField);
                        }
                        else
                        {
                            allGroupsCollector = null;
                        }

                        bool useWrappingCollector = Random.nextBoolean();

                        if (doCache)
                        {
                            double maxCacheMB = Random.NextDouble();
                            if (Verbose)
                            {
                                Console.WriteLine("TEST: maxCacheMB=" + maxCacheMB);
                            }

                            if (useWrappingCollector)
                            {
                                if (doAllGroups)
                                {
                                    cCache = CachingCollector.Create(c1, true, maxCacheMB);
                                    c = MultiCollector.Wrap(cCache, allGroupsCollector);
                                }
                                else
                                {
                                    c = cCache = CachingCollector.Create(c1, true, maxCacheMB);
                                }
                            }
                            else
                            {
                                // Collect only into cache, then replay multiple times:
                                c = cCache = CachingCollector.Create(false, true, maxCacheMB);
                            }
                        }
                        else
                        {
                            cCache = null;
                            if (doAllGroups)
                            {
                                c = MultiCollector.Wrap(c1, allGroupsCollector);
                            }
                            else
                            {
                                c = c1;
                            }
                        }

                        // Search top reader:
                        Query query = new TermQuery(new Term("content", searchTerm));

                        s.Search(query, c);

                        if (doCache && !useWrappingCollector)
                        {
                            if (cCache.IsCached)
                            {
                                // Replay for first-pass grouping
                                cCache.Replay(c1);
                                if (doAllGroups)
                                {
                                    // Replay for all groups:
                                    cCache.Replay(allGroupsCollector);
                                }
                            }
                            else
                            {
                                // Replay by re-running search:
                                s.Search(query, c1);
                                if (doAllGroups)
                                {
                                    s.Search(query, allGroupsCollector);
                                }
                            }
                        }

                        // Get 1st pass top groups
                        // LUCENENET NOTE: This is IEnumerable rather than ICollection because we need it to be
                        // covariant in order to mimic Java's wildcard generics
                        IEnumerable<ISearchGroup<BytesRef>> topGroups = GetSearchGroups(c1, groupOffset, fillFields);
                        ITopGroups<BytesRef> groupsResult;
                        if (Verbose)
                        {
                            Console.WriteLine("TEST: first pass topGroups");
                            if (topGroups is null)
                            {
                                Console.WriteLine("  null");
                            }
                            else
                            {
                                foreach (SearchGroup<BytesRef> searchGroup in topGroups)
                                {
                                    Console.WriteLine("  " + (searchGroup.GroupValue is null ? "null" : searchGroup.GroupValue.Utf8ToString()) + ": " + Arrays.ToString(searchGroup.SortValues));
                                }
                            }
                        }

                        // Get 1st pass top groups using shards

                        ValueHolder<bool> idvBasedImplsUsedSharded = new ValueHolder<bool>(false);
                        TopGroups<BytesRef> topGroupsShards = SearchShards(s, shards.subSearchers, query, groupSort, docSort,
                           groupOffset, topNGroups, docOffset, docsPerGroup, getScores, getMaxScores, canUseIDV, preFlex, idvBasedImplsUsedSharded);
                        IAbstractSecondPassGroupingCollector<object> c2;
                        if (topGroups != null)
                        {

                            if (Verbose)
                            {
                                Console.WriteLine("TEST: topGroups");
                                foreach (SearchGroup<BytesRef> searchGroup in topGroups)
                                {
                                    Console.WriteLine("  " + (searchGroup.GroupValue is null ? "null" : searchGroup.GroupValue.Utf8ToString()) + ": " + Arrays.ToString(searchGroup.SortValues));
                                }
                            }

                            c2 = CreateSecondPassCollector(c1, groupField, groupSort, docSort, groupOffset, docOffset + docsPerGroup, getScores, getMaxScores, fillFields);
                            if (doCache)
                            {
                                if (cCache.IsCached)
                                {
                                    if (Verbose)
                                    {
                                        Console.WriteLine("TEST: cache is intact");
                                    }
                                    cCache.Replay(c2);
                                }
                                else
                                {
                                    if (Verbose)
                                    {
                                        Console.WriteLine("TEST: cache was too large");
                                    }
                                    s.Search(query, c2);
                                }
                            }
                            else
                            {
                                s.Search(query, c2);
                            }

                            if (doAllGroups)
                            {
                                ITopGroups<BytesRef> tempTopGroups = GetTopGroups(c2, docOffset);
                                groupsResult = new TopGroups<BytesRef>(tempTopGroups, allGroupsCollector.GroupCount);
                            }
                            else
                            {
                                groupsResult = GetTopGroups(c2, docOffset);
                            }
                        }
                        else
                        {
                            c2 = null;
                            groupsResult = null;
                            if (Verbose)
                            {
                                Console.WriteLine("TEST:   no results");
                            }
                        }

                        TopGroups<BytesRef> expectedGroups = SlowGrouping(groupDocs, searchTerm, fillFields, getScores, getMaxScores, doAllGroups, groupSort, docSort, topNGroups, docsPerGroup, groupOffset, docOffset);

                        if (Verbose)
                        {
                            if (expectedGroups is null)
                            {
                                Console.WriteLine("TEST: no expected groups");
                            }
                            else
                            {
                                Console.WriteLine("TEST: expected groups totalGroupedHitCount=" + expectedGroups.TotalGroupedHitCount);
                                foreach (IGroupDocs<BytesRef> gd in expectedGroups.Groups)
                                {
                                    Console.WriteLine("  group=" + (gd.GroupValue is null ? "null" : gd.GroupValue.Utf8ToString()) + " totalHits=" + gd.TotalHits + " scoreDocs.len=" + gd.ScoreDocs.Length);
                                    foreach (ScoreDoc sd in gd.ScoreDocs)
                                    {
                                        Console.WriteLine("    id=" + sd.Doc + " score=" + sd.Score);
                                    }
                                }
                            }

                            if (groupsResult is null)
                            {
                                Console.WriteLine("TEST: no matched groups");
                            }
                            else
                            {
                                Console.WriteLine("TEST: matched groups totalGroupedHitCount=" + groupsResult.TotalGroupedHitCount);
                                foreach (GroupDocs<BytesRef> gd in groupsResult.Groups)
                                {
                                    Console.WriteLine("  group=" + (gd.GroupValue is null ? "null" : gd.GroupValue.Utf8ToString()) + " totalHits=" + gd.TotalHits);
                                    foreach (ScoreDoc sd in gd.ScoreDocs)
                                    {
                                        Console.WriteLine("    id=" + docIDToID.Get(sd.Doc) + " score=" + sd.Score);
                                    }
                                }

                                if (searchIter == 14)
                                {
                                    for (int docIDX = 0; docIDX < s.IndexReader.MaxDoc; docIDX++)
                                    {
                                        Console.WriteLine("ID=" + docIDToID.Get(docIDX) + " explain=" + s.Explain(query, docIDX));
                                    }
                                }
                            }

                            if (topGroupsShards is null)
                            {
                                Console.WriteLine("TEST: no matched-merged groups");
                            }
                            else
                            {
                                Console.WriteLine("TEST: matched-merged groups totalGroupedHitCount=" + topGroupsShards.TotalGroupedHitCount);
                                foreach (GroupDocs<BytesRef> gd in topGroupsShards.Groups)
                                {
                                    Console.WriteLine("  group=" + (gd.GroupValue is null ? "null" : gd.GroupValue.Utf8ToString()) + " totalHits=" + gd.TotalHits);
                                    foreach (ScoreDoc sd in gd.ScoreDocs)
                                    {
                                        Console.WriteLine("    id=" + docIDToID.Get(sd.Doc) + " score=" + sd.Score);
                                    }
                                }
                            }
                        }

                        AssertEquals(docIDToID, expectedGroups, groupsResult, true, true, true, getScores, groupField.EndsWith("_dv", StringComparison.Ordinal));

                        // Confirm merged shards match:
                        AssertEquals(docIDToID, expectedGroups, topGroupsShards, true, false, fillFields, getScores, idvBasedImplsUsedSharded.value);
                        if (topGroupsShards != null)
                        {
                            VerifyShards(shards.docStarts, topGroupsShards);
                        }

                        bool needsScores = getScores || getMaxScores || docSort is null;
                        BlockGroupingCollector c3 = new BlockGroupingCollector(groupSort, groupOffset + topNGroups, needsScores, lastDocInBlock);
                        TermAllGroupsCollector allGroupsCollector2;
                        ICollector c4;
                        if (doAllGroups)
                        {
                            // NOTE: must be "group" and not "group_dv"
                            // (groupField) because we didn't index doc
                            // values in the block index:
                            allGroupsCollector2 = new TermAllGroupsCollector("group");
                            c4 = MultiCollector.Wrap(c3, allGroupsCollector2);
                        }
                        else
                        {
                            allGroupsCollector2 = null;
                            c4 = c3;
                        }
                        // Get block grouping result:
                        sBlocks.Search(query, c4);
                        TopGroups<BytesRef> tempTopGroupsBlocks = (TopGroups<BytesRef>)c3.GetTopGroups<BytesRef>(docSort, groupOffset, docOffset, docOffset + docsPerGroup, fillFields);
                        TopGroups<BytesRef> groupsResultBlocks;
                        if (doAllGroups && tempTopGroupsBlocks != null)
                        {
                            assertEquals((int)tempTopGroupsBlocks.TotalGroupCount, allGroupsCollector2.GroupCount);
                            groupsResultBlocks = new TopGroups<BytesRef>(tempTopGroupsBlocks, allGroupsCollector2.GroupCount);
                        }
                        else
                        {
                            groupsResultBlocks = tempTopGroupsBlocks;
                        }

                        if (Verbose)
                        {
                            if (groupsResultBlocks is null)
                            {
                                Console.WriteLine("TEST: no block groups");
                            }
                            else
                            {
                                Console.WriteLine("TEST: block groups totalGroupedHitCount=" + groupsResultBlocks.TotalGroupedHitCount);
                                bool first = true;
                                foreach (GroupDocs<BytesRef> gd in groupsResultBlocks.Groups)
                                {
                                    Console.WriteLine("  group=" + (gd.GroupValue is null ? "null" : gd.GroupValue.Utf8ToString()) + " totalHits=" + gd.TotalHits);
                                    foreach (ScoreDoc sd in gd.ScoreDocs)
                                    {
                                        Console.WriteLine("    id=" + docIDToIDBlocks.Get(sd.Doc) + " score=" + sd.Score);
                                        if (first)
                                        {
                                            Console.WriteLine("explain: " + sBlocks.Explain(query, sd.Doc));
                                            first = false;
                                        }
                                    }
                                }
                            }
                        }

                        // Get shard'd block grouping result:
                        // Block index does not index DocValues so we pass
                        // false for canUseIDV:
                        TopGroups<BytesRef> topGroupsBlockShards = SearchShards(sBlocks, shardsBlocks.subSearchers, query,
                           groupSort, docSort, groupOffset, topNGroups, docOffset, docsPerGroup, getScores, getMaxScores, false, false, new ValueHolder<bool>(false));

                        if (expectedGroups != null)
                        {
                            // Fixup scores for reader2
                            foreach (var groupDocsHits in expectedGroups.Groups)
                            {
                                foreach (ScoreDoc hit in groupDocsHits.ScoreDocs)
                                {
                                    GroupDoc gd = groupDocsByID[hit.Doc];
                                    assertEquals(gd.id, hit.Doc);
                                    //Console.WriteLine("fixup score " + hit.score + " to " + gd.score2 + " vs " + gd.score);
                                    hit.Score = gd.score2;
                                }
                            }

                            SortField[] sortFields = groupSort.GetSort();
                            IDictionary<float, J2N.Numerics.Single> termScoreMap = scoreMap[searchTerm];
                            for (int groupSortIDX = 0; groupSortIDX < sortFields.Length; groupSortIDX++)
                            {
                                if (sortFields[groupSortIDX].Type == SortFieldType.SCORE)
                                {
                                    foreach (var groupDocsHits in expectedGroups.Groups)
                                    {
                                        if (groupDocsHits.GroupSortValues != null)
                                        {
                                            //Console.WriteLine("remap " + groupDocsHits.groupSortValues[groupSortIDX] + " to " + termScoreMap.get(groupDocsHits.groupSortValues[groupSortIDX]));
                                            groupDocsHits.GroupSortValues[groupSortIDX] = termScoreMap[Convert.ToSingle(groupDocsHits.GroupSortValues[groupSortIDX], CultureInfo.InvariantCulture)];
                                            assertNotNull(groupDocsHits.GroupSortValues[groupSortIDX]);
                                        }
                                    }
                                }
                            }

                            SortField[] docSortFields = docSort.GetSort();
                            for (int docSortIDX = 0; docSortIDX < docSortFields.Length; docSortIDX++)
                            {
                                if (docSortFields[docSortIDX].Type == SortFieldType.SCORE)
                                {
                                    foreach (var groupDocsHits in expectedGroups.Groups)
                                    {
                                        foreach (ScoreDoc _hit in groupDocsHits.ScoreDocs)
                                        {
                                            FieldDoc hit = (FieldDoc)_hit;
                                            if (hit.Fields != null)
                                            {
                                                hit.Fields[docSortIDX] = termScoreMap[Convert.ToSingle(hit.Fields[docSortIDX], CultureInfo.InvariantCulture)];
                                                assertNotNull(hit.Fields[docSortIDX]);
                                            }
                                        }
                                    }
                                }
                            }
                        }

                        AssertEquals(docIDToIDBlocks, expectedGroups, groupsResultBlocks, false, true, true, getScores, false);
                        AssertEquals(docIDToIDBlocks, expectedGroups, topGroupsBlockShards, false, false, fillFields, getScores, false);
                    }
                }
                finally
                {
                    QueryUtils.PurgeFieldCache(r);
                    if (rBlocks != null)
                    {
                        QueryUtils.PurgeFieldCache(rBlocks);
                    }
                }

                r.Dispose();
                dir.Dispose();

                rBlocks.Dispose();
                dirBlocks.Dispose();
            }
        }

        private void VerifyShards(int[] docStarts, ITopGroups<BytesRef> topGroups)
        {
            foreach (var group in topGroups.Groups)
            {
                for (int hitIDX = 0; hitIDX < group.ScoreDocs.Length; hitIDX++)
                {
                    ScoreDoc sd = group.ScoreDocs[hitIDX];
                    assertEquals("doc=" + sd.Doc + " wrong shard",
                                 ReaderUtil.SubIndex(sd.Doc, docStarts),
                                 sd.ShardIndex);
                }
            }
        }

        private TopGroups<BytesRef> SearchShards(IndexSearcher topSearcher, ShardSearcher[] subSearchers, Query query, Sort groupSort, Sort docSort, int groupOffset, int topNGroups, int docOffset,
                                                 int topNDocs, bool getScores, bool getMaxScores, bool canUseIDV, bool preFlex, ValueHolder<bool> usedIdvBasedImpl)
        {

            // TODO: swap in caching, all groups collector hereassertEquals(expected.totalHitCount, actual.totalHitCount);
            // too...
            if (Verbose)
            {
                Console.WriteLine("TEST: " + subSearchers.Length + " shards: " + Arrays.ToString(subSearchers) + " canUseIDV=" + canUseIDV);
            }
            // Run 1st pass collector to get top groups per shard
            Weight w = topSearcher.CreateNormalizedWeight(query);
            IList<IEnumerable<ISearchGroup<BytesRef>>> shardGroups = new JCG.List<IEnumerable<ISearchGroup<BytesRef>>>();
            IList<IAbstractFirstPassGroupingCollector<object>> firstPassGroupingCollectors = new JCG.List<IAbstractFirstPassGroupingCollector<object>>();
            IAbstractFirstPassGroupingCollector<object> firstPassCollector = null;
            bool shardsCanUseIDV;
            if (canUseIDV)
            {
                if (typeof(SlowCompositeReaderWrapper).IsAssignableFrom(subSearchers[0].IndexReader.GetType()))
                {
                    shardsCanUseIDV = false;
                }
                else
                {
                    shardsCanUseIDV = !preFlex;
                }
            }
            else
            {
                shardsCanUseIDV = false;
            }

            string groupField = "group";
            if (shardsCanUseIDV && Random.nextBoolean())
            {
                groupField += "_dv";
                usedIdvBasedImpl.value = true;
            }

            for (int shardIDX = 0; shardIDX < subSearchers.Length; shardIDX++)
            {

                // First shard determines whether we use IDV or not;
                // all other shards match that:
                if (firstPassCollector is null)
                {
                    firstPassCollector = CreateRandomFirstPassCollector(groupField, groupSort, groupOffset + topNGroups);
                }
                else
                {
                    firstPassCollector = CreateFirstPassCollector(groupField, groupSort, groupOffset + topNGroups, firstPassCollector);
                }
                if (Verbose)
                {
                    Console.WriteLine("  shard=" + shardIDX + " groupField=" + groupField);
                    Console.WriteLine("    1st pass collector=" + firstPassCollector);
                }
                firstPassGroupingCollectors.Add(firstPassCollector);
                subSearchers[shardIDX].Search(w, firstPassCollector);
                IEnumerable<ISearchGroup<BytesRef>> topGroups = GetSearchGroups(firstPassCollector, 0, true);
                if (topGroups != null)
                {
                    if (Verbose)
                    {
                        Console.WriteLine("  shard " + shardIDX + " s=" + subSearchers[shardIDX] + " totalGroupedHitCount=?" + " " + topGroups.Count() + " groups:");
                        foreach (SearchGroup<BytesRef> group in topGroups)
                        {
                            Console.WriteLine("    " + GroupToString(group.GroupValue) + " groupSort=" + Arrays.ToString(group.SortValues));
                        }
                    }
                    shardGroups.Add(topGroups);
                }
            }

            ICollection<SearchGroup<BytesRef>> mergedTopGroups = SearchGroup.Merge(shardGroups, groupOffset, topNGroups, groupSort);
            if (Verbose)
            {
                Console.WriteLine(" top groups merged:");
                if (mergedTopGroups is null)
                {
                    Console.WriteLine("    null");
                }
                else
                {
                    Console.WriteLine("    " + mergedTopGroups.size() + " top groups:");
                    foreach (SearchGroup<BytesRef> group in mergedTopGroups)
                    {
                        Console.WriteLine("    [" + GroupToString(group.GroupValue) + "] groupSort=" + Arrays.ToString(group.SortValues));
                    }
                }
            }

            if (mergedTopGroups != null)
            {
                // Now 2nd pass:
                ITopGroups<BytesRef>[] shardTopGroups = new ITopGroups<BytesRef>[subSearchers.Length];
                for (int shardIDX = 0; shardIDX < subSearchers.Length; shardIDX++)
                {
                    IAbstractSecondPassGroupingCollector<object> secondPassCollector = CreateSecondPassCollector(firstPassGroupingCollectors[shardIDX],
                        groupField, mergedTopGroups, groupSort, docSort, docOffset + topNDocs, getScores, getMaxScores, true);
                    subSearchers[shardIDX].Search(w, secondPassCollector);
                    shardTopGroups[shardIDX] = GetTopGroups(secondPassCollector, 0);
                    if (Verbose)
                    {
                        Console.WriteLine(" " + shardTopGroups[shardIDX].Groups.Length + " shard[" + shardIDX + "] groups:");
                        foreach (GroupDocs<BytesRef> group in shardTopGroups[shardIDX].Groups)
                        {
                            Console.WriteLine("    [" + GroupToString(group.GroupValue) + "] groupSort=" + Arrays.ToString(group.GroupSortValues) + " numDocs=" + group.ScoreDocs.Length);
                        }
                    }
                }

                TopGroups<BytesRef> mergedGroups = TopGroups.Merge(shardTopGroups, groupSort, docSort, docOffset, topNDocs, TopGroups.ScoreMergeMode.None);
                if (Verbose)
                {
                    Console.WriteLine(" " + mergedGroups.Groups.Length + " merged groups:");
                    foreach (GroupDocs<BytesRef> group in mergedGroups.Groups)
                    {
                        Console.WriteLine("    [" + GroupToString(group.GroupValue) + "] groupSort=" + Arrays.ToString(group.GroupSortValues) + " numDocs=" + group.ScoreDocs.Length);
                    }
                }
                return mergedGroups;
            }
            else
            {
                return null;
            }
        }

        private void AssertEquals(FieldCache.Int32s docIDtoID, ITopGroups<BytesRef> expected, ITopGroups<BytesRef> actual, bool verifyGroupValues, bool verifyTotalGroupCount, bool verifySortValues, bool testScores, bool idvBasedImplsUsed)
        {
            if (expected is null)
            {
                assertNull(actual);
                return;
            }
            assertNotNull(actual);

            assertEquals("expected.groups.length != actual.groups.length", expected.Groups.Length, actual.Groups.Length);
            assertEquals("expected.totalHitCount != actual.totalHitCount", expected.TotalHitCount, actual.TotalHitCount);
            assertEquals("expected.totalGroupedHitCount != actual.totalGroupedHitCount", expected.TotalGroupedHitCount, actual.TotalGroupedHitCount);
            if (expected.TotalGroupCount != null && verifyTotalGroupCount)
            {
                assertEquals("expected.totalGroupCount != actual.totalGroupCount", expected.TotalGroupCount, actual.TotalGroupCount);
            }

            for (int groupIDX = 0; groupIDX < expected.Groups.Length; groupIDX++)
            {
                if (Verbose)
                {
                    Console.WriteLine("  check groupIDX=" + groupIDX);
                }
                IGroupDocs<BytesRef> expectedGroup = expected.Groups[groupIDX];
                IGroupDocs<BytesRef> actualGroup = actual.Groups[groupIDX];
                if (verifyGroupValues)
                {
                    if (idvBasedImplsUsed)
                    {
                        if (actualGroup.GroupValue.Length == 0)
                        {
                            assertNull(expectedGroup.GroupValue);
                        }
                        else
                        {
                            assertEquals(expectedGroup.GroupValue, actualGroup.GroupValue);
                        }
                    }
                    else
                    {
                        assertEquals(expectedGroup.GroupValue, actualGroup.GroupValue);
                    }

                }
                if (verifySortValues)
                {
                    assertArrayEquals(expectedGroup.GroupSortValues, actualGroup.GroupSortValues);
                }

                // TODO
                // assertEquals(expectedGroup.maxScore, actualGroup.maxScore);
                assertEquals(expectedGroup.TotalHits, actualGroup.TotalHits);

                ScoreDoc[] expectedFDs = expectedGroup.ScoreDocs;
                ScoreDoc[] actualFDs = actualGroup.ScoreDocs;

                assertEquals(expectedFDs.Length, actualFDs.Length);
                for (int docIDX = 0; docIDX < expectedFDs.Length; docIDX++)
                {
                    FieldDoc expectedFD = (FieldDoc)expectedFDs[docIDX];
                    FieldDoc actualFD = (FieldDoc)actualFDs[docIDX];
                    //Console.WriteLine("  actual doc=" + docIDtoID.get(actualFD.doc) + " score=" + actualFD.score);
                    assertEquals(expectedFD.Doc, docIDtoID.Get(actualFD.Doc));
                    if (testScores)
                    {
                        assertEquals(expectedFD.Score, actualFD.Score, 0.1);
                    }
                    else
                    {
                        // TODO: too anal for now
                        //assertEquals(Float.NaN, actualFD.score);
                    }
                    if (verifySortValues)
                    {
                        assertArrayEquals(expectedFD.Fields, actualFD.Fields);
                    }
                }
            }
        }

        internal class ShardSearcher : IndexSearcher
        {
            private readonly IList<AtomicReaderContext> ctx;

            public ShardSearcher(AtomicReaderContext ctx, IndexReaderContext parent)
                            : base(parent)
            {
                this.ctx = new JCG.List<AtomicReaderContext>(new AtomicReaderContext[] { ctx });
            }

            public void Search(Weight weight, ICollector collector)
            {
                Search(ctx, weight, collector);
            }

            public override string ToString()
            {
                return "ShardSearcher(" + ctx[0].Reader + ")";
            }
        }

        internal class ValueHolder<V>
        {

            internal V value;

            internal ValueHolder(V value)
            {
                this.value = value;
            }
        }
    }
}
