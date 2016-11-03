using Lucene.Net.Documents;
using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Lucene.Net.Search
{
    using Lucene.Net.Index;
    using Lucene.Net.Randomized.Generators;
    using NUnit.Framework;
    using AtomicReaderContext = Lucene.Net.Index.AtomicReaderContext;
    using Bits = Lucene.Net.Util.Bits;
    using BytesRef = Lucene.Net.Util.BytesRef;
    using Directory = Lucene.Net.Store.Directory;

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

    using Document = Documents.Document;
    using Field = Field;
    using FixedBitSet = Lucene.Net.Util.FixedBitSet;
    using IndexReader = Lucene.Net.Index.IndexReader;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using NumericDocValuesField = NumericDocValuesField;
    using Occur = Lucene.Net.Search.BooleanClause.Occur;
    using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;
    using SortedDocValuesField = SortedDocValuesField;
    using StoredField = StoredField;
    using TestUtil = Lucene.Net.Util.TestUtil;

    /// <summary>
    /// random sorting tests </summary>
    [TestFixture]
    public class TestSortRandom : LuceneTestCase
    {
        [Test]
        public virtual void TestRandomStringSort()
        {
            Random random = new Random(Random().Next());

            int NUM_DOCS = AtLeast(100);
            Directory dir = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(random, dir, Similarity, TimeZone);
            bool allowDups = random.NextBoolean();
            HashSet<string> seen = new HashSet<string>();
            int maxLength = TestUtil.NextInt(random, 5, 100);
            if (VERBOSE)
            {
                Console.WriteLine("TEST: NUM_DOCS=" + NUM_DOCS + " maxLength=" + maxLength + " allowDups=" + allowDups);
            }

            int numDocs = 0;
            IList<BytesRef> docValues = new List<BytesRef>();
            // TODO: deletions
            while (numDocs < NUM_DOCS)
            {
                Document doc = new Document();

                // 10% of the time, the document is missing the value:
                BytesRef br;
                if (Random().Next(10) != 7)
                {
                    string s;
                    if (random.NextBoolean())
                    {
                        s = TestUtil.RandomSimpleString(random, maxLength);
                    }
                    else
                    {
                        s = TestUtil.RandomUnicodeString(random, maxLength);
                    }

                    if (!allowDups)
                    {
                        if (seen.Contains(s))
                        {
                            continue;
                        }
                        seen.Add(s);
                    }

                    if (VERBOSE)
                    {
                        Console.WriteLine("  " + numDocs + ": s=" + s);
                    }

                    br = new BytesRef(s);
                    if (DefaultCodecSupportsDocValues())
                    {
                        doc.Add(new SortedDocValuesField("stringdv", br));
                        doc.Add(new NumericDocValuesField("id", numDocs));
                    }
                    else
                    {
                        doc.Add(NewStringField("id", Convert.ToString(numDocs), Field.Store.NO));
                    }
                    doc.Add(NewStringField("string", s, Field.Store.NO));
                    docValues.Add(br);
                }
                else
                {
                    br = null;
                    if (VERBOSE)
                    {
                        Console.WriteLine("  " + numDocs + ": <missing>");
                    }
                    docValues.Add(null);
                    if (DefaultCodecSupportsDocValues())
                    {
                        doc.Add(new NumericDocValuesField("id", numDocs));
                    }
                    else
                    {
                        doc.Add(NewStringField("id", Convert.ToString(numDocs), Field.Store.NO));
                    }
                }

                doc.Add(new StoredField("id", numDocs));
                writer.AddDocument(doc);
                numDocs++;

                if (random.Next(40) == 17)
                {
                    // force flush
                    writer.Reader.Dispose();
                }
            }

            IndexReader r = writer.Reader;
            writer.Dispose();
            if (VERBOSE)
            {
                Console.WriteLine("  reader=" + r);
            }

            IndexSearcher idxS = NewSearcher(r, false, Similarity);
            int ITERS = AtLeast(100);
            for (int iter = 0; iter < ITERS; iter++)
            {
                bool reverse = random.NextBoolean();

                TopFieldDocs hits;
                SortField sf;
                bool sortMissingLast;
                bool missingIsNull;
                if (DefaultCodecSupportsDocValues() && random.NextBoolean())
                {
                    sf = new SortField("stringdv", SortField.Type_e.STRING, reverse);
                    // Can only use sort missing if the DVFormat
                    // supports docsWithField:
                    sortMissingLast = DefaultCodecSupportsDocsWithField() && Random().NextBoolean();
                    missingIsNull = DefaultCodecSupportsDocsWithField();
                }
                else
                {
                    sf = new SortField("string", SortField.Type_e.STRING, reverse);
                    sortMissingLast = Random().NextBoolean();
                    missingIsNull = true;
                }
                if (sortMissingLast)
                {
                    sf.MissingValue = SortField.STRING_LAST;
                }

                Sort sort;
                if (random.NextBoolean())
                {
                    sort = new Sort(sf);
                }
                else
                {
                    sort = new Sort(sf, SortField.FIELD_DOC);
                }
                int hitCount = TestUtil.NextInt(random, 1, r.MaxDoc + 20);
                RandomFilter f = new RandomFilter(random, (float)random.NextDouble(), docValues);
                int queryType = random.Next(3);
                if (queryType == 0)
                {
                    // force out of order
                    BooleanQuery bq = new BooleanQuery();
                    // Add a Query with SHOULD, since bw.Scorer() returns BooleanScorer2
                    // which delegates to BS if there are no mandatory clauses.
                    bq.Add(new MatchAllDocsQuery(), Occur.SHOULD);
                    // Set minNrShouldMatch to 1 so that BQ will not optimize rewrite to return
                    // the clause instead of BQ.
                    bq.MinimumNumberShouldMatch = 1;
                    hits = idxS.Search(bq, f, hitCount, sort, random.NextBoolean(), random.NextBoolean());
                }
                else if (queryType == 1)
                {
                    hits = idxS.Search(new ConstantScoreQuery(f), null, hitCount, sort, random.NextBoolean(), random.NextBoolean());
                }
                else
                {
                    hits = idxS.Search(new MatchAllDocsQuery(), f, hitCount, sort, random.NextBoolean(), random.NextBoolean());
                }

                if (VERBOSE)
                {
                    Console.WriteLine("\nTEST: iter=" + iter + " " + hits.TotalHits + " hits; topN=" + hitCount + "; reverse=" + reverse + "; sortMissingLast=" + sortMissingLast + " sort=" + sort);
                }

                // Compute expected results:
                var expected = f.MatchValues.ToList();
                expected.Sort(new ComparatorAnonymousInnerClassHelper(this, sortMissingLast));
                if (reverse)
                {
                    expected.Reverse();
                }

                if (VERBOSE)
                {
                    Console.WriteLine("  expected:");
                    for (int idx = 0; idx < expected.Count; idx++)
                    {
                        BytesRef br = expected[idx];
                        if (br == null && missingIsNull == false)
                        {
                            br = new BytesRef();
                        }
                        Console.WriteLine("    " + idx + ": " + (br == null ? "<missing>" : br.Utf8ToString()));
                        if (idx == hitCount - 1)
                        {
                            break;
                        }
                    }
                }

                if (VERBOSE)
                {
                    Console.WriteLine("  actual:");
                    for (int hitIDX = 0; hitIDX < hits.ScoreDocs.Length; hitIDX++)
                    {
                        FieldDoc fd = (FieldDoc)hits.ScoreDocs[hitIDX];
                        BytesRef br = (BytesRef)fd.Fields[0];

                        Console.WriteLine("    " + hitIDX + ": " + (br == null ? "<missing>" : br.Utf8ToString()) + " id=" + idxS.Doc(fd.Doc).Get("id"));
                    }
                }
                for (int hitIDX = 0; hitIDX < hits.ScoreDocs.Length; hitIDX++)
                {
                    FieldDoc fd = (FieldDoc)hits.ScoreDocs[hitIDX];
                    BytesRef br = expected[hitIDX];
                    if (br == null && missingIsNull == false)
                    {
                        br = new BytesRef();
                    }

                    // Normally, the old codecs (that don't support
                    // docsWithField via doc values) will always return
                    // an empty BytesRef for the missing case; however,
                    // if all docs in a given segment were missing, in
                    // that case it will return null!  So we must map
                    // null here, too:
                    BytesRef br2 = (BytesRef)fd.Fields[0];
                    if (br2 == null && missingIsNull == false)
                    {
                        br2 = new BytesRef();
                    }

                    Assert.AreEqual(br, br2, "hit=" + hitIDX + " has wrong sort value");
                }
            }

            r.Dispose();
            dir.Dispose();
        }

        private class ComparatorAnonymousInnerClassHelper : IComparer<BytesRef>
        {
            private readonly TestSortRandom OuterInstance;

            private bool SortMissingLast;

            public ComparatorAnonymousInnerClassHelper(TestSortRandom outerInstance, bool sortMissingLast)
            {
                this.OuterInstance = outerInstance;
                this.SortMissingLast = sortMissingLast;
            }

            public virtual int Compare(BytesRef a, BytesRef b)
            {
                if (a == null)
                {
                    if (b == null)
                    {
                        return 0;
                    }
                    if (SortMissingLast)
                    {
                        return 1;
                    }
                    else
                    {
                        return -1;
                    }
                }
                else if (b == null)
                {
                    if (SortMissingLast)
                    {
                        return -1;
                    }
                    else
                    {
                        return 1;
                    }
                }
                else
                {
                    return a.CompareTo(b);
                }
            }
        }

        private class RandomFilter : Filter
        {
            private readonly Random Random;
            private readonly float Density;
            private readonly IList<BytesRef> DocValues;
            public readonly IList<BytesRef> MatchValues = new SynchronizedList<BytesRef>();

            // density should be 0.0 ... 1.0
            public RandomFilter(Random random, float density, IList<BytesRef> docValues)
            {
                this.Random = random;
                this.Density = density;
                this.DocValues = docValues;
            }

            public override DocIdSet GetDocIdSet(AtomicReaderContext context, Bits acceptDocs)
            {
                int maxDoc = context.Reader.MaxDoc;
                FieldCache.Ints idSource = FieldCache.DEFAULT.GetInts(context.AtomicReader, "id", false);
                Assert.IsNotNull(idSource);
                FixedBitSet bits = new FixedBitSet(maxDoc);
                for (int docID = 0; docID < maxDoc; docID++)
                {
                    if ((float)Random.NextDouble() <= Density && (acceptDocs == null || acceptDocs.Get(docID)))
                    {
                        bits.Set(docID);
                        //System.out.println("  acc id=" + idSource.Get(docID) + " docID=" + docID + " id=" + idSource.Get(docID) + " v=" + docValues.Get(idSource.Get(docID)).Utf8ToString());
                        MatchValues.Add(DocValues[idSource.Get(docID)]);
                    }
                }

                return bits;
            }
        }
    }
}