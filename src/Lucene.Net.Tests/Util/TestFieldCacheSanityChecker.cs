using Lucene.Net.Documents;
using Lucene.Net.Search;
using NUnit.Framework;
using System;
using System.Globalization;
using System.IO;
using Assert = Lucene.Net.TestFramework.Assert;
using Console = Lucene.Net.Util.SystemConsole;

namespace Lucene.Net.Util
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
    using Directory = Lucene.Net.Store.Directory;
    using DirectoryReader = Lucene.Net.Index.DirectoryReader;
    using Document = Documents.Document;
    using Field = Field;
    using IndexWriter = Lucene.Net.Index.IndexWriter;
    using Insanity = Lucene.Net.Util.FieldCacheSanityChecker.Insanity;
    using InsanityType = Lucene.Net.Util.FieldCacheSanityChecker.InsanityType;
    using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
    using MultiReader = Lucene.Net.Index.MultiReader;
    using SlowCompositeReaderWrapper = Lucene.Net.Index.SlowCompositeReaderWrapper;

    [TestFixture]
    public class TestFieldCacheSanityChecker : LuceneTestCase
    {
        protected internal AtomicReader readerA;
        protected internal AtomicReader readerB;
        protected internal AtomicReader readerX;
        protected internal AtomicReader readerAclone;
        protected internal Directory dirA, dirB;
        private const int NUM_DOCS = 1000;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            dirA = NewDirectory();
            dirB = NewDirectory();

            IndexWriter wA = new IndexWriter(dirA, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)));
            IndexWriter wB = new IndexWriter(dirB, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)));

            long theLong = long.MaxValue;
            double theDouble = double.MaxValue;
            sbyte theByte = sbyte.MaxValue;
            short theShort = short.MaxValue;
            int theInt = int.MaxValue;
            float theFloat = float.MaxValue;
            for (int i = 0; i < NUM_DOCS; i++)
            {
                Document doc = new Document();
                doc.Add(NewStringField("theLong", J2N.Numerics.Int64.ToString(theLong--, CultureInfo.InvariantCulture), Field.Store.NO));
                doc.Add(NewStringField("theDouble", J2N.Numerics.Double.ToString(theDouble--, CultureInfo.InvariantCulture), Field.Store.NO));
                doc.Add(NewStringField("theByte", J2N.Numerics.SByte.ToString(theByte--, CultureInfo.InvariantCulture), Field.Store.NO));
                doc.Add(NewStringField("theShort", J2N.Numerics.Int16.ToString(theShort--, CultureInfo.InvariantCulture), Field.Store.NO));
                doc.Add(NewStringField("theInt", J2N.Numerics.Int32.ToString(theInt--, CultureInfo.InvariantCulture), Field.Store.NO));
                doc.Add(NewStringField("theFloat", J2N.Numerics.Single.ToString(theFloat--, CultureInfo.InvariantCulture), Field.Store.NO));
                if (0 == i % 3)
                {
                    wA.AddDocument(doc);
                }
                else
                {
                    wB.AddDocument(doc);
                }
            }
            wA.Dispose();
            wB.Dispose();
            DirectoryReader rA = DirectoryReader.Open(dirA);
            readerA = SlowCompositeReaderWrapper.Wrap(rA);
            readerAclone = SlowCompositeReaderWrapper.Wrap(rA);
            readerA = SlowCompositeReaderWrapper.Wrap(DirectoryReader.Open(dirA));
            readerB = SlowCompositeReaderWrapper.Wrap(DirectoryReader.Open(dirB));
            readerX = SlowCompositeReaderWrapper.Wrap(new MultiReader(readerA, readerB));

            // LUCENENET specific.Ensure we have an infostream attached to the default FieldCache
            // when running the tests. In Java, this was done in the Core.Search.TestFieldCache.TestInfoStream()
            // method (which polluted the state of these tests), but we need to make the tests self-contained
            // so they can be run correctly regardless of order. Not setting the InfoStream skips an execution
            // path within these tests, so we should do it to make sure we test all of the code.
            FieldCache.DEFAULT.InfoStream = new StringWriter();
        }

        [TearDown]
        public override void TearDown()
        {
            readerA.Dispose();
            readerAclone.Dispose();
            readerB.Dispose();
            readerX.Dispose();
            dirA.Dispose();
            dirB.Dispose();

            // LUCENENET specific. See <see cref="SetUp()"/>. Dispose our InfoStream and set it to null
            // to avoid polluting the state of other tests.
            FieldCache.DEFAULT.InfoStream.Dispose();
            FieldCache.DEFAULT.InfoStream = null;
            base.TearDown();
        }

        [Test]
        public virtual void TestSanity()
        {
            IFieldCache cache = FieldCache.DEFAULT;
            cache.PurgeAllCaches();

            cache.GetDoubles(readerA, "theDouble", false);
#pragma warning disable 612, 618
            cache.GetDoubles(readerA, "theDouble", FieldCache.DEFAULT_DOUBLE_PARSER, false);
            cache.GetDoubles(readerAclone, "theDouble", FieldCache.DEFAULT_DOUBLE_PARSER, false);
            cache.GetDoubles(readerB, "theDouble", FieldCache.DEFAULT_DOUBLE_PARSER, false);

            cache.GetInt32s(readerX, "theInt", false);
            cache.GetInt32s(readerX, "theInt", FieldCache.DEFAULT_INT32_PARSER, false);
#pragma warning restore 612, 618

            // // //

            Insanity[] insanity = FieldCacheSanityChecker.CheckSanity(cache.GetCacheEntries());

            if (0 < insanity.Length)
            {
                DumpArray(TestType.Name + "#" + TestName + " INSANITY", insanity, Console.Error);
            }

            Assert.AreEqual(0, insanity.Length, "shouldn't be any cache insanity");
            cache.PurgeAllCaches();
        }

        [Test]
        public virtual void TestInsanity1()
        {
            IFieldCache cache = FieldCache.DEFAULT;
            cache.PurgeAllCaches();

#pragma warning disable 612, 618
            cache.GetInt32s(readerX, "theInt", FieldCache.DEFAULT_INT32_PARSER, false);
            cache.GetTerms(readerX, "theInt", false);
            cache.GetBytes(readerX, "theByte", false);
#pragma warning restore 612, 618

            // // //

            Insanity[] insanity = FieldCacheSanityChecker.CheckSanity(cache.GetCacheEntries());

            Assert.AreEqual(1, insanity.Length, "wrong number of cache errors");
            Assert.AreEqual(InsanityType.VALUEMISMATCH, insanity[0].Type, "wrong type of cache error");
            Assert.AreEqual(2, insanity[0].CacheEntries.Length, "wrong number of entries in cache error");

            // we expect bad things, don't let tearDown complain about them
            cache.PurgeAllCaches();
        }

        [Test]
        public virtual void TestInsanity2()
        {
            IFieldCache cache = FieldCache.DEFAULT;
            cache.PurgeAllCaches();

            cache.GetTerms(readerA, "theInt", false);
            cache.GetTerms(readerB, "theInt", false);
            cache.GetTerms(readerX, "theInt", false);
#pragma warning disable 612, 618
            cache.GetBytes(readerX, "theByte", false);
#pragma warning restore 612, 618

            // // //

            Insanity[] insanity = FieldCacheSanityChecker.CheckSanity(cache.GetCacheEntries());

            Assert.AreEqual(1, insanity.Length, "wrong number of cache errors");
            Assert.AreEqual(InsanityType.SUBREADER, insanity[0].Type, "wrong type of cache error");
            Assert.AreEqual(3, insanity[0].CacheEntries.Length, "wrong number of entries in cache error");

            // we expect bad things, don't let tearDown complain about them
            cache.PurgeAllCaches();
        }
    }
}