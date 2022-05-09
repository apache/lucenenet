using J2N.Runtime.CompilerServices;
using Lucene.Net.Index;
using Lucene.Net.Index.Extensions;
using Lucene.Net.Util;
using NUnit.Framework;
using RandomizedTesting.Generators;
using System;
using System.Collections.Generic;
using Assert = Lucene.Net.TestFramework.Assert;
using JCG = J2N.Collections.Generic;

namespace Lucene.Net.Codecs.Lucene40
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
    using AtomicReaderContext = Lucene.Net.Index.AtomicReaderContext;
    using BytesRef = Lucene.Net.Util.BytesRef;
    using Directory = Lucene.Net.Store.Directory;
    using DirectoryReader = Lucene.Net.Index.DirectoryReader;
    using DocsEnum = Lucene.Net.Index.DocsEnum;
    using IBits = Lucene.Net.Util.IBits;
    using IndexWriter = Lucene.Net.Index.IndexWriter;
    using IOUtils = Lucene.Net.Util.IOUtils;
    using LineFileDocs = Lucene.Net.Util.LineFileDocs;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using MatchNoBits = Lucene.Net.Util.Bits.MatchNoBits;
    using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
    using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;
    using Terms = Lucene.Net.Index.Terms;
    using TermsEnum = Lucene.Net.Index.TermsEnum;
    using TestUtil = Lucene.Net.Util.TestUtil;

    // TODO: really this should be in BaseTestPF or somewhere else? useful test!
    // LUCENENET specific - Specify to unzip the line file docs
    [UseTempLineDocsFile]
    public class TestReuseDocsEnum : LuceneTestCase
    {
        [OneTimeSetUp]
        public override void BeforeClass()
        {
            base.BeforeClass();
            OldFormatImpersonationIsActive = true; // explicitly instantiates ancient codec
        }

        [Test]
        public virtual void TestReuseDocsEnumNoReuse()
        {
            Directory dir = NewDirectory();
            Codec cp = TestUtil.AlwaysPostingsFormat(new Lucene40RWPostingsFormat());
            RandomIndexWriter writer = new RandomIndexWriter(Random, dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)).SetCodec(cp));
            int numdocs = AtLeast(20);
            CreateRandomIndex(numdocs, writer, Random);
            writer.Commit();

            DirectoryReader open = DirectoryReader.Open(dir);
            foreach (AtomicReaderContext ctx in open.Leaves)
            {
                AtomicReader indexReader = (AtomicReader)ctx.Reader;
                Terms terms = indexReader.GetTerms("body");
                TermsEnum iterator = terms.GetEnumerator();
                IDictionary<DocsEnum, bool> enums = new JCG.Dictionary<DocsEnum, bool>(IdentityEqualityComparer<DocsEnum>.Default);
                MatchNoBits bits = new MatchNoBits(indexReader.MaxDoc);
                while (iterator.MoveNext())
                {
                    DocsEnum docs = iterator.Docs(Random.NextBoolean() ? bits : new MatchNoBits(indexReader.MaxDoc), null, Random.NextBoolean() ? DocsFlags.FREQS : DocsFlags.NONE);
                    enums[docs] = true;
                }

                Assert.AreEqual(terms.Count, enums.Count);
            }
            IOUtils.Dispose(writer, open, dir);
        }

        // tests for reuse only if bits are the same either null or the same instance
        [Test]
        public virtual void TestReuseDocsEnumSameBitsOrNull()
        {
            Directory dir = NewDirectory();
            Codec cp = TestUtil.AlwaysPostingsFormat(new Lucene40RWPostingsFormat());
            RandomIndexWriter writer = new RandomIndexWriter(Random, dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)).SetCodec(cp));
            int numdocs = AtLeast(20);
            CreateRandomIndex(numdocs, writer, Random);
            writer.Commit();

            DirectoryReader open = DirectoryReader.Open(dir);
            foreach (AtomicReaderContext ctx in open.Leaves)
            {
                Terms terms = ((AtomicReader)ctx.Reader).GetTerms("body");
                TermsEnum iterator = terms.GetEnumerator();
                IDictionary<DocsEnum, bool> enums = new JCG.Dictionary<DocsEnum, bool>(IdentityEqualityComparer<DocsEnum>.Default);
                MatchNoBits bits = new MatchNoBits(open.MaxDoc);
                DocsEnum docs = null;
                while (iterator.MoveNext())
                {
                    docs = iterator.Docs(bits, docs, Random.NextBoolean() ? DocsFlags.FREQS : DocsFlags.NONE);
                    enums[docs] = true;
                }

                Assert.AreEqual(1, enums.Count);
                enums.Clear();
                iterator = terms.GetEnumerator();
                docs = null;
                while (iterator.MoveNext())
                {
                    docs = iterator.Docs(new MatchNoBits(open.MaxDoc), docs, Random.NextBoolean() ? DocsFlags.FREQS : DocsFlags.NONE);
                    enums[docs] = true;
                }
                Assert.AreEqual(terms.Count, enums.Count);

                enums.Clear();
                iterator = terms.GetEnumerator();
                docs = null;
                while (iterator.MoveNext())
                {
                    docs = iterator.Docs(null, docs, Random.NextBoolean() ? DocsFlags.FREQS : DocsFlags.NONE);
                    enums[docs] = true;
                }
                Assert.AreEqual(1, enums.Count);
            }
            IOUtils.Dispose(writer, open, dir);
        }

        // make sure we never reuse from another reader even if it is the same field & codec etc
        [Test]
        public virtual void TestReuseDocsEnumDifferentReader()
        {
            Directory dir = NewDirectory();
            Codec cp = TestUtil.AlwaysPostingsFormat(new Lucene40RWPostingsFormat());
            MockAnalyzer analyzer = new MockAnalyzer(Random);
            analyzer.MaxTokenLength = TestUtil.NextInt32(Random, 1, IndexWriter.MAX_TERM_LENGTH);

            RandomIndexWriter writer = new RandomIndexWriter(Random, dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer).SetCodec(cp));
            int numdocs = AtLeast(20);
            CreateRandomIndex(numdocs, writer, Random);
            writer.Commit();

            DirectoryReader firstReader = DirectoryReader.Open(dir);
            DirectoryReader secondReader = DirectoryReader.Open(dir);
            IList<AtomicReaderContext> leaves = firstReader.Leaves;
            IList<AtomicReaderContext> leaves2 = secondReader.Leaves;

            foreach (AtomicReaderContext ctx in leaves)
            {
                Terms terms = ((AtomicReader)ctx.Reader).GetTerms("body");
                TermsEnum iterator = terms.GetEnumerator();
                IDictionary<DocsEnum, bool> enums = new JCG.Dictionary<DocsEnum, bool>(IdentityEqualityComparer<DocsEnum>.Default);
                MatchNoBits bits = new MatchNoBits(firstReader.MaxDoc);
                iterator = terms.GetEnumerator();
                DocsEnum docs = null;
                BytesRef term = null;
                while (iterator.MoveNext())
                {
                    term = iterator.Term;
                    docs = iterator.Docs(null, RandomDocsEnum("body", term, leaves2, bits), Random.NextBoolean() ? DocsFlags.FREQS : DocsFlags.NONE);
                    enums[docs] = true;
                }
                Assert.AreEqual(terms.Count, enums.Count);

                iterator = terms.GetEnumerator();
                enums.Clear();
                docs = null;
                while (iterator.MoveNext())
                {
                    term = iterator.Term;
                    docs = iterator.Docs(bits, RandomDocsEnum("body", term, leaves2, bits), Random.NextBoolean() ? DocsFlags.FREQS : DocsFlags.NONE);
                    enums[docs] = true;
                }
                Assert.AreEqual(terms.Count, enums.Count);
            }
            IOUtils.Dispose(writer, firstReader, secondReader, dir);
        }

        public virtual DocsEnum RandomDocsEnum(string field, BytesRef term, IList<AtomicReaderContext> readers, IBits bits)
        {
            if (Random.Next(10) == 0)
            {
                return null;
            }
            AtomicReader indexReader = (AtomicReader)readers[Random.Next(readers.Count)].Reader;
            Terms terms = indexReader.GetTerms(field);
            if (terms is null)
            {
                return null;
            }
            TermsEnum iterator = terms.GetEnumerator();
            if (iterator.SeekExact(term))
            {
                return iterator.Docs(bits, null, Random.NextBoolean() ? DocsFlags.FREQS : DocsFlags.NONE);
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