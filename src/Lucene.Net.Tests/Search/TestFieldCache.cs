using J2N.Threading;
using J2N.Threading.Atomic;
using Lucene.Net.Diagnostics;
using Lucene.Net.Index.Extensions;
using Lucene.Net.Support;
using Lucene.Net.Util;
using NUnit.Framework;
using RandomizedTesting.Generators;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using Assert = Lucene.Net.TestFramework.Assert;
using Console = Lucene.Net.Util.SystemConsole;
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

    using AtomicReader = Lucene.Net.Index.AtomicReader;
    using BinaryDocValues = Lucene.Net.Index.BinaryDocValues;
    using BinaryDocValuesField = Lucene.Net.Documents.BinaryDocValuesField;
    using Bytes = Lucene.Net.Search.FieldCache.Bytes;
    using BytesRef = Lucene.Net.Util.BytesRef;
    using Directory = Lucene.Net.Store.Directory;
    using DirectoryReader = Lucene.Net.Index.DirectoryReader;
    using DocTermOrds = Lucene.Net.Index.DocTermOrds;
    using Document = Lucene.Net.Documents.Document;
    using Doubles = Lucene.Net.Search.FieldCache.Doubles;
    using Field = Lucene.Net.Documents.Field;
    using IBits = Lucene.Net.Util.IBits;
    using IndexReader = Lucene.Net.Index.IndexReader;
    using IndexWriter = Lucene.Net.Index.IndexWriter;
    using IndexWriterConfig = Lucene.Net.Index.IndexWriterConfig;
    using Int16s = Lucene.Net.Search.FieldCache.Int16s;
    using Int32Field = Lucene.Net.Documents.Int32Field;
    using Int32s = Lucene.Net.Search.FieldCache.Int32s;
    using Int64Field = Lucene.Net.Documents.Int64Field;
    using Int64s = Lucene.Net.Search.FieldCache.Int64s;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
    using NumericDocValuesField = Lucene.Net.Documents.NumericDocValuesField;
    using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;
    using Singles = Lucene.Net.Search.FieldCache.Singles;
    using SlowCompositeReaderWrapper = Lucene.Net.Index.SlowCompositeReaderWrapper;
    using SortedDocValues = Lucene.Net.Index.SortedDocValues;
    using SortedDocValuesField = Lucene.Net.Documents.SortedDocValuesField;
    using SortedSetDocValues = Lucene.Net.Index.SortedSetDocValues;
    using SortedSetDocValuesField = Lucene.Net.Documents.SortedSetDocValuesField;
    using StoredField = Lucene.Net.Documents.StoredField;
    using TermsEnum = Lucene.Net.Index.TermsEnum;
    using TestUtil = Lucene.Net.Util.TestUtil;


    [TestFixture]
    public class TestFieldCache : LuceneTestCase
    {
        private static AtomicReader reader;
        private static int NUM_DOCS;
        private static int NUM_ORDS;
        private static string[] unicodeStrings;
        private static BytesRef[][] multiValued;
        private static Directory directory;

        /// <summary>
        /// LUCENENET specific. Ensure we have an infostream attached to the default FieldCache
        /// when running the tests. In Java, this was done in the Core.Search.TestFieldCache.TestInfoStream() 
        /// method (which polluted the state of these tests), but we need to make the tests self-contained 
        /// so they can be run correctly regardless of order. Not setting the InfoStream skips an execution
        /// path within these tests, so we should do it to make sure we test all of the code.
        /// </summary>
        public override void SetUp()
        {
            base.SetUp();
            FieldCache.DEFAULT.InfoStream = new StringWriter();
        }

        /// <summary>
        /// LUCENENET specific. See <see cref="SetUp()"/>. Dispose our InfoStream and set it to null
        /// to avoid polluting the state of other tests.
        /// </summary>
        public override void TearDown()
        {
            FieldCache.DEFAULT.InfoStream.Dispose();
            FieldCache.DEFAULT.InfoStream = null;
            base.TearDown();
        }


        // LUCENENET: Changed to non-static because NewIndexWriterConfig is non-static
        [OneTimeSetUp]
        public override void BeforeClass()
        {
            base.BeforeClass();

            NUM_DOCS = AtLeast(500);
            NUM_ORDS = AtLeast(2);
            directory = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random, directory, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)).SetMergePolicy(NewLogMergePolicy()));
            long theLong = long.MaxValue;
            double theDouble = double.MaxValue;
            sbyte theByte = sbyte.MaxValue;
            short theShort = short.MaxValue;
            int theInt = int.MaxValue;
            float theFloat = float.MaxValue;
            unicodeStrings = new string[NUM_DOCS];
            //MultiValued = new BytesRef[NUM_DOCS, NUM_ORDS];
            multiValued = RectangularArrays.ReturnRectangularArray<BytesRef>(NUM_DOCS, NUM_ORDS);
            if (Verbose)
            {
                Console.WriteLine("TEST: setUp");
            }
            for (int i = 0; i < NUM_DOCS; i++)
            {
                Document doc = new Document();
                doc.Add(NewStringField("theLong", J2N.Numerics.Int64.ToString(theLong--, CultureInfo.InvariantCulture), Field.Store.NO));
                doc.Add(NewStringField("theDouble", J2N.Numerics.Double.ToString(theDouble--, CultureInfo.InvariantCulture), Field.Store.NO));
                doc.Add(NewStringField("theByte", J2N.Numerics.SByte.ToString(theByte--, CultureInfo.InvariantCulture), Field.Store.NO));
                doc.Add(NewStringField("theShort", J2N.Numerics.Int16.ToString(theShort--, CultureInfo.InvariantCulture), Field.Store.NO));
                doc.Add(NewStringField("theInt", J2N.Numerics.Int32.ToString(theInt--, CultureInfo.InvariantCulture), Field.Store.NO));
                doc.Add(NewStringField("theFloat", J2N.Numerics.Single.ToString(theFloat--, CultureInfo.InvariantCulture), Field.Store.NO));
                if (i % 2 == 0)
                {
                    doc.Add(NewStringField("sparse", J2N.Numerics.Int32.ToString(i, CultureInfo.InvariantCulture), Field.Store.NO));
                }

                if (i % 2 == 0)
                {
                    doc.Add(new Int32Field("numInt", i, Field.Store.NO));
                }

                // sometimes skip the field:
                if (Random.Next(40) != 17)
                {
                    unicodeStrings[i] = GenerateString(i);
                    doc.Add(NewStringField("theRandomUnicodeString", unicodeStrings[i], Field.Store.YES));
                }

                // sometimes skip the field:
                if (Random.Next(10) != 8)
                {
                    for (int j = 0; j < NUM_ORDS; j++)
                    {
                        string newValue = GenerateString(i);
                        multiValued[i][j] = new BytesRef(newValue);
                        doc.Add(NewStringField("theRandomUnicodeMultiValuedField", newValue, Field.Store.YES));
                    }
                    Array.Sort(multiValued[i]);
                }
                writer.AddDocument(doc);
            }
            IndexReader r = writer.GetReader();
            reader = SlowCompositeReaderWrapper.Wrap(r);
            writer.Dispose();
        }

        [OneTimeTearDown]
        public override void AfterClass()
        {
            reader.Dispose();
            reader = null;
            directory.Dispose();
            directory = null;
            unicodeStrings = null;
            multiValued = null;
            base.AfterClass();
        }

        [Test]
        public virtual void TestInfoStream()
        {
            try
            {
                IFieldCache cache = FieldCache.DEFAULT;
                StringBuilder sb = new StringBuilder();
                using (var bos = new StringWriter(sb))
                {
                    cache.InfoStream = bos;
                    cache.GetDoubles(reader, "theDouble", false);
                    cache.GetSingles(reader, "theDouble", false);
                }
                Assert.IsTrue(sb.ToString(/*IOUtils.UTF_8*/).IndexOf("WARNING", StringComparison.Ordinal) != -1);
            }
            finally
            {
                FieldCache.DEFAULT.PurgeAllCaches();
            }
        }

        [Test]
        public virtual void Test()
        {
#pragma warning disable 612, 618
            IFieldCache cache = FieldCache.DEFAULT;
            FieldCache.Doubles doubles = cache.GetDoubles(reader, "theDouble", Random.NextBoolean());
            Assert.AreSame(doubles, cache.GetDoubles(reader, "theDouble", Random.NextBoolean()), "Second request to cache return same array");
            Assert.AreSame(doubles, cache.GetDoubles(reader, "theDouble", FieldCache.DEFAULT_DOUBLE_PARSER, Random.NextBoolean()), "Second request with explicit parser return same array");
            for (int i = 0; i < NUM_DOCS; i++)
            {
                Assert.IsTrue(doubles.Get(i) == (double.MaxValue - i), doubles.Get(i) + " does not equal: " + (double.MaxValue - i));
            }

            FieldCache.Int64s longs = cache.GetInt64s(reader, "theLong", Random.NextBoolean());
            Assert.AreSame(longs, cache.GetInt64s(reader, "theLong", Random.NextBoolean()), "Second request to cache return same array");
            Assert.AreSame(longs, cache.GetInt64s(reader, "theLong", FieldCache.DEFAULT_INT64_PARSER, Random.NextBoolean()), "Second request with explicit parser return same array");
            for (int i = 0; i < NUM_DOCS; i++)
            {
                Assert.IsTrue(longs.Get(i) == (long.MaxValue - i), longs.Get(i) + " does not equal: " + (long.MaxValue - i) + " i=" + i);
            }

            FieldCache.Bytes bytes = cache.GetBytes(reader, "theByte", Random.NextBoolean());
            Assert.AreSame(bytes, cache.GetBytes(reader, "theByte", Random.NextBoolean()), "Second request to cache return same array");
            Assert.AreSame(bytes, cache.GetBytes(reader, "theByte", FieldCache.DEFAULT_BYTE_PARSER, Random.NextBoolean()), "Second request with explicit parser return same array");
            for (int i = 0; i < NUM_DOCS; i++)
            {
                Assert.IsTrue((sbyte)bytes.Get(i) == (sbyte)(sbyte.MaxValue - i), (sbyte)bytes.Get(i) + " does not equal: " + (sbyte.MaxValue - i));
            }

            FieldCache.Int16s shorts = cache.GetInt16s(reader, "theShort", Random.NextBoolean());
            Assert.AreSame(shorts, cache.GetInt16s(reader, "theShort", Random.NextBoolean()), "Second request to cache return same array");
            Assert.AreSame(shorts, cache.GetInt16s(reader, "theShort", FieldCache.DEFAULT_INT16_PARSER, Random.NextBoolean()), "Second request with explicit parser return same array");
            for (int i = 0; i < NUM_DOCS; i++)
            {
                Assert.IsTrue(shorts.Get(i) == (short)(short.MaxValue - i), shorts.Get(i) + " does not equal: " + (short.MaxValue - i));
            }

            FieldCache.Int32s ints = cache.GetInt32s(reader, "theInt", Random.NextBoolean());
            Assert.AreSame(ints, cache.GetInt32s(reader, "theInt", Random.NextBoolean()), "Second request to cache return same array");
            Assert.AreSame(ints, cache.GetInt32s(reader, "theInt", FieldCache.DEFAULT_INT32_PARSER, Random.NextBoolean()), "Second request with explicit parser return same array");
            for (int i = 0; i < NUM_DOCS; i++)
            {
                Assert.IsTrue(ints.Get(i) == (int.MaxValue - i), ints.Get(i) + " does not equal: " + (int.MaxValue - i));
            }

            FieldCache.Singles floats = cache.GetSingles(reader, "theFloat", Random.NextBoolean());
            Assert.AreSame(floats, cache.GetSingles(reader, "theFloat", Random.NextBoolean()), "Second request to cache return same array");
            Assert.AreSame(floats, cache.GetSingles(reader, "theFloat", FieldCache.DEFAULT_SINGLE_PARSER, Random.NextBoolean()), "Second request with explicit parser return same array");
            for (int i = 0; i < NUM_DOCS; i++)
            {
                Assert.IsTrue(floats.Get(i) == (float.MaxValue - i), floats.Get(i) + " does not equal: " + (float.MaxValue - i));
            }
#pragma warning restore 612, 618

            IBits docsWithField = cache.GetDocsWithField(reader, "theLong");
            Assert.AreSame(docsWithField, cache.GetDocsWithField(reader, "theLong"), "Second request to cache return same array");
            Assert.IsTrue(docsWithField is Bits.MatchAllBits, "docsWithField(theLong) must be class Bits.MatchAllBits");
            Assert.IsTrue(docsWithField.Length == NUM_DOCS, "docsWithField(theLong) Size: " + docsWithField.Length + " is not: " + NUM_DOCS);
            for (int i = 0; i < docsWithField.Length; i++)
            {
                Assert.IsTrue(docsWithField.Get(i));
            }

            docsWithField = cache.GetDocsWithField(reader, "sparse");
            Assert.AreSame(docsWithField, cache.GetDocsWithField(reader, "sparse"), "Second request to cache return same array");
            Assert.IsFalse(docsWithField is Bits.MatchAllBits, "docsWithField(sparse) must not be class Bits.MatchAllBits");
            Assert.IsTrue(docsWithField.Length == NUM_DOCS, "docsWithField(sparse) Size: " + docsWithField.Length + " is not: " + NUM_DOCS);
            for (int i = 0; i < docsWithField.Length; i++)
            {
                Assert.AreEqual(i % 2 == 0, docsWithField.Get(i));
            }

            // getTermsIndex
            SortedDocValues termsIndex = cache.GetTermsIndex(reader, "theRandomUnicodeString");
            Assert.AreSame(termsIndex, cache.GetTermsIndex(reader, "theRandomUnicodeString"), "Second request to cache return same array");
            BytesRef br = new BytesRef();
            for (int i = 0; i < NUM_DOCS; i++)
            {
                BytesRef term;
                int ord = termsIndex.GetOrd(i);
                if (ord == -1)
                {
                    term = null;
                }
                else
                {
                    termsIndex.LookupOrd(ord, br);
                    term = br;
                }
                string s = term is null ? null : term.Utf8ToString();
                Assert.IsTrue(unicodeStrings[i] is null || unicodeStrings[i].Equals(s, StringComparison.Ordinal), "for doc " + i + ": " + s + " does not equal: " + unicodeStrings[i]);
            }

            int nTerms = termsIndex.ValueCount;

            TermsEnum tenum = termsIndex.GetTermsEnum();
            BytesRef val = new BytesRef();
            for (int i = 0; i < nTerms; i++)
            {
                tenum.MoveNext();
                BytesRef val1 = tenum.Term;
                termsIndex.LookupOrd(i, val);
                // System.out.println("i="+i);
                Assert.AreEqual(val, val1);
            }

            // seek the enum around (note this isn't a great test here)
            int num = AtLeast(100);
            for (int i = 0; i < num; i++)
            {
                int k = Random.Next(nTerms);
                termsIndex.LookupOrd(k, val);
                Assert.AreEqual(TermsEnum.SeekStatus.FOUND, tenum.SeekCeil(val));
                Assert.AreEqual(val, tenum.Term);
            }

            for (int i = 0; i < nTerms; i++)
            {
                termsIndex.LookupOrd(i, val);
                Assert.AreEqual(TermsEnum.SeekStatus.FOUND, tenum.SeekCeil(val));
                Assert.AreEqual(val, tenum.Term);
            }

            // test bad field
            termsIndex = cache.GetTermsIndex(reader, "bogusfield");

            // getTerms
            BinaryDocValues terms = cache.GetTerms(reader, "theRandomUnicodeString", true);
            Assert.AreSame(terms, cache.GetTerms(reader, "theRandomUnicodeString", true), "Second request to cache return same array");
            IBits bits = cache.GetDocsWithField(reader, "theRandomUnicodeString");
            for (int i = 0; i < NUM_DOCS; i++)
            {
                terms.Get(i, br);
                BytesRef term;
                if (!bits.Get(i))
                {
                    term = null;
                }
                else
                {
                    term = br;
                }
                string s = term is null ? null : term.Utf8ToString();
                Assert.IsTrue(unicodeStrings[i] is null || unicodeStrings[i].Equals(s, StringComparison.Ordinal), "for doc " + i + ": " + s + " does not equal: " + unicodeStrings[i]);
            }

            // test bad field
            terms = cache.GetTerms(reader, "bogusfield", false);

            // getDocTermOrds
            SortedSetDocValues termOrds = cache.GetDocTermOrds(reader, "theRandomUnicodeMultiValuedField");
            int numEntries = cache.GetCacheEntries().Length;
            // ask for it again, and check that we didnt create any additional entries:
            termOrds = cache.GetDocTermOrds(reader, "theRandomUnicodeMultiValuedField");
            Assert.AreEqual(numEntries, cache.GetCacheEntries().Length);

            for (int i = 0; i < NUM_DOCS; i++)
            {
                termOrds.SetDocument(i);
                // this will remove identical terms. A DocTermOrds doesn't return duplicate ords for a docId
                ISet<BytesRef> values = new JCG.LinkedHashSet<BytesRef>(multiValued[i]);
                foreach (BytesRef v in values)
                {
                    if (v is null)
                    {
                        // why does this test use null values... instead of an empty list: confusing
                        break;
                    }
                    long ord = termOrds.NextOrd();
                    if (Debugging.AssertsEnabled) Debugging.Assert(ord != SortedSetDocValues.NO_MORE_ORDS);
                    BytesRef scratch = new BytesRef();
                    termOrds.LookupOrd(ord, scratch);
                    Assert.AreEqual(v, scratch);
                }
                Assert.AreEqual(SortedSetDocValues.NO_MORE_ORDS, termOrds.NextOrd());
            }

            // test bad field
            termOrds = cache.GetDocTermOrds(reader, "bogusfield");
            Assert.IsTrue(termOrds.ValueCount == 0);

            FieldCache.DEFAULT.PurgeByCacheKey(reader.CoreCacheKey);
        }

        [Test]
        public virtual void TestEmptyIndex()
        {
            Directory dir = NewDirectory();
            IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)).SetMaxBufferedDocs(500));
            writer.Dispose();
            IndexReader r = DirectoryReader.Open(dir);
            AtomicReader reader = SlowCompositeReaderWrapper.Wrap(r);
            FieldCache.DEFAULT.GetTerms(reader, "foobar", true);
            FieldCache.DEFAULT.GetTermsIndex(reader, "foobar");
            FieldCache.DEFAULT.PurgeByCacheKey(reader.CoreCacheKey);
            r.Dispose();
            dir.Dispose();
        }

        private static string GenerateString(int i)
        {
            string s = null;
            if (i > 0 && Random.Next(3) == 1)
            {
                // reuse past string -- try to find one that's not null
                for (int iter = 0; iter < 10 && s is null; iter++)
                {
                    s = unicodeStrings[Random.Next(i)];
                }
                if (s is null)
                {
                    s = TestUtil.RandomUnicodeString(Random);
                }
            }
            else
            {
                s = TestUtil.RandomUnicodeString(Random);
            }
            return s;
        }

        [Test]
        public virtual void TestDocsWithField()
        {
            IFieldCache cache = FieldCache.DEFAULT;
            cache.PurgeAllCaches();
            Assert.AreEqual(0, cache.GetCacheEntries().Length);
            cache.GetDoubles(reader, "theDouble", true);

            // The double[] takes two slots (one w/ null parser, one
            // w/ real parser), and docsWithField should also
            // have been populated:
            Assert.AreEqual(3, cache.GetCacheEntries().Length);
            IBits bits = cache.GetDocsWithField(reader, "theDouble");

            // No new entries should appear:
            Assert.AreEqual(3, cache.GetCacheEntries().Length);
            Assert.IsTrue(bits is Bits.MatchAllBits);

            Int32s ints = cache.GetInt32s(reader, "sparse", true);
            Assert.AreEqual(6, cache.GetCacheEntries().Length);
            IBits docsWithField = cache.GetDocsWithField(reader, "sparse");
            Assert.AreEqual(6, cache.GetCacheEntries().Length);
            for (int i = 0; i < docsWithField.Length; i++)
            {
                if (i % 2 == 0)
                {
                    Assert.IsTrue(docsWithField.Get(i));
                    Assert.AreEqual(i, ints.Get(i));
                }
                else
                {
                    Assert.IsFalse(docsWithField.Get(i));
                }
            }

            Int32s numInts = cache.GetInt32s(reader, "numInt", Random.NextBoolean());
            docsWithField = cache.GetDocsWithField(reader, "numInt");
            for (int i = 0; i < docsWithField.Length; i++)
            {
                if (i % 2 == 0)
                {
                    Assert.IsTrue(docsWithField.Get(i));
                    Assert.AreEqual(i, numInts.Get(i));
                }
                else
                {
                    Assert.IsFalse(docsWithField.Get(i));
                }
            }
        }

        [Test]
        public virtual void TestGetDocsWithFieldThreadSafety()
        {
            IFieldCache cache = FieldCache.DEFAULT;
            cache.PurgeAllCaches();

            int NUM_THREADS = 3;
            ThreadJob[] threads = new ThreadJob[NUM_THREADS];
            AtomicBoolean failed = new AtomicBoolean();
            AtomicInt32 iters = new AtomicInt32();
            int NUM_ITER = 200 * RandomMultiplier;
            Barrier restart = new Barrier(NUM_THREADS, (barrier) => new RunnableAnonymousClass(this, cache, iters).Run());
            for (int threadIDX = 0; threadIDX < NUM_THREADS; threadIDX++)
            {
                threads[threadIDX] = new ThreadAnonymousClass(this, cache, failed, iters, NUM_ITER, restart);
                threads[threadIDX].Start();
            }

            for (int threadIDX = 0; threadIDX < NUM_THREADS; threadIDX++)
            {
                threads[threadIDX].Join();
            }
            Assert.IsFalse(failed);
        }

        private sealed class RunnableAnonymousClass //: IThreadRunnable
        {
            private readonly TestFieldCache outerInstance;

            private readonly IFieldCache cache;
            private readonly AtomicInt32 iters;

            public RunnableAnonymousClass(TestFieldCache outerInstance, IFieldCache cache, AtomicInt32 iters)
            {
                this.outerInstance = outerInstance;
                this.cache = cache;
                this.iters = iters;
            }

            public void Run()
            {
                cache.PurgeAllCaches();
                iters.IncrementAndGet();
            }
        }

        private sealed class ThreadAnonymousClass : ThreadJob
        {
            private readonly TestFieldCache outerInstance;

            private readonly IFieldCache cache;
            private readonly AtomicBoolean failed;
            private readonly AtomicInt32 iters;
            private readonly int NUM_ITER;
            private readonly Barrier restart;

            public ThreadAnonymousClass(TestFieldCache outerInstance, IFieldCache cache, AtomicBoolean failed, AtomicInt32 iters, int NUM_ITER, Barrier restart)
            {
                this.outerInstance = outerInstance;
                this.cache = cache;
                this.failed = failed;
                this.iters = iters;
                this.NUM_ITER = NUM_ITER;
                this.restart = restart;
            }

            public override void Run()
            {

                try
                {
                    while (!failed)
                    {
                        int op = Random.Next(3);
                        if (op == 0)
                        {
                            // Purge all caches & resume, once all
                            // threads get here:
                            restart.SignalAndWait();
                            if (iters >= NUM_ITER)
                            {
                                break;
                            }
                        }
                        else if (op == 1)
                        {
                            IBits docsWithField = cache.GetDocsWithField(reader, "sparse");
                            for (int i = 0; i < docsWithField.Length; i++)
                            {
                                Assert.AreEqual(i % 2 == 0, docsWithField.Get(i));
                            }
                        }
                        else
                        {
                            Int32s ints = cache.GetInt32s(reader, "sparse", true);
                            IBits docsWithField = cache.GetDocsWithField(reader, "sparse");
                            for (int i = 0; i < docsWithField.Length; i++)
                            {
                                if (i % 2 == 0)
                                {
                                    Assert.IsTrue(docsWithField.Get(i));
                                    Assert.AreEqual(i, ints.Get(i));
                                }
                                else
                                {
                                    Assert.IsFalse(docsWithField.Get(i));
                                }
                            }
                        }
                    }
                }
                catch (Exception t) when (t.IsThrowable())
                {
                    failed.Value = true;
                    throw RuntimeException.Create(t);
                }
            }
        }

        [Test]
        public virtual void TestDocValuesIntegration()
        {
            AssumeTrue("3.x does not support docvalues", DefaultCodecSupportsDocValues);
            Directory dir = NewDirectory();
            IndexWriterConfig iwc = NewIndexWriterConfig(TEST_VERSION_CURRENT, null);
            RandomIndexWriter iw = new RandomIndexWriter(Random, dir, iwc);
            Document doc = new Document();
            doc.Add(new BinaryDocValuesField("binary", new BytesRef("binary value")));
            doc.Add(new SortedDocValuesField("sorted", new BytesRef("sorted value")));
            doc.Add(new NumericDocValuesField("numeric", 42));
            if (DefaultCodecSupportsSortedSet)
            {
                doc.Add(new SortedSetDocValuesField("sortedset", new BytesRef("sortedset value1")));
                doc.Add(new SortedSetDocValuesField("sortedset", new BytesRef("sortedset value2")));
            }
            iw.AddDocument(doc);
            DirectoryReader ir = iw.GetReader();
            iw.Dispose();
            AtomicReader ar = GetOnlySegmentReader(ir);

            BytesRef scratch = new BytesRef();

            // Binary type: can be retrieved via getTerms()
            try
            {
                FieldCache.DEFAULT.GetInt32s(ar, "binary", false);
                Assert.Fail();
            }
            catch (Exception expected) when (expected.IsIllegalStateException())
            {
            }

            BinaryDocValues binary = FieldCache.DEFAULT.GetTerms(ar, "binary", true);
            binary.Get(0, scratch);
            Assert.AreEqual("binary value", scratch.Utf8ToString());

            try
            {
                FieldCache.DEFAULT.GetTermsIndex(ar, "binary");
                Assert.Fail();
            }
            catch (Exception expected) when (expected.IsIllegalStateException())
            {
            }

            try
            {
                FieldCache.DEFAULT.GetDocTermOrds(ar, "binary");
                Assert.Fail();
            }
            catch (Exception expected) when (expected.IsIllegalStateException())
            {
            }

            try
            {
                new DocTermOrds(ar, null, "binary");
                Assert.Fail();
            }
            catch (Exception expected) when (expected.IsIllegalStateException())
            {
            }

            IBits bits = FieldCache.DEFAULT.GetDocsWithField(ar, "binary");
            Assert.IsTrue(bits.Get(0));

            // Sorted type: can be retrieved via getTerms(), getTermsIndex(), getDocTermOrds()
            try
            {
                FieldCache.DEFAULT.GetInt32s(ar, "sorted", false);
                Assert.Fail();
            }
            catch (Exception expected) when (expected.IsIllegalStateException())
            {
            }

            try
            {
                new DocTermOrds(ar, null, "sorted");
                Assert.Fail();
            }
            catch (Exception expected) when (expected.IsIllegalStateException())
            {
            }

            binary = FieldCache.DEFAULT.GetTerms(ar, "sorted", true);
            binary.Get(0, scratch);
            Assert.AreEqual("sorted value", scratch.Utf8ToString());

            SortedDocValues sorted = FieldCache.DEFAULT.GetTermsIndex(ar, "sorted");
            Assert.AreEqual(0, sorted.GetOrd(0));
            Assert.AreEqual(1, sorted.ValueCount);
            sorted.Get(0, scratch);
            Assert.AreEqual("sorted value", scratch.Utf8ToString());

            SortedSetDocValues sortedSet = FieldCache.DEFAULT.GetDocTermOrds(ar, "sorted");
            sortedSet.SetDocument(0);
            Assert.AreEqual(0, sortedSet.NextOrd());
            Assert.AreEqual(SortedSetDocValues.NO_MORE_ORDS, sortedSet.NextOrd());
            Assert.AreEqual(1, sortedSet.ValueCount);

            bits = FieldCache.DEFAULT.GetDocsWithField(ar, "sorted");
            Assert.IsTrue(bits.Get(0));

            // Numeric type: can be retrieved via getInts() and so on
            Int32s numeric = FieldCache.DEFAULT.GetInt32s(ar, "numeric", false);
            Assert.AreEqual(42, numeric.Get(0));

            try
            {
                FieldCache.DEFAULT.GetTerms(ar, "numeric", true);
                Assert.Fail();
            }
            catch (Exception expected) when (expected.IsIllegalStateException())
            {
            }

            try
            {
                FieldCache.DEFAULT.GetTermsIndex(ar, "numeric");
                Assert.Fail();
            }
            catch (Exception expected) when (expected.IsIllegalStateException())
            {
            }

            try
            {
                FieldCache.DEFAULT.GetDocTermOrds(ar, "numeric");
                Assert.Fail();
            }
            catch (Exception expected) when (expected.IsIllegalStateException())
            {
            }

            try
            {
                new DocTermOrds(ar, null, "numeric");
                Assert.Fail();
            }
            catch (Exception expected) when (expected.IsIllegalStateException())
            {
            }

            bits = FieldCache.DEFAULT.GetDocsWithField(ar, "numeric");
            Assert.IsTrue(bits.Get(0));

            // SortedSet type: can be retrieved via getDocTermOrds() 
            if (DefaultCodecSupportsSortedSet)
            {
                try
                {
                    FieldCache.DEFAULT.GetInt32s(ar, "sortedset", false);
                    Assert.Fail();
                }
                catch (Exception expected) when (expected.IsIllegalStateException())
                {
                }

                try
                {
                    FieldCache.DEFAULT.GetTerms(ar, "sortedset", true);
                    Assert.Fail();
                }
                catch (Exception expected) when (expected.IsIllegalStateException())
                {
                }

                try
                {
                    FieldCache.DEFAULT.GetTermsIndex(ar, "sortedset");
                    Assert.Fail();
                }
                catch (Exception expected) when (expected.IsIllegalStateException())
                {
                }

                try
                {
                    new DocTermOrds(ar, null, "sortedset");
                    Assert.Fail();
                }
                catch (Exception expected) when (expected.IsIllegalStateException())
                {
                }

                sortedSet = FieldCache.DEFAULT.GetDocTermOrds(ar, "sortedset");
                sortedSet.SetDocument(0);
                Assert.AreEqual(0, sortedSet.NextOrd());
                Assert.AreEqual(1, sortedSet.NextOrd());
                Assert.AreEqual(SortedSetDocValues.NO_MORE_ORDS, sortedSet.NextOrd());
                Assert.AreEqual(2, sortedSet.ValueCount);

                bits = FieldCache.DEFAULT.GetDocsWithField(ar, "sortedset");
                Assert.IsTrue(bits.Get(0));
            }

            ir.Dispose();
            dir.Dispose();
        }

        [Test]
        public virtual void TestNonexistantFields()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter iw = new RandomIndexWriter(Random, dir);
            Document doc = new Document();
            iw.AddDocument(doc);
            DirectoryReader ir = iw.GetReader();
            iw.Dispose();

            AtomicReader ar = GetOnlySegmentReader(ir);

            IFieldCache cache = FieldCache.DEFAULT;
            cache.PurgeAllCaches();
            Assert.AreEqual(0, cache.GetCacheEntries().Length);

#pragma warning disable 612, 618
            Bytes bytes = cache.GetBytes(ar, "bogusbytes", true);
            Assert.AreEqual((byte)0, bytes.Get(0));

            Int16s shorts = cache.GetInt16s(ar, "bogusshorts", true);
            Assert.AreEqual(0, shorts.Get(0));
#pragma warning restore 612, 618

            Int32s ints = cache.GetInt32s(ar, "bogusints", true);
            Assert.AreEqual(0, ints.Get(0));

            Int64s longs = cache.GetInt64s(ar, "boguslongs", true);
            Assert.AreEqual(0, longs.Get(0));

            Singles floats = cache.GetSingles(ar, "bogusfloats", true);
            Assert.AreEqual(0, floats.Get(0), 0.0f);

            Doubles doubles = cache.GetDoubles(ar, "bogusdoubles", true);
            Assert.AreEqual(0, doubles.Get(0), 0.0D);

            BytesRef scratch = new BytesRef();
            BinaryDocValues binaries = cache.GetTerms(ar, "bogusterms", true);
            binaries.Get(0, scratch);
            Assert.AreEqual(0, scratch.Length);

            SortedDocValues sorted = cache.GetTermsIndex(ar, "bogustermsindex");
            Assert.AreEqual(-1, sorted.GetOrd(0));
            sorted.Get(0, scratch);
            Assert.AreEqual(0, scratch.Length);

            SortedSetDocValues sortedSet = cache.GetDocTermOrds(ar, "bogusmultivalued");
            sortedSet.SetDocument(0);
            Assert.AreEqual(SortedSetDocValues.NO_MORE_ORDS, sortedSet.NextOrd());

            IBits bits = cache.GetDocsWithField(ar, "bogusbits");
            Assert.IsFalse(bits.Get(0));

            // check that we cached nothing
            Assert.AreEqual(0, cache.GetCacheEntries().Length);
            ir.Dispose();
            dir.Dispose();
        }

        [Test]
        public virtual void TestNonIndexedFields()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter iw = new RandomIndexWriter(Random, dir);
            Document doc = new Document();
            doc.Add(new StoredField("bogusbytes", "bogus"));
            doc.Add(new StoredField("bogusshorts", "bogus"));
            doc.Add(new StoredField("bogusints", "bogus"));
            doc.Add(new StoredField("boguslongs", "bogus"));
            doc.Add(new StoredField("bogusfloats", "bogus"));
            doc.Add(new StoredField("bogusdoubles", "bogus"));
            doc.Add(new StoredField("bogusterms", "bogus"));
            doc.Add(new StoredField("bogustermsindex", "bogus"));
            doc.Add(new StoredField("bogusmultivalued", "bogus"));
            doc.Add(new StoredField("bogusbits", "bogus"));
            iw.AddDocument(doc);
            DirectoryReader ir = iw.GetReader();
            iw.Dispose();

            AtomicReader ar = GetOnlySegmentReader(ir);

            IFieldCache cache = FieldCache.DEFAULT;
            cache.PurgeAllCaches();
            Assert.AreEqual(0, cache.GetCacheEntries().Length);

#pragma warning disable 612, 618
            Bytes bytes = cache.GetBytes(ar, "bogusbytes", true);
            Assert.AreEqual((byte)0, bytes.Get(0));

            Int16s shorts = cache.GetInt16s(ar, "bogusshorts", true);
            Assert.AreEqual(0, shorts.Get(0));
#pragma warning restore 612, 618

            Int32s ints = cache.GetInt32s(ar, "bogusints", true);
            Assert.AreEqual(0, ints.Get(0));

            Int64s longs = cache.GetInt64s(ar, "boguslongs", true);
            Assert.AreEqual(0, longs.Get(0));

            Singles floats = cache.GetSingles(ar, "bogusfloats", true);
            Assert.AreEqual(0, floats.Get(0), 0.0f);

            Doubles doubles = cache.GetDoubles(ar, "bogusdoubles", true);
            Assert.AreEqual(0, doubles.Get(0), 0.0D);

            BytesRef scratch = new BytesRef();
            BinaryDocValues binaries = cache.GetTerms(ar, "bogusterms", true);
            binaries.Get(0, scratch);
            Assert.AreEqual(0, scratch.Length);

            SortedDocValues sorted = cache.GetTermsIndex(ar, "bogustermsindex");
            Assert.AreEqual(-1, sorted.GetOrd(0));
            sorted.Get(0, scratch);
            Assert.AreEqual(0, scratch.Length);

            SortedSetDocValues sortedSet = cache.GetDocTermOrds(ar, "bogusmultivalued");
            sortedSet.SetDocument(0);
            Assert.AreEqual(SortedSetDocValues.NO_MORE_ORDS, sortedSet.NextOrd());

            IBits bits = cache.GetDocsWithField(ar, "bogusbits");
            Assert.IsFalse(bits.Get(0));

            // check that we cached nothing
            Assert.AreEqual(0, cache.GetCacheEntries().Length);
            ir.Dispose();
            dir.Dispose();
        }

        // Make sure that the use of GrowableWriter doesn't prevent from using the full long range
        [Test]
        public virtual void TestLongFieldCache()
        {
            Directory dir = NewDirectory();
            IndexWriterConfig cfg = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random));
            cfg.SetMergePolicy(NewLogMergePolicy());
            RandomIndexWriter iw = new RandomIndexWriter(Random, dir, cfg);
            Document doc = new Document();
            Int64Field field = new Int64Field("f", 0L, Field.Store.YES);
            doc.Add(field);
            long[] values = new long[TestUtil.NextInt32(Random, 1, 10)];
            for (int i = 0; i < values.Length; ++i)
            {
                long v;
                switch (Random.Next(10))
                {
                    case 0:
                        v = long.MinValue;
                        break;
                    case 1:
                        v = 0;
                        break;
                    case 2:
                        v = long.MaxValue;
                        break;
                    default:
                        v = TestUtil.NextInt64(Random, -10, 10);
                        break;
                }
                values[i] = v;
                if (v == 0 && Random.NextBoolean())
                {
                    // missing
                    iw.AddDocument(new Document());
                }
                else
                {
                    field.SetInt64Value(v);
                    iw.AddDocument(doc);
                }
            }
            iw.ForceMerge(1);
            DirectoryReader reader = iw.GetReader();
            Int64s longs = FieldCache.DEFAULT.GetInt64s(GetOnlySegmentReader(reader), "f", false);
            for (int i = 0; i < values.Length; ++i)
            {
                Assert.AreEqual(values[i], longs.Get(i));
            }
            reader.Dispose();
            iw.Dispose();
            dir.Dispose();
        }

        // Make sure that the use of GrowableWriter doesn't prevent from using the full int range
        [Test]
        public virtual void TestIntFieldCache()
        {
            Directory dir = NewDirectory();
            IndexWriterConfig cfg = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random));
            cfg.SetMergePolicy(NewLogMergePolicy());
            RandomIndexWriter iw = new RandomIndexWriter(Random, dir, cfg);
            Document doc = new Document();
            Int32Field field = new Int32Field("f", 0, Field.Store.YES);
            doc.Add(field);
            int[] values = new int[TestUtil.NextInt32(Random, 1, 10)];
            for (int i = 0; i < values.Length; ++i)
            {
                int v;
                switch (Random.Next(10))
                {
                    case 0:
                        v = int.MinValue;
                        break;
                    case 1:
                        v = 0;
                        break;
                    case 2:
                        v = int.MaxValue;
                        break;
                    default:
                        v = TestUtil.NextInt32(Random, -10, 10);
                        break;
                }
                values[i] = v;
                if (v == 0 && Random.NextBoolean())
                {
                    // missing
                    iw.AddDocument(new Document());
                }
                else
                {
                    field.SetInt32Value(v);
                    iw.AddDocument(doc);
                }
            }
            iw.ForceMerge(1);
            DirectoryReader reader = iw.GetReader();
            Int32s ints = FieldCache.DEFAULT.GetInt32s(GetOnlySegmentReader(reader), "f", false);
            for (int i = 0; i < values.Length; ++i)
            {
                Assert.AreEqual(values[i], ints.Get(i));
            }
            reader.Dispose();
            iw.Dispose();
            dir.Dispose();
        }

    }

}