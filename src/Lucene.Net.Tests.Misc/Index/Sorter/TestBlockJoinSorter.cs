using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using Lucene.Net.Analysis;
using Lucene.Net.Util;
using Lucene.Net.Search;
using Lucene.Net.Documents;

namespace Lucene.Net.Index.Sorter
{
    public class TestBlockJoinSorter : LuceneTestCase
    {
        private class FixedBitSetCachingWrapperFilter : CachingWrapperFilter
        {

            public FixedBitSetCachingWrapperFilter(Filter filter)
                        : base(filter)
            {
            }


            protected override DocIdSet CacheImpl(DocIdSetIterator iterator, AtomicReader reader)
            {
                FixedBitSet cached = new FixedBitSet(reader.MaxDoc);
                cached.Or(iterator);
                return cached;
            }

        }

        [Test]
        public void Test()
        {
            RandomIndexWriter writer;
            DirectoryReader indexReader;
            int numParents = AtLeast(200);
            IndexWriterConfig cfg = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random()));
            cfg.SetMergePolicy(NewLogMergePolicy());
            using (writer = new RandomIndexWriter(Random(), NewDirectory(), cfg))
            {
                Document parentDoc = new Document();
                NumericDocValuesField parentVal = new NumericDocValuesField("parent_val", 0L);
                parentDoc.Add(parentVal);
                StringField parent = new StringField("parent", "true", Field.Store.YES);
                parentDoc.Add(parent);
                for (int i = 0; i < numParents; ++i)
                {
                    List<Document> documents = new List<Document>();
                    int numChildren = Random().nextInt(10);
                    for (int j = 0; j < numChildren; ++j)
                    {
                        Document childDoc = new Document();
                        childDoc.Add(new NumericDocValuesField("child_val", Random().nextInt(5)));
                        documents.Add(childDoc);
                    }
                    parentVal.SetInt64Value(Random().nextInt(50));
                    documents.Add(parentDoc);
                    writer.AddDocuments(documents);
                }
                writer.ForceMerge(1);
                indexReader = writer.Reader;
            }

            AtomicReader reader = GetOnlySegmentReader(indexReader);
            Filter parentsFilter = new FixedBitSetCachingWrapperFilter(new QueryWrapperFilter(new TermQuery(new Term("parent", "true"))));
            FixedBitSet parentBits = (FixedBitSet)parentsFilter.GetDocIdSet(reader.AtomicContext, null);
            NumericDocValues parentValues = reader.GetNumericDocValues("parent_val");

            NumericDocValues childValues = reader.GetNumericDocValues("child_val");

            Sort parentSort = new Sort(new SortField("parent_val", SortField.Type_e.LONG));
            Sort childSort = new Sort(new SortField("child_val", SortField.Type_e.LONG));

            Sort sort = new Sort(new SortField("custom", new BlockJoinComparatorSource(parentsFilter, parentSort, childSort)));
            Sorter sorter = new Sorter(sort);
            Sorter.DocMap docMap = sorter.Sort(reader);
            assertEquals(reader.MaxDoc, docMap.Count);

            int[] children = new int[1];
            int numChildren2 = 0;
            int previousParent = -1;
            for (int i = 0; i < docMap.Count; ++i)
            {
                int oldID = docMap.NewToOld(i);
                if (parentBits.Get(oldID))
                {
                    // check that we have the right children
                    for (int j = 0; j < numChildren2; ++j)
                    {
                        assertEquals(oldID, parentBits.NextSetBit(children[j]));
                    }
                    // check that children are sorted
                    for (int j = 1; j < numChildren2; ++j)
                    {
                        int doc1 = children[j - 1];
                        int doc2 = children[j];
                        if (childValues.Get(doc1) == childValues.Get(doc2))
                        {
                            assertTrue(doc1 < doc2); // sort is stable
                        }
                        else
                        {
                            assertTrue(childValues.Get(doc1) < childValues.Get(doc2));
                        }
                    }
                    // check that parents are sorted
                    if (previousParent != -1)
                    {
                        if (parentValues.Get(previousParent) == parentValues.Get(oldID))
                        {
                            assertTrue(previousParent < oldID);
                        }
                        else
                        {
                            assertTrue(parentValues.Get(previousParent) < parentValues.Get(oldID));
                        }
                    }
                    // reset
                    previousParent = oldID;
                    numChildren2 = 0;
                }
                else
                {
                    children = ArrayUtil.Grow(children, numChildren2 + 1);
                    children[numChildren2++] = oldID;
                }
            }
            indexReader.Dispose();
            writer.w.Directory.Dispose();
        }
    }
}
