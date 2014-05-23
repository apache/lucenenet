using System;
using System.Diagnostics;
using System.Collections.Generic;

namespace Lucene.Net.Codecs.Lucene41
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
	using MockFixedLengthPayloadFilter = Lucene.Net.Analysis.MockFixedLengthPayloadFilter;
	using MockTokenizer = Lucene.Net.Analysis.MockTokenizer;
	using MockVariableLengthPayloadFilter = Lucene.Net.Analysis.MockVariableLengthPayloadFilter;
	using TokenFilter = Lucene.Net.Analysis.TokenFilter;
	using Tokenizer = Lucene.Net.Analysis.Tokenizer;
	using Document = Lucene.Net.Document.Document;
	using Field = Lucene.Net.Document.Field;
	using FieldType = Lucene.Net.Document.FieldType;
	using TextField = Lucene.Net.Document.TextField;
	using AtomicReader = Lucene.Net.Index.AtomicReader;
	using AtomicReaderContext = Lucene.Net.Index.AtomicReaderContext;
	using IndexOptions = Lucene.Net.Index.FieldInfo.IndexOptions_e;
	using OpenMode = Lucene.Net.Index.IndexWriterConfig.OpenMode;
	using SeekStatus = Lucene.Net.Index.TermsEnum.SeekStatus;
	using DirectoryReader = Lucene.Net.Index.DirectoryReader;
	using DocsAndPositionsEnum = Lucene.Net.Index.DocsAndPositionsEnum;
	using DocsEnum = Lucene.Net.Index.DocsEnum;
	using IndexWriter = Lucene.Net.Index.IndexWriter;
	using IndexWriterConfig = Lucene.Net.Index.IndexWriterConfig;
	using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;
	using Terms = Lucene.Net.Index.Terms;
	using TermsEnum = Lucene.Net.Index.TermsEnum;
	using DocIdSetIterator = Lucene.Net.Search.DocIdSetIterator;
	using Directory = Lucene.Net.Store.Directory;
	using Bits = Lucene.Net.Util.Bits;
	using BytesRef = Lucene.Net.Util.BytesRef;
	using English = Lucene.Net.Util.English;
	using FixedBitSet = Lucene.Net.Util.FixedBitSet;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
	using TestUtil = Lucene.Net.Util.TestUtil;
	using TestUtil = Lucene.Net.Util.TestUtil;
	using AutomatonTestUtil = Lucene.Net.Util.Automaton.AutomatonTestUtil;
	using CompiledAutomaton = Lucene.Net.Util.Automaton.CompiledAutomaton;
	using RegExp = Lucene.Net.Util.Automaton.RegExp;

	/// <summary>
	/// Tests partial enumeration (only pulling a subset of the indexed data) 
	/// </summary>
	public class TestBlockPostingsFormat3 : LuceneTestCase
	{
	  internal static readonly int MAXDOC = Lucene41PostingsFormat.BLOCK_SIZE * 20;

	  // creates 8 fields with different options and does "duels" of fields against each other
	  public virtual void Test()
	  {
		Directory dir = newDirectory();
		Analyzer analyzer = new AnalyzerAnonymousInnerClassHelper(this, Analyzer.PER_FIELD_REUSE_STRATEGY);
		IndexWriterConfig iwc = newIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
		iwc.Codec = TestUtil.alwaysPostingsFormat(new Lucene41PostingsFormat());
		// TODO we could actually add more fields implemented with different PFs
		// or, just put this test into the usual rotation?
		RandomIndexWriter iw = new RandomIndexWriter(random(), dir, iwc.clone());
		Document doc = new Document();
		FieldType docsOnlyType = new FieldType(TextField.TYPE_NOT_STORED);
		// turn this on for a cross-check
		docsOnlyType.StoreTermVectors = true;
		docsOnlyType.IndexOptions = IndexOptions.DOCS_ONLY;

		FieldType docsAndFreqsType = new FieldType(TextField.TYPE_NOT_STORED);
		// turn this on for a cross-check
		docsAndFreqsType.StoreTermVectors = true;
		docsAndFreqsType.IndexOptions = IndexOptions.DOCS_AND_FREQS;

		FieldType positionsType = new FieldType(TextField.TYPE_NOT_STORED);
		// turn these on for a cross-check
		positionsType.StoreTermVectors = true;
		positionsType.StoreTermVectorPositions = true;
		positionsType.StoreTermVectorOffsets = true;
		positionsType.StoreTermVectorPayloads = true;
		FieldType offsetsType = new FieldType(positionsType);
		offsetsType.setIndexOptions(IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS);
		Field field1 = new Field("field1docs", "", docsOnlyType);
		Field field2 = new Field("field2freqs", "", docsAndFreqsType);
		Field field3 = new Field("field3positions", "", positionsType);
		Field field4 = new Field("field4offsets", "", offsetsType);
		Field field5 = new Field("field5payloadsFixed", "", positionsType);
		Field field6 = new Field("field6payloadsVariable", "", positionsType);
		Field field7 = new Field("field7payloadsFixedOffsets", "", offsetsType);
		Field field8 = new Field("field8payloadsVariableOffsets", "", offsetsType);
		doc.add(field1);
		doc.add(field2);
		doc.add(field3);
		doc.add(field4);
		doc.add(field5);
		doc.add(field6);
		doc.add(field7);
		doc.add(field8);
		for (int i = 0; i < MAXDOC; i++)
		{
		  string stringValue = Convert.ToString(i) + " verycommon " + English.intToEnglish(i).replace('-', ' ') + " " + TestUtil.randomSimpleString(random());
		  field1.StringValue = stringValue;
		  field2.StringValue = stringValue;
		  field3.StringValue = stringValue;
		  field4.StringValue = stringValue;
		  field5.StringValue = stringValue;
		  field6.StringValue = stringValue;
		  field7.StringValue = stringValue;
		  field8.StringValue = stringValue;
		  iw.addDocument(doc);
		}
		iw.close();
		Verify(dir);
		TestUtil.checkIndex(dir); // for some extra coverage, checkIndex before we forceMerge
		iwc.OpenMode = OpenMode.APPEND;
		IndexWriter iw2 = new IndexWriter(dir, iwc.clone());
		iw2.forceMerge(1);
		iw2.close();
		Verify(dir);
		dir.close();
	  }

	  private class AnalyzerAnonymousInnerClassHelper : Analyzer
	  {
		  private readonly TestBlockPostingsFormat3 OuterInstance;

		  public AnalyzerAnonymousInnerClassHelper(TestBlockPostingsFormat3 outerInstance, UnknownType PER_FIELD_REUSE_STRATEGY) : base(PER_FIELD_REUSE_STRATEGY)
		  {
			  this.OuterInstance = outerInstance;
		  }

		  protected internal override TokenStreamComponents CreateComponents(string fieldName, Reader reader)
		  {
			Tokenizer tokenizer = new MockTokenizer(reader);
			if (fieldName.Contains("payloadsFixed"))
			{
			  TokenFilter filter = new MockFixedLengthPayloadFilter(new Random(0), tokenizer, 1);
			  return new TokenStreamComponents(tokenizer, filter);
			}
			else if (fieldName.Contains("payloadsVariable"))
			{
			  TokenFilter filter = new MockVariableLengthPayloadFilter(new Random(0), tokenizer);
			  return new TokenStreamComponents(tokenizer, filter);
			}
			else
			{
			  return new TokenStreamComponents(tokenizer);
			}
		  }
	  }

	  private void Verify(Directory dir)
	  {
		DirectoryReader ir = DirectoryReader.open(dir);
		foreach (AtomicReaderContext leaf in ir.leaves())
		{
		  AtomicReader leafReader = leaf.reader();
		  AssertTerms(leafReader.terms("field1docs"), leafReader.terms("field2freqs"), true);
		  AssertTerms(leafReader.terms("field3positions"), leafReader.terms("field4offsets"), true);
		  AssertTerms(leafReader.terms("field4offsets"), leafReader.terms("field5payloadsFixed"), true);
		  AssertTerms(leafReader.terms("field5payloadsFixed"), leafReader.terms("field6payloadsVariable"), true);
		  AssertTerms(leafReader.terms("field6payloadsVariable"), leafReader.terms("field7payloadsFixedOffsets"), true);
		  AssertTerms(leafReader.terms("field7payloadsFixedOffsets"), leafReader.terms("field8payloadsVariableOffsets"), true);
		}
		ir.close();
	  }

	  // following code is almost an exact dup of code from TestDuelingCodecs: sorry!

	  public virtual void AssertTerms(Terms leftTerms, Terms rightTerms, bool deep)
	  {
		if (leftTerms == null || rightTerms == null)
		{
		  assertNull(leftTerms);
		  assertNull(rightTerms);
		  return;
		}
		AssertTermsStatistics(leftTerms, rightTerms);

		// NOTE: we don't assert hasOffsets/hasPositions/hasPayloads because they are allowed to be different

		TermsEnum leftTermsEnum = leftTerms.iterator(null);
		TermsEnum rightTermsEnum = rightTerms.iterator(null);
		AssertTermsEnum(leftTermsEnum, rightTermsEnum, true);

		AssertTermsSeeking(leftTerms, rightTerms);

		if (deep)
		{
		  int numIntersections = atLeast(3);
		  for (int i = 0; i < numIntersections; i++)
		  {
			string re = AutomatonTestUtil.randomRegexp(random());
			CompiledAutomaton automaton = new CompiledAutomaton((new RegExp(re, RegExp.NONE)).toAutomaton());
			if (automaton.type == CompiledAutomaton.AUTOMATON_TYPE.NORMAL)
			{
			  // TODO: test start term too
			  TermsEnum leftIntersection = leftTerms.intersect(automaton, null);
			  TermsEnum rightIntersection = rightTerms.intersect(automaton, null);
			  AssertTermsEnum(leftIntersection, rightIntersection, rarely());
			}
		  }
		}
	  }

	  private void AssertTermsSeeking(Terms leftTerms, Terms rightTerms)
	  {
		TermsEnum leftEnum = null;
		TermsEnum rightEnum = null;

		// just an upper bound
		int numTests = atLeast(20);
		Random random = random();

		// collect this number of terms from the left side
		HashSet<BytesRef> tests = new HashSet<BytesRef>();
		int numPasses = 0;
		while (numPasses < 10 && tests.Count < numTests)
		{
		  leftEnum = leftTerms.iterator(leftEnum);
		  BytesRef term = null;
		  while ((term = leftEnum.next()) != null)
		  {
			int code = random.Next(10);
			if (code == 0)
			{
			  // the term
			  tests.Add(BytesRef.deepCopyOf(term));
			}
			else if (code == 1)
			{
			  // truncated subsequence of term
			  term = BytesRef.deepCopyOf(term);
			  if (term.length > 0)
			  {
				// truncate it
				term.length = random.Next(term.length);
			  }
			}
			else if (code == 2)
			{
			  // term, but ensure a non-zero offset
			  sbyte[] newbytes = new sbyte[term.length + 5];
			  Array.Copy(term.bytes, term.offset, newbytes, 5, term.length);
			  tests.Add(new BytesRef(newbytes, 5, term.length));
			}
		  }
		  numPasses++;
		}

		List<BytesRef> shuffledTests = new List<BytesRef>(tests);
		Collections.shuffle(shuffledTests, random);

		foreach (BytesRef b in shuffledTests)
		{
		  leftEnum = leftTerms.iterator(leftEnum);
		  rightEnum = rightTerms.iterator(rightEnum);

		  Assert.AreEqual(leftEnum.seekExact(b), rightEnum.seekExact(b));
		  Assert.AreEqual(leftEnum.seekExact(b), rightEnum.seekExact(b));

		  SeekStatus leftStatus;
		  SeekStatus rightStatus;

		  leftStatus = leftEnum.seekCeil(b);
		  rightStatus = rightEnum.seekCeil(b);
		  Assert.AreEqual(leftStatus, rightStatus);
		  if (leftStatus != SeekStatus.END)
		  {
			Assert.AreEqual(leftEnum.term(), rightEnum.term());
		  }

		  leftStatus = leftEnum.seekCeil(b);
		  rightStatus = rightEnum.seekCeil(b);
		  Assert.AreEqual(leftStatus, rightStatus);
		  if (leftStatus != SeekStatus.END)
		  {
			Assert.AreEqual(leftEnum.term(), rightEnum.term());
		  }
		}
	  }

	  /// <summary>
	  /// checks collection-level statistics on Terms 
	  /// </summary>
	  public virtual void AssertTermsStatistics(Terms leftTerms, Terms rightTerms)
	  {
		Debug.Assert(leftTerms.Comparator == rightTerms.Comparator);
		if (leftTerms.DocCount != -1 && rightTerms.DocCount != -1)
		{
		  Assert.AreEqual(leftTerms.DocCount, rightTerms.DocCount);
		}
		if (leftTerms.SumDocFreq != -1 && rightTerms.SumDocFreq != -1)
		{
		  Assert.AreEqual(leftTerms.SumDocFreq, rightTerms.SumDocFreq);
		}
		if (leftTerms.SumTotalTermFreq != -1 && rightTerms.SumTotalTermFreq != -1)
		{
		  Assert.AreEqual(leftTerms.SumTotalTermFreq, rightTerms.SumTotalTermFreq);
		}
		if (leftTerms.size() != -1 && rightTerms.size() != -1)
		{
		  Assert.AreEqual(leftTerms.size(), rightTerms.size());
		}
	  }

	  /// <summary>
	  /// checks the terms enum sequentially
	  /// if deep is false, it does a 'shallow' test that doesnt go down to the docsenums
	  /// </summary>
	  public virtual void AssertTermsEnum(TermsEnum leftTermsEnum, TermsEnum rightTermsEnum, bool deep)
	  {
		BytesRef term;
		Bits randomBits = new RandomBits(MAXDOC, random().NextDouble(), random());
		DocsAndPositionsEnum leftPositions = null;
		DocsAndPositionsEnum rightPositions = null;
		DocsEnum leftDocs = null;
		DocsEnum rightDocs = null;

		while ((term = leftTermsEnum.next()) != null)
		{
		  Assert.AreEqual(term, rightTermsEnum.next());
		  AssertTermStats(leftTermsEnum, rightTermsEnum);
		  if (deep)
		  {
			// with payloads + off
			AssertDocsAndPositionsEnum(leftPositions = leftTermsEnum.docsAndPositions(null, leftPositions), rightPositions = rightTermsEnum.docsAndPositions(null, rightPositions));
			AssertDocsAndPositionsEnum(leftPositions = leftTermsEnum.docsAndPositions(randomBits, leftPositions), rightPositions = rightTermsEnum.docsAndPositions(randomBits, rightPositions));

			AssertPositionsSkipping(leftTermsEnum.docFreq(), leftPositions = leftTermsEnum.docsAndPositions(null, leftPositions), rightPositions = rightTermsEnum.docsAndPositions(null, rightPositions));
			AssertPositionsSkipping(leftTermsEnum.docFreq(), leftPositions = leftTermsEnum.docsAndPositions(randomBits, leftPositions), rightPositions = rightTermsEnum.docsAndPositions(randomBits, rightPositions));
			// with payloads only
			AssertDocsAndPositionsEnum(leftPositions = leftTermsEnum.docsAndPositions(null, leftPositions, DocsAndPositionsEnum.FLAG_PAYLOADS), rightPositions = rightTermsEnum.docsAndPositions(null, rightPositions, DocsAndPositionsEnum.FLAG_PAYLOADS));
			AssertDocsAndPositionsEnum(leftPositions = leftTermsEnum.docsAndPositions(randomBits, leftPositions, DocsAndPositionsEnum.FLAG_PAYLOADS), rightPositions = rightTermsEnum.docsAndPositions(randomBits, rightPositions, DocsAndPositionsEnum.FLAG_PAYLOADS));

			AssertPositionsSkipping(leftTermsEnum.docFreq(), leftPositions = leftTermsEnum.docsAndPositions(null, leftPositions, DocsAndPositionsEnum.FLAG_PAYLOADS), rightPositions = rightTermsEnum.docsAndPositions(null, rightPositions, DocsAndPositionsEnum.FLAG_PAYLOADS));
			AssertPositionsSkipping(leftTermsEnum.docFreq(), leftPositions = leftTermsEnum.docsAndPositions(randomBits, leftPositions, DocsAndPositionsEnum.FLAG_PAYLOADS), rightPositions = rightTermsEnum.docsAndPositions(randomBits, rightPositions, DocsAndPositionsEnum.FLAG_PAYLOADS));

			// with offsets only
			AssertDocsAndPositionsEnum(leftPositions = leftTermsEnum.docsAndPositions(null, leftPositions, DocsAndPositionsEnum.FLAG_OFFSETS), rightPositions = rightTermsEnum.docsAndPositions(null, rightPositions, DocsAndPositionsEnum.FLAG_OFFSETS));
			AssertDocsAndPositionsEnum(leftPositions = leftTermsEnum.docsAndPositions(randomBits, leftPositions, DocsAndPositionsEnum.FLAG_OFFSETS), rightPositions = rightTermsEnum.docsAndPositions(randomBits, rightPositions, DocsAndPositionsEnum.FLAG_OFFSETS));

			AssertPositionsSkipping(leftTermsEnum.docFreq(), leftPositions = leftTermsEnum.docsAndPositions(null, leftPositions, DocsAndPositionsEnum.FLAG_OFFSETS), rightPositions = rightTermsEnum.docsAndPositions(null, rightPositions, DocsAndPositionsEnum.FLAG_OFFSETS));
			AssertPositionsSkipping(leftTermsEnum.docFreq(), leftPositions = leftTermsEnum.docsAndPositions(randomBits, leftPositions, DocsAndPositionsEnum.FLAG_OFFSETS), rightPositions = rightTermsEnum.docsAndPositions(randomBits, rightPositions, DocsAndPositionsEnum.FLAG_OFFSETS));

			// with positions only
			AssertDocsAndPositionsEnum(leftPositions = leftTermsEnum.docsAndPositions(null, leftPositions, DocsEnum.FLAG_NONE), rightPositions = rightTermsEnum.docsAndPositions(null, rightPositions, DocsEnum.FLAG_NONE));
			AssertDocsAndPositionsEnum(leftPositions = leftTermsEnum.docsAndPositions(randomBits, leftPositions, DocsEnum.FLAG_NONE), rightPositions = rightTermsEnum.docsAndPositions(randomBits, rightPositions, DocsEnum.FLAG_NONE));

			AssertPositionsSkipping(leftTermsEnum.docFreq(), leftPositions = leftTermsEnum.docsAndPositions(null, leftPositions, DocsEnum.FLAG_NONE), rightPositions = rightTermsEnum.docsAndPositions(null, rightPositions, DocsEnum.FLAG_NONE));
			AssertPositionsSkipping(leftTermsEnum.docFreq(), leftPositions = leftTermsEnum.docsAndPositions(randomBits, leftPositions, DocsEnum.FLAG_NONE), rightPositions = rightTermsEnum.docsAndPositions(randomBits, rightPositions, DocsEnum.FLAG_NONE));

			// with freqs:
			AssertDocsEnum(leftDocs = leftTermsEnum.docs(null, leftDocs), rightDocs = rightTermsEnum.docs(null, rightDocs));
			AssertDocsEnum(leftDocs = leftTermsEnum.docs(randomBits, leftDocs), rightDocs = rightTermsEnum.docs(randomBits, rightDocs));

			// w/o freqs:
			AssertDocsEnum(leftDocs = leftTermsEnum.docs(null, leftDocs, DocsEnum.FLAG_NONE), rightDocs = rightTermsEnum.docs(null, rightDocs, DocsEnum.FLAG_NONE));
			AssertDocsEnum(leftDocs = leftTermsEnum.docs(randomBits, leftDocs, DocsEnum.FLAG_NONE), rightDocs = rightTermsEnum.docs(randomBits, rightDocs, DocsEnum.FLAG_NONE));

			// with freqs:
			AssertDocsSkipping(leftTermsEnum.docFreq(), leftDocs = leftTermsEnum.docs(null, leftDocs), rightDocs = rightTermsEnum.docs(null, rightDocs));
			AssertDocsSkipping(leftTermsEnum.docFreq(), leftDocs = leftTermsEnum.docs(randomBits, leftDocs), rightDocs = rightTermsEnum.docs(randomBits, rightDocs));

			// w/o freqs:
			AssertDocsSkipping(leftTermsEnum.docFreq(), leftDocs = leftTermsEnum.docs(null, leftDocs, DocsEnum.FLAG_NONE), rightDocs = rightTermsEnum.docs(null, rightDocs, DocsEnum.FLAG_NONE));
			AssertDocsSkipping(leftTermsEnum.docFreq(), leftDocs = leftTermsEnum.docs(randomBits, leftDocs, DocsEnum.FLAG_NONE), rightDocs = rightTermsEnum.docs(randomBits, rightDocs, DocsEnum.FLAG_NONE));
		  }
		}
		assertNull(rightTermsEnum.next());
	  }

	  /// <summary>
	  /// checks term-level statistics
	  /// </summary>
	  public virtual void AssertTermStats(TermsEnum leftTermsEnum, TermsEnum rightTermsEnum)
	  {
		Assert.AreEqual(leftTermsEnum.docFreq(), rightTermsEnum.docFreq());
		if (leftTermsEnum.totalTermFreq() != -1 && rightTermsEnum.totalTermFreq() != -1)
		{
		  Assert.AreEqual(leftTermsEnum.totalTermFreq(), rightTermsEnum.totalTermFreq());
		}
	  }

	  /// <summary>
	  /// checks docs + freqs + positions + payloads, sequentially
	  /// </summary>
	  public virtual void AssertDocsAndPositionsEnum(DocsAndPositionsEnum leftDocs, DocsAndPositionsEnum rightDocs)
	  {
		if (leftDocs == null || rightDocs == null)
		{
		  assertNull(leftDocs);
		  assertNull(rightDocs);
		  return;
		}
		Assert.AreEqual(-1, leftDocs.docID());
		Assert.AreEqual(-1, rightDocs.docID());
		int docid;
		while ((docid = leftDocs.nextDoc()) != DocIdSetIterator.NO_MORE_DOCS)
		{
		  Assert.AreEqual(docid, rightDocs.nextDoc());
		  int freq = leftDocs.freq();
		  Assert.AreEqual(freq, rightDocs.freq());
		  for (int i = 0; i < freq; i++)
		  {
			Assert.AreEqual(leftDocs.nextPosition(), rightDocs.nextPosition());
			// we don't assert offsets/payloads, they are allowed to be different
		  }
		}
		Assert.AreEqual(DocIdSetIterator.NO_MORE_DOCS, rightDocs.nextDoc());
	  }

	  /// <summary>
	  /// checks docs + freqs, sequentially
	  /// </summary>
	  public virtual void AssertDocsEnum(DocsEnum leftDocs, DocsEnum rightDocs)
	  {
		if (leftDocs == null)
		{
		  assertNull(rightDocs);
		  return;
		}
		Assert.AreEqual(-1, leftDocs.docID());
		Assert.AreEqual(-1, rightDocs.docID());
		int docid;
		while ((docid = leftDocs.nextDoc()) != DocIdSetIterator.NO_MORE_DOCS)
		{
		  Assert.AreEqual(docid, rightDocs.nextDoc());
		  // we don't assert freqs, they are allowed to be different
		}
		Assert.AreEqual(DocIdSetIterator.NO_MORE_DOCS, rightDocs.nextDoc());
	  }

	  /// <summary>
	  /// checks advancing docs
	  /// </summary>
	  public virtual void AssertDocsSkipping(int docFreq, DocsEnum leftDocs, DocsEnum rightDocs)
	  {
		if (leftDocs == null)
		{
		  assertNull(rightDocs);
		  return;
		}
		int docid = -1;
		int averageGap = MAXDOC / (1 + docFreq);
		int skipInterval = 16;

		while (true)
		{
		  if (random().nextBoolean())
		  {
			// nextDoc()
			docid = leftDocs.nextDoc();
			Assert.AreEqual(docid, rightDocs.nextDoc());
		  }
		  else
		  {
			// advance()
			int skip = docid + (int) Math.Ceiling(Math.Abs(skipInterval + random().nextGaussian() * averageGap));
			docid = leftDocs.advance(skip);
			Assert.AreEqual(docid, rightDocs.advance(skip));
		  }

		  if (docid == DocIdSetIterator.NO_MORE_DOCS)
		  {
			return;
		  }
		  // we don't assert freqs, they are allowed to be different
		}
	  }

	  /// <summary>
	  /// checks advancing docs + positions
	  /// </summary>
	  public virtual void AssertPositionsSkipping(int docFreq, DocsAndPositionsEnum leftDocs, DocsAndPositionsEnum rightDocs)
	  {
		if (leftDocs == null || rightDocs == null)
		{
		  assertNull(leftDocs);
		  assertNull(rightDocs);
		  return;
		}

		int docid = -1;
		int averageGap = MAXDOC / (1 + docFreq);
		int skipInterval = 16;

		while (true)
		{
		  if (random().nextBoolean())
		  {
			// nextDoc()
			docid = leftDocs.nextDoc();
			Assert.AreEqual(docid, rightDocs.nextDoc());
		  }
		  else
		  {
			// advance()
			int skip = docid + (int) Math.Ceiling(Math.Abs(skipInterval + random().nextGaussian() * averageGap));
			docid = leftDocs.advance(skip);
			Assert.AreEqual(docid, rightDocs.advance(skip));
		  }

		  if (docid == DocIdSetIterator.NO_MORE_DOCS)
		  {
			return;
		  }
		  int freq = leftDocs.freq();
		  Assert.AreEqual(freq, rightDocs.freq());
		  for (int i = 0; i < freq; i++)
		  {
			Assert.AreEqual(leftDocs.nextPosition(), rightDocs.nextPosition());
			// we don't compare the payloads, its allowed that one is empty etc
		  }
		}
	  }

	  private class RandomBits : Bits
	  {
		internal FixedBitSet Bits;

		internal RandomBits(int maxDoc, double pctLive, Random random)
		{
		  Bits = new FixedBitSet(maxDoc);
		  for (int i = 0; i < maxDoc; i++)
		  {
			if (random.NextDouble() <= pctLive)
			{
			  Bits.set(i);
			}
		  }
		}

		public override bool Get(int index)
		{
		  return Bits.get(index);
		}

		public override int Length()
		{
		  return Bits.length();
		}
	  }
	}

}