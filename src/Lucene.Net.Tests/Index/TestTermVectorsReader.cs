using Lucene.Net.Analysis;
using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Documents;
using Lucene.Net.Index.Extensions;
using NUnit.Framework;
using System;
using System.IO;
using Assert = Lucene.Net.TestFramework.Assert;

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
            positions = new int[testTerms.Length][];
            tokens = new TestToken[testTerms.Length * TERM_FREQ];
        }

        //Must be lexicographically sorted, will do in setup, versus trying to maintain here
        private string[] testFields = new string[] { "f1", "f2", "f3", "f4" };

        private bool[] testFieldsStorePos = new bool[] { true, false, true, false };
        private bool[] testFieldsStoreOff = new bool[] { true, false, false, true };
        private string[] testTerms = new string[] { "this", "is", "a", "test" };
        private int[][] positions;
        private Directory dir;
        private SegmentCommitInfo seg;
        private FieldInfos fieldInfos = new FieldInfos(new FieldInfo[0]);
        private static int TERM_FREQ = 3;

        internal class TestToken : IComparable<TestToken>
        {
            private readonly TestTermVectorsReader outerInstance;

            public TestToken(TestTermVectorsReader outerInstance)
            {
                this.outerInstance = outerInstance;
            }

            internal string text;
            internal int pos;
            internal int startOffset;
            internal int endOffset;

            public virtual int CompareTo(TestToken other)
            {
                return pos - other.pos;
            }
        }

        internal TestToken[] tokens;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            /*
            for (int i = 0; i < testFields.length; i++) {
              fieldInfos.Add(testFields[i], true, true, testFieldsStorePos[i], testFieldsStoreOff[i]);
            }
            */

            Array.Sort(testTerms, StringComparer.Ordinal);
            int tokenUpto = 0;
            for (int i = 0; i < testTerms.Length; i++)
            {
                positions[i] = new int[TERM_FREQ];
                // first position must be 0
                for (int j = 0; j < TERM_FREQ; j++)
                {
                    // positions are always sorted in increasing order
                    positions[i][j] = (int)(j * 10 + Random.NextDouble() * 10); // LUCENENET: Using Random because Math.random() doesn't exist in .NET and it seems to make sense to want this repeatable.
                    TestToken token = tokens[tokenUpto++] = new TestToken(this);
                    token.text = testTerms[i];
                    token.pos = positions[i][j];
                    token.startOffset = j * 10;
                    token.endOffset = j * 10 + testTerms[i].Length;
                }
            }
            Array.Sort(tokens);

            dir = NewDirectory();
            IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MyAnalyzer(this)).SetMaxBufferedDocs(-1).SetMergePolicy(NewLogMergePolicy(false, 10)).SetUseCompoundFile(false));

            Document doc = new Document();
            for (int i = 0; i < testFields.Length; i++)
            {
                FieldType customType = new FieldType(TextField.TYPE_NOT_STORED);
                if (testFieldsStorePos[i] && testFieldsStoreOff[i])
                {
                    customType.StoreTermVectors = true;
                    customType.StoreTermVectorPositions = true;
                    customType.StoreTermVectorOffsets = true;
                }
                else if (testFieldsStorePos[i] && !testFieldsStoreOff[i])
                {
                    customType.StoreTermVectors = true;
                    customType.StoreTermVectorPositions = true;
                }
                else if (!testFieldsStorePos[i] && testFieldsStoreOff[i])
                {
                    customType.StoreTermVectors = true;
                    customType.StoreTermVectorOffsets = true;
                }
                else
                {
                    customType.StoreTermVectors = true;
                }
                doc.Add(new Field(testFields[i], "", customType));
            }

            //Create 5 documents for testing, they all have the same
            //terms
            for (int j = 0; j < 5; j++)
            {
                writer.AddDocument(doc);
            }
            writer.Commit();
            seg = writer.NewestSegment();
            writer.Dispose();

            fieldInfos = SegmentReader.ReadFieldInfos(seg);
        }

        [TearDown]
        public override void TearDown()
        {
            dir.Dispose();
            base.TearDown();
        }

        private class MyTokenizer : Tokenizer
        {
            private readonly TestTermVectorsReader outerInstance;

            private int tokenUpto;

            private readonly ICharTermAttribute termAtt;
            private readonly IPositionIncrementAttribute posIncrAtt;
            private readonly IOffsetAttribute offsetAtt;

            public MyTokenizer(TestTermVectorsReader outerInstance, TextReader reader)
                : base(reader)
            {
                this.outerInstance = outerInstance;
                termAtt = AddAttribute<ICharTermAttribute>();
                posIncrAtt = AddAttribute<IPositionIncrementAttribute>();
                offsetAtt = AddAttribute<IOffsetAttribute>();
            }

            public sealed override bool IncrementToken()
            {
                if (tokenUpto >= outerInstance.tokens.Length)
                {
                    return false;
                }
                else
                {
                    TestToken testToken = outerInstance.tokens[tokenUpto++];
                    ClearAttributes();
                    termAtt.Append(testToken.text);
                    offsetAtt.SetOffset(testToken.startOffset, testToken.endOffset);
                    if (tokenUpto > 1)
                    {
                        posIncrAtt.PositionIncrement = testToken.pos - outerInstance.tokens[tokenUpto - 2].pos;
                    }
                    else
                    {
                        posIncrAtt.PositionIncrement = testToken.pos + 1;
                    }
                    return true;
                }
            }

            public override void Reset()
            {
                base.Reset();
                this.tokenUpto = 0;
            }
        }

        private class MyAnalyzer : Analyzer
        {
            private readonly TestTermVectorsReader outerInstance;

            public MyAnalyzer(TestTermVectorsReader outerInstance)
            {
                this.outerInstance = outerInstance;
            }

            protected internal override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
            {
                return new TokenStreamComponents(new MyTokenizer(outerInstance, reader));
            }
        }

        [Test]
        public virtual void Test()
        {
            //Check to see the files were created properly in setup
            DirectoryReader reader = DirectoryReader.Open(dir);
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
            TermVectorsReader reader = Codec.Default.TermVectorsFormat.VectorsReader(dir, seg.Info, fieldInfos, NewIOContext(Random));
            for (int j = 0; j < 5; j++)
            {
                Terms vector = reader.Get(j).GetTerms(testFields[0]);
                Assert.IsNotNull(vector);
                Assert.AreEqual(testTerms.Length, vector.Count);
                TermsEnum termsEnum = vector.GetEnumerator();
                for (int i = 0; i < testTerms.Length; i++)
                {
                    Assert.IsTrue(termsEnum.MoveNext());
                    BytesRef text = termsEnum.Term;
                    string term = text.Utf8ToString();
                    //System.out.println("Term: " + term);
                    Assert.AreEqual(testTerms[i], term);
                }
                Assert.IsFalse(termsEnum.MoveNext());
            }
            reader.Dispose();
        }

        [Test]
        public virtual void TestDocsEnum()
        {
            TermVectorsReader reader = Codec.Default.TermVectorsFormat.VectorsReader(dir, seg.Info, fieldInfos, NewIOContext(Random));
            for (int j = 0; j < 5; j++)
            {
                Terms vector = reader.Get(j).GetTerms(testFields[0]);
                Assert.IsNotNull(vector);
                Assert.AreEqual(testTerms.Length, vector.Count);
                TermsEnum termsEnum = vector.GetEnumerator();
                DocsEnum docsEnum = null;
                for (int i = 0; i < testTerms.Length; i++)
                {
                    Assert.IsTrue(termsEnum.MoveNext());
                    BytesRef text = termsEnum.Term;
                    string term = text.Utf8ToString();
                    //System.out.println("Term: " + term);
                    Assert.AreEqual(testTerms[i], term);

                    docsEnum = TestUtil.Docs(Random, termsEnum, null, docsEnum, DocsFlags.NONE);
                    Assert.IsNotNull(docsEnum);
                    int doc = docsEnum.DocID;
                    Assert.AreEqual(-1, doc);
                    Assert.IsTrue(docsEnum.NextDoc() != DocIdSetIterator.NO_MORE_DOCS);
                    Assert.AreEqual(DocIdSetIterator.NO_MORE_DOCS, docsEnum.NextDoc());
                }
                Assert.IsFalse(termsEnum.MoveNext());
            }
            reader.Dispose();
        }

        [Test]
        public virtual void TestPositionReader()
        {
            TermVectorsReader reader = Codec.Default.TermVectorsFormat.VectorsReader(dir, seg.Info, fieldInfos, NewIOContext(Random));
            //BytesRef[] terms; // LUCENENET NOTE: Not used in Lucene
            Terms vector = reader.Get(0).GetTerms(testFields[0]);
            Assert.IsNotNull(vector);
            Assert.AreEqual(testTerms.Length, vector.Count);
            TermsEnum termsEnum = vector.GetEnumerator();
            DocsAndPositionsEnum dpEnum = null;
            for (int i = 0; i < testTerms.Length; i++)
            {
                Assert.IsTrue(termsEnum.MoveNext());
                BytesRef text = termsEnum.Term;
                string term = text.Utf8ToString();
                //System.out.println("Term: " + term);
                Assert.AreEqual(testTerms[i], term);

                dpEnum = termsEnum.DocsAndPositions(null, dpEnum);
                Assert.IsNotNull(dpEnum);
                int doc = dpEnum.DocID;
                Assert.AreEqual(-1, doc);
                Assert.IsTrue(dpEnum.NextDoc() != DocIdSetIterator.NO_MORE_DOCS);
                Assert.AreEqual(dpEnum.Freq, positions[i].Length);
                for (int j = 0; j < positions[i].Length; j++)
                {
                    Assert.AreEqual(positions[i][j], dpEnum.NextPosition());
                }
                Assert.AreEqual(DocIdSetIterator.NO_MORE_DOCS, dpEnum.NextDoc());

                dpEnum = termsEnum.DocsAndPositions(null, dpEnum);
                doc = dpEnum.DocID;
                Assert.AreEqual(-1, doc);
                Assert.IsTrue(dpEnum.NextDoc() != DocIdSetIterator.NO_MORE_DOCS);
                Assert.IsNotNull(dpEnum);
                Assert.AreEqual(dpEnum.Freq, positions[i].Length);
                for (int j = 0; j < positions[i].Length; j++)
                {
                    Assert.AreEqual(positions[i][j], dpEnum.NextPosition());
                    Assert.AreEqual(j * 10, dpEnum.StartOffset);
                    Assert.AreEqual(j * 10 + testTerms[i].Length, dpEnum.EndOffset);
                }
                Assert.AreEqual(DocIdSetIterator.NO_MORE_DOCS, dpEnum.NextDoc());
            }

            Terms freqVector = reader.Get(0).GetTerms(testFields[1]); //no pos, no offset
            Assert.IsNotNull(freqVector);
            Assert.AreEqual(testTerms.Length, freqVector.Count);
            termsEnum = freqVector.GetEnumerator();
            Assert.IsNotNull(termsEnum);
            for (int i = 0; i < testTerms.Length; i++)
            {
                Assert.IsTrue(termsEnum.MoveNext());
                BytesRef text = termsEnum.Term;
                string term = text.Utf8ToString();
                //System.out.println("Term: " + term);
                Assert.AreEqual(testTerms[i], term);
                Assert.IsNotNull(termsEnum.Docs(null, null));
                Assert.IsNull(termsEnum.DocsAndPositions(null, null)); // no pos
            }
            reader.Dispose();
        }

        [Test]
        public virtual void TestOffsetReader()
        {
            TermVectorsReader reader = Codec.Default.TermVectorsFormat.VectorsReader(dir, seg.Info, fieldInfos, NewIOContext(Random));
            Terms vector = reader.Get(0).GetTerms(testFields[0]);
            Assert.IsNotNull(vector);
            TermsEnum termsEnum = vector.GetEnumerator();
            Assert.IsNotNull(termsEnum);
            Assert.AreEqual(testTerms.Length, vector.Count);
            DocsAndPositionsEnum dpEnum = null;
            for (int i = 0; i < testTerms.Length; i++)
            {
                Assert.IsTrue(termsEnum.MoveNext());
                BytesRef text = termsEnum.Term;
                string term = text.Utf8ToString();
                Assert.AreEqual(testTerms[i], term);

                dpEnum = termsEnum.DocsAndPositions(null, dpEnum);
                Assert.IsNotNull(dpEnum);
                Assert.IsTrue(dpEnum.NextDoc() != DocIdSetIterator.NO_MORE_DOCS);
                Assert.AreEqual(dpEnum.Freq, positions[i].Length);
                for (int j = 0; j < positions[i].Length; j++)
                {
                    Assert.AreEqual(positions[i][j], dpEnum.NextPosition());
                }
                Assert.AreEqual(DocIdSetIterator.NO_MORE_DOCS, dpEnum.NextDoc());

                dpEnum = termsEnum.DocsAndPositions(null, dpEnum);
                Assert.IsTrue(dpEnum.NextDoc() != DocIdSetIterator.NO_MORE_DOCS);
                Assert.IsNotNull(dpEnum);
                Assert.AreEqual(dpEnum.Freq, positions[i].Length);
                for (int j = 0; j < positions[i].Length; j++)
                {
                    Assert.AreEqual(positions[i][j], dpEnum.NextPosition());
                    Assert.AreEqual(j * 10, dpEnum.StartOffset);
                    Assert.AreEqual(j * 10 + testTerms[i].Length, dpEnum.EndOffset);
                }
                Assert.AreEqual(DocIdSetIterator.NO_MORE_DOCS, dpEnum.NextDoc());
            }
            reader.Dispose();
        }

        [Test]
        public virtual void TestIllegalIndexableField()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter w = new RandomIndexWriter(Random, dir);
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
            catch (Exception iae) when (iae.IsIllegalArgumentException())
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
            catch (Exception iae) when (iae.IsIllegalArgumentException())
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
            catch (Exception iae) when (iae.IsIllegalArgumentException())
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
            catch (Exception iae) when (iae.IsIllegalArgumentException())
            {
                // Expected
                Assert.AreEqual("cannot index term vector payloads when term vectors are not indexed (field=\"field\")", iae.Message);
            }

            w.Dispose();

            dir.Dispose();
        }
    }
}