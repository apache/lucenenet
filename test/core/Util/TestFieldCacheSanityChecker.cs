using NUnit.Framework;
using System;

namespace Lucene.Net.Util
{
    using AtomicReader = Lucene.Net.Index.AtomicReader;
    using Directory = Lucene.Net.Store.Directory;
    using DirectoryReader = Lucene.Net.Index.DirectoryReader;
    using Document = Lucene.Net.Document.Document;
    using Field = Lucene.Net.Document.Field;
    using FieldCache = Lucene.Net.Search.FieldCache;
    using FieldCache_Fields = Lucene.Net.Search.FieldCache_Fields;
    using IndexWriter = Lucene.Net.Index.IndexWriter;
    using Insanity = Lucene.Net.Util.FieldCacheSanityChecker.Insanity;
    using InsanityType = Lucene.Net.Util.FieldCacheSanityChecker.InsanityType;

    /*
        /// Copyright 2009 The Apache Software Foundation
        ///
        /// Licensed under the Apache License, Version 2.0 (the "License");
        /// you may not use this file except in compliance with the License.
        /// You may obtain a copy of the License at
        ///
        ///     http://www.apache.org/licenses/LICENSE-2.0
        ///
        /// Unless required by applicable law or agreed to in writing, software
        /// distributed under the License is distributed on an "AS IS" BASIS,
        /// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
        /// See the License for the specific language governing permissions and
        /// limitations under the License.
        */

    using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
    using MultiReader = Lucene.Net.Index.MultiReader;
    using SlowCompositeReaderWrapper = Lucene.Net.Index.SlowCompositeReaderWrapper;

    [TestFixture]
    public class TestFieldCacheSanityChecker : LuceneTestCase
    {
        protected internal AtomicReader ReaderA;
        protected internal AtomicReader ReaderB;
        protected internal AtomicReader ReaderX;
        protected internal AtomicReader ReaderAclone;
        protected internal Directory DirA, DirB;
        private const int NUM_DOCS = 1000;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            DirA = NewDirectory();
            DirB = NewDirectory();

            IndexWriter wA = new IndexWriter(DirA, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())));
            IndexWriter wB = new IndexWriter(DirB, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())));

            long theLong = long.MaxValue;
            double theDouble = double.MaxValue;
            sbyte theByte = sbyte.MaxValue;
            short theShort = short.MaxValue;
            int theInt = int.MaxValue;
            float theFloat = float.MaxValue;
            for (int i = 0; i < NUM_DOCS; i++)
            {
                Document doc = new Document();
                doc.Add(NewStringField("theLong", Convert.ToString(theLong--), Field.Store.NO));
                doc.Add(NewStringField("theDouble", Convert.ToString(theDouble--), Field.Store.NO));
                doc.Add(NewStringField("theByte", Convert.ToString(theByte--), Field.Store.NO));
                doc.Add(NewStringField("theShort", Convert.ToString(theShort--), Field.Store.NO));
                doc.Add(NewStringField("theInt", Convert.ToString(theInt--), Field.Store.NO));
                doc.Add(NewStringField("theFloat", Convert.ToString(theFloat--), Field.Store.NO));
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
            DirectoryReader rA = DirectoryReader.Open(DirA);
            ReaderA = SlowCompositeReaderWrapper.Wrap(rA);
            ReaderAclone = SlowCompositeReaderWrapper.Wrap(rA);
            ReaderA = SlowCompositeReaderWrapper.Wrap(DirectoryReader.Open(DirA));
            ReaderB = SlowCompositeReaderWrapper.Wrap(DirectoryReader.Open(DirB));
            ReaderX = SlowCompositeReaderWrapper.Wrap(new MultiReader(ReaderA, ReaderB));
        }

        [TearDown]
        public override void TearDown()
        {
            ReaderA.Dispose();
            ReaderAclone.Dispose();
            ReaderB.Dispose();
            ReaderX.Dispose();
            DirA.Dispose();
            DirB.Dispose();
            base.TearDown();
        }

        [Test]
        public virtual void TestSanity()
        {
            FieldCache cache = FieldCache_Fields.DEFAULT;
            cache.PurgeAllCaches();

            cache.GetDoubles(ReaderA, "theDouble", false);
            cache.GetDoubles(ReaderA, "theDouble", FieldCache_Fields.DEFAULT_DOUBLE_PARSER, false);
            cache.GetDoubles(ReaderAclone, "theDouble", FieldCache_Fields.DEFAULT_DOUBLE_PARSER, false);
            cache.GetDoubles(ReaderB, "theDouble", FieldCache_Fields.DEFAULT_DOUBLE_PARSER, false);

            cache.GetInts(ReaderX, "theInt", false);
            cache.GetInts(ReaderX, "theInt", FieldCache_Fields.DEFAULT_INT_PARSER, false);

            // // //

            Insanity[] insanity = FieldCacheSanityChecker.CheckSanity(cache.CacheEntries);

            if (0 < insanity.Length)
            {
                DumpArray(TestClass.Name + "#" + TestName + " INSANITY", insanity, Console.Error);
            }

            Assert.AreEqual(0, insanity.Length, "shouldn't be any cache insanity");
            cache.PurgeAllCaches();
        }

        [Test]
        public virtual void TestInsanity1()
        {
            FieldCache cache = FieldCache_Fields.DEFAULT;
            cache.PurgeAllCaches();

            cache.GetInts(ReaderX, "theInt", FieldCache_Fields.DEFAULT_INT_PARSER, false);
            cache.GetTerms(ReaderX, "theInt", false);
            cache.GetBytes(ReaderX, "theByte", false);

            // // //

            Insanity[] insanity = FieldCacheSanityChecker.CheckSanity(cache.CacheEntries);

            Assert.AreEqual(1, insanity.Length, "wrong number of cache errors");
            Assert.AreEqual(InsanityType.VALUEMISMATCH, insanity[0].Type, "wrong type of cache error");
            Assert.AreEqual(2, insanity[0].CacheEntries.Length, "wrong number of entries in cache error");

            // we expect bad things, don't let tearDown complain about them
            cache.PurgeAllCaches();
        }

        [Test]
        public virtual void TestInsanity2()
        {
            FieldCache cache = FieldCache_Fields.DEFAULT;
            cache.PurgeAllCaches();

            cache.GetTerms(ReaderA, "theInt", false);
            cache.GetTerms(ReaderB, "theInt", false);
            cache.GetTerms(ReaderX, "theInt", false);

            cache.GetBytes(ReaderX, "theByte", false);

            // // //

            Insanity[] insanity = FieldCacheSanityChecker.CheckSanity(cache.CacheEntries);

            Assert.AreEqual(1, insanity.Length, "wrong number of cache errors");
            Assert.AreEqual(InsanityType.SUBREADER, insanity[0].Type, "wrong type of cache error");
            Assert.AreEqual(3, insanity[0].CacheEntries.Length, "wrong number of entries in cache error");

            // we expect bad things, don't let tearDown complain about them
            cache.PurgeAllCaches();
        }
    }
}