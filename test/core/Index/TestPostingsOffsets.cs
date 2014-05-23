using System;
using System.Diagnostics;
using System.Collections.Generic;

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


	using Analyzer = Lucene.Net.Analysis.Analyzer;
	using CannedTokenStream = Lucene.Net.Analysis.CannedTokenStream;
	using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
	using MockPayloadAnalyzer = Lucene.Net.Analysis.MockPayloadAnalyzer;
	using Token = Lucene.Net.Analysis.Token;
	using TokenStream = Lucene.Net.Analysis.TokenStream;
	using Document = Lucene.Net.Document.Document;
	using Field = Lucene.Net.Document.Field;
	using FieldType = Lucene.Net.Document.FieldType;
	using IntField = Lucene.Net.Document.IntField;
	using StringField = Lucene.Net.Document.StringField;
	using TextField = Lucene.Net.Document.TextField;
	using IndexOptions = Lucene.Net.Index.FieldInfo.IndexOptions_e;
	using DocIdSetIterator = Lucene.Net.Search.DocIdSetIterator;
	using FieldCache = Lucene.Net.Search.FieldCache;
	using Directory = Lucene.Net.Store.Directory;
	using BytesRef = Lucene.Net.Util.BytesRef;
	using English = Lucene.Net.Util.English;
	using IOUtils = Lucene.Net.Util.IOUtils;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
	using SuppressCodecs = Lucene.Net.Util.LuceneTestCase.SuppressCodecs;
	using TestUtil = Lucene.Net.Util.TestUtil;

	// TODO: we really need to test indexingoffsets, but then getting only docs / docs + freqs.
	// not all codecs store prx separate...
	// TODO: fix sep codec to index offsets so we can greatly reduce this list!
//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @SuppressCodecs({"Lucene3x", "MockFixedIntBlock", "MockVariableIntBlock", "MockSep", "MockRandom"}) public class TestPostingsOffsets extends Lucene.Net.Util.LuceneTestCase
	public class TestPostingsOffsets : LuceneTestCase
	{
	  internal IndexWriterConfig Iwc;

	  public override void SetUp()
	  {
		base.setUp();
		Iwc = newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random()));
	  }

	  public virtual void TestBasic()
	  {
		Directory dir = newDirectory();

		RandomIndexWriter w = new RandomIndexWriter(random(), dir, Iwc);
		Document doc = new Document();

		FieldType ft = new FieldType(TextField.TYPE_NOT_STORED);
		ft.IndexOptions = FieldInfo.IndexOptions_e.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS;
		if (random().nextBoolean())
		{
		  ft.StoreTermVectors = true;
		  ft.StoreTermVectorPositions = random().nextBoolean();
		  ft.StoreTermVectorOffsets = random().nextBoolean();
		}
		Token[] tokens = new Token[] {MakeToken("a", 1, 0, 6), MakeToken("b", 1, 8, 9), MakeToken("a", 1, 9, 17), MakeToken("c", 1, 19, 50)};
		doc.add(new Field("content", new CannedTokenStream(tokens), ft));

		w.addDocument(doc);
		IndexReader r = w.Reader;
		w.close();

		DocsAndPositionsEnum dp = MultiFields.getTermPositionsEnum(r, null, "content", new BytesRef("a"));
		Assert.IsNotNull(dp);
		Assert.AreEqual(0, dp.nextDoc());
		Assert.AreEqual(2, dp.freq());
		Assert.AreEqual(0, dp.nextPosition());
		Assert.AreEqual(0, dp.StartOffset());
		Assert.AreEqual(6, dp.EndOffset());
		Assert.AreEqual(2, dp.nextPosition());
		Assert.AreEqual(9, dp.StartOffset());
		Assert.AreEqual(17, dp.EndOffset());
		Assert.AreEqual(DocIdSetIterator.NO_MORE_DOCS, dp.nextDoc());

		dp = MultiFields.getTermPositionsEnum(r, null, "content", new BytesRef("b"));
		Assert.IsNotNull(dp);
		Assert.AreEqual(0, dp.nextDoc());
		Assert.AreEqual(1, dp.freq());
		Assert.AreEqual(1, dp.nextPosition());
		Assert.AreEqual(8, dp.StartOffset());
		Assert.AreEqual(9, dp.EndOffset());
		Assert.AreEqual(DocIdSetIterator.NO_MORE_DOCS, dp.nextDoc());

		dp = MultiFields.getTermPositionsEnum(r, null, "content", new BytesRef("c"));
		Assert.IsNotNull(dp);
		Assert.AreEqual(0, dp.nextDoc());
		Assert.AreEqual(1, dp.freq());
		Assert.AreEqual(3, dp.nextPosition());
		Assert.AreEqual(19, dp.StartOffset());
		Assert.AreEqual(50, dp.EndOffset());
		Assert.AreEqual(DocIdSetIterator.NO_MORE_DOCS, dp.nextDoc());

		r.close();
		dir.close();
	  }

	  public virtual void TestSkipping()
	  {
		DoTestNumbers(false);
	  }

	  public virtual void TestPayloads()
	  {
		DoTestNumbers(true);
	  }

	  public virtual void DoTestNumbers(bool withPayloads)
	  {
		Directory dir = newDirectory();
		Analyzer analyzer = withPayloads ? new MockPayloadAnalyzer() : new MockAnalyzer(random());
		Iwc = newIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
		Iwc.MergePolicy = newLogMergePolicy(); // will rely on docids a bit for skipping
		RandomIndexWriter w = new RandomIndexWriter(random(), dir, Iwc);

		FieldType ft = new FieldType(TextField.TYPE_STORED);
		ft.IndexOptions = FieldInfo.IndexOptions_e.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS;
		if (random().nextBoolean())
		{
		  ft.StoreTermVectors = true;
		  ft.StoreTermVectorOffsets = random().nextBoolean();
		  ft.StoreTermVectorPositions = random().nextBoolean();
		}

		int numDocs = atLeast(500);
		for (int i = 0; i < numDocs; i++)
		{
		  Document doc = new Document();
		  doc.add(new Field("numbers", English.intToEnglish(i), ft));
		  doc.add(new Field("oddeven", (i % 2) == 0 ? "even" : "odd", ft));
		  doc.add(new StringField("id", "" + i, Field.Store.NO));
		  w.addDocument(doc);
		}

		IndexReader reader = w.Reader;
		w.close();

		string[] terms = new string[] {"one", "two", "three", "four", "five", "six", "seven", "eight", "nine", "ten", "hundred"};

		foreach (string term in terms)
		{
		  DocsAndPositionsEnum dp = MultiFields.getTermPositionsEnum(reader, null, "numbers", new BytesRef(term));
		  int doc;
		  while ((doc = dp.nextDoc()) != DocIdSetIterator.NO_MORE_DOCS)
		  {
			string storedNumbers = reader.document(doc).get("numbers");
			int freq = dp.freq();
			for (int i = 0; i < freq; i++)
			{
			  dp.nextPosition();
			  int start = dp.StartOffset();
			  Debug.Assert(start >= 0);
			  int end = dp.EndOffset();
			  Debug.Assert(end >= 0 && end >= start);
			  // check that the offsets correspond to the term in the src text
			  Assert.IsTrue(storedNumbers.Substring(start, end - start).Equals(term));
			  if (withPayloads)
			  {
				// check that we have a payload and it starts with "pos"
				Assert.IsNotNull(dp.Payload);
				BytesRef payload = dp.Payload;
				Assert.IsTrue(payload.utf8ToString().StartsWith("pos:"));
			  } // note: withPayloads=false doesnt necessarily mean we dont have them from MockAnalyzer!
			}
		  }
		}

		// check we can skip correctly
		int numSkippingTests = atLeast(50);

		for (int j = 0; j < numSkippingTests; j++)
		{
		  int num = TestUtil.Next(random(), 100, Math.Min(numDocs - 1, 999));
		  DocsAndPositionsEnum dp = MultiFields.getTermPositionsEnum(reader, null, "numbers", new BytesRef("hundred"));
		  int doc = dp.advance(num);
		  Assert.AreEqual(num, doc);
		  int freq = dp.freq();
		  for (int i = 0; i < freq; i++)
		  {
			string storedNumbers = reader.document(doc).get("numbers");
			dp.nextPosition();
			int start = dp.StartOffset();
			Debug.Assert(start >= 0);
			int end = dp.EndOffset();
			Debug.Assert(end >= 0 && end >= start);
			// check that the offsets correspond to the term in the src text
			Assert.IsTrue(storedNumbers.Substring(start, end - start).Equals("hundred"));
			if (withPayloads)
			{
			  // check that we have a payload and it starts with "pos"
			  Assert.IsNotNull(dp.Payload);
			  BytesRef payload = dp.Payload;
			  Assert.IsTrue(payload.utf8ToString().StartsWith("pos:"));
			} // note: withPayloads=false doesnt necessarily mean we dont have them from MockAnalyzer!
		  }
		}

		// check that other fields (without offsets) work correctly

		for (int i = 0; i < numDocs; i++)
		{
		  DocsEnum dp = MultiFields.getTermDocsEnum(reader, null, "id", new BytesRef("" + i), 0);
		  Assert.AreEqual(i, dp.nextDoc());
		  Assert.AreEqual(DocIdSetIterator.NO_MORE_DOCS, dp.nextDoc());
		}

		reader.close();
		dir.close();
	  }

	  public virtual void TestRandom()
	  {
		// token -> docID -> tokens
		IDictionary<string, IDictionary<int?, IList<Token>>> actualTokens = new Dictionary<string, IDictionary<int?, IList<Token>>>();

		Directory dir = newDirectory();
		RandomIndexWriter w = new RandomIndexWriter(random(), dir, Iwc);

		int numDocs = atLeast(20);
		//final int numDocs = atLeast(5);

		FieldType ft = new FieldType(TextField.TYPE_NOT_STORED);

		// TODO: randomize what IndexOptions we use; also test
		// changing this up in one IW buffered segment...:
		ft.IndexOptions = FieldInfo.IndexOptions_e.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS;
		if (random().nextBoolean())
		{
		  ft.StoreTermVectors = true;
		  ft.StoreTermVectorOffsets = random().nextBoolean();
		  ft.StoreTermVectorPositions = random().nextBoolean();
		}

		for (int docCount = 0;docCount < numDocs;docCount++)
		{
		  Document doc = new Document();
		  doc.add(new IntField("id", docCount, Field.Store.NO));
		  IList<Token> tokens = new List<Token>();
		  int numTokens = atLeast(100);
		  //final int numTokens = atLeast(20);
		  int pos = -1;
		  int offset = 0;
		  //System.out.println("doc id=" + docCount);
		  for (int tokenCount = 0;tokenCount < numTokens;tokenCount++)
		  {
			string text;
			if (random().nextBoolean())
			{
			  text = "a";
			}
			else if (random().nextBoolean())
			{
			  text = "b";
			}
			else if (random().nextBoolean())
			{
			  text = "c";
			}
			else
			{
			  text = "d";
			}

			int posIncr = random().nextBoolean() ? 1 : random().Next(5);
			if (tokenCount == 0 && posIncr == 0)
			{
			  posIncr = 1;
			}
			int offIncr = random().nextBoolean() ? 0 : random().Next(5);
			int tokenOffset = random().Next(5);

			Token token = MakeToken(text, posIncr, offset + offIncr, offset + offIncr + tokenOffset);
			if (!actualTokens.ContainsKey(text))
			{
			  actualTokens[text] = new Dictionary<int?, IList<Token>>();
			}
			IDictionary<int?, IList<Token>> postingsByDoc = actualTokens[text];
			if (!postingsByDoc.ContainsKey(docCount))
			{
			  postingsByDoc[docCount] = new List<Token>();
			}
			postingsByDoc[docCount].Add(token);
			tokens.Add(token);
			pos += posIncr;
			// stuff abs position into type:
			token.Type = "" + pos;
			offset += offIncr + tokenOffset;
			//System.out.println("  " + token + " posIncr=" + token.getPositionIncrement() + " pos=" + pos + " off=" + token.StartOffset() + "/" + token.EndOffset() + " (freq=" + postingsByDoc.get(docCount).size() + ")");
		  }
		  doc.add(new Field("content", new CannedTokenStream(tokens.ToArray()), ft));
		  w.addDocument(doc);
		}
		DirectoryReader r = w.Reader;
		w.close();

		string[] terms = new string[] {"a", "b", "c", "d"};
		foreach (AtomicReaderContext ctx in r.leaves())
		{
		  // TODO: improve this
		  AtomicReader sub = ctx.reader();
		  //System.out.println("\nsub=" + sub);
		  TermsEnum termsEnum = sub.fields().terms("content").iterator(null);
		  DocsEnum docs = null;
		  DocsAndPositionsEnum docsAndPositions = null;
		  DocsAndPositionsEnum docsAndPositionsAndOffsets = null;
		  FieldCache.Ints docIDToID = FieldCache.DEFAULT.getInts(sub, "id", false);
		  foreach (string term in terms)
		  {
			//System.out.println("  term=" + term);
			if (termsEnum.seekExact(new BytesRef(term)))
			{
			  docs = termsEnum.docs(null, docs);
			  Assert.IsNotNull(docs);
			  int doc;
			  //System.out.println("    doc/freq");
			  while ((doc = docs.nextDoc()) != DocIdSetIterator.NO_MORE_DOCS)
			  {
				IList<Token> expected = actualTokens[term][docIDToID.get(doc)];
				//System.out.println("      doc=" + docIDToID.get(doc) + " docID=" + doc + " " + expected.size() + " freq");
				Assert.IsNotNull(expected);
				Assert.AreEqual(expected.Count, docs.freq());
			  }

			  // explicitly exclude offsets here
			  docsAndPositions = termsEnum.docsAndPositions(null, docsAndPositions, DocsAndPositionsEnum.FLAG_PAYLOADS);
			  Assert.IsNotNull(docsAndPositions);
			  //System.out.println("    doc/freq/pos");
			  while ((doc = docsAndPositions.nextDoc()) != DocIdSetIterator.NO_MORE_DOCS)
			  {
				IList<Token> expected = actualTokens[term][docIDToID.get(doc)];
				//System.out.println("      doc=" + docIDToID.get(doc) + " " + expected.size() + " freq");
				Assert.IsNotNull(expected);
				Assert.AreEqual(expected.Count, docsAndPositions.freq());
				foreach (Token token in expected)
				{
				  int pos = Convert.ToInt32(token.type());
				  //System.out.println("        pos=" + pos);
				  Assert.AreEqual(pos, docsAndPositions.nextPosition());
				}
			  }

			  docsAndPositionsAndOffsets = termsEnum.docsAndPositions(null, docsAndPositions);
			  Assert.IsNotNull(docsAndPositionsAndOffsets);
			  //System.out.println("    doc/freq/pos/offs");
			  while ((doc = docsAndPositionsAndOffsets.nextDoc()) != DocIdSetIterator.NO_MORE_DOCS)
			  {
				IList<Token> expected = actualTokens[term][docIDToID.get(doc)];
				//System.out.println("      doc=" + docIDToID.get(doc) + " " + expected.size() + " freq");
				Assert.IsNotNull(expected);
				Assert.AreEqual(expected.Count, docsAndPositionsAndOffsets.freq());
				foreach (Token token in expected)
				{
				  int pos = Convert.ToInt32(token.type());
				  //System.out.println("        pos=" + pos);
				  Assert.AreEqual(pos, docsAndPositionsAndOffsets.nextPosition());
				  Assert.AreEqual(token.StartOffset(), docsAndPositionsAndOffsets.StartOffset());
				  Assert.AreEqual(token.EndOffset(), docsAndPositionsAndOffsets.EndOffset());
				}
			  }
			}
		  }
		  // TODO: test advance:
		}
		r.close();
		dir.close();
	  }

	  public virtual void TestWithUnindexedFields()
	  {
		Directory dir = newDirectory();
		RandomIndexWriter riw = new RandomIndexWriter(random(), dir, Iwc);
		for (int i = 0; i < 100; i++)
		{
		  Document doc = new Document();
		  // ensure at least one doc is indexed with offsets
		  if (i < 99 && random().Next(2) == 0)
		  {
			// stored only
			FieldType ft = new FieldType();
			ft.Indexed = false;
			ft.Stored = true;
			doc.add(new Field("foo", "boo!", ft));
		  }
		  else
		  {
			FieldType ft = new FieldType(TextField.TYPE_STORED);
			ft.IndexOptions = IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS;
			if (random().nextBoolean())
			{
			  // store some term vectors for the checkindex cross-check
			  ft.StoreTermVectors = true;
			  ft.StoreTermVectorPositions = true;
			  ft.StoreTermVectorOffsets = true;
			}
			doc.add(new Field("foo", "bar", ft));
		  }
		  riw.addDocument(doc);
		}
		CompositeReader ir = riw.Reader;
		AtomicReader slow = SlowCompositeReaderWrapper.wrap(ir);
		FieldInfos fis = slow.FieldInfos;
		Assert.AreEqual(IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS, fis.fieldInfo("foo").IndexOptions);
		slow.close();
		ir.close();
		riw.close();
		dir.close();
	  }

	  public virtual void TestAddFieldTwice()
	  {
		Directory dir = newDirectory();
		RandomIndexWriter iw = new RandomIndexWriter(random(), dir);
		Document doc = new Document();
		FieldType customType3 = new FieldType(TextField.TYPE_STORED);
		customType3.StoreTermVectors = true;
		customType3.StoreTermVectorPositions = true;
		customType3.StoreTermVectorOffsets = true;
		customType3.IndexOptions = IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS;
		doc.add(new Field("content3", "here is more content with aaa aaa aaa", customType3));
		doc.add(new Field("content3", "here is more content with aaa aaa aaa", customType3));
		iw.addDocument(doc);
		iw.close();
		dir.close(); // checkindex
	  }

	  // NOTE: the next two tests aren't that good as we need an EvilToken...
	  public virtual void TestNegativeOffsets()
	  {
		try
		{
		  CheckTokens(new Token[] {MakeToken("foo", 1, -1, -1)});
		  Assert.Fail();
		}
		catch (System.ArgumentException expected)
		{
		  //expected
		}
	  }

	  public virtual void TestIllegalOffsets()
	  {
		try
		{
		  CheckTokens(new Token[] {MakeToken("foo", 1, 1, 0)});
		  Assert.Fail();
		}
		catch (System.ArgumentException expected)
		{
		  //expected
		}
	  }

	  public virtual void TestBackwardsOffsets()
	  {
		try
		{
		  CheckTokens(new Token[] {MakeToken("foo", 1, 0, 3), MakeToken("foo", 1, 4, 7), MakeToken("foo", 0, 3, 6)});
		  Assert.Fail();
		}
		catch (System.ArgumentException expected)
		{
		  // expected
		}
	  }

	  public virtual void TestStackedTokens()
	  {
		CheckTokens(new Token[] {MakeToken("foo", 1, 0, 3), MakeToken("foo", 0, 0, 3), MakeToken("foo", 0, 0, 3)});
	  }

	  public virtual void TestLegalbutVeryLargeOffsets()
	  {
		Directory dir = newDirectory();
		IndexWriter iw = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, null));
		Document doc = new Document();
		Token t1 = new Token("foo", 0, int.MaxValue-500);
		if (random().nextBoolean())
		{
		  t1.Payload = new BytesRef("test");
		}
		Token t2 = new Token("foo", int.MaxValue-500, int.MaxValue);
		TokenStream tokenStream = new CannedTokenStream(new Token[] {t1, t2});
		FieldType ft = new FieldType(TextField.TYPE_NOT_STORED);
		ft.IndexOptions = IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS;
		// store some term vectors for the checkindex cross-check
		ft.StoreTermVectors = true;
		ft.StoreTermVectorPositions = true;
		ft.StoreTermVectorOffsets = true;
		Field field = new Field("foo", tokenStream, ft);
		doc.add(field);
		iw.addDocument(doc);
		iw.close();
		dir.close();
	  }
	  // TODO: more tests with other possibilities

	  private void CheckTokens(Token[] tokens)
	  {
		Directory dir = newDirectory();
		RandomIndexWriter riw = new RandomIndexWriter(random(), dir, Iwc);
		bool success = false;
		try
		{
		  FieldType ft = new FieldType(TextField.TYPE_NOT_STORED);
		  ft.IndexOptions = IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS;
		  // store some term vectors for the checkindex cross-check
		  ft.StoreTermVectors = true;
		  ft.StoreTermVectorPositions = true;
		  ft.StoreTermVectorOffsets = true;

		  Document doc = new Document();
		  doc.add(new Field("body", new CannedTokenStream(tokens), ft));
		  riw.addDocument(doc);
		  success = true;
		}
		finally
		{
		  if (success)
		  {
			IOUtils.close(riw, dir);
		  }
		  else
		  {
			IOUtils.closeWhileHandlingException(riw, dir);
		  }
		}
	  }

	  private Token MakeToken(string text, int posIncr, int startOffset, int endOffset)
	  {
		Token t = new Token();
		t.append(text);
		t.PositionIncrement = posIncr;
		t.SetOffset(startOffset, endOffset);
		return t;
	  }
	}

}