using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Search.Grouping.Function;
using Lucene.Net.Search.Grouping.Term;
using Lucene.Net.Index;
using Lucene.Net.Queries.Function.ValueSources;
using Lucene.Net.Search;
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
using System.Threading.Tasks;
using Lucene.Net.Search.Grouping;
using Lucene.Net.Search.Grouping.Terms;

namespace Lucene.Net.Search.Grouping
{
    public class DistinctValuesCollectorTest : AbstractGroupingTestCase
    {
        private readonly static NullComparator nullComparator = new NullComparator();

        private readonly string groupField = "author";
        private readonly string dvGroupField = "author_dv";
        private readonly string countField = "publisher";
        private readonly string dvCountField = "publisher_dv";

        internal class ComparerAnonymousHelper1 : IComparer<AbstractGroupCount<IComparable<object>>>
        {
            private readonly DistinctValuesCollectorTest outerInstance;

            public ComparerAnonymousHelper1(DistinctValuesCollectorTest outerInstance)
            {
                this.outerInstance = outerInstance;
            }

            public int Compare(AbstractGroupCount<IComparable<object>> groupCount1, AbstractGroupCount<IComparable<object>> groupCount2)
            {
                if (groupCount1.groupValue == null)
                {
                    if (groupCount2.groupValue == null)
                    {
                        return 0;
                    }
                    return -1;
                }
                else if (groupCount2.groupValue == null)
                {
                    return 1;
                }
                else
                {
                    return groupCount1.groupValue.CompareTo(groupCount2.groupValue);
                }
            }
        }

        [Test]
        public void TestSimple()
        {
            Random random = Random();
            FieldInfo.DocValuesType_e[] dvTypes = new FieldInfo.DocValuesType_e[]{
                FieldInfo.DocValuesType_e.NUMERIC,
                FieldInfo.DocValuesType_e.BINARY,
                FieldInfo.DocValuesType_e.SORTED,
            };
            Directory dir = NewDirectory();
            RandomIndexWriter w = new RandomIndexWriter(
                random,
                dir,
                NewIndexWriterConfig(TEST_VERSION_CURRENT,
                    new MockAnalyzer(random)).SetMergePolicy(NewLogMergePolicy()));
            bool canUseDV = !"Lucene3x".equals(w.w.Config.Codec.Name);
            FieldInfo.DocValuesType_e? dvType = canUseDV ? dvTypes[random.nextInt(dvTypes.Length)] : (FieldInfo.DocValuesType_e?)null;

            Document doc = new Document();
            addField(doc, groupField, "1", dvType);
            addField(doc, countField, "1", dvType);
            doc.Add(new TextField("content", "random text", Field.Store.NO));
            doc.Add(new StringField("id", "1", Field.Store.NO));
            w.AddDocument(doc);

            // 1
            doc = new Document();
            addField(doc, groupField, "1", dvType);
            addField(doc, countField, "1", dvType);
            doc.Add(new TextField("content", "some more random text blob", Field.Store.NO));
            doc.Add(new StringField("id", "2", Field.Store.NO));
            w.AddDocument(doc);

            // 2
            doc = new Document();
            addField(doc, groupField, "1", dvType);
            addField(doc, countField, "2", dvType);
            doc.Add(new TextField("content", "some more random textual data", Field.Store.NO));
            doc.Add(new StringField("id", "3", Field.Store.NO));
            w.AddDocument(doc);
            w.Commit(); // To ensure a second segment

            // 3
            doc = new Document();
            addField(doc, groupField, "2", dvType);
            doc.Add(new TextField("content", "some random text", Field.Store.NO));
            doc.Add(new StringField("id", "4", Field.Store.NO));
            w.AddDocument(doc);

            // 4
            doc = new Document();
            addField(doc, groupField, "3", dvType);
            addField(doc, countField, "1", dvType);
            doc.Add(new TextField("content", "some more random text", Field.Store.NO));
            doc.Add(new StringField("id", "5", Field.Store.NO));
            w.AddDocument(doc);

            // 5
            doc = new Document();
            addField(doc, groupField, "3", dvType);
            addField(doc, countField, "1", dvType);
            doc.Add(new TextField("content", "random blob", Field.Store.NO));
            doc.Add(new StringField("id", "6", Field.Store.NO));
            w.AddDocument(doc);

            // 6 -- no author field
            doc = new Document();
            doc.Add(new TextField("content", "random word stuck in alot of other text", Field.Store.YES));
            addField(doc, countField, "1", dvType);
            doc.Add(new StringField("id", "6", Field.Store.NO));
            w.AddDocument(doc);

            IndexSearcher indexSearcher = NewSearcher(w.Reader);
            w.Dispose();

            var cmp = new ComparerAnonymousHelper1(this);

            //    Comparator<AbstractDistinctValuesCollector.GroupCount<Comparable<Object>>> cmp = new Comparator<AbstractDistinctValuesCollector.GroupCount<Comparable<Object>>>() {

            //      @Override
            //      public int compare(AbstractDistinctValuesCollector.GroupCount<Comparable<Object>> groupCount1, AbstractDistinctValuesCollector.GroupCount<Comparable<Object>> groupCount2)
            //    {
            //        if (groupCount1.groupValue == null)
            //        {
            //            if (groupCount2.groupValue == null)
            //            {
            //                return 0;
            //            }
            //            return -1;
            //        }
            //        else if (groupCount2.groupValue == null)
            //        {
            //            return 1;
            //        }
            //        else
            //        {
            //            return groupCount1.groupValue.compareTo(groupCount2.groupValue);
            //        }
            //    }

            //};

            // === Search for content:random
            AbstractFirstPassGroupingCollector<IComparable<object>> firstCollector = createRandomFirstPassCollector(dvType, new Sort(), groupField, 10);
            indexSearcher.Search(new TermQuery(new Term("content", "random")), firstCollector);
            Collector distinctValuesCollector
                = createDistinctCountCollector(firstCollector, groupField, countField, dvType.GetValueOrDefault());
            indexSearcher.Search(new TermQuery(new Term("content", "random")), distinctValuesCollector);

            var gcs = distinctValuesCollector.GetGroups();
            //Collections.sort(gcs, cmp);
            gcs.Sort(cmp);
            assertEquals(4, gcs.Count);

            compareNull(gcs[0].groupValue);
            List<IComparable> countValues = new List<IComparable>(gcs[0].uniqueValues);
            assertEquals(1, countValues.size());
            compare("1", countValues[0]);

            compare("1", gcs[1].groupValue);
            countValues = new List<IComparable>(gcs[1].uniqueValues);
            //Collections.sort(countValues, nullComparator);
            countValues.Sort(nullComparator);
            assertEquals(2, countValues.size());
            compare("1", countValues[0]);
            compare("2", countValues[1]);

            compare("2", gcs[2].groupValue);
            countValues = new List<IComparable>(gcs[2].uniqueValues);
            assertEquals(1, countValues.size());
            compareNull(countValues[0]);

            compare("3", gcs[3].groupValue);
            countValues = new List<IComparable>(gcs[3].uniqueValues);
            assertEquals(1, countValues.size());
            compare("1", countValues[0]);

            // === Search for content:some
            firstCollector = createRandomFirstPassCollector(dvType, new Sort(), groupField, 10);
            indexSearcher.Search(new TermQuery(new Term("content", "some")), firstCollector);
            distinctValuesCollector = createDistinctCountCollector(firstCollector, groupField, countField, dvType);
            indexSearcher.Search(new TermQuery(new Term("content", "some")), distinctValuesCollector);

            gcs = distinctValuesCollector.getGroups();
            //Collections.sort(gcs, cmp);
            gcs.Sort(cmp);
            assertEquals(3, gcs.Count);

            compare("1", gcs.get(0).groupValue);
            countValues = new List<IComparable>(gcs[0].uniqueValues);
            assertEquals(2, countValues.size());
            //Collections.sort(countValues, nullComparator);
            countValues.Sort(nullComparator);
            compare("1", countValues[0]);
            compare("2", countValues[1]);

            compare("2", gcs[1].groupValue);
            countValues = new List<IComparable>(gcs[1].uniqueValues);
            assertEquals(1, countValues.size());
            compareNull(countValues[0]);

            compare("3", gcs.get(2).groupValue);
            countValues = new List<IComparable>(gcs.get(2).uniqueValues);
            assertEquals(1, countValues.size());
            compare("1", countValues[0]);

            // === Search for content:blob
            firstCollector = createRandomFirstPassCollector(dvType, new Sort(), groupField, 10);
            indexSearcher.search(new TermQuery(new Term("content", "blob")), firstCollector);
            distinctValuesCollector = createDistinctCountCollector(firstCollector, groupField, countField, dvType);
            indexSearcher.search(new TermQuery(new Term("content", "blob")), distinctValuesCollector);

            gcs = distinctValuesCollector.getGroups();
            //Collections.sort(gcs, cmp);
            gcs.Sort(cmp);
            assertEquals(2, gcs.Count);

            compare("1", gcs[0].groupValue);
            countValues = new List<IComparable>(gcs[0].uniqueValues);
            // B/c the only one document matched with blob inside the author 1 group
            assertEquals(1, countValues.Count);
            compare("1", countValues[0]);

            compare("3", gcs[1].groupValue);
            countValues = new List<IComparable>(gcs[1].uniqueValues);
            assertEquals(1, countValues.Count);
            compare("1", countValues[0]);

            indexSearcher.IndexReader.Dispose();
            dir.Dispose();
        }

        [Test]
        public void testRandom()
        {
            Random random = Random();
            int numberOfRuns = TestUtil.NextInt(random, 3, 6);
            for (int indexIter = 0; indexIter < numberOfRuns; indexIter++)
            {
                IndexContext context = createIndexContext();
                for (int searchIter = 0; searchIter < 100; searchIter++)
                {
                    IndexSearcher searcher = NewSearcher(context.indexReader);
                    bool useDv = context.dvType != null && random.nextBoolean();
                    FieldInfo.DocValuesType_e? dvType = useDv ? context.dvType : (FieldInfo.DocValuesType_e?)null;
                    string term = context.contentStrings[random.nextInt(context.contentStrings.Length)];
                    Sort groupSort = new Sort(new SortField("id", SortField.Type_e.STRING));
                    int topN = 1 + random.nextInt(10);

                    List<AbstractGroupCount<IComparable>> expectedResult = createExpectedResult(context, term, groupSort, topN);

                    AbstractFirstPassGroupingCollector < Comparable <?>> firstCollector = createRandomFirstPassCollector(dvType, groupSort, groupField, topN);
                    searcher.Search(new TermQuery(new Term("content", term)), firstCollector);
                    AbstractDistinctValuesCollector <? extends AbstractDistinctValuesCollector.GroupCount < Comparable <?>>> distinctValuesCollector
                        = createDistinctCountCollector(firstCollector, groupField, countField, dvType);
                    searcher.Search(new TermQuery(new Term("content", term)), distinctValuesCollector);

                    List<AbstractGroupCount<IComparable>> actualResult = (List<AbstractGroupCount<IComparable>>)distinctValuesCollector.Groups;

                    if (VERBOSE)
                    {
                        Console.WriteLine("Index iter=" + indexIter);
                        Console.WriteLine("Search iter=" + searchIter);
                        Console.WriteLine("1st pass collector class name=" + firstCollector.GetType().Name);
                        Console.WriteLine("2nd pass collector class name=" + distinctValuesCollector.GetType().Name);
                        Console.WriteLine("Search term=" + term);
                        Console.WriteLine("DVType=" + dvType);
                        Console.WriteLine("1st pass groups=" + firstCollector.GetTopGroups(0, false));
                        Console.WriteLine("Expected:");
                        printGroups(expectedResult);
                        Console.WriteLine("Actual:");
                        printGroups(actualResult);
                    }

                    assertEquals(expectedResult.Count, actualResult.Count);
                    for (int i = 0; i < expectedResult.size(); i++)
                    {
                        AbstractDistinctValuesCollector.GroupCount < Comparable <?>> expected = expectedResult.get(i);
                        AbstractDistinctValuesCollector.GroupCount < Comparable <?>> actual = actualResult.get(i);
                        assertValues(expected.groupValue, actual.groupValue);
                        assertEquals(expected.uniqueValues.size(), actual.uniqueValues.size());
                        List < Comparable <?>> expectedUniqueValues = new ArrayList<>(expected.uniqueValues);
                        Collections.sort(expectedUniqueValues, nullComparator);
                        List < Comparable <?>> actualUniqueValues = new ArrayList<>(actual.uniqueValues);
                        Collections.sort(actualUniqueValues, nullComparator);
                        for (int j = 0; j < expectedUniqueValues.size(); j++)
                        {
                            assertValues(expectedUniqueValues.get(j), actualUniqueValues.get(j));
                        }
                    }
                }
                context.indexReader.Dispose();
                context.directory.Dispose();
            }
        }

        private void printGroups(List<AbstractDistinctValuesCollector.GroupCount<IComparable>> results)
        {
            for (int i = 0; i < results.size(); i++)
            {
                var group = results[i];
                object gv = group.groupValue;
                if (gv is BytesRef)
                {
                    Console.WriteLine(i + ": groupValue=" + ((BytesRef)gv).Utf8ToString());
                }
                else
                {
                    Console.WriteLine(i + ": groupValue=" + gv);
                }
                foreach (object o in group.uniqueValues)
                {
                    if (o is BytesRef)
                    {
                        Console.WriteLine("  " + ((BytesRef)o).Utf8ToString());
                    }
                    else
                    {
                        Console.WriteLine("  " + o);
                    }
                }
            }
        }

        private void assertValues(object expected, object actual)
        {
            if (expected == null)
            {
                compareNull(actual);
            }
            else
            {
                compare(((BytesRef)expected).Utf8ToString(), actual);
            }
        }

        private void compare(string expected, object groupValue)
        {
            if (typeof(BytesRef).IsAssignableFrom(groupValue.GetType()))
            {
                assertEquals(expected, ((BytesRef)groupValue).Utf8ToString());
            }
            else if (typeof(double).IsAssignableFrom(groupValue.GetType()))
            {
                assertEquals(double.Parse(expected, CultureInfo.InvariantCulture), groupValue);
            }
            else if (typeof(long).IsAssignableFrom(groupValue.GetType()))
            {
                assertEquals(long.Parse(expected, CultureInfo.InvariantCulture), groupValue);
            }
            else if (typeof(MutableValue).IsAssignableFrom(groupValue.GetType()))
            {
                MutableValueStr mutableValue = new MutableValueStr();
                mutableValue.Value = new BytesRef(expected);
                assertEquals(mutableValue, groupValue);
            }
            else
            {
                fail();
            }
        }

        private void compareNull(object groupValue)
        {
            if (groupValue == null)
            {
                return; // term based impl...
            }
            // DV based impls..
            if (typeof(BytesRef).IsAssignableFrom(groupValue.GetType()))
            {
                assertEquals("", ((BytesRef)groupValue).Utf8ToString());
            }
            else if (typeof(double).IsAssignableFrom(groupValue.GetType()))
            {
                assertEquals(0.0d, groupValue);
            }
            else if (typeof(long).IsAssignableFrom(groupValue.GetType()))
            {
                assertEquals(0L, groupValue);
                // Function based impl
            }
            else if (typeof(MutableValue).IsAssignableFrom(groupValue.GetType()))
            {
                assertFalse(((MutableValue)groupValue).Exists);
            }
            else
            {
                fail();
            }
        }

        private void addField(Document doc, string field, string value, FieldInfo.DocValuesType_e? type)
        {
            doc.Add(new StringField(field, value, Field.Store.YES));
            if (type == null)
            {
                return;
            }
            string dvField = field + "_dv";

            Field valuesField = null;
            switch (type)
            {
                case FieldInfo.DocValuesType_e.NUMERIC:
                    valuesField = new NumericDocValuesField(dvField, int.Parse(value, CultureInfo.InvariantCulture));
                    break;
                case FieldInfo.DocValuesType_e.BINARY:
                    valuesField = new BinaryDocValuesField(dvField, new BytesRef(value));
                    break;
                case FieldInfo.DocValuesType_e.SORTED:
                    valuesField = new SortedDocValuesField(dvField, new BytesRef(value));
                    break;
            }
            doc.Add(valuesField);
        }

        private AbstractDistinctValuesCollector<AbstractGroupCount<T>> createDistinctCountCollector<T>(AbstractFirstPassGroupingCollector<T> firstPassGroupingCollector,
                                                                            string groupField,
                                                                            string countField,
                                                                            FieldInfo.DocValuesType_e dvType)
                  where T : IComparable
        {
            Random random = Random();
            ICollection<SearchGroup<T>> searchGroups = firstPassGroupingCollector.GetTopGroups(0, false);
            if (typeof(FunctionFirstPassGroupingCollector).IsAssignableFrom(firstPassGroupingCollector.GetType()))
            {
                return (AbstractDistinctValuesCollector)new FunctionDistinctValuesCollector(new Hashtable(), new BytesRefFieldSource(groupField), new BytesRefFieldSource(countField), searchGroups as ICollection<SearchGroup<MutableValue>>);
            }
            else
            {
                return (AbstractDistinctValuesCollector)new TermDistinctValuesCollector(groupField, countField, searchGroups as ICollection<SearchGroup<BytesRef>>);
            }
        }

        private AbstractFirstPassGroupingCollector<T> createRandomFirstPassCollector<T>(FieldInfo.DocValuesType_e dvType, Sort groupSort, string groupField, int topNGroups)
        {
            Random random = Random();
            if (dvType != null)
            {
                if (random.nextBoolean())
                {
                    return (AbstractFirstPassGroupingCollector<T>)new FunctionFirstPassGroupingCollector(new BytesRefFieldSource(groupField), new Hashtable(), groupSort, topNGroups);
                }
                else
                {
                    return (AbstractFirstPassGroupingCollector<T>)new TermFirstPassGroupingCollector(groupField, groupSort, topNGroups);
                }
            }
            else
            {
                if (random.nextBoolean())
                {
                    return (AbstractFirstPassGroupingCollector<T>)new FunctionFirstPassGroupingCollector(new BytesRefFieldSource(groupField), new Hashtable(), groupSort, topNGroups);
                }
                else
                {
                    return (AbstractFirstPassGroupingCollector<T>)new TermFirstPassGroupingCollector(groupField, groupSort, topNGroups);
                }
            }
        }

        internal class GroupCount : AbstractGroupCount<BytesRef>
        {
            internal GroupCount(BytesRef groupValue, ICollection<BytesRef> uniqueValues)
                : base(groupValue)
            {
                this.uniqueValues.UnionWith(uniqueValues);
            }
        }

        private List<AbstractGroupCount<IComparable>> createExpectedResult(IndexContext context, string term, Sort groupSort, int topN)
        {


            List<AbstractGroupCount<IComparable>> result = new List<AbstractGroupCount<IComparable>>();
            IDictionary<string, ISet<string>> groupCounts = context.searchTermToGroupCounts[term];
            int i = 0;
            foreach (string group in groupCounts.Keys)
            {
                if (topN <= i++)
                {
                    break;
                }
                ISet<BytesRef> uniqueValues = new HashSet<BytesRef>();
                foreach (string val in groupCounts[group])
                {
                    uniqueValues.Add(val != null ? new BytesRef(val) : null);
                }
                result.Add(new GroupCount(group != null ? new BytesRef(group) : (BytesRef)null, uniqueValues));
            }
            return result;
        }

        private IndexContext createIndexContext()
        {
            Random random = Random();
            FieldInfo.DocValuesType_e[] dvTypes = new FieldInfo.DocValuesType_e[]{
        FieldInfo.DocValuesType_e.BINARY,
        FieldInfo.DocValuesType_e.SORTED
    };

            Directory dir = NewDirectory();
            RandomIndexWriter w = new RandomIndexWriter(
                random,
                dir,
                NewIndexWriterConfig(TEST_VERSION_CURRENT,
                new MockAnalyzer(random)).SetMergePolicy(NewLogMergePolicy())
              );

            bool canUseDV = !"Lucene3x".equals(w.w.Config.Codec.Name);
            FieldInfo.DocValuesType_e? dvType = canUseDV ? dvTypes[random.nextInt(dvTypes.Length)] : (FieldInfo.DocValuesType_e?)null;

            int numDocs = 86 + random.nextInt(1087) * RANDOM_MULTIPLIER;
            string[] groupValues = new string[numDocs / 5];
            string[] countValues = new string[numDocs / 10];
            for (int i = 0; i < groupValues.Length; i++)
            {
                groupValues[i] = GenerateRandomNonEmptyString();
            }
            for (int i = 0; i < countValues.Length; i++)
            {
                countValues[i] = GenerateRandomNonEmptyString();
            }

            List<string> contentStrings = new List<string>();
            IDictionary<string, IDictionary<string, ISet<string>>> searchTermToGroupCounts = new Dictionary<string, IDictionary<string, ISet<string>>>();
            for (int i = 1; i <= numDocs; i++)
            {
                string groupValue = random.nextInt(23) == 14 ? null : groupValues[random.nextInt(groupValues.Length)];
                string countValue = random.nextInt(21) == 13 ? null : countValues[random.nextInt(countValues.Length)];
                string content = "random" + random.nextInt(numDocs / 20);
                //IDictionary<string, ISet<string>> groupToCounts = searchTermToGroupCounts[content];
                //      if (groupToCounts == null)
                IDictionary<string, ISet<string>> groupToCounts;
                if (!searchTermToGroupCounts.TryGetValue(content, out groupToCounts))
                {
                    // Groups sort always DOCID asc...
                    searchTermToGroupCounts[content] = groupToCounts = new LurchTable<string, ISet<string>>(16);
                    contentStrings.Add(content);
                }

                //ISet<string> countsVals = groupToCounts.get(groupValue);
                //if (countsVals == null)
                ISet<string> countsVals;
                if (!groupToCounts.TryGetValue(groupValue, out countsVals))
                {
                    groupToCounts[groupValue] = countsVals = new HashSet<string>();
                }
                countsVals.Add(countValue);

                Document doc = new Document();
                doc.Add(new StringField("id", string.Format(CultureInfo.InvariantCulture, "{0:D9}", i), Field.Store.YES));
                if (groupValue != null)
                {
                    addField(doc, groupField, groupValue, dvType);
                }
                if (countValue != null)
                {
                    addField(doc, countField, countValue, dvType);
                }
                doc.Add(new TextField("content", content, Field.Store.YES));
                w.AddDocument(doc);
            }

            DirectoryReader reader = w.Reader;
            if (VERBOSE)
            {
                for (int docID = 0; docID < reader.MaxDoc; docID++)
                {
                    Document doc = reader.Document(docID);
                    Console.WriteLine("docID=" + docID + " id=" + doc.Get("id") + " content=" + doc.Get("content") + " author=" + doc.Get("author") + " publisher=" + doc.Get("publisher"));
                }
            }

            w.Dispose();
            return new IndexContext(dir, reader, dvType.GetValueOrDefault(), searchTermToGroupCounts, contentStrings.ToArray(/*new String[contentStrings.size()]*/));
        }

        internal class IndexContext
        {

            internal readonly Directory directory;
            internal readonly DirectoryReader indexReader;
            internal readonly FieldInfo.DocValuesType_e dvType;
            internal readonly IDictionary<string, IDictionary<string, ISet<string>>> searchTermToGroupCounts;
            internal readonly string[] contentStrings;

            internal IndexContext(Directory directory, DirectoryReader indexReader, FieldInfo.DocValuesType_e dvType,
                         IDictionary<string, IDictionary<string, ISet<string>>> searchTermToGroupCounts, string[] contentStrings)
            {
                this.directory = directory;
                this.indexReader = indexReader;
                this.dvType = dvType;
                this.searchTermToGroupCounts = searchTermToGroupCounts;
                this.contentStrings = contentStrings;
            }
        }

        internal class NullComparator : IComparer<IComparable>
        {

            public int Compare(IComparable a, IComparable b)
            {
                if (a == b)
                {
                    return 0;
                }
                else if (a == null)
                {
                    return -1;
                }
                else if (b == null)
                {
                    return 1;
                }
                else
                {
                    return a.CompareTo(b);
                }
            }

        }
    }
}
