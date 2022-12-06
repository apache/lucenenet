using Lucene.Net.Documents;
using Lucene.Net.Index.Extensions;
using NUnit.Framework;
using System;
using Assert = Lucene.Net.TestFramework.Assert;
using BitSet = J2N.Collections.BitSet;
using Console = Lucene.Net.Util.SystemConsole;

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
    using Directory = Lucene.Net.Store.Directory;
    using DirectoryReader = Lucene.Net.Index.DirectoryReader;
    using DocIdBitSet = Lucene.Net.Util.DocIdBitSet;
    using Document = Documents.Document;
    using Field = Field;
    using IBits = Lucene.Net.Util.IBits;
    using IndexReader = Lucene.Net.Index.IndexReader;
    using IndexWriter = Lucene.Net.Index.IndexWriter;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
    using OpenMode = Lucene.Net.Index.OpenMode;
    using Term = Lucene.Net.Index.Term;

    [TestFixture]
    public class TestScorerPerf : LuceneTestCase
    {
        internal bool validate = true; // set to false when doing performance testing

        internal BitSet[] sets;
        internal Term[] terms;
        internal IndexSearcher s;
        internal IndexReader r;
        internal Directory d;

        // TODO: this should be setUp()....
        public virtual void CreateDummySearcher()
        {
            // Create a dummy index with nothing in it.
            // this could possibly fail if Lucene starts checking for docid ranges...
            d = NewDirectory();
            IndexWriter iw = new IndexWriter(d, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)));
            iw.AddDocument(new Document());
            iw.Dispose();
            r = DirectoryReader.Open(d);
            s = NewSearcher(r);
        }

        public virtual void CreateRandomTerms(int nDocs, int nTerms, double power, Directory dir)
        {
            int[] freq = new int[nTerms];
            terms = new Term[nTerms];
            for (int i = 0; i < nTerms; i++)
            {
                int f = (nTerms + 1) - i; // make first terms less frequent
                freq[i] = (int)Math.Ceiling(Math.Pow(f, power));
                terms[i] = new Term("f", char.ToString((char)('A' + i)));
            }

            IndexWriter iw = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)).SetOpenMode(OpenMode.CREATE));
            for (int i = 0; i < nDocs; i++)
            {
                Document d = new Document();
                for (int j = 0; j < nTerms; j++)
                {
                    if (Random.Next(freq[j]) == 0)
                    {
                        d.Add(NewStringField("f", terms[j].Text, Field.Store.NO));
                        //System.out.println(d);
                    }
                }
                iw.AddDocument(d);
            }
            iw.ForceMerge(1);
            iw.Dispose();
        }

        public virtual BitSet RandBitSet(int sz, int numBitsToSet)
        {
            BitSet set = new BitSet(sz);
            for (int i = 0; i < numBitsToSet; i++)
            {
                set.Set(Random.Next(sz));
            }
            return set;
        }

        public virtual BitSet[] RandBitSets(int numSets, int setSize)
        {
            BitSet[] sets = new BitSet[numSets];
            for (int i = 0; i < sets.Length; i++)
            {
                sets[i] = RandBitSet(setSize, Random.Next(setSize));
            }
            return sets;
        }

        public class CountingHitCollector : ICollector
        {
            internal int count = 0;
            internal int sum = 0;
            protected internal int docBase = 0;

            public virtual void SetScorer(Scorer scorer)
            {
            }

            public virtual void Collect(int doc)
            {
                count++;
                sum += docBase + doc; // use it to avoid any possibility of being eliminated by hotspot
            }

            public virtual int Count => count;

            public virtual int Sum => sum;

            public virtual void SetNextReader(AtomicReaderContext context)
            {
                docBase = context.DocBase;
            }

            public virtual bool AcceptsDocsOutOfOrder => true;
        }

        public class MatchingHitCollector : CountingHitCollector
        {
            internal BitSet answer;
            internal int pos = -1;

            public MatchingHitCollector(BitSet answer)
            {
                this.answer = answer;
            }

            public virtual void Collect(int doc, float score)
            {
                pos = answer.NextSetBit(pos + 1);
                if (pos != doc + docBase)
                {
                    throw RuntimeException.Create("Expected doc " + pos + " but got " + doc + docBase);
                }
                base.Collect(doc);
            }
        }

        internal virtual BitSet AddClause(BooleanQuery bq, BitSet result)
        {
            BitSet rnd = sets[Random.Next(sets.Length)];
            Query q = new ConstantScoreQuery(new FilterAnonymousClass(rnd));
            bq.Add(q, Occur.MUST);
            if (validate)
            {
                if (result is null)
                {
                    result =  (BitSet)rnd.Clone();
                }
                else
                {
                    result.And(rnd);
                }
            }
            return result;
        }

        private sealed class FilterAnonymousClass : Filter
        {
            private readonly BitSet rnd;

            public FilterAnonymousClass(BitSet rnd)
            {
                this.rnd = rnd;
            }

            public override DocIdSet GetDocIdSet(AtomicReaderContext context, IBits acceptDocs)
            {
                Assert.IsNull(acceptDocs, "acceptDocs should be null, as we have an index without deletions");
                return new DocIdBitSet(rnd);
            }
        }

        public virtual int DoConjunctions(int iter, int maxClauses)
        {
            int ret = 0;

            for (int i = 0; i < iter; i++)
            {
                int nClauses = Random.Next(maxClauses - 1) + 2; // min 2 clauses
                BooleanQuery bq = new BooleanQuery();
                BitSet result = null;
                for (int j = 0; j < nClauses; j++)
                {
                    result = AddClause(bq, result);
                }

                CountingHitCollector hc = validate ? new MatchingHitCollector(result) : new CountingHitCollector();
                s.Search(bq, hc);
                ret += hc.Sum;

                if (validate)
                {
                    Assert.AreEqual(result.Cardinality, hc.Count);
                }
                // System.out.println(hc.getCount());
            }

            return ret;
        }

        public virtual int DoNestedConjunctions(int iter, int maxOuterClauses, int maxClauses)
        {
            int ret = 0;
            long nMatches = 0;

            for (int i = 0; i < iter; i++)
            {
                int oClauses = Random.Next(maxOuterClauses - 1) + 2;
                BooleanQuery oq = new BooleanQuery();
                BitSet result = null;

                for (int o = 0; o < oClauses; o++)
                {
                    int nClauses = Random.Next(maxClauses - 1) + 2; // min 2 clauses
                    BooleanQuery bq = new BooleanQuery();
                    for (int j = 0; j < nClauses; j++)
                    {
                        result = AddClause(bq, result);
                    }

                    oq.Add(bq, Occur.MUST);
                } // outer

                CountingHitCollector hc = validate ? new MatchingHitCollector(result) : new CountingHitCollector();
                s.Search(oq, hc);
                nMatches += hc.Count;
                ret += hc.Sum;
                if (validate)
                {
                    Assert.AreEqual(result.Cardinality, hc.Count);
                }
                // System.out.println(hc.getCount());
            }
            if (Verbose)
            {
                Console.WriteLine("Average number of matches=" + (nMatches / iter));
            }
            return ret;
        }

        public virtual int DoTermConjunctions(IndexSearcher s, int termsInIndex, int maxClauses, int iter)
        {
            int ret = 0;

            long nMatches = 0;
            for (int i = 0; i < iter; i++)
            {
                int nClauses = Random.Next(maxClauses - 1) + 2; // min 2 clauses
                BooleanQuery bq = new BooleanQuery();
                BitSet termflag = new BitSet(termsInIndex);
                for (int j = 0; j < nClauses; j++)
                {
                    int tnum;
                    // don't pick same clause twice
                    tnum = Random.Next(termsInIndex);
                    if (termflag.Get(tnum))
                    {
                        tnum = termflag.NextClearBit(tnum);
                    }
                    if (tnum < 0 || tnum >= termsInIndex)
                    {
                        tnum = termflag.NextClearBit(0);
                    }
                    termflag.Set(tnum);
                    Query tq = new TermQuery(terms[tnum]);
                    bq.Add(tq, Occur.MUST);
                }

                CountingHitCollector hc = new CountingHitCollector();
                s.Search(bq, hc);
                nMatches += hc.Count;
                ret += hc.Sum;
            }
            if (Verbose)
            {
                Console.WriteLine("Average number of matches=" + (nMatches / iter));
            }

            return ret;
        }

        public virtual int DoNestedTermConjunctions(IndexSearcher s, int termsInIndex, int maxOuterClauses, int maxClauses, int iter)
        {
            int ret = 0;
            long nMatches = 0;
            for (int i = 0; i < iter; i++)
            {
                int oClauses = Random.Next(maxOuterClauses - 1) + 2;
                BooleanQuery oq = new BooleanQuery();
                for (int o = 0; o < oClauses; o++)
                {
                    int nClauses = Random.Next(maxClauses - 1) + 2; // min 2 clauses
                    BooleanQuery bq = new BooleanQuery();
                    BitSet termflag = new BitSet(termsInIndex);
                    for (int j = 0; j < nClauses; j++)
                    {
                        int tnum;
                        // don't pick same clause twice
                        tnum = Random.Next(termsInIndex);
                        if (termflag.Get(tnum))
                        {
                            tnum = termflag.NextClearBit(tnum);
                        }
                        if (tnum < 0 || tnum >= 25)
                        {
                            tnum = termflag.NextClearBit(0);
                        }
                        termflag.Set(tnum);
                        Query tq = new TermQuery(terms[tnum]);
                        bq.Add(tq, Occur.MUST);
                    } // inner

                    oq.Add(bq, Occur.MUST);
                } // outer

                CountingHitCollector hc = new CountingHitCollector();
                s.Search(oq, hc);
                nMatches += hc.Count;
                ret += hc.Sum;
            }
            if (Verbose)
            {
                Console.WriteLine("Average number of matches=" + (nMatches / iter));
            }
            return ret;
        }

        public virtual int DoSloppyPhrase(IndexSearcher s, int termsInIndex, int maxClauses, int iter)
        {
            int ret = 0;

            for (int i = 0; i < iter; i++)
            {
                int nClauses = Random.Next(maxClauses - 1) + 2; // min 2 clauses
                PhraseQuery q = new PhraseQuery();
                for (int j = 0; j < nClauses; j++)
                {
                    int tnum = Random.Next(termsInIndex);
                    q.Add(new Term("f", char.ToString((char)(tnum + 'A'))), j);
                }
                q.Slop = termsInIndex; // this could be random too

                CountingHitCollector hc = new CountingHitCollector();
                s.Search(q, hc);
                ret += hc.Sum;
            }

            return ret;
        }

        [Test]
        public virtual void TestConjunctions()
        {
            // test many small sets... the bugs will be found on boundary conditions
            CreateDummySearcher();
            validate = true;
            sets = RandBitSets(AtLeast(1000), AtLeast(10));
            DoConjunctions(AtLeast(10000), AtLeast(5));
            DoNestedConjunctions(AtLeast(10000), AtLeast(3), AtLeast(3));
            r.Dispose();
            d.Dispose();
        }

        /*
         ///*
         /// int bigIter=10;
         ///
         /// public void testConjunctionPerf() throws Exception {
         ///  r = newRandom();
         ///  createDummySearcher();
         ///  validate=false;
         ///  sets=randBitSets(32,1000000);
         ///  for (int i=0; i<bigIter; i++) {
         ///    long start = DateTime.Now.Millisecond;
         ///    doConjunctions(500,6);
         ///    long end = DateTime.Now.Millisecond;
         ///    if (VERBOSE) System.out.println("milliseconds="+(end-start));
         ///  }
         ///  s.Dispose();
         /// }
         ///
         /// public void testNestedConjunctionPerf() throws Exception {
         ///  r = newRandom();
         ///  createDummySearcher();
         ///  validate=false;
         ///  sets=randBitSets(32,1000000);
         ///  for (int i=0; i<bigIter; i++) {
         ///    long start = DateTime.Now.Millisecond;
         ///    doNestedConjunctions(500,3,3);
         ///    long end = DateTime.Now.Millisecond;
         ///    if (VERBOSE) System.out.println("milliseconds="+(end-start));
         ///  }
         ///  s.Dispose();
         /// }
         ///
         ///
         /// public void testConjunctionTerms() throws Exception {
         ///  r = newRandom();
         ///  validate=false;
         ///  RAMDirectory dir = new RAMDirectory();
         ///  if (VERBOSE) System.out.println("Creating index");
         ///  createRandomTerms(100000,25,.5, dir);
         ///  s = NewSearcher(dir, true);
         ///  if (VERBOSE) System.out.println("Starting performance test");
         ///  for (int i=0; i<bigIter; i++) {
         ///    long start = DateTime.Now.Millisecond;
         ///    doTermConjunctions(s,25,5,1000);
         ///    long end = DateTime.Now.Millisecond;
         ///    if (VERBOSE) System.out.println("milliseconds="+(end-start));
         ///  }
         ///  s.Dispose();
         /// }
         ///
         /// public void testNestedConjunctionTerms() throws Exception {
         ///  r = newRandom();
         ///  validate=false;
         ///  RAMDirectory dir = new RAMDirectory();
         ///  if (VERBOSE) System.out.println("Creating index");
         ///  createRandomTerms(100000,25,.2, dir);
         ///  s = NewSearcher(dir, true);
         ///  if (VERBOSE) System.out.println("Starting performance test");
         ///  for (int i=0; i<bigIter; i++) {
         ///    long start = DateTime.Now.Millisecond;
         ///    doNestedTermConjunctions(s,25,3,3,200);
         ///    long end = DateTime.Now.Millisecond;
         ///    if (VERBOSE) System.out.println("milliseconds="+(end-start));
         ///  }
         ///  s.Dispose();
         /// }
         ///
         ///
         /// public void testSloppyPhrasePerf() throws Exception {
         ///  r = newRandom();
         ///  validate=false;
         ///  RAMDirectory dir = new RAMDirectory();
         ///  if (VERBOSE) System.out.println("Creating index");
         ///  createRandomTerms(100000,25,2,dir);
         ///  s = NewSearcher(dir, true);
         ///  if (VERBOSE) System.out.println("Starting performance test");
         ///  for (int i=0; i<bigIter; i++) {
         ///    long start = DateTime.Now.Millisecond;
         ///    doSloppyPhrase(s,25,2,1000);
         ///    long end = DateTime.Now.Millisecond;
         ///    if (VERBOSE) System.out.println("milliseconds="+(end-start));
         ///  }
         ///  s.Dispose();
         /// }
         /// **
         */
    }
}