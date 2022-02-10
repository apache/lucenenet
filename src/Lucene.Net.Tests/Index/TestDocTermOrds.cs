using J2N.Collections.Generic.Extensions;
using Lucene.Net.Documents;
using Lucene.Net.Index.Extensions;
using Lucene.Net.Search;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using Assert = Lucene.Net.TestFramework.Assert;
using Console = Lucene.Net.Util.SystemConsole;
using JCG = J2N.Collections.Generic;

namespace Lucene.Net.Index
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

    using Analyzer = Lucene.Net.Analysis.Analyzer;
    using BytesRef = Lucene.Net.Util.BytesRef;
    using Codec = Lucene.Net.Codecs.Codec;
    using Directory = Lucene.Net.Store.Directory;
    using Document = Documents.Document;
    using Field = Field;
    using Int32Field = Int32Field;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
    using PostingsFormat = Lucene.Net.Codecs.PostingsFormat;
    using SeekStatus = Lucene.Net.Index.TermsEnum.SeekStatus;
    using StringField = StringField;
    using StringHelper = Lucene.Net.Util.StringHelper;
    using TestUtil = Lucene.Net.Util.TestUtil;

    // TODO:
    //   - test w/ del docs
    //   - test prefix
    //   - test w/ cutoff
    //   - crank docs way up so we get some merging sometimes
    [TestFixture]
    public class TestDocTermOrds : LuceneTestCase
    {
        [Test]
        public virtual void TestSimple()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter w = new RandomIndexWriter(Random, dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)).SetMergePolicy(NewLogMergePolicy()));
            Document doc = new Document();
            Field field = NewTextField("field", "", Field.Store.NO);
            doc.Add(field);
            field.SetStringValue("a b c");
            w.AddDocument(doc);

            field.SetStringValue("d e f");
            w.AddDocument(doc);

            field.SetStringValue("a f");
            w.AddDocument(doc);

            IndexReader r = w.GetReader();
            w.Dispose();

            AtomicReader ar = SlowCompositeReaderWrapper.Wrap(r);
            DocTermOrds dto = new DocTermOrds(ar, ar.LiveDocs, "field");
            SortedSetDocValues iter = dto.GetIterator(ar);

            iter.SetDocument(0);
            Assert.AreEqual(0, iter.NextOrd());
            Assert.AreEqual(1, iter.NextOrd());
            Assert.AreEqual(2, iter.NextOrd());
            Assert.AreEqual(SortedSetDocValues.NO_MORE_ORDS, iter.NextOrd());

            iter.SetDocument(1);
            Assert.AreEqual(3, iter.NextOrd());
            Assert.AreEqual(4, iter.NextOrd());
            Assert.AreEqual(5, iter.NextOrd());
            Assert.AreEqual(SortedSetDocValues.NO_MORE_ORDS, iter.NextOrd());

            iter.SetDocument(2);
            Assert.AreEqual(0, iter.NextOrd());
            Assert.AreEqual(5, iter.NextOrd());
            Assert.AreEqual(SortedSetDocValues.NO_MORE_ORDS, iter.NextOrd());

            r.Dispose();
            dir.Dispose();
        }

        [Test]
        public virtual void TestRandom()
        {
            Directory dir = NewDirectory();

            int NUM_TERMS = AtLeast(20);
            ISet<BytesRef> terms = new JCG.HashSet<BytesRef>();
            while (terms.Count < NUM_TERMS)
            {
                string s = TestUtil.RandomRealisticUnicodeString(Random);
                //final String s = TestUtil.RandomSimpleString(random);
                if (s.Length > 0)
                {
                    terms.Add(new BytesRef(s));
                }
            }
            BytesRef[] termsArray = terms.ToArray(/*new BytesRef[terms.Count]*/);
            Array.Sort(termsArray);

            int NUM_DOCS = AtLeast(100);

            IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random));

            // Sometimes swap in codec that impls ord():
            if (Random.Next(10) == 7)
            {
                // Make sure terms index has ords:
                Codec codec = TestUtil.AlwaysPostingsFormat(PostingsFormat.ForName("Lucene41WithOrds"));
                conf.SetCodec(codec);
            }

            RandomIndexWriter w = new RandomIndexWriter(Random, dir, conf);

            int[][] idToOrds = new int[NUM_DOCS][];
            ISet<int> ordsForDocSet = new JCG.HashSet<int>();

            for (int id = 0; id < NUM_DOCS; id++)
            {
                Document doc = new Document();

                doc.Add(new Int32Field("id", id, Field.Store.NO));

                int termCount = TestUtil.NextInt32(Random, 0, 20 * RandomMultiplier);
                while (ordsForDocSet.Count < termCount)
                {
                    ordsForDocSet.Add(Random.Next(termsArray.Length));
                }
                int[] ordsForDoc = new int[termCount];
                int upto = 0;
                if (Verbose)
                {
                    Console.WriteLine("TEST: doc id=" + id);
                }
                foreach (int ord in ordsForDocSet)
                {
                    ordsForDoc[upto++] = ord;
                    Field field = NewStringField("field", termsArray[ord].Utf8ToString(), Field.Store.NO);
                    if (Verbose)
                    {
                        Console.WriteLine("  f=" + termsArray[ord].Utf8ToString());
                    }
                    doc.Add(field);
                }
                ordsForDocSet.Clear();
                Array.Sort(ordsForDoc);
                idToOrds[id] = ordsForDoc;
                w.AddDocument(doc);
            }

            DirectoryReader r = w.GetReader();
            w.Dispose();

            if (Verbose)
            {
                Console.WriteLine("TEST: reader=" + r);
            }

            foreach (AtomicReaderContext ctx in r.Leaves)
            {
                if (Verbose)
                {
                    Console.WriteLine("\nTEST: sub=" + ctx.Reader);
                }
                Verify((AtomicReader)ctx.Reader, idToOrds, termsArray, null);
            }

            // Also test top-level reader: its enum does not support
            // ord, so this forces the OrdWrapper to run:
            if (Verbose)
            {
                Console.WriteLine("TEST: top reader");
            }
            AtomicReader slowR = SlowCompositeReaderWrapper.Wrap(r);
            Verify(slowR, idToOrds, termsArray, null);

            FieldCache.DEFAULT.PurgeByCacheKey(slowR.CoreCacheKey);

            r.Dispose();
            dir.Dispose();
        }

        [Test]
        public virtual void TestRandomWithPrefix()
        {
            Directory dir = NewDirectory();

            ISet<string> prefixes = new JCG.HashSet<string>();
            int numPrefix = TestUtil.NextInt32(Random, 2, 7);
            if (Verbose)
            {
                Console.WriteLine("TEST: use " + numPrefix + " prefixes");
            }
            while (prefixes.Count < numPrefix)
            {
                prefixes.Add(TestUtil.RandomRealisticUnicodeString(Random));
                //prefixes.Add(TestUtil.RandomSimpleString(random));
            }
            string[] prefixesArray = prefixes.ToArray(/*new string[prefixes.Count]*/);

            int NUM_TERMS = AtLeast(20);
            ISet<BytesRef> terms = new JCG.HashSet<BytesRef>();
            while (terms.Count < NUM_TERMS)
            {
                string s = prefixesArray[Random.Next(prefixesArray.Length)] + TestUtil.RandomRealisticUnicodeString(Random);
                //final String s = prefixesArray[random.nextInt(prefixesArray.Length)] + TestUtil.RandomSimpleString(random);
                if (s.Length > 0)
                {
                    terms.Add(new BytesRef(s));
                }
            }
            BytesRef[] termsArray = terms.ToArray();
            Array.Sort(termsArray);

            int NUM_DOCS = AtLeast(100);

            IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random));

            // Sometimes swap in codec that impls ord():
            if (Random.Next(10) == 7)
            {
                Codec codec = TestUtil.AlwaysPostingsFormat(PostingsFormat.ForName("Lucene41WithOrds"));
                conf.SetCodec(codec);
            }

            RandomIndexWriter w = new RandomIndexWriter(Random, dir, conf);

            int[][] idToOrds = new int[NUM_DOCS][];
            ISet<int> ordsForDocSet = new JCG.HashSet<int>();

            for (int id = 0; id < NUM_DOCS; id++)
            {
                Document doc = new Document();

                doc.Add(new Int32Field("id", id, Field.Store.NO));

                int termCount = TestUtil.NextInt32(Random, 0, 20 * RandomMultiplier);
                while (ordsForDocSet.Count < termCount)
                {
                    ordsForDocSet.Add(Random.Next(termsArray.Length));
                }
                int[] ordsForDoc = new int[termCount];
                int upto = 0;
                if (Verbose)
                {
                    Console.WriteLine("TEST: doc id=" + id);
                }
                foreach (int ord in ordsForDocSet)
                {
                    ordsForDoc[upto++] = ord;
                    Field field = NewStringField("field", termsArray[ord].Utf8ToString(), Field.Store.NO);
                    if (Verbose)
                    {
                        Console.WriteLine("  f=" + termsArray[ord].Utf8ToString());
                    }
                    doc.Add(field);
                }
                ordsForDocSet.Clear();
                Array.Sort(ordsForDoc);
                idToOrds[id] = ordsForDoc;
                w.AddDocument(doc);
            }

            DirectoryReader r = w.GetReader();
            w.Dispose();

            if (Verbose)
            {
                Console.WriteLine("TEST: reader=" + r);
            }

            AtomicReader slowR = SlowCompositeReaderWrapper.Wrap(r);
            foreach (string prefix in prefixesArray)
            {
                BytesRef prefixRef = prefix is null ? null : new BytesRef(prefix);

                int[][] idToOrdsPrefix = new int[NUM_DOCS][];
                for (int id = 0; id < NUM_DOCS; id++)
                {
                    int[] docOrds = idToOrds[id];
                    IList<int> newOrds = new JCG.List<int>();
                    foreach (int ord in idToOrds[id])
                    {
                        if (StringHelper.StartsWith(termsArray[ord], prefixRef))
                        {
                            newOrds.Add(ord);
                        }
                    }
                    int[] newOrdsArray = new int[newOrds.Count];
                    int upto = 0;
                    foreach (int ord in newOrds)
                    {
                        newOrdsArray[upto++] = ord;
                    }
                    idToOrdsPrefix[id] = newOrdsArray;
                }

                foreach (AtomicReaderContext ctx in r.Leaves)
                {
                    if (Verbose)
                    {
                        Console.WriteLine("\nTEST: sub=" + ctx.Reader);
                    }
                    Verify((AtomicReader)ctx.Reader, idToOrdsPrefix, termsArray, prefixRef);
                }

                // Also test top-level reader: its enum does not support
                // ord, so this forces the OrdWrapper to run:
                if (Verbose)
                {
                    Console.WriteLine("TEST: top reader");
                }
                Verify(slowR, idToOrdsPrefix, termsArray, prefixRef);
            }

            FieldCache.DEFAULT.PurgeByCacheKey(slowR.CoreCacheKey);

            r.Dispose();
            dir.Dispose();
        }

        private void Verify(AtomicReader r, int[][] idToOrds, BytesRef[] termsArray, BytesRef prefixRef)
        {
            DocTermOrds dto = new DocTermOrds(r, r.LiveDocs, "field", prefixRef, int.MaxValue, TestUtil.NextInt32(Random, 2, 10));

            FieldCache.Int32s docIDToID = FieldCache.DEFAULT.GetInt32s(r, "id", false);
            /*
              for(int docID=0;docID<subR.MaxDoc;docID++) {
              System.out.println("  docID=" + docID + " id=" + docIDToID[docID]);
              }
            */

            if (Verbose)
            {
                Console.WriteLine("TEST: verify prefix=" + (prefixRef is null ? "null" : prefixRef.Utf8ToString()));
                Console.WriteLine("TEST: all TERMS:");
                TermsEnum allTE = MultiFields.GetTerms(r, "field").GetEnumerator();
                int ord = 0;
                while (allTE.MoveNext())
                {
                    Console.WriteLine("  ord=" + (ord++) + " term=" + allTE.Term.Utf8ToString());
                }
            }

            //final TermsEnum te = subR.Fields.Terms("field").iterator();
            TermsEnum te = dto.GetOrdTermsEnum(r);
            if (dto.NumTerms == 0)
            {
                if (prefixRef is null)
                {
                    Assert.IsNull(MultiFields.GetTerms(r, "field"));
                }
                else
                {
                    Terms terms = MultiFields.GetTerms(r, "field");
                    if (terms != null)
                    {
                        TermsEnum termsEnum = terms.GetEnumerator();
                        TermsEnum.SeekStatus result = termsEnum.SeekCeil(prefixRef);
                        if (result != TermsEnum.SeekStatus.END)
                        {
                            Assert.IsFalse(StringHelper.StartsWith(termsEnum.Term, prefixRef), "term=" + termsEnum.Term.Utf8ToString() + " matches prefix=" + prefixRef.Utf8ToString());
                        }
                        else
                        {
                            // ok
                        }
                    }
                    else
                    {
                        // ok
                    }
                }
                return;
            }

            if (Verbose)
            {
                Console.WriteLine("TEST: TERMS:");
                te.SeekExact(0);
                while (true)
                {
                    Console.WriteLine("  ord=" + te.Ord + " term=" + te.Term.Utf8ToString());
                    if (!te.MoveNext())
                    {
                        break;
                    }
                }
            }

            SortedSetDocValues iter = dto.GetIterator(r);
            for (int docID = 0; docID < r.MaxDoc; docID++)
            {
                if (Verbose)
                {
                    Console.WriteLine("TEST: docID=" + docID + " of " + r.MaxDoc + " (id=" + docIDToID.Get(docID) + ")");
                }
                iter.SetDocument(docID);
                int[] answers = idToOrds[docIDToID.Get(docID)];
                int upto = 0;
                long ord;
                while ((ord = iter.NextOrd()) != SortedSetDocValues.NO_MORE_ORDS)
                {
                    te.SeekExact(ord);
                    BytesRef expected = termsArray[answers[upto++]];
                    if (Verbose)
                    {
                        Console.WriteLine("  exp=" + expected.Utf8ToString() + " actual=" + te.Term.Utf8ToString());
                    }
                    Assert.AreEqual(expected, te.Term, "expected=" + expected.Utf8ToString() + " actual=" + te.Term.Utf8ToString() + " ord=" + ord);
                }
                Assert.AreEqual(answers.Length, upto);
            }
        }

        [Test]
        public virtual void TestBackToTheFuture()
        {
            Directory dir = NewDirectory();
            IndexWriter iw = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, null));

            Document doc = new Document();
            doc.Add(NewStringField("foo", "bar", Field.Store.NO));
            iw.AddDocument(doc);

            doc = new Document();
            doc.Add(NewStringField("foo", "baz", Field.Store.NO));
            iw.AddDocument(doc);

            DirectoryReader r1 = DirectoryReader.Open(iw, true);

            iw.DeleteDocuments(new Term("foo", "baz"));
            DirectoryReader r2 = DirectoryReader.Open(iw, true);

            FieldCache.DEFAULT.GetDocTermOrds(GetOnlySegmentReader(r2), "foo");

            SortedSetDocValues v = FieldCache.DEFAULT.GetDocTermOrds(GetOnlySegmentReader(r1), "foo");
            Assert.AreEqual(2, v.ValueCount);
            v.SetDocument(1);
            Assert.AreEqual(1, v.NextOrd());

            iw.Dispose();
            r1.Dispose();
            r2.Dispose();
            dir.Dispose();
        }

        [Test]
        public virtual void TestSortedTermsEnum()
        {
            Directory directory = NewDirectory();
            Analyzer analyzer = new MockAnalyzer(Random);
            IndexWriterConfig iwconfig = NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
            iwconfig.SetMergePolicy(NewLogMergePolicy());
            RandomIndexWriter iwriter = new RandomIndexWriter(Random, directory, iwconfig);

            Document doc = new Document();
            doc.Add(new StringField("field", "hello", Field.Store.NO));
            iwriter.AddDocument(doc);

            doc = new Document();
            doc.Add(new StringField("field", "world", Field.Store.NO));
            iwriter.AddDocument(doc);

            doc = new Document();
            doc.Add(new StringField("field", "beer", Field.Store.NO));
            iwriter.AddDocument(doc);
            iwriter.ForceMerge(1);

            DirectoryReader ireader = iwriter.GetReader();
            iwriter.Dispose();

            AtomicReader ar = GetOnlySegmentReader(ireader);
            SortedSetDocValues dv = FieldCache.DEFAULT.GetDocTermOrds(ar, "field");
            Assert.AreEqual(3, dv.ValueCount);

            TermsEnum termsEnum = dv.GetTermsEnum();

            // next()
            Assert.IsTrue(termsEnum.MoveNext());
            Assert.AreEqual("beer", termsEnum.Term.Utf8ToString());
            Assert.AreEqual(0, termsEnum.Ord);
            Assert.IsTrue(termsEnum.MoveNext());
            Assert.AreEqual("hello", termsEnum.Term.Utf8ToString());
            Assert.AreEqual(1, termsEnum.Ord);
            Assert.IsTrue(termsEnum.MoveNext());
            Assert.AreEqual("world", termsEnum.Term.Utf8ToString());
            Assert.AreEqual(2, termsEnum.Ord);

            // seekCeil()
            Assert.AreEqual(SeekStatus.NOT_FOUND, termsEnum.SeekCeil(new BytesRef("ha!")));
            Assert.AreEqual("hello", termsEnum.Term.Utf8ToString());
            Assert.AreEqual(1, termsEnum.Ord);
            Assert.AreEqual(SeekStatus.FOUND, termsEnum.SeekCeil(new BytesRef("beer")));
            Assert.AreEqual("beer", termsEnum.Term.Utf8ToString());
            Assert.AreEqual(0, termsEnum.Ord);
            Assert.AreEqual(SeekStatus.END, termsEnum.SeekCeil(new BytesRef("zzz")));

            // seekExact()
            Assert.IsTrue(termsEnum.SeekExact(new BytesRef("beer")));
            Assert.AreEqual("beer", termsEnum.Term.Utf8ToString());
            Assert.AreEqual(0, termsEnum.Ord);
            Assert.IsTrue(termsEnum.SeekExact(new BytesRef("hello")));
            Assert.AreEqual("hello", termsEnum.Term.Utf8ToString());
            Assert.AreEqual(1, termsEnum.Ord);
            Assert.IsTrue(termsEnum.SeekExact(new BytesRef("world")));
            Assert.AreEqual("world", termsEnum.Term.Utf8ToString());
            Assert.AreEqual(2, termsEnum.Ord);
            Assert.IsFalse(termsEnum.SeekExact(new BytesRef("bogus")));

            // seek(ord)
            termsEnum.SeekExact(0);
            Assert.AreEqual("beer", termsEnum.Term.Utf8ToString());
            Assert.AreEqual(0, termsEnum.Ord);
            termsEnum.SeekExact(1);
            Assert.AreEqual("hello", termsEnum.Term.Utf8ToString());
            Assert.AreEqual(1, termsEnum.Ord);
            termsEnum.SeekExact(2);
            Assert.AreEqual("world", termsEnum.Term.Utf8ToString());
            Assert.AreEqual(2, termsEnum.Ord);
            ireader.Dispose();
            directory.Dispose();
        }
    }
}