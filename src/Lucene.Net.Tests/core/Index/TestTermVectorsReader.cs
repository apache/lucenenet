using Lucene.Net.Analysis.TokenAttributes;
using System;
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
    using NUnit.Framework;
    using System.IO;
    using BytesRef = Lucene.Net.Util.BytesRef;
    using Codec = Lucene.Net.Codecs.Codec;
    using Directory = Lucene.Net.Store.Directory;
    using DocIdSetIterator = Lucene.Net.Search.DocIdSetIterator;
    using Document = Documents.Document;
    using Field = Field;
    using FieldType = FieldType;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using TermVectorsReader = Lucene.Net.Codecs.TermVectorsReader;
    using TestUtil = Lucene.Net.Util.TestUtil;
    using TextField = TextField;

    [TestFixture]
    public class TestTermVectorsReader : LuceneTestCase
    {
        public TestTermVectorsReader()
        {
            InitializeInstanceFields();
        }

        private void InitializeInstanceFields()
        {
            Positions = new int[TestTerms.Length][];
            Tokens = new TestToken[TestTerms.Length * TERM_FREQ];
        }

        //Must be lexicographically sorted, will do in setup, versus trying to maintain here
        private string[] TestFields = new string[] { "f1", "f2", "f3", "f4" };

        private bool[] TestFieldsStorePos = new bool[] { true, false, true, false };
        private bool[] TestFieldsStoreOff = new bool[] { true, false, false, true };
        private string[] TestTerms = new string[] { "this", "is", "a", "test" };
        private int[][] Positions;
        private Directory Dir;
        private SegmentCommitInfo Seg;
        private FieldInfos FieldInfos = new FieldInfos(new FieldInfo[0]);
        private static int TERM_FREQ = 3;

        internal class TestToken : IComparable<TestToken>
        {
            private readonly TestTermVectorsReader OuterInstance;

            public TestToken(TestTermVectorsReader outerInstance)
            {
                this.OuterInstance = outerInstance;
            }

            internal string Text;
            internal int Pos;
            internal int StartOffset;
            internal int EndOffset;

            public virtual int CompareTo(TestToken other)
            {
                return Pos - other.Pos;
            }
        }

        internal TestToken[] Tokens;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            /*
            for (int i = 0; i < testFields.Length; i++) {
              fieldInfos.Add(testFields[i], true, true, testFieldsStorePos[i], testFieldsStoreOff[i]);
            }
            */

            Array.Sort(TestTerms);
            int tokenUpto = 0;
            for (int i = 0; i < TestTerms.Length; i++)
            {
                Positions[i] = new int[TERM_FREQ];
                // first position must be 0
                for (int j = 0; j < TERM_FREQ; j++)
                {
                    // positions are always sorted in increasing order
                    Positions[i][j] = (int)(j * 10 + new Random(1).NextDouble() * 10);
                    TestToken token = Tokens[tokenUpto++] = new TestToken(this);
                    token.Text = TestTerms[i];
                    token.Pos = Positions[i][j];
                    token.StartOffset = j * 10;
                    token.EndOffset = j * 10 + TestTerms[i].Length;
                }
            }
            Array.Sort(Tokens);

            Dir = NewDirectory();
            IndexWriter writer = new IndexWriter(Dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MyAnalyzer(this)).SetMaxBufferedDocs(-1).SetMergePolicy(NewLogMergePolicy(false, 10)).SetUseCompoundFile(false));

            Document doc = new Document();
            for (int i = 0; i < TestFields.Length; i++)
            {
                FieldType customType = new FieldType(TextField.TYPE_NOT_STORED);
                if (TestFieldsStorePos[i] && TestFieldsStoreOff[i])
                {
                    customType.StoreTermVectors = true;
                    customType.StoreTermVectorPositions = true;
                    customType.StoreTermVectorOffsets = true;
                }
                else if (TestFieldsStorePos[i] && !TestFieldsStoreOff[i])
                {
                    customType.StoreTermVectors = true;
                    customType.StoreTermVectorPositions = true;
                }
                else if (!TestFieldsStorePos[i] && TestFieldsStoreOff[i])
                {
                    customType.StoreTermVectors = true;
                    customType.StoreTermVectorOffsets = true;
                }
                else
                {
                    customType.StoreTermVectors = true;
                }
                doc.Add(new Field(TestFields[i], "", customType));
            }

            //Create 5 documents for testing, they all have the same
            //terms
            for (int j = 0; j < 5; j++)
            {
                writer.AddDocument(doc);
            }
            writer.Commit();
            Seg = writer.NewestSegment();
            writer.Dispose();

            FieldInfos = SegmentReader.ReadFieldInfos(Seg);
        }

        [TearDown]
        public override void TearDown()
        {
            Dir.Dispose();
            base.TearDown();
        }

        private class MyTokenizer : Tokenizer
        {
            private readonly TestTermVectorsReader OuterInstance;

            internal int TokenUpto;

            internal readonly ICharTermAttribute TermAtt;
            internal readonly IPositionIncrementAttribute PosIncrAtt;
            internal readonly IOffsetAttribute OffsetAtt;

            public MyTokenizer(TestTermVectorsReader outerInstance, TextReader reader)
                : base(reader)
            {
                this.OuterInstance = outerInstance;
                TermAtt = AddAttribute<ICharTermAttribute>();
                PosIncrAtt = AddAttribute<IPositionIncrementAttribute>();
                OffsetAtt = AddAttribute<IOffsetAttribute>();
            }

            public sealed override bool IncrementToken()
            {
                if (TokenUpto >= OuterInstance.Tokens.Length)
                {
                    return false;
                }
                else
                {
                    TestToken testToken = OuterInstance.Tokens[TokenUpto++];
                    ClearAttributes();
                    TermAtt.Append(testToken.Text);
                    OffsetAtt.SetOffset(testToken.StartOffset, testToken.EndOffset);
                    if (TokenUpto > 1)
                    {
                        PosIncrAtt.PositionIncrement = testToken.Pos - OuterInstance.Tokens[TokenUpto - 2].Pos;
                    }
                    else
                    {
                        PosIncrAtt.PositionIncrement = testToken.Pos + 1;
                    }
                    return true;
                }
            }

            public override void Reset()
            {
                base.Reset();
                this.TokenUpto = 0;
            }
        }

        private class MyAnalyzer : Analyzer
        {
            private readonly TestTermVectorsReader OuterInstance;

            public MyAnalyzer(TestTermVectorsReader outerInstance)
            {
                this.OuterInstance = outerInstance;
            }

            protected internal override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
            {
                return new TokenStreamComponents(new MyTokenizer(OuterInstance, reader));
            }
        }

        [Test]
        public virtual void Test()
        {
            //Check to see the files were created properly in setup
            DirectoryReader reader = DirectoryReader.Open(Dir);
            foreach (AtomicReaderContext ctx in reader.Leaves)
            {
                SegmentReader sr = (SegmentReader)ctx.Reader;
                Assert.IsTrue(sr.FieldInfos.HasVectors);
            }
            reader.Dispose();
        }

        [Test]
        public virtual void TestReader()
        {
            TermVectorsReader reader = Codec.Default.TermVectorsFormat.VectorsReader(Dir, Seg.Info, FieldInfos, NewIOContext(Random()));
            for (int j = 0; j < 5; j++)
            {
                Terms vector = reader.Get(j).Terms(TestFields[0]);
                Assert.IsNotNull(vector);
                Assert.AreEqual(TestTerms.Length, vector.Count);
                TermsEnum termsEnum = vector.Iterator(null);
                for (int i = 0; i < TestTerms.Length; i++)
                {
                    BytesRef text = termsEnum.Next();
                    Assert.IsNotNull(text);
                    string term = text.Utf8ToString();
                    //System.out.println("Term: " + term);
                    Assert.AreEqual(TestTerms[i], term);
                }
                Assert.IsNull(termsEnum.Next());
            }
            reader.Dispose();
        }

        [Test]
        public virtual void TestDocsEnum()
        {
            TermVectorsReader reader = Codec.Default.TermVectorsFormat.VectorsReader(Dir, Seg.Info, FieldInfos, NewIOContext(Random()));
            for (int j = 0; j < 5; j++)
            {
                Terms vector = reader.Get(j).Terms(TestFields[0]);
                Assert.IsNotNull(vector);
                Assert.AreEqual(TestTerms.Length, vector.Count);
                TermsEnum termsEnum = vector.Iterator(null);
                DocsEnum docsEnum = null;
                for (int i = 0; i < TestTerms.Length; i++)
                {
                    BytesRef text = termsEnum.Next();
                    Assert.IsNotNull(text);
                    string term = text.Utf8ToString();
                    //System.out.println("Term: " + term);
                    Assert.AreEqual(TestTerms[i], term);

                    docsEnum = TestUtil.Docs(Random(), termsEnum, null, docsEnum, DocsEnum.FLAG_NONE);
                    Assert.IsNotNull(docsEnum);
                    int doc = docsEnum.DocID;
                    Assert.AreEqual(-1, doc);
                    Assert.IsTrue(docsEnum.NextDoc() != DocIdSetIterator.NO_MORE_DOCS);
                    Assert.AreEqual(DocIdSetIterator.NO_MORE_DOCS, docsEnum.NextDoc());
                }
                Assert.IsNull(termsEnum.Next());
            }
            reader.Dispose();
        }

        [Test]
        public virtual void TestPositionReader()
        {
            TermVectorsReader reader = Codec.Default.TermVectorsFormat.VectorsReader(Dir, Seg.Info, FieldInfos, NewIOContext(Random()));
            BytesRef[] terms;
            Terms vector = reader.Get(0).Terms(TestFields[0]);
            Assert.IsNotNull(vector);
            Assert.AreEqual(TestTerms.Length, vector.Count);
            TermsEnum termsEnum = vector.Iterator(null);
            DocsAndPositionsEnum dpEnum = null;
            for (int i = 0; i < TestTerms.Length; i++)
            {
                BytesRef text = termsEnum.Next();
                Assert.IsNotNull(text);
                string term = text.Utf8ToString();
                //System.out.println("Term: " + term);
                Assert.AreEqual(TestTerms[i], term);

                dpEnum = termsEnum.DocsAndPositions(null, dpEnum);
                Assert.IsNotNull(dpEnum);
                int doc = dpEnum.DocID;
                Assert.AreEqual(-1, doc);
                Assert.IsTrue(dpEnum.NextDoc() != DocIdSetIterator.NO_MORE_DOCS);
                Assert.AreEqual(dpEnum.Freq, Positions[i].Length);
                for (int j = 0; j < Positions[i].Length; j++)
                {
                    Assert.AreEqual(Positions[i][j], dpEnum.NextPosition());
                }
                Assert.AreEqual(DocIdSetIterator.NO_MORE_DOCS, dpEnum.NextDoc());

                dpEnum = termsEnum.DocsAndPositions(null, dpEnum);
                doc = dpEnum.DocID;
                Assert.AreEqual(-1, doc);
                Assert.IsTrue(dpEnum.NextDoc() != DocIdSetIterator.NO_MORE_DOCS);
                Assert.IsNotNull(dpEnum);
                Assert.AreEqual(dpEnum.Freq, Positions[i].Length);
                for (int j = 0; j < Positions[i].Length; j++)
                {
                    Assert.AreEqual(Positions[i][j], dpEnum.NextPosition());
                    Assert.AreEqual(j * 10, dpEnum.StartOffset);
                    Assert.AreEqual(j * 10 + TestTerms[i].Length, dpEnum.EndOffset);
                }
                Assert.AreEqual(DocIdSetIterator.NO_MORE_DOCS, dpEnum.NextDoc());
            }

            Terms freqVector = reader.Get(0).Terms(TestFields[1]); //no pos, no offset
            Assert.IsNotNull(freqVector);
            Assert.AreEqual(TestTerms.Length, freqVector.Count);
            termsEnum = freqVector.Iterator(null);
            Assert.IsNotNull(termsEnum);
            for (int i = 0; i < TestTerms.Length; i++)
            {
                BytesRef text = termsEnum.Next();
                Assert.IsNotNull(text);
                string term = text.Utf8ToString();
                //System.out.println("Term: " + term);
                Assert.AreEqual(TestTerms[i], term);
                Assert.IsNotNull(termsEnum.Docs(null, null));
                Assert.IsNull(termsEnum.DocsAndPositions(null, null)); // no pos
            }
            reader.Dispose();
        }

        [Test]
        public virtual void TestOffsetReader()
        {
            TermVectorsReader reader = Codec.Default.TermVectorsFormat.VectorsReader(Dir, Seg.Info, FieldInfos, NewIOContext(Random()));
            Terms vector = reader.Get(0).Terms(TestFields[0]);
            Assert.IsNotNull(vector);
            TermsEnum termsEnum = vector.Iterator(null);
            Assert.IsNotNull(termsEnum);
            Assert.AreEqual(TestTerms.Length, vector.Count);
            DocsAndPositionsEnum dpEnum = null;
            for (int i = 0; i < TestTerms.Length; i++)
            {
                BytesRef text = termsEnum.Next();
                Assert.IsNotNull(text);
                string term = text.Utf8ToString();
                Assert.AreEqual(TestTerms[i], term);

                dpEnum = termsEnum.DocsAndPositions(null, dpEnum);
                Assert.IsNotNull(dpEnum);
                Assert.IsTrue(dpEnum.NextDoc() != DocIdSetIterator.NO_MORE_DOCS);
                Assert.AreEqual(dpEnum.Freq, Positions[i].Length);
                for (int j = 0; j < Positions[i].Length; j++)
                {
                    Assert.AreEqual(Positions[i][j], dpEnum.NextPosition());
                }
                Assert.AreEqual(DocIdSetIterator.NO_MORE_DOCS, dpEnum.NextDoc());

                dpEnum = termsEnum.DocsAndPositions(null, dpEnum);
                Assert.IsTrue(dpEnum.NextDoc() != DocIdSetIterator.NO_MORE_DOCS);
                Assert.IsNotNull(dpEnum);
                Assert.AreEqual(dpEnum.Freq, Positions[i].Length);
                for (int j = 0; j < Positions[i].Length; j++)
                {
                    Assert.AreEqual(Positions[i][j], dpEnum.NextPosition());
                    Assert.AreEqual(j * 10, dpEnum.StartOffset);
                    Assert.AreEqual(j * 10 + TestTerms[i].Length, dpEnum.EndOffset);
                }
                Assert.AreEqual(DocIdSetIterator.NO_MORE_DOCS, dpEnum.NextDoc());
            }
            reader.Dispose();
        }

        [Test]
        public virtual void TestIllegalIndexableField()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter w = new RandomIndexWriter(Random(), dir, Similarity, TimeZone);
            FieldType ft = new FieldType(TextField.TYPE_NOT_STORED);
            ft.StoreTermVectors = true;
            ft.StoreTermVectorPayloads = true;
            Document doc = new Document();
            doc.Add(new Field("field", "value", ft));
            try
            {
                w.AddDocument(doc);
                Assert.Fail("did not hit exception");
            }
            catch (System.ArgumentException iae)
            {
                // Expected
                Assert.AreEqual("cannot index term vector payloads without term vector positions (field=\"field\")", iae.Message);
            }

            ft = new FieldType(TextField.TYPE_NOT_STORED);
            ft.StoreTermVectors = false;
            ft.StoreTermVectorOffsets = true;
            doc = new Document();
            doc.Add(new Field("field", "value", ft));
            try
            {
                w.AddDocument(doc);
                Assert.Fail("did not hit exception");
            }
            catch (System.ArgumentException iae)
            {
                // Expected
                Assert.AreEqual("cannot index term vector offsets when term vectors are not indexed (field=\"field\")", iae.Message);
            }

            ft = new FieldType(TextField.TYPE_NOT_STORED);
            ft.StoreTermVectors = false;
            ft.StoreTermVectorPositions = true;
            doc = new Document();
            doc.Add(new Field("field", "value", ft));
            try
            {
                w.AddDocument(doc);
                Assert.Fail("did not hit exception");
            }
            catch (System.ArgumentException iae)
            {
                // Expected
                Assert.AreEqual("cannot index term vector positions when term vectors are not indexed (field=\"field\")", iae.Message);
            }

            ft = new FieldType(TextField.TYPE_NOT_STORED);
            ft.StoreTermVectors = false;
            ft.StoreTermVectorPayloads = true;
            doc = new Document();
            doc.Add(new Field("field", "value", ft));
            try
            {
                w.AddDocument(doc);
                Assert.Fail("did not hit exception");
            }
            catch (System.ArgumentException iae)
            {
                // Expected
                Assert.AreEqual("cannot index term vector payloads when term vectors are not indexed (field=\"field\")", iae.Message);
            }

            w.Dispose();

            dir.Dispose();
        }
    }
}