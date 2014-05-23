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
	using CharTermAttribute = Lucene.Net.Analysis.Tokenattributes.CharTermAttribute;
	using PayloadAttribute = Lucene.Net.Analysis.Tokenattributes.PayloadAttribute;
	using PositionIncrementAttribute = Lucene.Net.Analysis.Tokenattributes.PositionIncrementAttribute;
	using Document = Lucene.Net.Document.Document;
	using Field = Lucene.Net.Document.Field;
	using FieldType = Lucene.Net.Document.FieldType;
	using StringField = Lucene.Net.Document.StringField;
	using TextField = Lucene.Net.Document.TextField;
	using IndexOptions = Lucene.Net.Index.FieldInfo.IndexOptions_e;
	using DocIdSetIterator = Lucene.Net.Search.DocIdSetIterator;
	using Directory = Lucene.Net.Store.Directory;
	using AttributeSource = Lucene.Net.Util.AttributeSource;
	using BytesRef = Lucene.Net.Util.BytesRef;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
	using TestUtil = Lucene.Net.Util.TestUtil;
	using TestUtil = Lucene.Net.Util.TestUtil;

	public class TestDocumentWriter : LuceneTestCase
	{
	  private Directory Dir;

	  public override void SetUp()
	  {
		base.setUp();
		Dir = newDirectory();
	  }

	  public override void TearDown()
	  {
		Dir.close();
		base.tearDown();
	  }

	  public virtual void Test()
	  {
		Assert.IsTrue(Dir != null);
	  }

	  public virtual void TestAddDocument()
	  {
		Document testDoc = new Document();
		DocHelper.setupDoc(testDoc);
		IndexWriter writer = new IndexWriter(Dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())));
		writer.addDocument(testDoc);
		writer.commit();
		SegmentCommitInfo info = writer.newestSegment();
		writer.close();
		//After adding the document, we should be able to read it back in
		SegmentReader reader = new SegmentReader(info, DirectoryReader.DEFAULT_TERMS_INDEX_DIVISOR, newIOContext(random()));
		Assert.IsTrue(reader != null);
		Document doc = reader.document(0);
		Assert.IsTrue(doc != null);

		//System.out.println("Document: " + doc);
		IndexableField[] fields = doc.getFields("textField2");
		Assert.IsTrue(fields != null && fields.Length == 1);
		Assert.IsTrue(fields[0].stringValue().Equals(DocHelper.FIELD_2_TEXT));
		Assert.IsTrue(fields[0].fieldType().storeTermVectors());

		fields = doc.getFields("textField1");
		Assert.IsTrue(fields != null && fields.Length == 1);
		Assert.IsTrue(fields[0].stringValue().Equals(DocHelper.FIELD_1_TEXT));
		Assert.IsFalse(fields[0].fieldType().storeTermVectors());

		fields = doc.getFields("keyField");
		Assert.IsTrue(fields != null && fields.Length == 1);
		Assert.IsTrue(fields[0].stringValue().Equals(DocHelper.KEYWORD_TEXT));

		fields = doc.getFields(DocHelper.NO_NORMS_KEY);
		Assert.IsTrue(fields != null && fields.Length == 1);
		Assert.IsTrue(fields[0].stringValue().Equals(DocHelper.NO_NORMS_TEXT));

		fields = doc.getFields(DocHelper.TEXT_FIELD_3_KEY);
		Assert.IsTrue(fields != null && fields.Length == 1);
		Assert.IsTrue(fields[0].stringValue().Equals(DocHelper.FIELD_3_TEXT));

		// test that the norms are not present in the segment if
		// omitNorms is true
		foreach (FieldInfo fi in reader.FieldInfos)
		{
		  if (fi.Indexed)
		  {
			Assert.IsTrue(fi.omitsNorms() == (reader.getNormValues(fi.name) == null));
		  }
		}
		reader.close();
	  }

	  public virtual void TestPositionIncrementGap()
	  {
		Analyzer analyzer = new AnalyzerAnonymousInnerClassHelper(this);

		IndexWriter writer = new IndexWriter(Dir, newIndexWriterConfig(TEST_VERSION_CURRENT, analyzer));

		Document doc = new Document();
		doc.add(newTextField("repeated", "repeated one", Field.Store.YES));
		doc.add(newTextField("repeated", "repeated two", Field.Store.YES));

		writer.addDocument(doc);
		writer.commit();
		SegmentCommitInfo info = writer.newestSegment();
		writer.close();
		SegmentReader reader = new SegmentReader(info, DirectoryReader.DEFAULT_TERMS_INDEX_DIVISOR, newIOContext(random()));

		DocsAndPositionsEnum termPositions = MultiFields.getTermPositionsEnum(reader, MultiFields.getLiveDocs(reader), "repeated", new BytesRef("repeated"));
		Assert.IsTrue(termPositions.nextDoc() != DocIdSetIterator.NO_MORE_DOCS);
		int freq = termPositions.freq();
		Assert.AreEqual(2, freq);
		Assert.AreEqual(0, termPositions.nextPosition());
		Assert.AreEqual(502, termPositions.nextPosition());
		reader.close();
	  }

	  private class AnalyzerAnonymousInnerClassHelper : Analyzer
	  {
		  private readonly TestDocumentWriter OuterInstance;

		  public AnalyzerAnonymousInnerClassHelper(TestDocumentWriter outerInstance)
		  {
			  this.OuterInstance = outerInstance;
		  }

		  public override TokenStreamComponents CreateComponents(string fieldName, Reader reader)
		  {
			return new TokenStreamComponents(new MockTokenizer(reader, MockTokenizer.WHITESPACE, false));
		  }

		  public override int GetPositionIncrementGap(string fieldName)
		  {
			return 500;
		  }
	  }

	  public virtual void TestTokenReuse()
	  {
		Analyzer analyzer = new AnalyzerAnonymousInnerClassHelper2(this);

		IndexWriter writer = new IndexWriter(Dir, newIndexWriterConfig(TEST_VERSION_CURRENT, analyzer));

		Document doc = new Document();
		doc.add(newTextField("f1", "a 5 a a", Field.Store.YES));

		writer.addDocument(doc);
		writer.commit();
		SegmentCommitInfo info = writer.newestSegment();
		writer.close();
		SegmentReader reader = new SegmentReader(info, DirectoryReader.DEFAULT_TERMS_INDEX_DIVISOR, newIOContext(random()));

		DocsAndPositionsEnum termPositions = MultiFields.getTermPositionsEnum(reader, reader.LiveDocs, "f1", new BytesRef("a"));
		Assert.IsTrue(termPositions.nextDoc() != DocIdSetIterator.NO_MORE_DOCS);
		int freq = termPositions.freq();
		Assert.AreEqual(3, freq);
		Assert.AreEqual(0, termPositions.nextPosition());
		Assert.IsNotNull(termPositions.Payload);
		Assert.AreEqual(6, termPositions.nextPosition());
		assertNull(termPositions.Payload);
		Assert.AreEqual(7, termPositions.nextPosition());
		assertNull(termPositions.Payload);
		reader.close();
	  }

	  private class AnalyzerAnonymousInnerClassHelper2 : Analyzer
	  {
		  private readonly TestDocumentWriter OuterInstance;

		  public AnalyzerAnonymousInnerClassHelper2(TestDocumentWriter outerInstance)
		  {
			  this.OuterInstance = outerInstance;
		  }

		  public override TokenStreamComponents CreateComponents(string fieldName, Reader reader)
		  {
			Tokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
			return new TokenStreamComponents(tokenizer, new TokenFilterAnonymousInnerClassHelper(this, tokenizer));
		  }

		  private class TokenFilterAnonymousInnerClassHelper : TokenFilter
		  {
			  private readonly AnalyzerAnonymousInnerClassHelper2 OuterInstance;

			  public TokenFilterAnonymousInnerClassHelper(AnalyzerAnonymousInnerClassHelper2 outerInstance, Tokenizer tokenizer) : base(tokenizer)
			  {
				  this.outerInstance = outerInstance;
				  first = true;
				  termAtt = addAttribute(typeof(CharTermAttribute));
				  payloadAtt = addAttribute(typeof(PayloadAttribute));
				  posIncrAtt = addAttribute(typeof(PositionIncrementAttribute));
			  }

			  internal bool first;
			  internal AttributeSource.State state;

			  public override bool IncrementToken()
			  {
				if (state != null)
				{
				  restoreState(state);
				  payloadAtt.Payload = null;
				  posIncrAtt.PositionIncrement = 0;
				  termAtt.SetEmpty().append("b");
				  state = null;
				  return true;
				}

				bool hasNext = input.IncrementToken();
				if (!hasNext)
				{
					return false;
				}
				if (char.IsDigit(termAtt.buffer()[0]))
				{
				  posIncrAtt.PositionIncrement = termAtt.buffer()[0] - '0';
				}
				if (first)
				{
				  // set payload on first position only
				  payloadAtt.Payload = new BytesRef(new sbyte[]{100});
				  first = false;
				}

				// index a "synonym" for every token
				state = captureState();
				return true;

			  }

			  public override void Reset()
			  {
				base.reset();
				first = true;
				state = null;
			  }

			  internal readonly CharTermAttribute termAtt;
			  internal readonly PayloadAttribute payloadAtt;
			  internal readonly PositionIncrementAttribute posIncrAtt;
		  }
	  }


	  public virtual void TestPreAnalyzedField()
	  {
		IndexWriter writer = new IndexWriter(Dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())));
		Document doc = new Document();

		doc.add(new TextField("preanalyzed", new TokenStreamAnonymousInnerClassHelper(this)));

		writer.addDocument(doc);
		writer.commit();
		SegmentCommitInfo info = writer.newestSegment();
		writer.close();
		SegmentReader reader = new SegmentReader(info, DirectoryReader.DEFAULT_TERMS_INDEX_DIVISOR, newIOContext(random()));

		DocsAndPositionsEnum termPositions = reader.termPositionsEnum(new Term("preanalyzed", "term1"));
		Assert.IsTrue(termPositions.nextDoc() != DocIdSetIterator.NO_MORE_DOCS);
		Assert.AreEqual(1, termPositions.freq());
		Assert.AreEqual(0, termPositions.nextPosition());

		termPositions = reader.termPositionsEnum(new Term("preanalyzed", "term2"));
		Assert.IsTrue(termPositions.nextDoc() != DocIdSetIterator.NO_MORE_DOCS);
		Assert.AreEqual(2, termPositions.freq());
		Assert.AreEqual(1, termPositions.nextPosition());
		Assert.AreEqual(3, termPositions.nextPosition());

		termPositions = reader.termPositionsEnum(new Term("preanalyzed", "term3"));
		Assert.IsTrue(termPositions.nextDoc() != DocIdSetIterator.NO_MORE_DOCS);
		Assert.AreEqual(1, termPositions.freq());
		Assert.AreEqual(2, termPositions.nextPosition());
		reader.close();
	  }

	  private class TokenStreamAnonymousInnerClassHelper : TokenStream
	  {
		  private readonly TestDocumentWriter OuterInstance;

		  public TokenStreamAnonymousInnerClassHelper(TestDocumentWriter outerInstance)
		  {
			  this.OuterInstance = outerInstance;
			  tokens = new string[] {"term1", "term2", "term3", "term2"};
			  index = 0;
			  termAtt = addAttribute(typeof(CharTermAttribute));
		  }

		  private string[] tokens;
		  private int index;

		  private CharTermAttribute termAtt;

		  public override bool IncrementToken()
		  {
			if (index == tokens.length)
			{
			  return false;
			}
			else
			{
			  ClearAttributes();
			  termAtt.SetEmpty().append(tokens[index++]);
			  return true;
			}
		  }
	  }

	  /// <summary>
	  /// Test adding two fields with the same name, but 
	  /// with different term vector setting (LUCENE-766).
	  /// </summary>
	  public virtual void TestMixedTermVectorSettingsSameField()
	  {
		Document doc = new Document();
		// f1 first without tv then with tv
		doc.add(newStringField("f1", "v1", Field.Store.YES));
		FieldType customType2 = new FieldType(StringField.TYPE_STORED);
		customType2.StoreTermVectors = true;
		customType2.StoreTermVectorOffsets = true;
		customType2.StoreTermVectorPositions = true;
		doc.add(newField("f1", "v2", customType2));
		// f2 first with tv then without tv
		doc.add(newField("f2", "v1", customType2));
		doc.add(newStringField("f2", "v2", Field.Store.YES));

		IndexWriter writer = new IndexWriter(Dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())));
		writer.addDocument(doc);
		writer.close();

		TestUtil.checkIndex(Dir);

		IndexReader reader = DirectoryReader.open(Dir);
		// f1
		Terms tfv1 = reader.getTermVectors(0).terms("f1");
		Assert.IsNotNull(tfv1);
		Assert.AreEqual("the 'with_tv' setting should rule!",2,tfv1.size());
		// f2
		Terms tfv2 = reader.getTermVectors(0).terms("f2");
		Assert.IsNotNull(tfv2);
		Assert.AreEqual("the 'with_tv' setting should rule!",2,tfv2.size());
		reader.close();
	  }

	  /// <summary>
	  /// Test adding two fields with the same name, one indexed
	  /// the other stored only. The omitNorms and omitTermFreqAndPositions setting
	  /// of the stored field should not affect the indexed one (LUCENE-1590)
	  /// </summary>
	  public virtual void TestLUCENE_1590()
	  {
		Document doc = new Document();
		// f1 has no norms
		FieldType customType = new FieldType(TextField.TYPE_NOT_STORED);
		customType.OmitNorms = true;
		FieldType customType2 = new FieldType();
		customType2.Stored = true;
		doc.add(newField("f1", "v1", customType));
		doc.add(newField("f1", "v2", customType2));
		// f2 has no TF
		FieldType customType3 = new FieldType(TextField.TYPE_NOT_STORED);
		customType3.IndexOptions = IndexOptions.DOCS_ONLY;
		Field f = newField("f2", "v1", customType3);
		doc.add(f);
		doc.add(newField("f2", "v2", customType2));

		IndexWriter writer = new IndexWriter(Dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())));
		writer.addDocument(doc);
		writer.forceMerge(1); // be sure to have a single segment
		writer.close();

		TestUtil.checkIndex(Dir);

		SegmentReader reader = getOnlySegmentReader(DirectoryReader.open(Dir));
		FieldInfos fi = reader.FieldInfos;
		// f1
		Assert.IsFalse("f1 should have no norms", fi.fieldInfo("f1").hasNorms());
		Assert.AreEqual("omitTermFreqAndPositions field bit should not be set for f1", IndexOptions.DOCS_AND_FREQS_AND_POSITIONS, fi.fieldInfo("f1").IndexOptions);
		// f2
		Assert.IsTrue("f2 should have norms", fi.fieldInfo("f2").hasNorms());
		Assert.AreEqual("omitTermFreqAndPositions field bit should be set for f2", IndexOptions.DOCS_ONLY, fi.fieldInfo("f2").IndexOptions);
		reader.close();
	  }
	}

}