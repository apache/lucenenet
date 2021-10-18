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

using Lucene.Net.Analysis;
using Lucene.Net.Util;
using Lucene.Net.Search;
using Lucene.Net.Documents;
using Lucene.Net.Index.Extensions;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using JCG = J2N.Collections.Generic;

namespace Lucene.Net.Index.Sorter
{
    [SuppressCodecs("Lucene3x")]
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
            IndexWriterConfig cfg = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random));
            cfg.SetMergePolicy(NewLogMergePolicy());
            using (writer = new RandomIndexWriter(Random, NewDirectory(), cfg))
            {
                Document parentDoc = new Document();
                NumericDocValuesField parentVal = new NumericDocValuesField("parent_val", 0L);
                parentDoc.Add(parentVal);
                StringField parent = new StringField("parent", "true", Field.Store.YES);
                parentDoc.Add(parent);
                for (int i = 0; i < numParents; ++i)
                {
                    IList<Document> documents = new JCG.List<Document>();
                    int numChildren = Random.nextInt(10);
                    for (int j = 0; j < numChildren; ++j)
                    {
                        Document childDoc = new Document();
                        childDoc.Add(new NumericDocValuesField("child_val", Random.nextInt(5)));
                        documents.Add(childDoc);
                    }
                    parentVal.SetInt64Value(Random.nextInt(50));
                    documents.Add(parentDoc);
                    writer.AddDocuments(documents);
                }
                writer.ForceMerge(1);
                indexReader = writer.GetReader();
            }

            AtomicReader reader = GetOnlySegmentReader(indexReader);
            Filter parentsFilter = new FixedBitSetCachingWrapperFilter(new QueryWrapperFilter(new TermQuery(new Term("parent", "true"))));
            FixedBitSet parentBits = (FixedBitSet)parentsFilter.GetDocIdSet(reader.AtomicContext, null);
            NumericDocValues parentValues = reader.GetNumericDocValues("parent_val");

            NumericDocValues childValues = reader.GetNumericDocValues("child_val");

            Sort parentSort = new Sort(new SortField("parent_val", SortFieldType.INT64));
            Sort childSort = new Sort(new SortField("child_val", SortFieldType.INT64));

            Sort sort = new Sort(new SortField("custom", new BlockJoinComparerSource(parentsFilter, parentSort, childSort)));
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
            writer.IndexWriter.Directory.Dispose();
        }
    }
}
