using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Index.Extensions;
using Lucene.Net.Store;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using Assert = Lucene.Net.TestFramework.Assert;
using JCG = J2N.Collections.Generic;

namespace Lucene.Net.Search
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

    using BytesRef = Lucene.Net.Util.BytesRef;
    using DefaultSimilarity = Lucene.Net.Search.Similarities.DefaultSimilarity;
    using Document = Documents.Document;
    using Entry = Lucene.Net.Search.FieldValueHitQueue.Entry;
    using Field = Field;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;

    [TestFixture]
    public class TestElevationComparer : LuceneTestCase
    {
        private readonly IDictionary<BytesRef, int> priority = new Dictionary<BytesRef, int>();

        [Test]
        public virtual void TestSorting()
        {
            Directory directory = NewDirectory();
            IndexWriter writer = new IndexWriter(directory, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)).SetMaxBufferedDocs(2).SetMergePolicy(NewLogMergePolicy(1000)).SetSimilarity(new DefaultSimilarity()));
            writer.AddDocument(Adoc(new string[] { "id", "a", "title", "ipod", "str_s", "a" }));
            writer.AddDocument(Adoc(new string[] { "id", "b", "title", "ipod ipod", "str_s", "b" }));
            writer.AddDocument(Adoc(new string[] { "id", "c", "title", "ipod ipod ipod", "str_s", "c" }));
            writer.AddDocument(Adoc(new string[] { "id", "x", "title", "boosted", "str_s", "x" }));
            writer.AddDocument(Adoc(new string[] { "id", "y", "title", "boosted boosted", "str_s", "y" }));
            writer.AddDocument(Adoc(new string[] { "id", "z", "title", "boosted boosted boosted", "str_s", "z" }));

            IndexReader r = DirectoryReader.Open(writer, true);
            writer.Dispose();

            IndexSearcher searcher = NewSearcher(r);
            searcher.Similarity = new DefaultSimilarity();

            RunTest(searcher, true);
            RunTest(searcher, false);

            r.Dispose();
            directory.Dispose();
        }

        private void RunTest(IndexSearcher searcher, bool reversed)
        {
            BooleanQuery newq = new BooleanQuery(false);
            TermQuery query = new TermQuery(new Term("title", "ipod"));

            newq.Add(query, Occur.SHOULD);
            newq.Add(GetElevatedQuery(new string[] { "id", "a", "id", "x" }), Occur.SHOULD);

            Sort sort = new Sort(new SortField("id", new ElevationComparerSource(priority), false), new SortField(null, SortFieldType.SCORE, reversed)
             );

            TopDocsCollector<Entry> topCollector = TopFieldCollector.Create(sort, 50, false, true, true, true);
            searcher.Search(newq, null, topCollector);

            TopDocs topDocs = topCollector.GetTopDocs(0, 10);
            int nDocsReturned = topDocs.ScoreDocs.Length;

            Assert.AreEqual(4, nDocsReturned);

            // 0 & 3 were elevated
            Assert.AreEqual(0, topDocs.ScoreDocs[0].Doc);
            Assert.AreEqual(3, topDocs.ScoreDocs[1].Doc);

            if (reversed)
            {
                Assert.AreEqual(2, topDocs.ScoreDocs[2].Doc);
                Assert.AreEqual(1, topDocs.ScoreDocs[3].Doc);
            }
            else
            {
                Assert.AreEqual(1, topDocs.ScoreDocs[2].Doc);
                Assert.AreEqual(2, topDocs.ScoreDocs[3].Doc);
            }

            /*
            for (int i = 0; i < nDocsReturned; i++) {
             ScoreDoc scoreDoc = topDocs.ScoreDocs[i];
             ids[i] = scoreDoc.Doc;
             scores[i] = scoreDoc.Score;
             documents[i] = searcher.Doc(ids[i]);
             System.out.println("ids[i] = " + ids[i]);
             System.out.println("documents[i] = " + documents[i]);
             System.out.println("scores[i] = " + scores[i]);
           }
            */
        }

        private Query GetElevatedQuery(string[] vals)
        {
            BooleanQuery q = new BooleanQuery(false);
            q.Boost = 0;
            int max = (vals.Length / 2) + 5;
            for (int i = 0; i < vals.Length - 1; i += 2)
            {
                q.Add(new TermQuery(new Term(vals[i], vals[i + 1])), Occur.SHOULD);
                priority[new BytesRef(vals[i + 1])] = Convert.ToInt32(max--);
                // System.out.println(" pri doc=" + vals[i+1] + " pri=" + (1+max));
            }
            return q;
        }

        private Document Adoc(string[] vals)
        {
            Document doc = new Document();
            for (int i = 0; i < vals.Length - 2; i += 2)
            {
                doc.Add(NewTextField(vals[i], vals[i + 1], Field.Store.YES));
            }
            return doc;
        }
    }

    internal class ElevationComparerSource : FieldComparerSource
    {
        private readonly IDictionary<BytesRef, int> priority;

        public ElevationComparerSource(IDictionary<BytesRef, int> boosts)
        {
            this.priority = boosts;
        }

        public override FieldComparer NewComparer(string fieldname, int numHits, int sortPos, bool reversed)
        {
            return new FieldComparerAnonymousClass(this, fieldname, numHits);
        }

        private sealed class FieldComparerAnonymousClass : FieldComparer<J2N.Numerics.Int32>
        {
            private readonly ElevationComparerSource outerInstance;

            private readonly string fieldname;
            private int numHits;

            public FieldComparerAnonymousClass(ElevationComparerSource outerInstance, string fieldname, int numHits)
            {
                this.outerInstance = outerInstance;
                this.fieldname = fieldname;
                this.numHits = numHits;
                values = new int[numHits];
                tempBR = new BytesRef();
            }

            internal SortedDocValues idIndex;
            private readonly int[] values;
            private readonly BytesRef tempBR;
            internal int bottomVal;

            public override int CompareValues(J2N.Numerics.Int32 first, J2N.Numerics.Int32 second)
            {
                return JCG.Comparer<J2N.Numerics.Int32>.Default.Compare(first, second);
            }

            public override int Compare(int slot1, int slot2)
            {
                return values[slot2] - values[slot1]; // values will be small enough that there is no overflow concern
            }

            public override void SetBottom(int slot)
            {
                bottomVal = values[slot];
            }

            public override void SetTopValue(J2N.Numerics.Int32 value)
            {
                throw UnsupportedOperationException.Create();
            }

            private int DocVal(int doc)
            {
                int ord = idIndex.GetOrd(doc);
                if (ord == -1)
                {
                    return 0;
                }
                else
                {
                    idIndex.LookupOrd(ord, tempBR);
                    if (outerInstance.priority.TryGetValue(tempBR, out int prio))
                    {
                        return prio;
                    }
                    return 0;
                }
            }

            public override int CompareBottom(int doc)
            {
                return DocVal(doc) - bottomVal;
            }

            public override void Copy(int slot, int doc)
            {
                values[slot] = DocVal(doc);
            }

            public override FieldComparer SetNextReader(AtomicReaderContext context)
            {
                idIndex = FieldCache.DEFAULT.GetTermsIndex(context.AtomicReader, fieldname);
                return this;
            }

            // LUCENENET NOTE: This was value(int) in Lucene.
            public override J2N.Numerics.Int32 this[int slot] => J2N.Numerics.Int32.GetInstance(values[slot]);

            public override int CompareTop(int doc)
            {
                throw UnsupportedOperationException.Create();
            }
        }
    }
}