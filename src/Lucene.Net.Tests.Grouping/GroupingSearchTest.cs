using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Queries.Function;
using Lucene.Net.Queries.Function.ValueSources;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Lucene.Net.Util.Mutable;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.Search.Grouping
{
    public class GroupingSearchTest : LuceneTestCase
    {
        // Tests some very basic usages...
        [Test]
        public void testBasic()
        {

            string groupField = "author";

            FieldType customType = new FieldType();
            customType.Stored = (true);

            Directory dir = NewDirectory();
            RandomIndexWriter w = new RandomIndexWriter(
                Random(),
                dir,
                NewIndexWriterConfig(TEST_VERSION_CURRENT,
                    new MockAnalyzer(Random())).SetMergePolicy(NewLogMergePolicy()));
            bool canUseIDV = !"Lucene3x".equals(w.w.Config.Codec.Name);
            List<Document> documents = new List<Document>();
            // 0
            Document doc = new Document();
            addGroupField(doc, groupField, "author1", canUseIDV);
            doc.Add(new TextField("content", "random text", Field.Store.YES));
            doc.Add(new Field("id", "1", customType));
            documents.Add(doc);

            // 1
            doc = new Document();
            addGroupField(doc, groupField, "author1", canUseIDV);
            doc.Add(new TextField("content", "some more random text", Field.Store.YES));
            doc.Add(new Field("id", "2", customType));
            documents.Add(doc);

            // 2
            doc = new Document();
            addGroupField(doc, groupField, "author1", canUseIDV);
            doc.Add(new TextField("content", "some more random textual data", Field.Store.YES));
            doc.Add(new Field("id", "3", customType));
            doc.Add(new StringField("groupend", "x", Field.Store.NO));
            documents.Add(doc);
            w.AddDocuments(documents);
            documents.Clear();

            // 3
            doc = new Document();
            addGroupField(doc, groupField, "author2", canUseIDV);
            doc.Add(new TextField("content", "some random text", Field.Store.YES));
            doc.Add(new Field("id", "4", customType));
            doc.Add(new StringField("groupend", "x", Field.Store.NO));
            w.AddDocument(doc);

            // 4
            doc = new Document();
            addGroupField(doc, groupField, "author3", canUseIDV);
            doc.Add(new TextField("content", "some more random text", Field.Store.YES));
            doc.Add(new Field("id", "5", customType));
            documents.Add(doc);

            // 5
            doc = new Document();
            addGroupField(doc, groupField, "author3", canUseIDV);
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

            IndexSearcher indexSearcher = NewSearcher(w.Reader);
            w.Dispose();

            Sort groupSort = Sort.RELEVANCE;
            GroupingSearch groupingSearch = createRandomGroupingSearch(groupField, groupSort, 5, canUseIDV);

            var groups = groupingSearch.Search(indexSearcher, (Filter)null, new TermQuery(new Index.Term("content", "random")), 0, 10);

            assertEquals(7, groups.totalHitCount);
            assertEquals(7, groups.totalGroupedHitCount);
            assertEquals(4, groups.groups.length);

            // relevance order: 5, 0, 3, 4, 1, 2, 6

            // the later a document is added the higher this docId
            // value
            GroupDocs <?> group = groups.groups[0];
            compareGroupValue("author3", group);
            assertEquals(2, group.scoreDocs.length);
            assertEquals(5, group.scoreDocs[0].doc);
            assertEquals(4, group.scoreDocs[1].doc);
            assertTrue(group.scoreDocs[0].score > group.scoreDocs[1].score);

            group = groups.groups[1];
            compareGroupValue("author1", group);
            assertEquals(3, group.scoreDocs.length);
            assertEquals(0, group.scoreDocs[0].doc);
            assertEquals(1, group.scoreDocs[1].doc);
            assertEquals(2, group.scoreDocs[2].doc);
            assertTrue(group.scoreDocs[0].score > group.scoreDocs[1].score);
            assertTrue(group.scoreDocs[1].score > group.scoreDocs[2].score);

            group = groups.groups[2];
            compareGroupValue("author2", group);
            assertEquals(1, group.scoreDocs.length);
            assertEquals(3, group.scoreDocs[0].doc);

            group = groups.groups[3];
            compareGroupValue(null, group);
            assertEquals(1, group.scoreDocs.length);
            assertEquals(6, group.scoreDocs[0].doc);

            Filter lastDocInBlock = new CachingWrapperFilter(new QueryWrapperFilter(new TermQuery(new Index.Term("groupend", "x"))));
            groupingSearch = new GroupingSearch(lastDocInBlock);
            groups = groupingSearch.Search(indexSearcher, null, new TermQuery(new Index.Term("content", "random")), 0, 10);

            assertEquals(7, groups.totalHitCount);
            assertEquals(7, groups.totalGroupedHitCount);
            assertEquals(4, groups.totalGroupCount.longValue());
            assertEquals(4, groups.groups.length);

            indexSearcher.IndexReader.Dispose();
            dir.Dispose();
        }

        private void addGroupField(Document doc, string groupField, string value, bool canUseIDV)
        {
            doc.Add(new TextField(groupField, value, Field.Store.YES));
            if (canUseIDV)
            {
                doc.Add(new SortedDocValuesField(groupField, new BytesRef(value)));
            }
        }

        private void compareGroupValue(string expected, GroupDocs<?> group)
        {
            if (expected == null)
            {
                if (group.groupValue == null)
                {
                    return;
                }
                else if (group.groupValue.GetType().IsAssignableFrom(typeof(MutableValueStr)))
                {
                    return;
                }
                else if (((BytesRef)group.groupValue).length == 0)
                {
                    return;
                }
                fail();
            }

            if (group.groupValue.GetType().IsAssignableFrom(typeof(BytesRef)))
            {
                assertEquals(new BytesRef(expected), group.groupValue);
            }
            else if (group.groupValue.GetType().IsAssignableFrom(typeof(MutableValueStr)))
            {
                MutableValueStr v = new MutableValueStr();
                v.value = new BytesRef(expected);
                assertEquals(v, group.groupValue);
            }
            else
            {
                fail();
            }
        }

        private GroupingSearch createRandomGroupingSearch(String groupField, Sort groupSort, int docsInGroup, bool canUseIDV)
        {
            GroupingSearch groupingSearch;
            if (Random().nextBoolean())
            {
                ValueSource vs = new BytesRefFieldSource(groupField);
                groupingSearch = new GroupingSearch(vs, new Dictionary());
            }
            else
            {
                groupingSearch = new GroupingSearch(groupField);
            }

            groupingSearch.SetGroupSort(groupSort);
            groupingSearch.SetGroupDocsLimit(docsInGroup);

            if (Random().nextBoolean())
            {
                groupingSearch.SetCachingInMB(4.0, true);
            }

            return groupingSearch;
        }

        [Test]
        public void testSetAllGroups()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter w = new RandomIndexWriter(
                Random(),
                dir,
                NewIndexWriterConfig(TEST_VERSION_CURRENT,
                    new MockAnalyzer(Random())).SetMergePolicy(NewLogMergePolicy()));
            Document doc = new Document();
            doc.Add(NewField("group", "foo", StringField.TYPE_NOT_STORED));
            w.AddDocument(doc);

            IndexSearcher indexSearcher = NewSearcher(w.Reader);
            w.Dispose();

            GroupingSearch gs = new GroupingSearch("group");
            gs.SetAllGroups(true);
            TopGroups <?> groups = gs.Search(indexSearcher, null, new TermQuery(new Index.Term("group", "foo")), 0, 10);
            assertEquals(1, groups.totalHitCount);
            //assertEquals(1, groups.totalGroupCount.intValue());
            assertEquals(1, groups.totalGroupedHitCount);
            assertEquals(1, gs.GetAllMatchingGroups().size());
            indexSearcher.IndexReader.Dispose();
            dir.Dispose();
        }
    }
}
