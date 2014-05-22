using System;

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
	using OffsetAttribute = Lucene.Net.Analysis.Tokenattributes.OffsetAttribute;
	using PositionIncrementAttribute = Lucene.Net.Analysis.Tokenattributes.PositionIncrementAttribute;
	using Codec = Lucene.Net.Codecs.Codec;
	using TermVectorsReader = Lucene.Net.Codecs.TermVectorsReader;
	using Document = Lucene.Net.Document.Document;
	using Field = Lucene.Net.Document.Field;
	using FieldType = Lucene.Net.Document.FieldType;
	using TextField = Lucene.Net.Document.TextField;
	using DocIdSetIterator = Lucene.Net.Search.DocIdSetIterator;
	using Directory = Lucene.Net.Store.Directory;
	using BytesRef = Lucene.Net.Util.BytesRef;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
	using TestUtil = Lucene.Net.Util.TestUtil;

	public class TestTermVectorsReader : LuceneTestCase
	{
		private bool InstanceFieldsInitialized = false;

		public TestTermVectorsReader()
		{
			if (!InstanceFieldsInitialized)
			{
				InitializeInstanceFields();
				InstanceFieldsInitialized = true;
			}
		}

		private void InitializeInstanceFields()
		{
			Positions = new int[TestTerms.Length][];
			Tokens = new TestToken[TestTerms.Length * TERM_FREQ];
		}

	  //Must be lexicographically sorted, will do in setup, versus trying to maintain here
	  private string[] TestFields = new string[] {"f1", "f2", "f3", "f4"};
	  private bool[] TestFieldsStorePos = new bool[] {true, false, true, false};
	  private bool[] TestFieldsStoreOff = new bool[] {true, false, false, true};
	  private string[] TestTerms = new string[] {"this", "is", "a", "test"};
	  private int[][] Positions;
	  private Directory Dir;
	  private SegmentCommitInfo Seg;
	  private FieldInfos FieldInfos = new FieldInfos(new FieldInfo[0]);
	  private static int TERM_FREQ = 3;

	  private class TestToken : IComparable<TestToken>
	  {
		  private readonly TestTermVectorsReader OuterInstance;

		  public TestToken(TestTermVectorsReader outerInstance)
		  {
			  this.outerInstance = outerInstance;
		  }

		internal string Text;
		internal int Pos;
		internal int StartOffset;
		internal int EndOffset;
		public virtual int CompareTo(TestToken other)
		{
		  return Pos - other.pos;
		}
	  }

	  internal TestToken[] Tokens;

	  public override void SetUp()
	  {
		base.setUp();
		/*
		for (int i = 0; i < testFields.length; i++) {
		  fieldInfos.add(testFields[i], true, true, testFieldsStorePos[i], testFieldsStoreOff[i]);
		}
		*/

		Arrays.sort(TestTerms);
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
			token.text = TestTerms[i];
			token.pos = Positions[i][j];
			token.startOffset = j * 10;
			token.endOffset = j * 10 + TestTerms[i].Length;
		  }
		}
		Arrays.sort(Tokens);

		Dir = newDirectory();
		IndexWriter writer = new IndexWriter(Dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MyAnalyzer(this)).setMaxBufferedDocs(-1).setMergePolicy(newLogMergePolicy(false, 10)).setUseCompoundFile(false));

		Document doc = new Document();
		for (int i = 0;i < TestFields.Length;i++)
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
		  doc.add(new Field(TestFields[i], "", customType));
		}

		//Create 5 documents for testing, they all have the same
		//terms
		for (int j = 0;j < 5;j++)
		{
		  writer.addDocument(doc);
		}
		writer.commit();
		Seg = writer.newestSegment();
		writer.close();

		FieldInfos = SegmentReader.readFieldInfos(Seg);
	  }

	  public override void TearDown()
	  {
		Dir.close();
		base.tearDown();
	  }

	  private class MyTokenizer : Tokenizer
	  {
		  private readonly TestTermVectorsReader OuterInstance;

		internal int TokenUpto;

		internal readonly CharTermAttribute TermAtt;
		internal readonly PositionIncrementAttribute PosIncrAtt;
		internal readonly OffsetAttribute OffsetAtt;

		public MyTokenizer(TestTermVectorsReader outerInstance, Reader reader) : base(reader)
		{
			this.OuterInstance = outerInstance;
		  TermAtt = addAttribute(typeof(CharTermAttribute));
		  PosIncrAtt = addAttribute(typeof(PositionIncrementAttribute));
		  OffsetAtt = addAttribute(typeof(OffsetAttribute));
		}

		public override bool IncrementToken()
		{
		  if (TokenUpto >= outerInstance.Tokens.Length)
		  {
			return false;
		  }
		  else
		  {
			TestToken testToken = outerInstance.Tokens[TokenUpto++];
			ClearAttributes();
			TermAtt.append(testToken.text);
			OffsetAtt.SetOffset(testToken.startOffset, testToken.endOffset);
			if (TokenUpto > 1)
			{
			  PosIncrAtt.PositionIncrement = testToken.pos - outerInstance.Tokens[TokenUpto - 2].pos;
			}
			else
			{
			  PosIncrAtt.PositionIncrement = testToken.pos + 1;
			}
			return true;
		  }
		}

		public override void Reset()
		{
		  base.reset();
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

		public override TokenStreamComponents CreateComponents(string fieldName, Reader reader)
		{
		  return new TokenStreamComponents(new MyTokenizer(OuterInstance, reader));
		}
	  }

	  public virtual void Test()
	  {
		//Check to see the files were created properly in setup
		DirectoryReader reader = DirectoryReader.open(Dir);
		foreach (AtomicReaderContext ctx in reader.leaves())
		{
		  SegmentReader sr = (SegmentReader) ctx.reader();
		  Assert.IsTrue(sr.FieldInfos.hasVectors());
		}
		reader.close();
	  }

	  public virtual void TestReader()
	  {
		TermVectorsReader reader = Codec.Default.termVectorsFormat().vectorsReader(Dir, Seg.info, FieldInfos, newIOContext(random()));
		for (int j = 0; j < 5; j++)
		{
		  Terms vector = reader.get(j).terms(TestFields[0]);
		  Assert.IsNotNull(vector);
		  Assert.AreEqual(TestTerms.Length, vector.size());
		  TermsEnum termsEnum = vector.iterator(null);
		  for (int i = 0; i < TestTerms.Length; i++)
		  {
			BytesRef text = termsEnum.next();
			Assert.IsNotNull(text);
			string term = text.utf8ToString();
			//System.out.println("Term: " + term);
			Assert.AreEqual(TestTerms[i], term);
		  }
		  assertNull(termsEnum.next());
		}
		reader.close();
	  }

	  public virtual void TestDocsEnum()
	  {
		TermVectorsReader reader = Codec.Default.termVectorsFormat().vectorsReader(Dir, Seg.info, FieldInfos, newIOContext(random()));
		for (int j = 0; j < 5; j++)
		{
		  Terms vector = reader.get(j).terms(TestFields[0]);
		  Assert.IsNotNull(vector);
		  Assert.AreEqual(TestTerms.Length, vector.size());
		  TermsEnum termsEnum = vector.iterator(null);
		  DocsEnum docsEnum = null;
		  for (int i = 0; i < TestTerms.Length; i++)
		  {
			BytesRef text = termsEnum.next();
			Assert.IsNotNull(text);
			string term = text.utf8ToString();
			//System.out.println("Term: " + term);
			Assert.AreEqual(TestTerms[i], term);

			docsEnum = TestUtil.docs(random(), termsEnum, null, docsEnum, DocsEnum.FLAG_NONE);
			Assert.IsNotNull(docsEnum);
			int doc = docsEnum.docID();
			Assert.AreEqual(-1, doc);
			Assert.IsTrue(docsEnum.nextDoc() != DocIdSetIterator.NO_MORE_DOCS);
			Assert.AreEqual(DocIdSetIterator.NO_MORE_DOCS, docsEnum.nextDoc());
		  }
		  assertNull(termsEnum.next());
		}
		reader.close();
	  }

	  public virtual void TestPositionReader()
	  {
		TermVectorsReader reader = Codec.Default.termVectorsFormat().vectorsReader(Dir, Seg.info, FieldInfos, newIOContext(random()));
		BytesRef[] terms;
		Terms vector = reader.get(0).terms(TestFields[0]);
		Assert.IsNotNull(vector);
		Assert.AreEqual(TestTerms.Length, vector.size());
		TermsEnum termsEnum = vector.iterator(null);
		DocsAndPositionsEnum dpEnum = null;
		for (int i = 0; i < TestTerms.Length; i++)
		{
		  BytesRef text = termsEnum.next();
		  Assert.IsNotNull(text);
		  string term = text.utf8ToString();
		  //System.out.println("Term: " + term);
		  Assert.AreEqual(TestTerms[i], term);

		  dpEnum = termsEnum.docsAndPositions(null, dpEnum);
		  Assert.IsNotNull(dpEnum);
		  int doc = dpEnum.docID();
		  Assert.AreEqual(-1, doc);
		  Assert.IsTrue(dpEnum.nextDoc() != DocIdSetIterator.NO_MORE_DOCS);
		  Assert.AreEqual(dpEnum.freq(), Positions[i].Length);
		  for (int j = 0; j < Positions[i].Length; j++)
		  {
			Assert.AreEqual(Positions[i][j], dpEnum.nextPosition());
		  }
		  Assert.AreEqual(DocIdSetIterator.NO_MORE_DOCS, dpEnum.nextDoc());

		  dpEnum = termsEnum.docsAndPositions(null, dpEnum);
		  doc = dpEnum.docID();
		  Assert.AreEqual(-1, doc);
		  Assert.IsTrue(dpEnum.nextDoc() != DocIdSetIterator.NO_MORE_DOCS);
		  Assert.IsNotNull(dpEnum);
		  Assert.AreEqual(dpEnum.freq(), Positions[i].Length);
		  for (int j = 0; j < Positions[i].Length; j++)
		  {
			Assert.AreEqual(Positions[i][j], dpEnum.nextPosition());
			Assert.AreEqual(j * 10, dpEnum.StartOffset());
			Assert.AreEqual(j * 10 + TestTerms[i].Length, dpEnum.EndOffset());
		  }
		  Assert.AreEqual(DocIdSetIterator.NO_MORE_DOCS, dpEnum.nextDoc());
		}

		Terms freqVector = reader.get(0).terms(TestFields[1]); //no pos, no offset
		Assert.IsNotNull(freqVector);
		Assert.AreEqual(TestTerms.Length, freqVector.size());
		termsEnum = freqVector.iterator(null);
		Assert.IsNotNull(termsEnum);
		for (int i = 0; i < TestTerms.Length; i++)
		{
		  BytesRef text = termsEnum.next();
		  Assert.IsNotNull(text);
		  string term = text.utf8ToString();
		  //System.out.println("Term: " + term);
		  Assert.AreEqual(TestTerms[i], term);
		  Assert.IsNotNull(termsEnum.docs(null, null));
		  assertNull(termsEnum.docsAndPositions(null, null)); // no pos
		}
		reader.close();
	  }

	  public virtual void TestOffsetReader()
	  {
		TermVectorsReader reader = Codec.Default.termVectorsFormat().vectorsReader(Dir, Seg.info, FieldInfos, newIOContext(random()));
		Terms vector = reader.get(0).terms(TestFields[0]);
		Assert.IsNotNull(vector);
		TermsEnum termsEnum = vector.iterator(null);
		Assert.IsNotNull(termsEnum);
		Assert.AreEqual(TestTerms.Length, vector.size());
		DocsAndPositionsEnum dpEnum = null;
		for (int i = 0; i < TestTerms.Length; i++)
		{
		  BytesRef text = termsEnum.next();
		  Assert.IsNotNull(text);
		  string term = text.utf8ToString();
		  Assert.AreEqual(TestTerms[i], term);

		  dpEnum = termsEnum.docsAndPositions(null, dpEnum);
		  Assert.IsNotNull(dpEnum);
		  Assert.IsTrue(dpEnum.nextDoc() != DocIdSetIterator.NO_MORE_DOCS);
		  Assert.AreEqual(dpEnum.freq(), Positions[i].Length);
		  for (int j = 0; j < Positions[i].Length; j++)
		  {
			Assert.AreEqual(Positions[i][j], dpEnum.nextPosition());
		  }
		  Assert.AreEqual(DocIdSetIterator.NO_MORE_DOCS, dpEnum.nextDoc());

		  dpEnum = termsEnum.docsAndPositions(null, dpEnum);
		  Assert.IsTrue(dpEnum.nextDoc() != DocIdSetIterator.NO_MORE_DOCS);
		  Assert.IsNotNull(dpEnum);
		  Assert.AreEqual(dpEnum.freq(), Positions[i].Length);
		  for (int j = 0; j < Positions[i].Length; j++)
		  {
			Assert.AreEqual(Positions[i][j], dpEnum.nextPosition());
			Assert.AreEqual(j * 10, dpEnum.StartOffset());
			Assert.AreEqual(j * 10 + TestTerms[i].Length, dpEnum.EndOffset());
		  }
		  Assert.AreEqual(DocIdSetIterator.NO_MORE_DOCS, dpEnum.nextDoc());
		}
		reader.close();
	  }

	  public virtual void TestIllegalIndexableField()
	  {
		Directory dir = newDirectory();
		RandomIndexWriter w = new RandomIndexWriter(random(), dir);
		FieldType ft = new FieldType(TextField.TYPE_NOT_STORED);
		ft.StoreTermVectors = true;
		ft.StoreTermVectorPayloads = true;
		Document doc = new Document();
		doc.add(new Field("field", "value", ft));
		try
		{
		  w.addDocument(doc);
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
		doc.add(new Field("field", "value", ft));
		try
		{
		  w.addDocument(doc);
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
		doc.add(new Field("field", "value", ft));
		try
		{
		  w.addDocument(doc);
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
		doc.add(new Field("field", "value", ft));
		try
		{
		  w.addDocument(doc);
		  Assert.Fail("did not hit exception");
		}
		catch (System.ArgumentException iae)
		{
		  // Expected
		  Assert.AreEqual("cannot index term vector payloads when term vectors are not indexed (field=\"field\")", iae.Message);
		}

		w.close();

		dir.close();
	  }
	}

}