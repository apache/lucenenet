using Lucene.Net.Documents;
using Lucene.Net.Index;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using JCG = J2N.Collections.Generic;
using Assert = Lucene.Net.TestFramework.Assert;
using Console = Lucene.Net.Util.SystemConsole;
using RandomizedTesting.Generators;

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

    using AtomicReaderContext = Lucene.Net.Index.AtomicReaderContext;
    using IBits = Lucene.Net.Util.IBits;
    using BytesRef = Lucene.Net.Util.BytesRef;
    using Directory = Lucene.Net.Store.Directory;
    using Document = Documents.Document;
    using Field = Field;
    using FixedBitSet = Lucene.Net.Util.FixedBitSet;
    using IndexReader = Lucene.Net.Index.IndexReader;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using NumericDocValuesField = NumericDocValuesField;
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
            Random random = new J2N.Randomizer(Random.NextInt64());

            int NUM_DOCS = AtLeast(100);
            Directory dir = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(random, dir);
            bool allowDups = random.NextBoolean();
            ISet<string> seen = new JCG.HashSet<string>();
            int maxLength = TestUtil.NextInt32(random, 5, 100);
            if (Verbose)
            {
                Console.WriteLine("TEST: NUM_DOCS=" + NUM_DOCS + " maxLength=" + maxLength + " allowDups=" + allowDups);
            }

            int numDocs = 0;
            IList<BytesRef> docValues = new JCG.List<BytesRef>();
            // TODO: deletions
            while (numDocs < NUM_DOCS)
            {
                Document doc = new Document();

                // 10% of the time, the document is missing the value:
                BytesRef br;
                if (LuceneTestCase.Random.Next(10) != 7)
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

                    if (Verbose)
                    {
                        Console.WriteLine("  " + numDocs + ": s=" + s);
                    }

                    br = new BytesRef(s);
                    if (DefaultCodecSupportsDocValues)
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
                    if (Verbose)
                    {
                        Console.WriteLine("  " + numDocs + ": <missing>");
                    }
                    docValues.Add(null);
                    if (DefaultCodecSupportsDocValues)
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
                    writer.GetReader().Dispose();
                }
            }

            IndexReader r = writer.GetReader();
            writer.Dispose();
            if (Verbose)
            {
                Console.WriteLine("  reader=" + r);
            }

            IndexSearcher idxS = NewSearcher(r, false);
            int ITERS = AtLeast(100);
            for (int iter = 0; iter < ITERS; iter++)
            {
                bool reverse = random.NextBoolean();

                TopFieldDocs hits;
                SortField sf;
                bool sortMissingLast;
                bool missingIsNull;
                if (DefaultCodecSupportsDocValues && random.NextBoolean())
                {
                    sf = new SortField("stringdv", SortFieldType.STRING, reverse);
                    // Can only use sort missing if the DVFormat
                    // supports docsWithField:
                    sortMissingLast = DefaultCodecSupportsDocsWithField && Random.NextBoolean();
                    missingIsNull = DefaultCodecSupportsDocsWithField;
                }
                else
                {
                    sf = new SortField("string", SortFieldType.STRING, reverse);
                    sortMissingLast = Random.NextBoolean();
                    missingIsNull = true;
                }
                if (sortMissingLast)
                {
                    sf.SetMissingValue(SortField.STRING_LAST);
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
                int hitCount = TestUtil.NextInt32(random, 1, r.MaxDoc + 20);
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

                if (Verbose)
                {
                    Console.WriteLine("\nTEST: iter=" + iter + " " + hits.TotalHits + " hits; topN=" + hitCount + "; reverse=" + reverse + "; sortMissingLast=" + sortMissingLast + " sort=" + sort);
                }

                // Compute expected results:
                var expected = f.matchValues.ToList();
                
                expected.Sort(Comparer<BytesRef>.Create((a,b) =>
                    {
                        if (a is null)
                        {
                            if (b is null)
                            {
                                return 0;
                            }
                            if (sortMissingLast)
                            {
                                return 1;
                            }
                            else
                            {
                                return -1;
                            }
                        }
                        else if (b is null)
                        {
                            if (sortMissingLast)
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
                    }));

                if (reverse)
                {
                    expected.Reverse();
                }

                if (Verbose)
                {
                    Console.WriteLine("  expected:");
                    for (int idx = 0; idx < expected.Count; idx++)
                    {
                        BytesRef br = expected[idx];
                        if (br is null && missingIsNull == false)
                        {
                            br = new BytesRef();
                        }
                        Console.WriteLine("    " + idx + ": " + (br is null ? "<missing>" : br.Utf8ToString()));
                        if (idx == hitCount - 1)
                        {
                            break;
                        }
                    }
                }

                if (Verbose)
                {
                    Console.WriteLine("  actual:");
                    for (int hitIDX = 0; hitIDX < hits.ScoreDocs.Length; hitIDX++)
                    {
                        FieldDoc fd = (FieldDoc)hits.ScoreDocs[hitIDX];
                        BytesRef br = (BytesRef)fd.Fields[0];

                        Console.WriteLine("    " + hitIDX + ": " + (br is null ? "<missing>" : br.Utf8ToString()) + " id=" + idxS.Doc(fd.Doc).Get("id"));
                    }
                }
                for (int hitIDX = 0; hitIDX < hits.ScoreDocs.Length; hitIDX++)
                {
                    FieldDoc fd = (FieldDoc)hits.ScoreDocs[hitIDX];
                    BytesRef br = expected[hitIDX];
                    if (br is null && missingIsNull == false)
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
                    if (br2 is null && missingIsNull == false)
                    {
                        br2 = new BytesRef();
                    }

                    Assert.AreEqual(br, br2, "hit=" + hitIDX + " has wrong sort value");
                }
            }

            r.Dispose();
            dir.Dispose();
        }

        private class RandomFilter : Filter
        {
            private readonly Random random;
            private readonly float density;
            private readonly IList<BytesRef> docValues;
            public readonly IList<BytesRef> matchValues = new SynchronizedList<BytesRef>();

            // density should be 0.0 ... 1.0
            public RandomFilter(Random random, float density, IList<BytesRef> docValues)
            {
                this.random = random;
                this.density = density;
                this.docValues = docValues;
            }

            public override DocIdSet GetDocIdSet(AtomicReaderContext context, IBits acceptDocs)
            {
                int maxDoc = context.Reader.MaxDoc;
                FieldCache.Int32s idSource = FieldCache.DEFAULT.GetInt32s(context.AtomicReader, "id", false);
                Assert.IsNotNull(idSource);
                FixedBitSet bits = new FixedBitSet(maxDoc);
                for (int docID = 0; docID < maxDoc; docID++)
                {
                    if ((float)random.NextDouble() <= density && (acceptDocs is null || acceptDocs.Get(docID)))
                    {
                        bits.Set(docID);
                        //System.out.println("  acc id=" + idSource.Get(docID) + " docID=" + docID + " id=" + idSource.Get(docID) + " v=" + docValues.Get(idSource.Get(docID)).Utf8ToString());
                        matchValues.Add(docValues[idSource.Get(docID)]);
                    }
                }

                return bits;
            }
        }
    }
}