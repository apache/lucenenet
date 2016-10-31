using Lucene.Net.Codecs.MockSep;
using Lucene.Net.Documents;
using Lucene.Net.Search;
using Lucene.Net.Support;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace Lucene.Net.Index
{
    using BytesRef = Lucene.Net.Util.BytesRef;
    using Codec = Lucene.Net.Codecs.Codec;
    using Constants = Lucene.Net.Util.Constants;
    using Directory = Lucene.Net.Store.Directory;
    using DocIdSetIterator = Lucene.Net.Search.DocIdSetIterator;

    //using MockSepPostingsFormat = Lucene.Net.Codecs.mocksep.MockSepPostingsFormat;
    using Document = Documents.Document;
    using DocValuesType_e = Lucene.Net.Index.FieldInfo.DocValuesType_e;
    using FieldsConsumer = Lucene.Net.Codecs.FieldsConsumer;
    using FieldsProducer = Lucene.Net.Codecs.FieldsProducer;
    using FieldType = FieldType;
    using IndexSearcher = Lucene.Net.Search.IndexSearcher;
    using InfoStream = Lucene.Net.Util.InfoStream;
    using Lucene3xCodec = Lucene.Net.Codecs.Lucene3x.Lucene3xCodec;
    using Lucene40RWCodec = Lucene.Net.Codecs.Lucene40.Lucene40RWCodec;
    using Lucene41RWCodec = Lucene.Net.Codecs.Lucene41.Lucene41RWCodec;
    using Lucene42RWCodec = Lucene.Net.Codecs.Lucene42.Lucene42RWCodec;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;

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
    using NumericDocValuesField = NumericDocValuesField;
    using OpenBitSet = Lucene.Net.Util.OpenBitSet;
    using PostingsConsumer = Lucene.Net.Codecs.PostingsConsumer;
    using Query = Lucene.Net.Search.Query;
    using ScoreDoc = Lucene.Net.Search.ScoreDoc;
    using Store = Field.Store;
    using StringField = StringField;
    using TermsConsumer = Lucene.Net.Codecs.TermsConsumer;
    using TermStats = Lucene.Net.Codecs.TermStats;
    using TestUtil = Lucene.Net.Util.TestUtil;

    // TODO: test multiple codecs here?

    // TODO
    //   - test across fields
    //   - fix this test to run once for all codecs
    //   - make more docs per term, to test > 1 level skipping
    //   - test all combinations of payloads/not and omitTF/not
    //   - test w/ different indexDivisor
    //   - test field where payload length rarely changes
    //   - 0-term fields
    //   - seek/skip to same term/doc i'm already on
    //   - mix in deleted docs
    //   - seek, skip beyond end -- assert returns false
    //   - seek, skip to things that don't exist -- ensure it
    //     goes to 1 before next one known to exist
    //   - skipTo(term)
    //   - skipTo(doc)

    [TestFixture]
    public class TestCodecs : LuceneTestCase
    {
        private static string[] FieldNames = new string[] { "one", "two", "three", "four" };

        private static int NUM_TEST_ITER;
        private const int NUM_TEST_THREADS = 3;
        private const int NUM_FIELDS = 4;
        private const int NUM_TERMS_RAND = 50; // must be > 16 to test skipping
        private const int DOC_FREQ_RAND = 500; // must be > 16 to test skipping
        private const int TERM_DOC_FREQ_RAND = 20;

        [OneTimeSetUp]
        public static void BeforeClass()
        {
            NUM_TEST_ITER = AtLeast(20);
        }

        internal class FieldData : IComparable<FieldData>
        {
            private readonly TestCodecs OuterInstance;

            internal readonly FieldInfo FieldInfo;
            internal readonly TermData[] Terms;
            internal readonly bool OmitTF;
            internal readonly bool StorePayloads;

            public FieldData(TestCodecs outerInstance, string name, FieldInfos.Builder fieldInfos, TermData[] terms, bool omitTF, bool storePayloads)
            {
                this.OuterInstance = outerInstance;
                this.OmitTF = omitTF;
                this.StorePayloads = storePayloads;
                // TODO: change this test to use all three
                FieldInfo = fieldInfos.AddOrUpdate(name, new IndexableFieldTypeAnonymousInnerClassHelper(this, omitTF));
                if (storePayloads)
                {
                    FieldInfo.SetStorePayloads();
                }
                this.Terms = terms;
                for (int i = 0; i < terms.Length; i++)
                {
                    terms[i].Field = this;
                }

                Array.Sort(terms);
            }

            private class IndexableFieldTypeAnonymousInnerClassHelper : IndexableFieldType
            {
                private readonly FieldData OuterInstance;

                private bool OmitTF;

                public IndexableFieldTypeAnonymousInnerClassHelper(FieldData outerInstance, bool omitTF)
                {
                    this.OuterInstance = outerInstance;
                    this.OmitTF = omitTF;
                }

                public bool Indexed
                {
                    get { return true; }
                    set { }
                }

                public bool Stored
                {
                    get { return false; }
                    set { }
                }

                public bool Tokenized
                {
                    get { return false; }
                    set { }
                }

                public bool StoreTermVectors
                {
                    get { return false; }
                    set { }
                }

                public bool StoreTermVectorOffsets
                {
                    get { return false; }
                    set { }
                }

                public bool StoreTermVectorPositions
                {
                    get { return false; }
                    set { }
                }

                public bool StoreTermVectorPayloads
                {
                    get { return false; }
                    set { }
                }

                public bool OmitNorms
                {
                    get { return false; }
                    set { }
                }

                public FieldInfo.IndexOptions? IndexOptions
                {
                    get { return OmitTF ? FieldInfo.IndexOptions.DOCS_ONLY : FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS; }
                    set { }
                }

                public FieldType.NumericType? NumericTypeValue
                {
                    get { throw new NotImplementedException(); }
                    set { }
                }

                public DocValuesType_e? DocValueType
                {
                    get { return null; }
                    set { }
                }
            }

            public int CompareTo(FieldData other)
            {
                return FieldInfo.Name.CompareTo(other.FieldInfo.Name);
            }

            public virtual void Write(FieldsConsumer consumer)
            {
                Array.Sort(Terms);
                TermsConsumer termsConsumer = consumer.AddField(FieldInfo);
                long sumTotalTermCount = 0;
                long sumDF = 0;
                OpenBitSet visitedDocs = new OpenBitSet();
                foreach (TermData term in Terms)
                {
                    for (int i = 0; i < term.Docs.Length; i++)
                    {
                        visitedDocs.Set(term.Docs[i]);
                    }
                    sumDF += term.Docs.Length;
                    sumTotalTermCount += term.Write(termsConsumer);
                }
                termsConsumer.Finish(OmitTF ? -1 : sumTotalTermCount, sumDF, (int)visitedDocs.Cardinality());
            }
        }

        internal class PositionData
        {
            private readonly TestCodecs OuterInstance;

            internal int Pos;
            internal BytesRef Payload;

            internal PositionData(TestCodecs outerInstance, int pos, BytesRef payload)
            {
                this.OuterInstance = outerInstance;
                this.Pos = pos;
                this.Payload = payload;
            }
        }

        internal class TermData : IComparable<TermData>
        {
            private readonly TestCodecs OuterInstance;

            internal string Text2;
            internal readonly BytesRef Text;
            internal int[] Docs;
            internal PositionData[][] Positions;
            internal FieldData Field;

            public TermData(TestCodecs outerInstance, string text, int[] docs, PositionData[][] positions)
            {
                this.OuterInstance = outerInstance;
                this.Text = new BytesRef(text);
                this.Text2 = text;
                this.Docs = docs;
                this.Positions = positions;
            }

            public virtual int CompareTo(TermData o)
            {
                return Text.CompareTo(o.Text);
            }

            public virtual long Write(TermsConsumer termsConsumer)
            {
                PostingsConsumer postingsConsumer = termsConsumer.StartTerm(Text);
                long totTF = 0;
                for (int i = 0; i < Docs.Length; i++)
                {
                    int termDocFreq;
                    if (Field.OmitTF)
                    {
                        termDocFreq = -1;
                    }
                    else
                    {
                        termDocFreq = Positions[i].Length;
                    }
                    postingsConsumer.StartDoc(Docs[i], termDocFreq);
                    if (!Field.OmitTF)
                    {
                        totTF += Positions[i].Length;
                        for (int j = 0; j < Positions[i].Length; j++)
                        {
                            PositionData pos = Positions[i][j];
                            postingsConsumer.AddPosition(pos.Pos, pos.Payload, -1, -1);
                        }
                    }
                    postingsConsumer.FinishDoc();
                }
                termsConsumer.FinishTerm(Text, new TermStats(Docs.Length, Field.OmitTF ? -1 : totTF));
                return totTF;
            }
        }

        private const string SEGMENT = "0";

        internal virtual TermData[] MakeRandomTerms(bool omitTF, bool storePayloads)
        {
            int numTerms = 1 + Random().Next(NUM_TERMS_RAND);
            //final int numTerms = 2;
            TermData[] terms = new TermData[numTerms];

            HashSet<string> termsSeen = new HashSet<string>();

            for (int i = 0; i < numTerms; i++)
            {
                // Make term text
                string text2;
                while (true)
                {
                    text2 = TestUtil.RandomUnicodeString(Random());
                    if (!termsSeen.Contains(text2) && !text2.EndsWith("."))
                    {
                        termsSeen.Add(text2);
                        break;
                    }
                }

                int docFreq = 1 + Random().Next(DOC_FREQ_RAND);
                int[] docs = new int[docFreq];
                PositionData[][] positions;

                if (!omitTF)
                {
                    positions = new PositionData[docFreq][];
                }
                else
                {
                    positions = null;
                }

                int docID = 0;
                for (int j = 0; j < docFreq; j++)
                {
                    docID += TestUtil.NextInt(Random(), 1, 10);
                    docs[j] = docID;

                    if (!omitTF)
                    {
                        int termFreq = 1 + Random().Next(TERM_DOC_FREQ_RAND);
                        positions[j] = new PositionData[termFreq];
                        int position = 0;
                        for (int k = 0; k < termFreq; k++)
                        {
                            position += TestUtil.NextInt(Random(), 1, 10);

                            BytesRef payload;
                            if (storePayloads && Random().Next(4) == 0)
                            {
                                var bytes = new byte[1 + Random().Next(5)];
                                for (int l = 0; l < bytes.Length; l++)
                                {
                                    bytes[l] = (byte)Random().Next(255);
                                }
                                payload = new BytesRef(bytes);
                            }
                            else
                            {
                                payload = null;
                            }

                            positions[j][k] = new PositionData(this, position, payload);
                        }
                    }
                }

                terms[i] = new TermData(this, text2, docs, positions);
            }

            return terms;
        }

        [Test]
        public virtual void TestFixedPostings()
        {
            const int NUM_TERMS = 100;
            TermData[] terms = new TermData[NUM_TERMS];
            for (int i = 0; i < NUM_TERMS; i++)
            {
                int[] docs = new int[] { i };
                string text = Convert.ToString(i);
                terms[i] = new TermData(this, text, docs, null);
            }

            FieldInfos.Builder builder = new FieldInfos.Builder();

            FieldData field = new FieldData(this, "field", builder, terms, true, false);
            FieldData[] fields = new FieldData[] { field };
            FieldInfos fieldInfos = builder.Finish();
            Directory dir = NewDirectory();
            this.Write(fieldInfos, dir, fields, true);
            Codec codec = Codec.Default;
            SegmentInfo si = new SegmentInfo(dir, Constants.LUCENE_MAIN_VERSION, SEGMENT, 10000, false, codec, null);

            FieldsProducer reader = codec.PostingsFormat().FieldsProducer(new SegmentReadState(dir, si, fieldInfos, NewIOContext(Random()), DirectoryReader.DEFAULT_TERMS_INDEX_DIVISOR));

            IEnumerator<string> fieldsEnum = reader.GetEnumerator();
            fieldsEnum.MoveNext();
            string fieldName = fieldsEnum.Current;
            Assert.IsNotNull(fieldName);
            Terms terms2 = reader.Terms(fieldName);
            Assert.IsNotNull(terms2);

            TermsEnum termsEnum = terms2.Iterator(null);

            DocsEnum docsEnum = null;
            for (int i = 0; i < NUM_TERMS; i++)
            {
                BytesRef term = termsEnum.Next();
                Assert.IsNotNull(term);
                Assert.AreEqual(terms[i].Text2, term.Utf8ToString());

                // do this twice to stress test the codec's reuse, ie,
                // make sure it properly fully resets (rewinds) its
                // internal state:
                for (int iter = 0; iter < 2; iter++)
                {
                    docsEnum = TestUtil.Docs(Random(), termsEnum, null, docsEnum, DocsEnum.FLAG_NONE);
                    Assert.AreEqual(terms[i].Docs[0], docsEnum.NextDoc());
                    Assert.AreEqual(DocIdSetIterator.NO_MORE_DOCS, docsEnum.NextDoc());
                }
            }
            Assert.IsNull(termsEnum.Next());

            for (int i = 0; i < NUM_TERMS; i++)
            {
                Assert.AreEqual(termsEnum.SeekCeil(new BytesRef(terms[i].Text2)), TermsEnum.SeekStatus.FOUND);
            }

            Assert.IsFalse(fieldsEnum.MoveNext());
            reader.Dispose();
            dir.Dispose();
        }

        [Test]
        public virtual void TestRandomPostings()
        {
            FieldInfos.Builder builder = new FieldInfos.Builder();

            FieldData[] fields = new FieldData[NUM_FIELDS];
            for (int i = 0; i < NUM_FIELDS; i++)
            {
                bool omitTF = 0 == (i % 3);
                bool storePayloads = 1 == (i % 3);
                fields[i] = new FieldData(this, FieldNames[i], builder, this.MakeRandomTerms(omitTF, storePayloads), omitTF, storePayloads);
            }

            Directory dir = NewDirectory();
            FieldInfos fieldInfos = builder.Finish();

            if (VERBOSE)
            {
                Console.WriteLine("TEST: now write postings");
            }

            this.Write(fieldInfos, dir, fields, false);
            Codec codec = Codec.Default;
            SegmentInfo si = new SegmentInfo(dir, Constants.LUCENE_MAIN_VERSION, SEGMENT, 10000, false, codec, null);

            if (VERBOSE)
            {
                Console.WriteLine("TEST: now read postings");
            }
            FieldsProducer terms = codec.PostingsFormat().FieldsProducer(new SegmentReadState(dir, si, fieldInfos, NewIOContext(Random()), DirectoryReader.DEFAULT_TERMS_INDEX_DIVISOR));

            Verify[] threads = new Verify[NUM_TEST_THREADS - 1];
            for (int i = 0; i < NUM_TEST_THREADS - 1; i++)
            {
                threads[i] = new Verify(this, si, fields, terms);
                threads[i].SetDaemon(true);
                threads[i].Start();
            }

            (new Verify(this, si, fields, terms)).Run();

            for (int i = 0; i < NUM_TEST_THREADS - 1; i++)
            {
                threads[i].Join();
                Debug.Assert(!threads[i].Failed);
            }

            terms.Dispose();
            dir.Dispose();
        }

        [Test]
        public virtual void TestSepPositionAfterMerge()
        {
            Directory dir = NewDirectory();
            IndexWriterConfig config = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random()));
            config.SetMergePolicy(NewLogMergePolicy());
            config.SetCodec(TestUtil.AlwaysPostingsFormat(new MockSepPostingsFormat()));
            IndexWriter writer = new IndexWriter(dir, config);

            try
            {
                PhraseQuery pq = new PhraseQuery();
                pq.Add(new Term("content", "bbb"));
                pq.Add(new Term("content", "ccc"));

                Document doc = new Document();
                FieldType customType = new FieldType(TextField.TYPE_NOT_STORED);
                customType.OmitNorms = true;
                doc.Add(NewField("content", "aaa bbb ccc ddd", customType));

                // add document and force commit for creating a first segment
                writer.AddDocument(doc);
                writer.Commit();

                ScoreDoc[] results = this.Search(writer, pq, 5);
                Assert.AreEqual(1, results.Length);
                Assert.AreEqual(0, results[0].Doc);

                // add document and force commit for creating a second segment
                writer.AddDocument(doc);
                writer.Commit();

                // at this point, there should be at least two segments
                results = this.Search(writer, pq, 5);
                Assert.AreEqual(2, results.Length);
                Assert.AreEqual(0, results[0].Doc);

                writer.ForceMerge(1);

                // optimise to merge the segments.
                results = this.Search(writer, pq, 5);
                Assert.AreEqual(2, results.Length);
                Assert.AreEqual(0, results[0].Doc);
            }
            finally
            {
                writer.Dispose();
                dir.Dispose();
            }
        }

        private ScoreDoc[] Search(IndexWriter writer, Query q, int n)
        {
            IndexReader reader = writer.Reader;
            IndexSearcher searcher = NewSearcher(reader);
            try
            {
                return searcher.Search(q, null, n).ScoreDocs;
            }
            finally
            {
                reader.Dispose();
            }
        }

        private class Verify : ThreadClass
        {
            private readonly TestCodecs OuterInstance;

            internal readonly Fields TermsDict;
            internal readonly FieldData[] Fields;
            internal readonly SegmentInfo Si;
            internal volatile bool Failed;

            internal Verify(TestCodecs outerInstance, SegmentInfo si, FieldData[] fields, Fields termsDict)
            {
                this.OuterInstance = outerInstance;
                this.Fields = fields;
                this.TermsDict = termsDict;
                this.Si = si;
            }

            public override void Run()
            {
                try
                {
                    this._run();
                }
                catch (Exception t)
                {
                    Failed = true;
                    throw new Exception(t.Message, t);
                }
            }

            internal virtual void VerifyDocs(int[] docs, PositionData[][] positions, DocsEnum docsEnum, bool doPos)
            {
                for (int i = 0; i < docs.Length; i++)
                {
                    int doc = docsEnum.NextDoc();
                    Assert.IsTrue(doc != DocIdSetIterator.NO_MORE_DOCS);
                    Assert.AreEqual(docs[i], doc);
                    if (doPos)
                    {
                        this.VerifyPositions(positions[i], ((DocsAndPositionsEnum)docsEnum));
                    }
                }
                Assert.AreEqual(DocIdSetIterator.NO_MORE_DOCS, docsEnum.NextDoc());
            }

            internal sbyte[] Data = new sbyte[10];

            internal virtual void VerifyPositions(PositionData[] positions, DocsAndPositionsEnum posEnum)
            {
                for (int i = 0; i < positions.Length; i++)
                {
                    int pos = posEnum.NextPosition();
                    Assert.AreEqual(positions[i].Pos, pos);
                    if (positions[i].Payload != null)
                    {
                        Assert.IsNotNull(posEnum.Payload);
                        if (Random().Next(3) < 2)
                        {
                            // Verify the payload bytes
                            BytesRef otherPayload = posEnum.Payload;
                            Assert.IsTrue(positions[i].Payload.Equals(otherPayload), "expected=" + positions[i].Payload.ToString() + " got=" + otherPayload.ToString());
                        }
                    }
                    else
                    {
                        Assert.IsNull(posEnum.Payload);
                    }
                }
            }

            public virtual void _run()
            {
                for (int iter = 0; iter < NUM_TEST_ITER; iter++)
                {
                    FieldData field = Fields[Random().Next(Fields.Length)];
                    TermsEnum termsEnum = TermsDict.Terms(field.FieldInfo.Name).Iterator(null);
                    if (Si.Codec is Lucene3xCodec)
                    {
                        // code below expects unicode sort order
                        continue;
                    }

                    int upto = 0;
                    // Test straight enum of the terms:
                    while (true)
                    {
                        BytesRef term = termsEnum.Next();
                        if (term == null)
                        {
                            break;
                        }
                        BytesRef expected = new BytesRef(field.Terms[upto++].Text2);
                        Assert.IsTrue(expected.BytesEquals(term), "expected=" + expected + " vs actual " + term);
                    }
                    Assert.AreEqual(upto, field.Terms.Length);

                    // Test random seek:
                    TermData term2 = field.Terms[Random().Next(field.Terms.Length)];
                    TermsEnum.SeekStatus status = termsEnum.SeekCeil(new BytesRef(term2.Text2));
                    Assert.AreEqual(status, TermsEnum.SeekStatus.FOUND);
                    Assert.AreEqual(term2.Docs.Length, termsEnum.DocFreq());
                    if (field.OmitTF)
                    {
                        this.VerifyDocs(term2.Docs, term2.Positions, TestUtil.Docs(Random(), termsEnum, null, null, DocsEnum.FLAG_NONE), false);
                    }
                    else
                    {
                        this.VerifyDocs(term2.Docs, term2.Positions, termsEnum.DocsAndPositions(null, null), true);
                    }

                    // Test random seek by ord:
                    int idx = Random().Next(field.Terms.Length);
                    term2 = field.Terms[idx];
                    bool success = false;
                    try
                    {
                        termsEnum.SeekExact(idx);
                        success = true;
                    }
                    catch (System.NotSupportedException uoe)
                    {
                        // ok -- skip it
                    }
                    if (success)
                    {
                        Assert.AreEqual(status, TermsEnum.SeekStatus.FOUND);
                        Assert.IsTrue(termsEnum.Term().BytesEquals(new BytesRef(term2.Text2)));
                        Assert.AreEqual(term2.Docs.Length, termsEnum.DocFreq());
                        if (field.OmitTF)
                        {
                            this.VerifyDocs(term2.Docs, term2.Positions, TestUtil.Docs(Random(), termsEnum, null, null, DocsEnum.FLAG_NONE), false);
                        }
                        else
                        {
                            this.VerifyDocs(term2.Docs, term2.Positions, termsEnum.DocsAndPositions(null, null), true);
                        }
                    }

                    // Test seek to non-existent terms:
                    if (VERBOSE)
                    {
                        Console.WriteLine("TEST: seek non-exist terms");
                    }
                    for (int i = 0; i < 100; i++)
                    {
                        string text2 = TestUtil.RandomUnicodeString(Random()) + ".";
                        status = termsEnum.SeekCeil(new BytesRef(text2));
                        Assert.IsTrue(status == TermsEnum.SeekStatus.NOT_FOUND || status == TermsEnum.SeekStatus.END);
                    }

                    // Seek to each term, backwards:
                    if (VERBOSE)
                    {
                        Console.WriteLine("TEST: seek terms backwards");
                    }
                    for (int i = field.Terms.Length - 1; i >= 0; i--)
                    {
                        Assert.AreEqual(TermsEnum.SeekStatus.FOUND, termsEnum.SeekCeil(new BytesRef(field.Terms[i].Text2)), Thread.CurrentThread.Name + ": field=" + field.FieldInfo.Name + " term=" + field.Terms[i].Text2);
                        Assert.AreEqual(field.Terms[i].Docs.Length, termsEnum.DocFreq());
                    }

                    // Seek to each term by ord, backwards
                    for (int i = field.Terms.Length - 1; i >= 0; i--)
                    {
                        try
                        {
                            termsEnum.SeekExact(i);
                            Assert.AreEqual(field.Terms[i].Docs.Length, termsEnum.DocFreq());
                            Assert.IsTrue(termsEnum.Term().BytesEquals(new BytesRef(field.Terms[i].Text2)));
                        }
                        catch (System.NotSupportedException uoe)
                        {
                        }
                    }

                    // Seek to non-existent empty-string term
                    status = termsEnum.SeekCeil(new BytesRef(""));
                    Assert.IsNotNull(status);
                    //Assert.AreEqual(TermsEnum.SeekStatus.NOT_FOUND, status);

                    // Make sure we're now pointing to first term
                    Assert.IsTrue(termsEnum.Term().BytesEquals(new BytesRef(field.Terms[0].Text2)));

                    // Test docs enum
                    termsEnum.SeekCeil(new BytesRef(""));
                    upto = 0;
                    do
                    {
                        term2 = field.Terms[upto];
                        if (Random().Next(3) == 1)
                        {
                            DocsEnum docs;
                            DocsEnum docsAndFreqs;
                            DocsAndPositionsEnum postings;
                            if (!field.OmitTF)
                            {
                                postings = termsEnum.DocsAndPositions(null, null);
                                if (postings != null)
                                {
                                    docs = docsAndFreqs = postings;
                                }
                                else
                                {
                                    docs = docsAndFreqs = TestUtil.Docs(Random(), termsEnum, null, null, DocsEnum.FLAG_FREQS);
                                }
                            }
                            else
                            {
                                postings = null;
                                docsAndFreqs = null;
                                docs = TestUtil.Docs(Random(), termsEnum, null, null, DocsEnum.FLAG_NONE);
                            }
                            Assert.IsNotNull(docs);
                            int upto2 = -1;
                            bool ended = false;
                            while (upto2 < term2.Docs.Length - 1)
                            {
                                // Maybe skip:
                                int left = term2.Docs.Length - upto2;
                                int doc;
                                if (Random().Next(3) == 1 && left >= 1)
                                {
                                    int inc = 1 + Random().Next(left - 1);
                                    upto2 += inc;
                                    if (Random().Next(2) == 1)
                                    {
                                        doc = docs.Advance(term2.Docs[upto2]);
                                        Assert.AreEqual(term2.Docs[upto2], doc);
                                    }
                                    else
                                    {
                                        doc = docs.Advance(1 + term2.Docs[upto2]);
                                        if (doc == DocIdSetIterator.NO_MORE_DOCS)
                                        {
                                            // skipped past last doc
                                            Debug.Assert(upto2 == term2.Docs.Length - 1);
                                            ended = true;
                                            break;
                                        }
                                        else
                                        {
                                            // skipped to next doc
                                            Debug.Assert(upto2 < term2.Docs.Length - 1);
                                            if (doc >= term2.Docs[1 + upto2])
                                            {
                                                upto2++;
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    doc = docs.NextDoc();
                                    Assert.IsTrue(doc != -1);
                                    upto2++;
                                }
                                Assert.AreEqual(term2.Docs[upto2], doc);
                                if (!field.OmitTF)
                                {
                                    Assert.AreEqual(term2.Positions[upto2].Length, postings.Freq());
                                    if (Random().Next(2) == 1)
                                    {
                                        this.VerifyPositions(term2.Positions[upto2], postings);
                                    }
                                }
                            }

                            if (!ended)
                            {
                                Assert.AreEqual(DocIdSetIterator.NO_MORE_DOCS, docs.NextDoc());
                            }
                        }
                        upto++;
                    } while (termsEnum.Next() != null);

                    Assert.AreEqual(upto, field.Terms.Length);
                }
            }
        }

        private void Write(FieldInfos fieldInfos, Directory dir, FieldData[] fields, bool allowPreFlex)
        {
            int termIndexInterval = TestUtil.NextInt(Random(), 13, 27);
            Codec codec = Codec.Default;
            SegmentInfo si = new SegmentInfo(dir, Constants.LUCENE_MAIN_VERSION, SEGMENT, 10000, false, codec, null);
            SegmentWriteState state = new SegmentWriteState(InfoStream.Default, dir, si, fieldInfos, termIndexInterval, null, NewIOContext(Random()));

            FieldsConsumer consumer = codec.PostingsFormat().FieldsConsumer(state);
            Array.Sort(fields);
            foreach (FieldData field in fields)
            {
                if (!allowPreFlex && codec is Lucene3xCodec)
                {
                    // code below expects unicode sort order
                    continue;
                }
                field.Write(consumer);
            }
            consumer.Dispose();
        }

        [Test]
        public virtual void TestDocsOnlyFreq()
        {
            // tests that when fields are indexed with DOCS_ONLY, the Codec
            // returns 1 in docsEnum.Freq()
            Directory dir = NewDirectory();
            Random random = Random();
            IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random)));
            // we don't need many documents to assert this, but don't use one document either
            int numDocs = AtLeast(random, 50);
            for (int i = 0; i < numDocs; i++)
            {
                Document doc = new Document();
                doc.Add(new StringField("f", "doc", Store.NO));
                writer.AddDocument(doc);
            }
            writer.Dispose();

            Term term = new Term("f", new BytesRef("doc"));
            DirectoryReader reader = DirectoryReader.Open(dir);
            foreach (AtomicReaderContext ctx in reader.Leaves)
            {
                DocsEnum de = ((AtomicReader)ctx.Reader).TermDocsEnum(term);
                while (de.NextDoc() != DocIdSetIterator.NO_MORE_DOCS)
                {
                    Assert.AreEqual(1, de.Freq(), "wrong freq for doc " + de.DocID());
                }
            }
            reader.Dispose();

            dir.Dispose();
        }

        [Test]
        public virtual void TestDisableImpersonation()
        {
            Codec[] oldCodecs = new Codec[] { new Lucene40RWCodec(OLD_FORMAT_IMPERSONATION_IS_ACTIVE), new Lucene41RWCodec(OLD_FORMAT_IMPERSONATION_IS_ACTIVE), new Lucene42RWCodec(OLD_FORMAT_IMPERSONATION_IS_ACTIVE) };
            Directory dir = NewDirectory();
            IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random()));
            conf.SetCodec(oldCodecs[Random().Next(oldCodecs.Length)]);
            IndexWriter writer = new IndexWriter(dir, conf);

            Document doc = new Document();
            doc.Add(new StringField("f", "bar", Store.YES));
            doc.Add(new NumericDocValuesField("n", 18L));
            writer.AddDocument(doc);

            OLD_FORMAT_IMPERSONATION_IS_ACTIVE = false;
            try
            {
                writer.Dispose();
                Assert.Fail("should not have succeeded to impersonate an old format!");
            }
            catch (System.NotSupportedException e)
            {
                writer.Rollback();
            }
            finally
            {
                OLD_FORMAT_IMPERSONATION_IS_ACTIVE = true;
            }

            dir.Dispose();
        }
    }
}