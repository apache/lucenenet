using Lucene.Net.Analysis.TokenAttributes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Lucene.Net.Documents;

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

    using Lucene.Net.Analysis;
    using Lucene.Net.Support;
    using Lucene.Net.Util;
    using NUnit.Framework;
    using System.IO;
    using Bits = Lucene.Net.Util.Bits;
    using BytesRef = Lucene.Net.Util.BytesRef;
    using Directory = Lucene.Net.Store.Directory;
    using DocIdSetIterator = Lucene.Net.Search.DocIdSetIterator;
    using Document = Documents.Document;
    using Field = Field;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using OpenMode_e = Lucene.Net.Index.IndexWriterConfig.OpenMode_e;
    using PayloadAttribute = Lucene.Net.Analysis.TokenAttributes.PayloadAttribute;
    using TestUtil = Lucene.Net.Util.TestUtil;
    using TextField = TextField;

    [TestFixture]
    public class TestPayloads : LuceneTestCase
    {
        // Simple tests to test the payloads
        [Test]
        public virtual void TestPayload()
        {
            BytesRef payload = new BytesRef("this is a test!");
            Assert.AreEqual(payload.Length, "this is a test!".Length, "Wrong payload length.");

            BytesRef clone = (BytesRef)payload.Clone();
            Assert.AreEqual(payload.Length, clone.Length);
            for (int i = 0; i < payload.Length; i++)
            {
                Assert.AreEqual(payload.Bytes[i + payload.Offset], clone.Bytes[i + clone.Offset]);
            }
        }

        // Tests whether the DocumentWriter and SegmentMerger correctly enable the
        // payload bit in the FieldInfo
        [Test]
        public virtual void TestPayloadFieldBit()
        {
            Directory ram = NewDirectory();
            PayloadAnalyzer analyzer = new PayloadAnalyzer();
            IndexWriter writer = new IndexWriter(ram, NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer));
            Document d = new Document();
            // this field won't have any payloads
            d.Add(NewTextField("f1", "this field has no payloads", Field.Store.NO));
            // this field will have payloads in all docs, however not for all term positions,
            // so this field is used to check if the DocumentWriter correctly enables the payloads bit
            // even if only some term positions have payloads
            d.Add(NewTextField("f2", "this field has payloads in all docs", Field.Store.NO));
            d.Add(NewTextField("f2", "this field has payloads in all docs NO PAYLOAD", Field.Store.NO));
            // this field is used to verify if the SegmentMerger enables payloads for a field if it has payloads
            // enabled in only some documents
            d.Add(NewTextField("f3", "this field has payloads in some docs", Field.Store.NO));
            // only add payload data for field f2
            analyzer.SetPayloadData("f2", "somedata".GetBytes(IOUtils.CHARSET_UTF_8), 0, 1);
            writer.AddDocument(d);
            // flush
            writer.Dispose();

            SegmentReader reader = GetOnlySegmentReader(DirectoryReader.Open(ram));
            FieldInfos fi = reader.FieldInfos;
            Assert.IsFalse(fi.FieldInfo("f1").HasPayloads, "Payload field bit should not be set.");
            Assert.IsTrue(fi.FieldInfo("f2").HasPayloads, "Payload field bit should be set.");
            Assert.IsFalse(fi.FieldInfo("f3").HasPayloads, "Payload field bit should not be set.");
            reader.Dispose();

            // now we add another document which has payloads for field f3 and verify if the SegmentMerger
            // enabled payloads for that field
            analyzer = new PayloadAnalyzer(); // Clear payload state for each field
            writer = new IndexWriter(ram, NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer).SetOpenMode(OpenMode_e.CREATE));
            d = new Document();
            d.Add(NewTextField("f1", "this field has no payloads", Field.Store.NO));
            d.Add(NewTextField("f2", "this field has payloads in all docs", Field.Store.NO));
            d.Add(NewTextField("f2", "this field has payloads in all docs", Field.Store.NO));
            d.Add(NewTextField("f3", "this field has payloads in some docs", Field.Store.NO));
            // add payload data for field f2 and f3
            analyzer.SetPayloadData("f2", "somedata".GetBytes(IOUtils.CHARSET_UTF_8), 0, 1);
            analyzer.SetPayloadData("f3", "somedata".GetBytes(IOUtils.CHARSET_UTF_8), 0, 3);
            writer.AddDocument(d);

            // force merge
            writer.ForceMerge(1);
            // flush
            writer.Dispose();

            reader = GetOnlySegmentReader(DirectoryReader.Open(ram));
            fi = reader.FieldInfos;
            Assert.IsFalse(fi.FieldInfo("f1").HasPayloads, "Payload field bit should not be set.");
            Assert.IsTrue(fi.FieldInfo("f2").HasPayloads, "Payload field bit should be set.");
            Assert.IsTrue(fi.FieldInfo("f3").HasPayloads, "Payload field bit should be set.");
            reader.Dispose();
            ram.Dispose();
        }

        // Tests if payloads are correctly stored and loaded using both RamDirectory and FSDirectory
        [Test]
        public virtual void TestPayloadsEncoding()
        {
            Directory dir = NewDirectory();
            PerformTest(dir);
            dir.Dispose();
        }

        // builds an index with payloads in the given Directory and performs
        // different tests to verify the payload encoding
        private void PerformTest(Directory dir)
        {
            PayloadAnalyzer analyzer = new PayloadAnalyzer();
            IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer).SetOpenMode(OpenMode_e.CREATE).SetMergePolicy(NewLogMergePolicy()));

            // should be in sync with value in TermInfosWriter
            const int skipInterval = 16;

            const int numTerms = 5;
            const string fieldName = "f1";

            int numDocs = skipInterval + 1;
            // create content for the test documents with just a few terms
            Term[] terms = GenerateTerms(fieldName, numTerms);
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < terms.Length; i++)
            {
                sb.Append(terms[i].Text());
                sb.Append(" ");
            }
            string content = sb.ToString();

            int payloadDataLength = numTerms * numDocs * 2 + numTerms * numDocs * (numDocs - 1) / 2;
            var payloadData = GenerateRandomData(payloadDataLength);

            Document d = new Document();
            d.Add(NewTextField(fieldName, content, Field.Store.NO));
            // add the same document multiple times to have the same payload lengths for all
            // occurrences within two consecutive skip intervals
            int offset = 0;
            for (int i = 0; i < 2 * numDocs; i++)
            {
                analyzer = new PayloadAnalyzer(fieldName, payloadData, offset, 1);
                offset += numTerms;
                writer.AddDocument(d, analyzer);
            }

            // make sure we create more than one segment to test merging
            writer.Commit();

            // now we make sure to have different payload lengths next at the next skip point
            for (int i = 0; i < numDocs; i++)
            {
                analyzer = new PayloadAnalyzer(fieldName, payloadData, offset, i);
                offset += i * numTerms;
                writer.AddDocument(d, analyzer);
            }

            writer.ForceMerge(1);
            // flush
            writer.Dispose();

            /*
             * Verify the index
             * first we test if all payloads are stored correctly
             */
            IndexReader reader = DirectoryReader.Open(dir);

            var verifyPayloadData = new byte[payloadDataLength];
            offset = 0;
            var tps = new DocsAndPositionsEnum[numTerms];
            for (int i = 0; i < numTerms; i++)
            {
                tps[i] = MultiFields.GetTermPositionsEnum(reader, MultiFields.GetLiveDocs(reader), terms[i].Field, new BytesRef(terms[i].Text()));
            }

            while (tps[0].NextDoc() != DocIdSetIterator.NO_MORE_DOCS)
            {
                for (int i = 1; i < numTerms; i++)
                {
                    tps[i].NextDoc();
                }
                int freq = tps[0].Freq;

                for (int i = 0; i < freq; i++)
                {
                    for (int j = 0; j < numTerms; j++)
                    {
                        tps[j].NextPosition();
                        BytesRef br = tps[j].Payload;
                        if (br != null)
                        {
                            Array.Copy(br.Bytes, br.Offset, verifyPayloadData, offset, br.Length);
                            offset += br.Length;
                        }
                    }
                }
            }

            AssertByteArrayEquals(payloadData, verifyPayloadData);

            /*
             *  test lazy skipping
             */
            DocsAndPositionsEnum tp = MultiFields.GetTermPositionsEnum(reader, MultiFields.GetLiveDocs(reader), terms[0].Field, new BytesRef(terms[0].Text()));
            tp.NextDoc();
            tp.NextPosition();
            // NOTE: prior rev of this test was failing to first
            // call next here:
            tp.NextDoc();
            // now we don't read this payload
            tp.NextPosition();
            BytesRef payload = tp.Payload;
            Assert.AreEqual(1, payload.Length, "Wrong payload length.");
            Assert.AreEqual(payload.Bytes[payload.Offset], payloadData[numTerms]);
            tp.NextDoc();
            tp.NextPosition();

            // we don't read this payload and skip to a different document
            tp.Advance(5);
            tp.NextPosition();
            payload = tp.Payload;
            Assert.AreEqual(1, payload.Length, "Wrong payload length.");
            Assert.AreEqual(payload.Bytes[payload.Offset], payloadData[5 * numTerms]);

            /*
             * Test different lengths at skip points
             */
            tp = MultiFields.GetTermPositionsEnum(reader, MultiFields.GetLiveDocs(reader), terms[1].Field, new BytesRef(terms[1].Text()));
            tp.NextDoc();
            tp.NextPosition();
            Assert.AreEqual(1, tp.Payload.Length, "Wrong payload length.");
            tp.Advance(skipInterval - 1);
            tp.NextPosition();
            Assert.AreEqual(1, tp.Payload.Length, "Wrong payload length.");
            tp.Advance(2 * skipInterval - 1);
            tp.NextPosition();
            Assert.AreEqual(1, tp.Payload.Length, "Wrong payload length.");
            tp.Advance(3 * skipInterval - 1);
            tp.NextPosition();
            Assert.AreEqual(3 * skipInterval - 2 * numDocs - 1, tp.Payload.Length, "Wrong payload length.");

            reader.Dispose();

            // test long payload
            analyzer = new PayloadAnalyzer();
            writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer).SetOpenMode(OpenMode_e.CREATE));
            string singleTerm = "lucene";

            d = new Document();
            d.Add(NewTextField(fieldName, singleTerm, Field.Store.NO));
            // add a payload whose length is greater than the buffer size of BufferedIndexOutput
            payloadData = GenerateRandomData(2000);
            analyzer.SetPayloadData(fieldName, payloadData, 100, 1500);
            writer.AddDocument(d);

            writer.ForceMerge(1);
            // flush
            writer.Dispose();

            reader = DirectoryReader.Open(dir);
            tp = MultiFields.GetTermPositionsEnum(reader, MultiFields.GetLiveDocs(reader), fieldName, new BytesRef(singleTerm));
            tp.NextDoc();
            tp.NextPosition();

            BytesRef bref = tp.Payload;
            verifyPayloadData = new byte[bref.Length];
            var portion = new byte[1500];
            Array.Copy(payloadData, 100, portion, 0, 1500);

            AssertByteArrayEquals(portion, bref.Bytes, bref.Offset, bref.Length);
            reader.Dispose();
        }

        internal static readonly Encoding Utf8 = IOUtils.CHARSET_UTF_8;

        private void GenerateRandomData(byte[] data)
        {
            // this test needs the random data to be valid unicode
            string s = TestUtil.RandomFixedByteLengthUnicodeString(Random(), data.Length);
            var b = s.GetBytes(Utf8);
            Debug.Assert(b.Length == data.Length);
            System.Buffer.BlockCopy(b, 0, data, 0, b.Length);
        }

        private byte[] GenerateRandomData(int n)
        {
            var data = new byte[n];
            GenerateRandomData(data);
            return data;
        }

        private Term[] GenerateTerms(string fieldName, int n)
        {
            int maxDigits = (int)(Math.Log(n) / Math.Log(10));
            Term[] terms = new Term[n];
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < n; i++)
            {
                sb.Length = 0;
                sb.Append("t");
                int zeros = maxDigits - (int)(Math.Log(i) / Math.Log(10));
                for (int j = 0; j < zeros; j++)
                {
                    sb.Append("0");
                }
                sb.Append(i);
                terms[i] = new Term(fieldName, sb.ToString());
            }
            return terms;
        }

        internal virtual void AssertByteArrayEquals(byte[] b1, byte[] b2)
        {
            if (b1.Length != b2.Length)
            {
                Assert.Fail("Byte arrays have different lengths: " + b1.Length + ", " + b2.Length);
            }

            for (int i = 0; i < b1.Length; i++)
            {
                if (b1[i] != b2[i])
                {
                    Assert.Fail("Byte arrays different at index " + i + ": " + b1[i] + ", " + b2[i]);
                }
            }
        }

        internal virtual void AssertByteArrayEquals(byte[] b1, byte[] b2, int b2offset, int b2length)
        {
            if (b1.Length != b2length)
            {
                Assert.Fail("Byte arrays have different lengths: " + b1.Length + ", " + b2length);
            }

            for (int i = 0; i < b1.Length; i++)
            {
                if (b1[i] != b2[b2offset + i])
                {
                    Assert.Fail("Byte arrays different at index " + i + ": " + b1[i] + ", " + b2[b2offset + i]);
                }
            }
        }

        /// <summary>
        /// this Analyzer uses an WhitespaceTokenizer and PayloadFilter.
        /// </summary>
        private class PayloadAnalyzer : Analyzer
        {
            internal readonly IDictionary<string, PayloadData> FieldToData = new Dictionary<string, PayloadData>();

            public PayloadAnalyzer()
                : base(PER_FIELD_REUSE_STRATEGY)
            {
            }

            public PayloadAnalyzer(string field, byte[] data, int offset, int length)
                : base(PER_FIELD_REUSE_STRATEGY)
            {
                SetPayloadData(field, data, offset, length);
            }

            internal virtual void SetPayloadData(string field, byte[] data, int offset, int length)
            {
                FieldToData[field] = new PayloadData(data, offset, length);
            }

            protected internal override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
            {
                PayloadData payload;
                FieldToData.TryGetValue(fieldName, out payload);
                Tokenizer ts = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
                TokenStream tokenStream = (payload != null) ? (TokenStream)new PayloadFilter(ts, payload.Data, payload.Offset, payload.Length) : ts;
                return new TokenStreamComponents(ts, tokenStream);
            }

            internal class PayloadData
            {
                internal byte[] Data;
                internal int Offset;
                internal int Length;

                internal PayloadData(byte[] data, int offset, int length)
                {
                    this.Data = data;
                    this.Offset = offset;
                    this.Length = length;
                }
            }
        }

        /// <summary>
        /// this Filter adds payloads to the tokens.
        /// </summary>
        private class PayloadFilter : TokenFilter
        {
            internal byte[] Data;
            internal int Length;
            internal int Offset;
            internal int StartOffset;
            internal IPayloadAttribute PayloadAtt;
            internal ICharTermAttribute TermAttribute;

            public PayloadFilter(TokenStream @in, byte[] data, int offset, int length)
                : base(@in)
            {
                this.Data = data;
                this.Length = length;
                this.Offset = offset;
                this.StartOffset = offset;
                PayloadAtt = AddAttribute<IPayloadAttribute>();
                TermAttribute = AddAttribute<ICharTermAttribute>();
            }

            public sealed override bool IncrementToken()
            {
                bool hasNext = input.IncrementToken();
                if (!hasNext)
                {
                    return false;
                }

                // Some values of the same field are to have payloads and others not
                if (Offset + Length <= Data.Length && !TermAttribute.ToString().EndsWith("NO PAYLOAD"))
                {
                    BytesRef p = new BytesRef(Data, Offset, Length);
                    PayloadAtt.Payload = p;
                    Offset += Length;
                }
                else
                {
                    PayloadAtt.Payload = null;
                }

                return true;
            }

            public override void Reset()
            {
                base.Reset();
                this.Offset = StartOffset;
            }
        }

        [Test]
        public virtual void TestThreadSafety()
        {
            const int numThreads = 5;
            int numDocs = AtLeast(50);
            ByteArrayPool pool = new ByteArrayPool(numThreads, 5);

            Directory dir = NewDirectory();
            IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())));
            const string field = "test";

            ThreadClass[] ingesters = new ThreadClass[numThreads];
            for (int i = 0; i < numThreads; i++)
            {
                ingesters[i] = new ThreadAnonymousInnerClassHelper(this, numDocs, pool, writer, field);
                ingesters[i].Start();
            }

            for (int i = 0; i < numThreads; i++)
            {
                ingesters[i].Join();
            }
            writer.Dispose();
            IndexReader reader = DirectoryReader.Open(dir);
            TermsEnum terms = MultiFields.GetFields(reader).Terms(field).Iterator(null);
            Bits liveDocs = MultiFields.GetLiveDocs(reader);
            DocsAndPositionsEnum tp = null;
            while (terms.Next() != null)
            {
                string termText = terms.Term().Utf8ToString();
                tp = terms.DocsAndPositions(liveDocs, tp);
                while (tp.NextDoc() != DocIdSetIterator.NO_MORE_DOCS)
                {
                    int freq = tp.Freq;
                    for (int i = 0; i < freq; i++)
                    {
                        tp.NextPosition();
                        BytesRef payload = tp.Payload;
                        Assert.AreEqual(termText, payload.Utf8ToString());
                    }
                }
            }
            reader.Dispose();
            dir.Dispose();
            Assert.AreEqual(pool.Size(), numThreads);
        }

        private class ThreadAnonymousInnerClassHelper : ThreadClass
        {
            private readonly TestPayloads OuterInstance;

            private int NumDocs;
            private Lucene.Net.Index.TestPayloads.ByteArrayPool Pool;
            private IndexWriter Writer;
            private string Field;

            public ThreadAnonymousInnerClassHelper(TestPayloads outerInstance, int numDocs, Lucene.Net.Index.TestPayloads.ByteArrayPool pool, IndexWriter writer, string field)
            {
                this.OuterInstance = outerInstance;
                this.NumDocs = numDocs;
                this.Pool = pool;
                this.Writer = writer;
                this.Field = field;
            }

            public override void Run()
            {
                try
                {
                    for (int j = 0; j < NumDocs; j++)
                    {
                        Document d = new Document();
                        d.Add(new TextField(Field, new PoolingPayloadTokenStream(OuterInstance, Pool)));
                        Writer.AddDocument(d);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                    Console.Write(e.StackTrace);
                    Assert.Fail(e.ToString());
                }
            }
        }

        private class PoolingPayloadTokenStream : TokenStream
        {
            private readonly TestPayloads OuterInstance;

            private byte[] Payload;
            internal bool First;
            internal ByteArrayPool Pool;
            internal string Term;

            internal ICharTermAttribute TermAtt;
            internal IPayloadAttribute PayloadAtt;

            internal PoolingPayloadTokenStream(TestPayloads outerInstance, ByteArrayPool pool)
            {
                this.OuterInstance = outerInstance;
                this.Pool = pool;
                Payload = pool.Get();
                OuterInstance.GenerateRandomData(Payload);
                Term = Encoding.UTF8.GetString((byte[])(Array)Payload);
                First = true;
                PayloadAtt = AddAttribute<IPayloadAttribute>();
                TermAtt = AddAttribute<ICharTermAttribute>();
            }

            public sealed override bool IncrementToken()
            {
                if (!First)
                {
                    return false;
                }
                First = false;
                ClearAttributes();
                TermAtt.Append(Term);
                PayloadAtt.Payload = new BytesRef(Payload);
                return true;
            }

            public override void Dispose()
            {
                Pool.Release(Payload);
            }
        }

        private class ByteArrayPool
        {
            internal readonly IList<byte[]> Pool;

            internal ByteArrayPool(int capacity, int size)
            {
                Pool = new List<byte[]>();
                for (int i = 0; i < capacity; i++)
                {
                    Pool.Add(new byte[size]);
                }
            }

            internal virtual byte[] Get()
            {
                lock (this) // TODO use BlockingCollection / BCL datastructures instead
                {
                    var retArray = Pool[0];
                    Pool.RemoveAt(0);
                    return retArray;
                }
            }

            internal virtual void Release(byte[] b)
            {
                lock (this)
                {
                    Pool.Add(b);
                }
            }

            internal virtual int Size()
            {
                lock (this)
                {
                    return Pool.Count;
                }
            }
        }

        [Test]
        public virtual void TestAcrossFields()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random(), dir, new MockAnalyzer(Random(), MockTokenizer.WHITESPACE, true), Similarity, TimeZone);
            Document doc = new Document();
            doc.Add(new TextField("hasMaybepayload", "here we go", Field.Store.YES));
            writer.AddDocument(doc);
            writer.Dispose();

            writer = new RandomIndexWriter(Random(), dir, new MockAnalyzer(Random(), MockTokenizer.WHITESPACE, true), Similarity, TimeZone);
            doc = new Document();
            doc.Add(new TextField("hasMaybepayload2", "here we go", Field.Store.YES));
            writer.AddDocument(doc);
            writer.AddDocument(doc);
            writer.ForceMerge(1);
            writer.Dispose();

            dir.Dispose();
        }

        /// <summary>
        /// some docs have payload att, some not </summary>
        [Test]
        public virtual void TestMixupDocs()
        {
            Directory dir = NewDirectory();
            IndexWriterConfig iwc = NewIndexWriterConfig(TEST_VERSION_CURRENT, null);
            iwc.SetMergePolicy(NewLogMergePolicy());
            RandomIndexWriter writer = new RandomIndexWriter(Random(), dir, iwc);
            Document doc = new Document();
            Field field = new TextField("field", "", Field.Store.NO);
            TokenStream ts = new MockTokenizer(new StringReader("here we go"), MockTokenizer.WHITESPACE, true);
            Assert.IsFalse(ts.HasAttribute<PayloadAttribute>());
            field.SetTokenStream(ts);
            doc.Add(field);
            writer.AddDocument(doc);
            Token withPayload = new Token("withPayload", 0, 11);
            withPayload.Payload = new BytesRef("test");
            ts = new CannedTokenStream(withPayload);
            Assert.IsTrue(ts.HasAttribute<IPayloadAttribute>());
            field.SetTokenStream(ts);
            writer.AddDocument(doc);
            ts = new MockTokenizer(new StringReader("another"), MockTokenizer.WHITESPACE, true);
            Assert.IsFalse(ts.HasAttribute<PayloadAttribute>());
            field.SetTokenStream(ts);
            writer.AddDocument(doc);
            DirectoryReader reader = writer.Reader;
            AtomicReader sr = SlowCompositeReaderWrapper.Wrap(reader);
            DocsAndPositionsEnum de = sr.TermPositionsEnum(new Term("field", "withPayload"));
            de.NextDoc();
            de.NextPosition();
            Assert.AreEqual(new BytesRef("test"), de.Payload);
            writer.Dispose();
            reader.Dispose();
            dir.Dispose();
        }

        /// <summary>
        /// some field instances have payload att, some not </summary>
        [Test]
        public virtual void TestMixupMultiValued()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random(), dir, Similarity, TimeZone);
            Document doc = new Document();
            Field field = new TextField("field", "", Field.Store.NO);
            TokenStream ts = new MockTokenizer(new StringReader("here we go"), MockTokenizer.WHITESPACE, true);
            Assert.IsFalse(ts.HasAttribute<PayloadAttribute>());
            field.SetTokenStream(ts);
            doc.Add(field);
            Field field2 = new TextField("field", "", Field.Store.NO);
            Token withPayload = new Token("withPayload", 0, 11);
            withPayload.Payload = new BytesRef("test");
            ts = new CannedTokenStream(withPayload);
            Assert.IsTrue(ts.HasAttribute<IPayloadAttribute>());
            field2.SetTokenStream(ts);
            doc.Add(field2);
            Field field3 = new TextField("field", "", Field.Store.NO);
            ts = new MockTokenizer(new StringReader("nopayload"), MockTokenizer.WHITESPACE, true);
            Assert.IsFalse(ts.HasAttribute<PayloadAttribute>());
            field3.SetTokenStream(ts);
            doc.Add(field3);
            writer.AddDocument(doc);
            DirectoryReader reader = writer.Reader;
            SegmentReader sr = GetOnlySegmentReader(reader);
            DocsAndPositionsEnum de = sr.TermPositionsEnum(new Term("field", "withPayload"));
            de.NextDoc();
            de.NextPosition();
            Assert.AreEqual(new BytesRef("test"), de.Payload);
            writer.Dispose();
            reader.Dispose();
            dir.Dispose();
        }
    }
}