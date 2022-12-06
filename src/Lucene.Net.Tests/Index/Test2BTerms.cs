using J2N.Collections.Generic.Extensions;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Documents;
using Lucene.Net.Index.Extensions;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Support;
using Lucene.Net.Util;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
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

    using Codec = Lucene.Net.Codecs.Codec;

    // NOTE: this test will fail w/ PreFlexRW codec!  (Because
    // this test uses full binary term space, but PreFlex cannot
    // handle this since it requires the terms are UTF8 bytes).
    //
    // Also, SimpleText codec will consume very large amounts of
    // disk (but, should run successfully).  Best to run w/
    // -Dtests.codec=Standard, and w/ plenty of RAM, eg:
    //
    //   ant test -Dtest.slow=true -Dtests.heapsize=8g
    //
    //   java -server -Xmx8g -d64 -cp .:lib/junit-4.10.jar:./build/classes/test:./build/classes/test-framework:./build/classes/java -Dlucene.version=4.0-dev -Dtests.directory=MMapDirectory -DtempDir=build -ea org.junit.runner.JUnitCore Lucene.Net.Index.Test2BTerms
    //
    [SuppressCodecs("SimpleText", "Memory", "Direct")]
    [Ignore("SimpleText codec will consume very large amounts of memory.")]
    [TestFixture]
    public class Test2BTerms : LuceneTestCase
    {
        private const int TOKEN_LEN = 5;

        private static readonly BytesRef bytes = new BytesRef(TOKEN_LEN);

        private sealed class MyTokenStream : TokenStream
        {
            internal readonly int tokensPerDoc;
            internal int tokenCount;
            public readonly IList<BytesRef> savedTerms = new JCG.List<BytesRef>();
            internal int nextSave;
            internal long termCounter;
            internal readonly Random random;

            public MyTokenStream(Random random, int tokensPerDoc)
                : base(new MyAttributeFactory(AttributeFactory.DEFAULT_ATTRIBUTE_FACTORY))
            {
                this.tokensPerDoc = tokensPerDoc;
                AddAttribute<ITermToBytesRefAttribute>();
                bytes.Length = TOKEN_LEN;
                this.random = random;
                nextSave = TestUtil.NextInt32(random, 500000, 1000000);
            }

            public override bool IncrementToken()
            {
                ClearAttributes();
                if (tokenCount >= tokensPerDoc)
                {
                    return false;
                }
                int shift = 32;
                for (int i = 0; i < 5; i++)
                {
                    bytes.Bytes[i] = unchecked((byte)((termCounter >> shift) & 0xFF));
                    shift -= 8;
                }
                termCounter++;
                tokenCount++;
                if (--nextSave == 0)
                {
                    savedTerms.Add(BytesRef.DeepCopyOf(bytes));
                    Console.WriteLine("TEST: save term=" + bytes);
                    nextSave = TestUtil.NextInt32(random, 500000, 1000000);
                }
                return true;
            }

            public override void Reset()
            {
                tokenCount = 0;
            }

            private sealed class MyTermAttributeImpl : Util.Attribute, ITermToBytesRefAttribute
            {
                public void FillBytesRef()
                {
                    // no-op: the bytes was already filled by our owner's incrementToken
                }

                public BytesRef BytesRef => bytes;

                public override void Clear()
                {
                }

                public override bool Equals(object other)
                {
                    return other == this;
                }

                public override int GetHashCode()
                {
                    return RuntimeHelpers.GetHashCode(this);
                }

                public override void CopyTo(IAttribute target)
                {
                }

                public override object Clone()
                {
                    throw UnsupportedOperationException.Create();
                }
            }

            private sealed class MyAttributeFactory : AttributeFactory
            {
                internal readonly AttributeFactory @delegate;

                public MyAttributeFactory(AttributeFactory @delegate)
                {
                    this.@delegate = @delegate;
                }

                public override Util.Attribute CreateAttributeInstance<T>()
                {
                    var attClass = typeof(T);
                    if (attClass == typeof(ITermToBytesRefAttribute))
                    {
                        return new MyTermAttributeImpl();
                    }
                    if (typeof(ICharTermAttribute).IsAssignableFrom(attClass))
                    {
                        throw new ArgumentException("no");
                    }
                    return @delegate.CreateAttributeInstance<T>();
                }
            }
        }

        [Ignore("Very slow. Enable manually by removing Ignore.")]
        [Test]
        public virtual void Test2BTerms_Mem()
        {
            if ("Lucene3x".Equals(Codec.Default.Name, StringComparison.Ordinal))
            {
                throw RuntimeException.Create("this test cannot run with PreFlex codec");
            }
            Console.WriteLine("Starting Test2B");
            long TERM_COUNT = ((long)int.MaxValue) + 100000000;

            int TERMS_PER_DOC = TestUtil.NextInt32(Random, 100000, 1000000);

            IList<BytesRef> savedTerms = null;

            BaseDirectoryWrapper dir = NewFSDirectory(CreateTempDir("2BTerms"));
            //MockDirectoryWrapper dir = NewFSDirectory(new File("/p/lucene/indices/2bindex"));
            if (dir is MockDirectoryWrapper)
            {
                ((MockDirectoryWrapper)dir).Throttling = Throttling.NEVER;
            }
            dir.CheckIndexOnDispose = false; // don't double-checkindex

            if (true)
            {
                IndexWriter w = new IndexWriter(dir, new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random))
                                           .SetMaxBufferedDocs(IndexWriterConfig.DISABLE_AUTO_FLUSH)
                                           .SetRAMBufferSizeMB(256.0)
                                           .SetMergeScheduler(new ConcurrentMergeScheduler())
                                           .SetMergePolicy(NewLogMergePolicy(false, 10))
                                           .SetOpenMode(OpenMode.CREATE));

                MergePolicy mp = w.Config.MergePolicy;
                if (mp is LogByteSizeMergePolicy)
                {
                    // 1 petabyte:
                    ((LogByteSizeMergePolicy)mp).MaxMergeMB = 1024 * 1024 * 1024;
                }

                Documents.Document doc = new Documents.Document();
                MyTokenStream ts = new MyTokenStream(Random, TERMS_PER_DOC);

                FieldType customType = new FieldType(TextField.TYPE_NOT_STORED);
                customType.IndexOptions = IndexOptions.DOCS_ONLY;
                customType.OmitNorms = true;
                Field field = new Field("field", ts, customType);
                doc.Add(field);
                //w.setInfoStream(System.out);
                int numDocs = (int)(TERM_COUNT / TERMS_PER_DOC);

                Console.WriteLine("TERMS_PER_DOC=" + TERMS_PER_DOC);
                Console.WriteLine("numDocs=" + numDocs);

                for (int i = 0; i < numDocs; i++)
                {
                    long t0 = J2N.Time.NanoTime() / J2N.Time.MillisecondsPerNanosecond; // LUCENENET: Use NanoTime() rather than CurrentTimeMilliseconds() for more accurate/reliable results
                    w.AddDocument(doc);
                    Console.WriteLine(i + " of " + numDocs + " " + ((J2N.Time.NanoTime() / J2N.Time.MillisecondsPerNanosecond) - t0) + " msec"); // LUCENENET: Use NanoTime() rather than CurrentTimeMilliseconds() for more accurate/reliable results
                }
                savedTerms = ts.savedTerms;

                Console.WriteLine("TEST: full merge");
                w.ForceMerge(1);
                Console.WriteLine("TEST: close writer");
                w.Dispose();
            }

            Console.WriteLine("TEST: open reader");
            IndexReader r = DirectoryReader.Open(dir);
            if (savedTerms is null)
            {
                savedTerms = FindTerms(r);
            }
            int numSavedTerms = savedTerms.Count;
            IList<BytesRef> bigOrdTerms = new JCG.List<BytesRef>(savedTerms.GetView(numSavedTerms - 10, 10)); // LUCENENET: Converted end index to length
            Console.WriteLine("TEST: test big ord terms...");
            TestSavedTerms(r, bigOrdTerms);
            Console.WriteLine("TEST: test all saved terms...");
            TestSavedTerms(r, savedTerms);
            r.Dispose();

            Console.WriteLine("TEST: now CheckIndex...");
            CheckIndex.Status status = TestUtil.CheckIndex(dir);
            long tc = status.SegmentInfos[0].TermIndexStatus.TermCount;
            Assert.IsTrue(tc > int.MaxValue, "count " + tc + " is not > " + int.MaxValue);

            dir.Dispose();
            Console.WriteLine("TEST: done!");
        }

        private IList<BytesRef> FindTerms(IndexReader r)
        {
            Console.WriteLine("TEST: findTerms");
            TermsEnum termsEnum = MultiFields.GetTerms(r, "field").GetEnumerator();
            IList<BytesRef> savedTerms = new JCG.List<BytesRef>();
            int nextSave = TestUtil.NextInt32(Random, 500000, 1000000);
            BytesRef term;
            while (termsEnum.MoveNext())
            {
                term = termsEnum.Term;
                if (--nextSave == 0)
                {
                    savedTerms.Add(BytesRef.DeepCopyOf(term));
                    Console.WriteLine("TEST: add " + term);
                    nextSave = TestUtil.NextInt32(Random, 500000, 1000000);
                }
            }
            return savedTerms;
        }

        private void TestSavedTerms(IndexReader r, IList<BytesRef> terms)
        {
            Console.WriteLine("TEST: run " + terms.Count + " terms on reader=" + r);
            IndexSearcher s = NewSearcher(r);
            terms.Shuffle(Random);
            TermsEnum termsEnum = MultiFields.GetTerms(r, "field").GetEnumerator();
            bool failed = false;
            for (int iter = 0; iter < 10 * terms.Count; iter++)
            {
                BytesRef term = terms[Random.Next(terms.Count)];
                Console.WriteLine("TEST: search " + term);
                long t0 = J2N.Time.NanoTime() / J2N.Time.MillisecondsPerNanosecond; // LUCENENET: Use NanoTime() rather than CurrentTimeMilliseconds() for more accurate/reliable results
                int count = s.Search(new TermQuery(new Term("field", term)), 1).TotalHits;
                if (count <= 0)
                {
                    Console.WriteLine("  FAILED: count=" + count);
                    failed = true;
                }
                long t1 = J2N.Time.NanoTime() / J2N.Time.MillisecondsPerNanosecond; // LUCENENET: Use NanoTime() rather than CurrentTimeMilliseconds() for more accurate/reliable results
                Console.WriteLine("  took " + (t1 - t0) + " millis");

                TermsEnum.SeekStatus result = termsEnum.SeekCeil(term);
                if (result != TermsEnum.SeekStatus.FOUND)
                {
                    if (result == TermsEnum.SeekStatus.END)
                    {
                        Console.WriteLine("  FAILED: got END");
                    }
                    else
                    {
                        Console.WriteLine("  FAILED: wrong term: got " + termsEnum.Term);
                    }
                    failed = true;
                }
            }
            Assert.IsFalse(failed);
        }
    }
}