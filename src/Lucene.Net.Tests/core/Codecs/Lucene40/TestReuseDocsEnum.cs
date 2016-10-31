using System;
using System.Collections.Generic;

namespace Lucene.Net.Codecs.Lucene40
{
    using Lucene.Net.Randomized.Generators;
    using Lucene.Net.Support;
    using NUnit.Framework;
    using AtomicReader = Lucene.Net.Index.AtomicReader;
    using AtomicReaderContext = Lucene.Net.Index.AtomicReaderContext;
    using Bits = Lucene.Net.Util.Bits;
    using BytesRef = Lucene.Net.Util.BytesRef;
    using Directory = Lucene.Net.Store.Directory;
    using DirectoryReader = Lucene.Net.Index.DirectoryReader;
    using DocsEnum = Lucene.Net.Index.DocsEnum;
    using IndexWriter = Lucene.Net.Index.IndexWriter;
    using IOUtils = Lucene.Net.Util.IOUtils;
    using LineFileDocs = Lucene.Net.Util.LineFileDocs;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using MatchNoBits = Lucene.Net.Util.Bits_MatchNoBits;

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

    using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
    using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;
    using Terms = Lucene.Net.Index.Terms;
    using TermsEnum = Lucene.Net.Index.TermsEnum;
    using TestUtil = Lucene.Net.Util.TestUtil;

    // TODO: really this should be in BaseTestPF or somewhere else? useful test!
    [TestFixture]
    public class TestReuseDocsEnum : LuceneTestCase
    {
        /// <summary>
        /// LUCENENET specific
        /// Is non-static because OLD_FORMAT_IMPERSONATION_IS_ACTIVE is no longer static.
        /// </summary>
        [OneTimeSetUp]
        public void BeforeClass()
        {
            OLD_FORMAT_IMPERSONATION_IS_ACTIVE = true; // explicitly instantiates ancient codec
        }

        [Test]
        public virtual void TestReuseDocsEnumNoReuse()
        {
            Directory dir = NewDirectory();
            Codec cp = TestUtil.AlwaysPostingsFormat(new Lucene40RWPostingsFormat(OLD_FORMAT_IMPERSONATION_IS_ACTIVE));
            RandomIndexWriter writer = new RandomIndexWriter(Random(), dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetCodec(cp));
            int numdocs = AtLeast(20);
            CreateRandomIndex(numdocs, writer, Random());
            writer.Commit();

            DirectoryReader open = DirectoryReader.Open(dir);
            foreach (AtomicReaderContext ctx in open.Leaves)
            {
                AtomicReader indexReader = (AtomicReader)ctx.Reader;
                Terms terms = indexReader.Terms("body");
                TermsEnum iterator = terms.Iterator(null);
                IdentityHashMap<DocsEnum, bool?> enums = new IdentityHashMap<DocsEnum, bool?>();
                MatchNoBits bits = new MatchNoBits(indexReader.MaxDoc);
                while ((iterator.Next()) != null)
                {
                    DocsEnum docs = iterator.Docs(Random().NextBoolean() ? bits : new MatchNoBits(indexReader.MaxDoc), null, Random().NextBoolean() ? DocsEnum.FLAG_FREQS : DocsEnum.FLAG_NONE);
                    enums[docs] = true;
                }

                Assert.AreEqual(terms.Size(), enums.Count);
            }
            IOUtils.Close(writer, open, dir);
        }

        // tests for reuse only if bits are the same either null or the same instance
        [Test]
        public virtual void TestReuseDocsEnumSameBitsOrNull()
        {
            Directory dir = NewDirectory();
            Codec cp = TestUtil.AlwaysPostingsFormat(new Lucene40RWPostingsFormat(OLD_FORMAT_IMPERSONATION_IS_ACTIVE));
            RandomIndexWriter writer = new RandomIndexWriter(Random(), dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetCodec(cp));
            int numdocs = AtLeast(20);
            CreateRandomIndex(numdocs, writer, Random());
            writer.Commit();

            DirectoryReader open = DirectoryReader.Open(dir);
            foreach (AtomicReaderContext ctx in open.Leaves)
            {
                Terms terms = ((AtomicReader)ctx.Reader).Terms("body");
                TermsEnum iterator = terms.Iterator(null);
                IdentityHashMap<DocsEnum, bool?> enums = new IdentityHashMap<DocsEnum, bool?>();
                MatchNoBits bits = new MatchNoBits(open.MaxDoc);
                DocsEnum docs = null;
                while ((iterator.Next()) != null)
                {
                    docs = iterator.Docs(bits, docs, Random().NextBoolean() ? DocsEnum.FLAG_FREQS : DocsEnum.FLAG_NONE);
                    enums[docs] = true;
                }

                Assert.AreEqual(1, enums.Count);
                enums.Clear();
                iterator = terms.Iterator(null);
                docs = null;
                while ((iterator.Next()) != null)
                {
                    docs = iterator.Docs(new MatchNoBits(open.MaxDoc), docs, Random().NextBoolean() ? DocsEnum.FLAG_FREQS : DocsEnum.FLAG_NONE);
                    enums[docs] = true;
                }
                Assert.AreEqual(terms.Size(), enums.Count);

                enums.Clear();
                iterator = terms.Iterator(null);
                docs = null;
                while ((iterator.Next()) != null)
                {
                    docs = iterator.Docs(null, docs, Random().NextBoolean() ? DocsEnum.FLAG_FREQS : DocsEnum.FLAG_NONE);
                    enums[docs] = true;
                }
                Assert.AreEqual(1, enums.Count);
            }
            IOUtils.Close(writer, open, dir);
        }

        // make sure we never reuse from another reader even if it is the same field & codec etc
        [Test]
        public virtual void TestReuseDocsEnumDifferentReader()
        {
            Directory dir = NewDirectory();
            Codec cp = TestUtil.AlwaysPostingsFormat(new Lucene40RWPostingsFormat(OLD_FORMAT_IMPERSONATION_IS_ACTIVE));
            MockAnalyzer analyzer = new MockAnalyzer(Random());
            analyzer.MaxTokenLength = TestUtil.NextInt(Random(), 1, IndexWriter.MAX_TERM_LENGTH);

            RandomIndexWriter writer = new RandomIndexWriter(Random(), dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer).SetCodec(cp));
            int numdocs = AtLeast(20);
            CreateRandomIndex(numdocs, writer, Random());
            writer.Commit();

            DirectoryReader firstReader = DirectoryReader.Open(dir);
            DirectoryReader secondReader = DirectoryReader.Open(dir);
            IList<AtomicReaderContext> leaves = firstReader.Leaves;
            IList<AtomicReaderContext> leaves2 = secondReader.Leaves;

            foreach (AtomicReaderContext ctx in leaves)
            {
                Terms terms = ((AtomicReader)ctx.Reader).Terms("body");
                TermsEnum iterator = terms.Iterator(null);
                IdentityHashMap<DocsEnum, bool?> enums = new IdentityHashMap<DocsEnum, bool?>();
                MatchNoBits bits = new MatchNoBits(firstReader.MaxDoc);
                iterator = terms.Iterator(null);
                DocsEnum docs = null;
                BytesRef term = null;
                while ((term = iterator.Next()) != null)
                {
                    docs = iterator.Docs(null, RandomDocsEnum("body", term, leaves2, bits), Random().NextBoolean() ? DocsEnum.FLAG_FREQS : DocsEnum.FLAG_NONE);
                    enums[docs] = true;
                }
                Assert.AreEqual(terms.Size(), enums.Count);

                iterator = terms.Iterator(null);
                enums.Clear();
                docs = null;
                while ((term = iterator.Next()) != null)
                {
                    docs = iterator.Docs(bits, RandomDocsEnum("body", term, leaves2, bits), Random().NextBoolean() ? DocsEnum.FLAG_FREQS : DocsEnum.FLAG_NONE);
                    enums[docs] = true;
                }
                Assert.AreEqual(terms.Size(), enums.Count);
            }
            IOUtils.Close(writer, firstReader, secondReader, dir);
        }

        public virtual DocsEnum RandomDocsEnum(string field, BytesRef term, IList<AtomicReaderContext> readers, Bits bits)
        {
            if (Random().Next(10) == 0)
            {
                return null;
            }
            AtomicReader indexReader = (AtomicReader)readers[Random().Next(readers.Count)].Reader;
            Terms terms = indexReader.Terms(field);
            if (terms == null)
            {
                return null;
            }
            TermsEnum iterator = terms.Iterator(null);
            if (iterator.SeekExact(term))
            {
                return iterator.Docs(bits, null, Random().NextBoolean() ? DocsEnum.FLAG_FREQS : DocsEnum.FLAG_NONE);
            }
            return null;
        }

        /// <summary>
        /// populates a writer with random stuff. this must be fully reproducable with
        /// the seed!
        /// </summary>
        public static void CreateRandomIndex(int numdocs, RandomIndexWriter writer, Random random)
        {
            LineFileDocs lineFileDocs = new LineFileDocs(random);

            for (int i = 0; i < numdocs; i++)
            {
                writer.AddDocument(lineFileDocs.NextDoc());
            }

            lineFileDocs.Dispose();
        }
    }
}