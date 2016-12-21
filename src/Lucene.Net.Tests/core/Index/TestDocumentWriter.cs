using Lucene.Net.Analysis.TokenAttributes;
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
    using AttributeSource = Lucene.Net.Util.AttributeSource;
    using BytesRef = Lucene.Net.Util.BytesRef;
    using Directory = Lucene.Net.Store.Directory;
    using DocIdSetIterator = Lucene.Net.Search.DocIdSetIterator;
    using Document = Documents.Document;
    using Field = Field;
    using FieldType = FieldType;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using StringField = StringField;
    using TestUtil = Lucene.Net.Util.TestUtil;
    using TextField = TextField;

    [TestFixture]
    public class TestDocumentWriter : LuceneTestCase
    {
        private Directory Dir;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            Dir = NewDirectory();
        }

        [TearDown]
        public override void TearDown()
        {
            Dir.Dispose();
            base.TearDown();
        }

        [Test]
        public virtual void Test()
        {
            Assert.IsTrue(Dir != null);
        }

        [Test]
        public virtual void TestAddDocument()
        {
            Document testDoc = new Document();
            DocHelper.SetupDoc(testDoc);
            IndexWriter writer = new IndexWriter(Dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())));
            writer.AddDocument(testDoc);
            writer.Commit();
            SegmentCommitInfo info = writer.NewestSegment();
            writer.Dispose();
            //After adding the document, we should be able to read it back in
            SegmentReader reader = new SegmentReader(info, DirectoryReader.DEFAULT_TERMS_INDEX_DIVISOR, NewIOContext(Random()));
            Assert.IsTrue(reader != null);
            Document doc = reader.Document(0);
            Assert.IsTrue(doc != null);

            //System.out.println("Document: " + doc);
            IndexableField[] fields = doc.GetFields("textField2");
            Assert.IsTrue(fields != null && fields.Length == 1);
            Assert.IsTrue(fields[0].GetStringValue().Equals(DocHelper.FIELD_2_TEXT));
            Assert.IsTrue(fields[0].FieldType.StoreTermVectors);

            fields = doc.GetFields("textField1");
            Assert.IsTrue(fields != null && fields.Length == 1);
            Assert.IsTrue(fields[0].GetStringValue().Equals(DocHelper.FIELD_1_TEXT));
            Assert.IsFalse(fields[0].FieldType.StoreTermVectors);

            fields = doc.GetFields("keyField");
            Assert.IsTrue(fields != null && fields.Length == 1);
            Assert.IsTrue(fields[0].GetStringValue().Equals(DocHelper.KEYWORD_TEXT));

            fields = doc.GetFields(DocHelper.NO_NORMS_KEY);
            Assert.IsTrue(fields != null && fields.Length == 1);
            Assert.IsTrue(fields[0].GetStringValue().Equals(DocHelper.NO_NORMS_TEXT));

            fields = doc.GetFields(DocHelper.TEXT_FIELD_3_KEY);
            Assert.IsTrue(fields != null && fields.Length == 1);
            Assert.IsTrue(fields[0].GetStringValue().Equals(DocHelper.FIELD_3_TEXT));

            // test that the norms are not present in the segment if
            // omitNorms is true
            foreach (FieldInfo fi in reader.FieldInfos)
            {
                if (fi.Indexed)
                {
                    Assert.IsTrue(fi.OmitsNorms() == (reader.GetNormValues(fi.Name) == null));
                }
            }
            reader.Dispose();
        }

        [Test]
        public virtual void TestPositionIncrementGap()
        {
            Analyzer analyzer = new AnalyzerAnonymousInnerClassHelper(this);

            IndexWriter writer = new IndexWriter(Dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer));

            Document doc = new Document();
            doc.Add(NewTextField("repeated", "repeated one", Field.Store.YES));
            doc.Add(NewTextField("repeated", "repeated two", Field.Store.YES));

            writer.AddDocument(doc);
            writer.Commit();
            SegmentCommitInfo info = writer.NewestSegment();
            writer.Dispose();
            SegmentReader reader = new SegmentReader(info, DirectoryReader.DEFAULT_TERMS_INDEX_DIVISOR, NewIOContext(Random()));

            DocsAndPositionsEnum termPositions = MultiFields.GetTermPositionsEnum(reader, MultiFields.GetLiveDocs(reader), "repeated", new BytesRef("repeated"));
            Assert.IsTrue(termPositions.NextDoc() != DocIdSetIterator.NO_MORE_DOCS);
            int freq = termPositions.Freq;
            Assert.AreEqual(2, freq);
            Assert.AreEqual(0, termPositions.NextPosition());
            Assert.AreEqual(502, termPositions.NextPosition());
            reader.Dispose();
        }

        private class AnalyzerAnonymousInnerClassHelper : Analyzer
        {
            private readonly TestDocumentWriter OuterInstance;

            public AnalyzerAnonymousInnerClassHelper(TestDocumentWriter outerInstance)
            {
                this.OuterInstance = outerInstance;
            }

            protected internal override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
            {
                return new TokenStreamComponents(new MockTokenizer(reader, MockTokenizer.WHITESPACE, false));
            }

            public override int GetPositionIncrementGap(string fieldName)
            {
                return 500;
            }
        }

        [Test]
        public virtual void TestTokenReuse()
        {
            Analyzer analyzer = new AnalyzerAnonymousInnerClassHelper2(this);

            IndexWriter writer = new IndexWriter(Dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer));

            Document doc = new Document();
            doc.Add(NewTextField("f1", "a 5 a a", Field.Store.YES));

            writer.AddDocument(doc);
            writer.Commit();
            SegmentCommitInfo info = writer.NewestSegment();
            writer.Dispose();
            SegmentReader reader = new SegmentReader(info, DirectoryReader.DEFAULT_TERMS_INDEX_DIVISOR, NewIOContext(Random()));

            DocsAndPositionsEnum termPositions = MultiFields.GetTermPositionsEnum(reader, reader.LiveDocs, "f1", new BytesRef("a"));
            Assert.IsTrue(termPositions.NextDoc() != DocIdSetIterator.NO_MORE_DOCS);
            int freq = termPositions.Freq;
            Assert.AreEqual(3, freq);
            Assert.AreEqual(0, termPositions.NextPosition());
            Assert.IsNotNull(termPositions.Payload);
            Assert.AreEqual(6, termPositions.NextPosition());
            Assert.IsNull(termPositions.Payload);
            Assert.AreEqual(7, termPositions.NextPosition());
            Assert.IsNull(termPositions.Payload);
            reader.Dispose();
        }

        private class AnalyzerAnonymousInnerClassHelper2 : Analyzer
        {
            private readonly TestDocumentWriter OuterInstance;

            public AnalyzerAnonymousInnerClassHelper2(TestDocumentWriter outerInstance)
            {
                this.OuterInstance = outerInstance;
            }

            protected internal override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
            {
                Tokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
                return new TokenStreamComponents(tokenizer, new TokenFilterAnonymousInnerClassHelper(this, tokenizer));
            }

            private class TokenFilterAnonymousInnerClassHelper : TokenFilter
            {
                private readonly AnalyzerAnonymousInnerClassHelper2 OuterInstance;

                public TokenFilterAnonymousInnerClassHelper(AnalyzerAnonymousInnerClassHelper2 outerInstance, Tokenizer tokenizer)
                    : base(tokenizer)
                {
                    this.OuterInstance = outerInstance;
                    first = true;
                    termAtt = AddAttribute<ICharTermAttribute>();
                    payloadAtt = AddAttribute<IPayloadAttribute>();
                    posIncrAtt = AddAttribute<IPositionIncrementAttribute>();
                }

                internal bool first;
                internal AttributeSource.State state;

                public sealed override bool IncrementToken()
                {
                    if (state != null)
                    {
                        RestoreState(state);
                        payloadAtt.Payload = null;
                        posIncrAtt.PositionIncrement = 0;
                        termAtt.SetEmpty().Append("b");
                        state = null;
                        return true;
                    }

                    bool hasNext = input.IncrementToken();
                    if (!hasNext)
                    {
                        return false;
                    }
                    if (char.IsDigit(termAtt.Buffer()[0]))
                    {
                        posIncrAtt.PositionIncrement = termAtt.Buffer()[0] - '0';
                    }
                    if (first)
                    {
                        // set payload on first position only
                        payloadAtt.Payload = new BytesRef(new byte[] { 100 });
                        first = false;
                    }

                    // index a "synonym" for every token
                    state = CaptureState();
                    return true;
                }

                public sealed override void Reset()
                {
                    base.Reset();
                    first = true;
                    state = null;
                }

                internal readonly ICharTermAttribute termAtt;
                internal readonly IPayloadAttribute payloadAtt;
                internal readonly IPositionIncrementAttribute posIncrAtt;
            }
        }

        [Test]
        public virtual void TestPreAnalyzedField()
        {
            IndexWriter writer = new IndexWriter(Dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())));
            Document doc = new Document();

            doc.Add(new TextField("preanalyzed", new TokenStreamAnonymousInnerClassHelper(this)));

            writer.AddDocument(doc);
            writer.Commit();
            SegmentCommitInfo info = writer.NewestSegment();
            writer.Dispose();
            SegmentReader reader = new SegmentReader(info, DirectoryReader.DEFAULT_TERMS_INDEX_DIVISOR, NewIOContext(Random()));

            DocsAndPositionsEnum termPositions = reader.TermPositionsEnum(new Term("preanalyzed", "term1"));
            Assert.IsTrue(termPositions.NextDoc() != DocIdSetIterator.NO_MORE_DOCS);
            Assert.AreEqual(1, termPositions.Freq);
            Assert.AreEqual(0, termPositions.NextPosition());

            termPositions = reader.TermPositionsEnum(new Term("preanalyzed", "term2"));
            Assert.IsTrue(termPositions.NextDoc() != DocIdSetIterator.NO_MORE_DOCS);
            Assert.AreEqual(2, termPositions.Freq);
            Assert.AreEqual(1, termPositions.NextPosition());
            Assert.AreEqual(3, termPositions.NextPosition());

            termPositions = reader.TermPositionsEnum(new Term("preanalyzed", "term3"));
            Assert.IsTrue(termPositions.NextDoc() != DocIdSetIterator.NO_MORE_DOCS);
            Assert.AreEqual(1, termPositions.Freq);
            Assert.AreEqual(2, termPositions.NextPosition());
            reader.Dispose();
        }

        private class TokenStreamAnonymousInnerClassHelper : TokenStream
        {
            private readonly TestDocumentWriter OuterInstance;

            public TokenStreamAnonymousInnerClassHelper(TestDocumentWriter outerInstance) 
            {
                this.OuterInstance = outerInstance;
                tokens = new string[] { "term1", "term2", "term3", "term2" };
                index = 0;
                termAtt = AddAttribute<ICharTermAttribute>();
            }

            private string[] tokens;
            private int index;

            private ICharTermAttribute termAtt;

            public sealed override bool IncrementToken()
            {
                if (index == tokens.Length)
                {
                    return false;
                }
                else
                {
                    ClearAttributes();
                    termAtt.SetEmpty().Append(tokens[index++]);
                    return true;
                }
            }
        }

        /// <summary>
        /// Test adding two fields with the same name, but
        /// with different term vector setting (LUCENE-766).
        /// </summary>
        [Test]
        public virtual void TestMixedTermVectorSettingsSameField()
        {
            Document doc = new Document();
            // f1 first without tv then with tv
            doc.Add(NewStringField("f1", "v1", Field.Store.YES));
            FieldType customType2 = new FieldType(StringField.TYPE_STORED);
            customType2.StoreTermVectors = true;
            customType2.StoreTermVectorOffsets = true;
            customType2.StoreTermVectorPositions = true;
            doc.Add(NewField("f1", "v2", customType2));
            // f2 first with tv then without tv
            doc.Add(NewField("f2", "v1", customType2));
            doc.Add(NewStringField("f2", "v2", Field.Store.YES));

            IndexWriter writer = new IndexWriter(Dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())));
            writer.AddDocument(doc);
            writer.Dispose();

            TestUtil.CheckIndex(Dir);

            IndexReader reader = DirectoryReader.Open(Dir);
            // f1
            Terms tfv1 = reader.GetTermVectors(0).Terms("f1");
            Assert.IsNotNull(tfv1);
            Assert.AreEqual(2, tfv1.Size(), "the 'with_tv' setting should rule!");
            // f2
            Terms tfv2 = reader.GetTermVectors(0).Terms("f2");
            Assert.IsNotNull(tfv2);
            Assert.AreEqual(2, tfv2.Size(), "the 'with_tv' setting should rule!");
            reader.Dispose();
        }

        /// <summary>
        /// Test adding two fields with the same name, one indexed
        /// the other stored only. The omitNorms and omitTermFreqAndPositions setting
        /// of the stored field should not affect the indexed one (LUCENE-1590)
        /// </summary>
        [Test]
        public virtual void TestLUCENE_1590()
        {
            Document doc = new Document();
            // f1 has no norms
            FieldType customType = new FieldType(TextField.TYPE_NOT_STORED);
            customType.OmitNorms = true;
            FieldType customType2 = new FieldType();
            customType2.IsStored = true;
            doc.Add(NewField("f1", "v1", customType));
            doc.Add(NewField("f1", "v2", customType2));
            // f2 has no TF
            FieldType customType3 = new FieldType(TextField.TYPE_NOT_STORED);
            customType3.IndexOptions = IndexOptions.DOCS_ONLY;
            Field f = NewField("f2", "v1", customType3);
            doc.Add(f);
            doc.Add(NewField("f2", "v2", customType2));

            IndexWriter writer = new IndexWriter(Dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())));
            writer.AddDocument(doc);
            writer.ForceMerge(1); // be sure to have a single segment
            writer.Dispose();

            TestUtil.CheckIndex(Dir);

            SegmentReader reader = GetOnlySegmentReader(DirectoryReader.Open(Dir));
            FieldInfos fi = reader.FieldInfos;
            // f1
            Assert.IsFalse(fi.FieldInfo("f1").HasNorms(), "f1 should have no norms");
            Assert.AreEqual(IndexOptions.DOCS_AND_FREQS_AND_POSITIONS, fi.FieldInfo("f1").IndexOptions, "omitTermFreqAndPositions field bit should not be set for f1");
            // f2
            Assert.IsTrue(fi.FieldInfo("f2").HasNorms(), "f2 should have norms");
            Assert.AreEqual(IndexOptions.DOCS_ONLY, fi.FieldInfo("f2").IndexOptions, "omitTermFreqAndPositions field bit should be set for f2");
            reader.Dispose();
        }
    }
}