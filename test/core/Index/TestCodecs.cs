using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Threading;

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


	using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
	using Codec = Lucene.Net.Codecs.Codec;
	using FieldsConsumer = Lucene.Net.Codecs.FieldsConsumer;
	using FieldsProducer = Lucene.Net.Codecs.FieldsProducer;
	using PostingsConsumer = Lucene.Net.Codecs.PostingsConsumer;
	using TermStats = Lucene.Net.Codecs.TermStats;
	using TermsConsumer = Lucene.Net.Codecs.TermsConsumer;
	using Lucene3xCodec = Lucene.Net.Codecs.Lucene3x.Lucene3xCodec;
	using Lucene40RWCodec = Lucene.Net.Codecs.Lucene40.Lucene40RWCodec;
	using Lucene41RWCodec = Lucene.Net.Codecs.Lucene41.Lucene41RWCodec;
	using Lucene42RWCodec = Lucene.Net.Codecs.Lucene42.Lucene42RWCodec;
	using MockSepPostingsFormat = Lucene.Net.Codecs.mocksep.MockSepPostingsFormat;
	using Document = Lucene.Net.Document.Document;
	using Store = Lucene.Net.Document.Field.Store;
	using FieldType = Lucene.Net.Document.FieldType;
	using NumericDocValuesField = Lucene.Net.Document.NumericDocValuesField;
	using StringField = Lucene.Net.Document.StringField;
	using TextField = Lucene.Net.Document.TextField;
	using DocValuesType = Lucene.Net.Index.FieldInfo.DocValuesType_e;
	using IndexOptions = Lucene.Net.Index.FieldInfo.IndexOptions;
	using DocIdSetIterator = Lucene.Net.Search.DocIdSetIterator;
	using IndexSearcher = Lucene.Net.Search.IndexSearcher;
	using PhraseQuery = Lucene.Net.Search.PhraseQuery;
	using Query = Lucene.Net.Search.Query;
	using ScoreDoc = Lucene.Net.Search.ScoreDoc;
	using Directory = Lucene.Net.Store.Directory;
	using BytesRef = Lucene.Net.Util.BytesRef;
	using Constants = Lucene.Net.Util.Constants;
	using InfoStream = Lucene.Net.Util.InfoStream;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
	using OpenBitSet = Lucene.Net.Util.OpenBitSet;
	using TestUtil = Lucene.Net.Util.TestUtil;
	using BeforeClass = org.junit.BeforeClass;

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

	public class TestCodecs : LuceneTestCase
	{
	  private static string[] FieldNames = new string[] {"one", "two", "three", "four"};

	  private static int NUM_TEST_ITER;
	  private const int NUM_TEST_THREADS = 3;
	  private const int NUM_FIELDS = 4;
	  private const int NUM_TERMS_RAND = 50; // must be > 16 to test skipping
	  private const int DOC_FREQ_RAND = 500; // must be > 16 to test skipping
	  private const int TERM_DOC_FREQ_RAND = 20;

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @BeforeClass public static void beforeClass()
	  public static void BeforeClass()
	  {
		NUM_TEST_ITER = atLeast(20);
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
		  FieldInfo = fieldInfos.addOrUpdate(name, new IndexableFieldTypeAnonymousInnerClassHelper(this, omitTF));
		  if (storePayloads)
		  {
			FieldInfo.setStorePayloads();
		  }
		  this.Terms = terms;
		  for (int i = 0;i < terms.Length;i++)
		  {
			terms[i].Field = this;
		  }

		  Arrays.sort(terms);
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


			public override bool Indexed()
			{
				return true;
			}
			public override bool Stored()
			{
				return false;
			}
			public override bool Tokenized()
			{
				return false;
			}
			public override bool StoreTermVectors()
			{
				return false;
			}
			public override bool StoreTermVectorOffsets()
			{
				return false;
			}
			public override bool StoreTermVectorPositions()
			{
				return false;
			}
			public override bool StoreTermVectorPayloads()
			{
				return false;
			}
			public override bool OmitNorms()
			{
				return false;
			}
			public override IndexOptions IndexOptions()
			{
				return OmitTF ? IndexOptions.DOCS_ONLY : IndexOptions.DOCS_AND_FREQS_AND_POSITIONS;
			}
			public override DocValuesType DocValueType()
			{
				return null;
			}
		}

		public override int CompareTo(FieldData other)
		{
		  return FieldInfo.name.compareTo(other.FieldInfo.name);
		}

		public virtual void Write(FieldsConsumer consumer)
		{
		  Arrays.sort(Terms);
		  TermsConsumer termsConsumer = consumer.addField(FieldInfo);
		  long sumTotalTermCount = 0;
		  long sumDF = 0;
		  OpenBitSet visitedDocs = new OpenBitSet();
		  foreach (TermData term in Terms)
		  {
			for (int i = 0; i < term.docs.length; i++)
			{
			  visitedDocs.set(term.docs[i]);
			}
			sumDF += term.docs.length;
			sumTotalTermCount += term.write(termsConsumer);
		  }
		  termsConsumer.finish(OmitTF ? - 1 : sumTotalTermCount, sumDF, (int) visitedDocs.cardinality());
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
		  return Text.compareTo(o.Text);
		}

		public virtual long Write(TermsConsumer termsConsumer)
		{
		  PostingsConsumer postingsConsumer = termsConsumer.startTerm(Text);
		  long totTF = 0;
		  for (int i = 0;i < Docs.Length;i++)
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
			postingsConsumer.startDoc(Docs[i], termDocFreq);
			if (!Field.OmitTF)
			{
			  totTF += Positions[i].Length;
			  for (int j = 0;j < Positions[i].Length;j++)
			  {
				PositionData pos = Positions[i][j];
				postingsConsumer.addPosition(pos.Pos, pos.Payload, -1, -1);
			  }
			}
			postingsConsumer.finishDoc();
		  }
		  termsConsumer.finishTerm(Text, new TermStats(Docs.Length, Field.OmitTF ? - 1 : totTF));
		  return totTF;
		}
	  }

	  private const string SEGMENT = "0";

	  internal virtual TermData[] MakeRandomTerms(bool omitTF, bool storePayloads)
	  {
		int numTerms = 1 + random().Next(NUM_TERMS_RAND);
		//final int numTerms = 2;
		TermData[] terms = new TermData[numTerms];

		HashSet<string> termsSeen = new HashSet<string>();

		for (int i = 0;i < numTerms;i++)
		{

		  // Make term text
		  string text2;
		  while (true)
		  {
			text2 = TestUtil.randomUnicodeString(random());
			if (!termsSeen.Contains(text2) && !text2.EndsWith("."))
			{
			  termsSeen.Add(text2);
			  break;
			}
		  }

		  int docFreq = 1 + random().Next(DOC_FREQ_RAND);
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
		  for (int j = 0;j < docFreq;j++)
		  {
			docID += TestUtil.Next(random(), 1, 10);
			docs[j] = docID;

			if (!omitTF)
			{
			  int termFreq = 1 + random().Next(TERM_DOC_FREQ_RAND);
			  positions[j] = new PositionData[termFreq];
			  int position = 0;
			  for (int k = 0;k < termFreq;k++)
			  {
				position += TestUtil.Next(random(), 1, 10);

				BytesRef payload;
				if (storePayloads && random().Next(4) == 0)
				{
				  sbyte[] bytes = new sbyte[1 + random().Next(5)];
				  for (int l = 0;l < bytes.Length;l++)
				  {
					bytes[l] = (sbyte) random().Next(255);
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

	  public virtual void TestFixedPostings()
	  {
		const int NUM_TERMS = 100;
		TermData[] terms = new TermData[NUM_TERMS];
		for (int i = 0;i < NUM_TERMS;i++)
		{
		  int[] docs = new int[] {i};
		  string text = Convert.ToString(i, char.MAX_RADIX);
		  terms[i] = new TermData(this, text, docs, null);
		}

		FieldInfos.Builder builder = new FieldInfos.Builder();

		FieldData field = new FieldData(this, "field", builder, terms, true, false);
		FieldData[] fields = new FieldData[] {field};
		FieldInfos fieldInfos = builder.finish();
		Directory dir = newDirectory();
		this.Write(fieldInfos, dir, fields, true);
		Codec codec = Codec.Default;
		SegmentInfo si = new SegmentInfo(dir, Constants.LUCENE_MAIN_VERSION, SEGMENT, 10000, false, codec, null);

		FieldsProducer reader = codec.postingsFormat().fieldsProducer(new SegmentReadState(dir, si, fieldInfos, newIOContext(random()), DirectoryReader.DEFAULT_TERMS_INDEX_DIVISOR));

		IEnumerator<string> fieldsEnum = reader.GetEnumerator();
//JAVA TO C# CONVERTER TODO TASK: Java iterators are only converted within the context of 'while' and 'for' loops:
		string fieldName = fieldsEnum.next();
		Assert.IsNotNull(fieldName);
		Terms terms2 = reader.terms(fieldName);
		Assert.IsNotNull(terms2);

		TermsEnum termsEnum = terms2.iterator(null);

		DocsEnum docsEnum = null;
		for (int i = 0;i < NUM_TERMS;i++)
		{
		  BytesRef term = termsEnum.next();
		  Assert.IsNotNull(term);
		  Assert.AreEqual(terms[i].Text2, term.utf8ToString());

		  // do this twice to stress test the codec's reuse, ie,
		  // make sure it properly fully resets (rewinds) its
		  // internal state:
		  for (int iter = 0;iter < 2;iter++)
		  {
			docsEnum = TestUtil.docs(random(), termsEnum, null, docsEnum, DocsEnum.FLAG_NONE);
			Assert.AreEqual(terms[i].Docs[0], docsEnum.nextDoc());
			Assert.AreEqual(DocIdSetIterator.NO_MORE_DOCS, docsEnum.nextDoc());
		  }
		}
		assertNull(termsEnum.next());

		for (int i = 0;i < NUM_TERMS;i++)
		{
		  Assert.AreEqual(termsEnum.seekCeil(new BytesRef(terms[i].Text2)), TermsEnum.SeekStatus.FOUND);
		}

//JAVA TO C# CONVERTER TODO TASK: Java iterators are only converted within the context of 'while' and 'for' loops:
		Assert.IsFalse(fieldsEnum.hasNext());
		reader.close();
		dir.close();
	  }

	  public virtual void TestRandomPostings()
	  {
		FieldInfos.Builder builder = new FieldInfos.Builder();

		FieldData[] fields = new FieldData[NUM_FIELDS];
		for (int i = 0;i < NUM_FIELDS;i++)
		{
		  bool omitTF = 0 == (i % 3);
		  bool storePayloads = 1 == (i % 3);
		  fields[i] = new FieldData(this, FieldNames[i], builder, this.MakeRandomTerms(omitTF, storePayloads), omitTF, storePayloads);
		}

		Directory dir = newDirectory();
		FieldInfos fieldInfos = builder.finish();

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
		FieldsProducer terms = codec.postingsFormat().fieldsProducer(new SegmentReadState(dir, si, fieldInfos, newIOContext(random()), DirectoryReader.DEFAULT_TERMS_INDEX_DIVISOR));

		Verify[] threads = new Verify[NUM_TEST_THREADS - 1];
		for (int i = 0;i < NUM_TEST_THREADS - 1;i++)
		{
		  threads[i] = new Verify(this, si, fields, terms);
		  threads[i].Daemon = true;
		  threads[i].Start();
		}

		(new Verify(this, si, fields, terms)).Run();

		for (int i = 0;i < NUM_TEST_THREADS - 1;i++)
		{
		  threads[i].Join();
		  Debug.Assert(!threads[i].Failed);
		}

		terms.close();
		dir.close();
	  }

	  public virtual void TestSepPositionAfterMerge()
	  {
		Directory dir = newDirectory();
		IndexWriterConfig config = newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random()));
		config.MergePolicy = newLogMergePolicy();
		config.Codec = TestUtil.alwaysPostingsFormat(new MockSepPostingsFormat());
		IndexWriter writer = new IndexWriter(dir, config);

		try
		{
		  PhraseQuery pq = new PhraseQuery();
		  pq.add(new Term("content", "bbb"));
		  pq.add(new Term("content", "ccc"));

		  Document doc = new Document();
		  FieldType customType = new FieldType(TextField.TYPE_NOT_STORED);
		  customType.OmitNorms = true;
		  doc.add(newField("content", "aaa bbb ccc ddd", customType));

		  // add document and force commit for creating a first segment
		  writer.addDocument(doc);
		  writer.commit();

		  ScoreDoc[] results = this.Search(writer, pq, 5);
		  Assert.AreEqual(1, results.Length);
		  Assert.AreEqual(0, results[0].doc);

		  // add document and force commit for creating a second segment
		  writer.addDocument(doc);
		  writer.commit();

		  // at this point, there should be at least two segments
		  results = this.Search(writer, pq, 5);
		  Assert.AreEqual(2, results.Length);
		  Assert.AreEqual(0, results[0].doc);

		  writer.forceMerge(1);

		  // optimise to merge the segments.
		  results = this.Search(writer, pq, 5);
		  Assert.AreEqual(2, results.Length);
		  Assert.AreEqual(0, results[0].doc);
		}
		finally
		{
		  writer.close();
		  dir.close();
		}
	  }

	  private ScoreDoc[] Search(IndexWriter writer, Query q, int n)
	  {
		IndexReader reader = writer.Reader;
		IndexSearcher searcher = newSearcher(reader);
		try
		{
		  return searcher.search(q, null, n).scoreDocs;
		}
		finally
		{
		  reader.close();
		}
	  }

	  private class Verify : System.Threading.Thread
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
//JAVA TO C# CONVERTER WARNING: 'final' catch parameters are not allowed in C#:
//ORIGINAL LINE: catch (final Throwable t)
		  catch (Exception t)
		  {
			Failed = true;
			throw new Exception(t);
		  }
		}

		internal virtual void VerifyDocs(int[] docs, PositionData[][] positions, DocsEnum docsEnum, bool doPos)
		{
		  for (int i = 0;i < docs.Length;i++)
		  {
			int doc = docsEnum.nextDoc();
			Assert.IsTrue(doc != DocIdSetIterator.NO_MORE_DOCS);
			Assert.AreEqual(docs[i], doc);
			if (doPos)
			{
			  this.VerifyPositions(positions[i], ((DocsAndPositionsEnum) docsEnum));
			}
		  }
		  Assert.AreEqual(DocIdSetIterator.NO_MORE_DOCS, docsEnum.nextDoc());
		}

		internal sbyte[] Data = new sbyte[10];

		internal virtual void VerifyPositions(PositionData[] positions, DocsAndPositionsEnum posEnum)
		{
		  for (int i = 0;i < positions.Length;i++)
		  {
			int pos = posEnum.nextPosition();
			Assert.AreEqual(positions[i].Pos, pos);
			if (positions[i].Payload != null)
			{
			  Assert.IsNotNull(posEnum.Payload);
			  if (random().Next(3) < 2)
			  {
				// Verify the payload bytes
				BytesRef otherPayload = posEnum.Payload;
				Assert.IsTrue("expected=" + positions[i].Payload.ToString() + " got=" + otherPayload.ToString(), positions[i].Payload.Equals(otherPayload));
			  }
			}
			else
			{
			  assertNull(posEnum.Payload);
			}
		  }
		}

		public virtual void _run()
		{

		  for (int iter = 0;iter < NUM_TEST_ITER;iter++)
		  {
			FieldData field = Fields[random().Next(Fields.Length)];
			TermsEnum termsEnum = TermsDict.terms(field.FieldInfo.name).iterator(null);
			if (Si.Codec is Lucene3xCodec)
			{
			  // code below expects unicode sort order
			  continue;
			}

			int upto = 0;
			// Test straight enum of the terms:
			while (true)
			{
			  BytesRef term = termsEnum.next();
			  if (term == null)
			  {
				break;
			  }
			  BytesRef expected = new BytesRef(field.Terms[upto++].text2);
			  Assert.IsTrue("expected=" + expected + " vs actual " + term, expected.bytesEquals(term));
			}
			Assert.AreEqual(upto, field.Terms.Length);

			// Test random seek:
			TermData term = field.Terms[random().Next(field.Terms.Length)];
			TermsEnum.SeekStatus status = termsEnum.seekCeil(new BytesRef(term.Text2));
			Assert.AreEqual(status, TermsEnum.SeekStatus.FOUND);
			Assert.AreEqual(term.Docs.Length, termsEnum.docFreq());
			if (field.OmitTF)
			{
			  this.VerifyDocs(term.Docs, term.Positions, TestUtil.docs(random(), termsEnum, null, null, DocsEnum.FLAG_NONE), false);
			}
			else
			{
			  this.VerifyDocs(term.Docs, term.Positions, termsEnum.docsAndPositions(null, null), true);
			}

			// Test random seek by ord:
			int idx = random().Next(field.Terms.Length);
			term = field.Terms[idx];
			bool success = false;
			try
			{
			  termsEnum.seekExact(idx);
			  success = true;
			}
			catch (System.NotSupportedException uoe)
			{
			  // ok -- skip it
			}
			if (success)
			{
			  Assert.AreEqual(status, TermsEnum.SeekStatus.FOUND);
			  Assert.IsTrue(termsEnum.term().bytesEquals(new BytesRef(term.Text2)));
			  Assert.AreEqual(term.Docs.Length, termsEnum.docFreq());
			  if (field.OmitTF)
			  {
				this.VerifyDocs(term.Docs, term.Positions, TestUtil.docs(random(), termsEnum, null, null, DocsEnum.FLAG_NONE), false);
			  }
			  else
			  {
				this.VerifyDocs(term.Docs, term.Positions, termsEnum.docsAndPositions(null, null), true);
			  }
			}

			// Test seek to non-existent terms:
			if (VERBOSE)
			{
			  Console.WriteLine("TEST: seek non-exist terms");
			}
			for (int i = 0;i < 100;i++)
			{
			  string text2 = TestUtil.randomUnicodeString(random()) + ".";
			  status = termsEnum.seekCeil(new BytesRef(text2));
			  Assert.IsTrue(status == TermsEnum.SeekStatus.NOT_FOUND || status == TermsEnum.SeekStatus.END);
			}

			// Seek to each term, backwards:
			if (VERBOSE)
			{
			  Console.WriteLine("TEST: seek terms backwards");
			}
			for (int i = field.Terms.Length - 1;i >= 0;i--)
			{
			  Assert.AreEqual(Thread.CurrentThread.Name + ": field=" + field.FieldInfo.name + " term=" + field.Terms[i].text2, TermsEnum.SeekStatus.FOUND, termsEnum.seekCeil(new BytesRef(field.Terms[i].text2)));
			  Assert.AreEqual(field.Terms[i].docs.length, termsEnum.docFreq());
			}

			// Seek to each term by ord, backwards
			for (int i = field.Terms.Length - 1;i >= 0;i--)
			{
			  try
			  {
				termsEnum.seekExact(i);
				Assert.AreEqual(field.Terms[i].docs.length, termsEnum.docFreq());
				Assert.IsTrue(termsEnum.term().bytesEquals(new BytesRef(field.Terms[i].text2)));
			  }
			  catch (System.NotSupportedException uoe)
			  {
			  }
			}

			// Seek to non-existent empty-string term
			status = termsEnum.seekCeil(new BytesRef(""));
			Assert.IsNotNull(status);
			//Assert.AreEqual(TermsEnum.SeekStatus.NOT_FOUND, status);

			// Make sure we're now pointing to first term
			Assert.IsTrue(termsEnum.term().bytesEquals(new BytesRef(field.Terms[0].text2)));

			// Test docs enum
			termsEnum.seekCeil(new BytesRef(""));
			upto = 0;
			do
			{
			  term = field.Terms[upto];
			  if (random().Next(3) == 1)
			  {
				DocsEnum docs;
				DocsEnum docsAndFreqs;
				DocsAndPositionsEnum postings;
				if (!field.OmitTF)
				{
				  postings = termsEnum.docsAndPositions(null, null);
				  if (postings != null)
				  {
					docs = docsAndFreqs = postings;
				  }
				  else
				  {
					docs = docsAndFreqs = TestUtil.docs(random(), termsEnum, null, null, DocsEnum.FLAG_FREQS);
				  }
				}
				else
				{
				  postings = null;
				  docsAndFreqs = null;
				  docs = TestUtil.docs(random(), termsEnum, null, null, DocsEnum.FLAG_NONE);
				}
				Assert.IsNotNull(docs);
				int upto2 = -1;
				bool ended = false;
				while (upto2 < term.Docs.Length - 1)
				{
				  // Maybe skip:
				  int left = term.Docs.Length - upto2;
				  int doc;
				  if (random().Next(3) == 1 && left >= 1)
				  {
					int inc = 1 + random().Next(left - 1);
					upto2 += inc;
					if (random().Next(2) == 1)
					{
					  doc = docs.advance(term.Docs[upto2]);
					  Assert.AreEqual(term.Docs[upto2], doc);
					}
					else
					{
					  doc = docs.advance(1 + term.Docs[upto2]);
					  if (doc == DocIdSetIterator.NO_MORE_DOCS)
					  {
						// skipped past last doc
						Debug.Assert(upto2 == term.Docs.Length - 1);
						ended = true;
						break;
					  }
					  else
					  {
						// skipped to next doc
						Debug.Assert(upto2 < term.Docs.Length - 1);
						if (doc >= term.Docs[1 + upto2])
						{
						  upto2++;
						}
					  }
					}
				  }
				  else
				  {
					doc = docs.nextDoc();
					Assert.IsTrue(doc != -1);
					upto2++;
				  }
				  Assert.AreEqual(term.Docs[upto2], doc);
				  if (!field.OmitTF)
				  {
					Assert.AreEqual(term.Positions[upto2].Length, postings.freq());
					if (random().Next(2) == 1)
					{
					  this.VerifyPositions(term.Positions[upto2], postings);
					}
				  }
				}

				if (!ended)
				{
				  Assert.AreEqual(DocIdSetIterator.NO_MORE_DOCS, docs.nextDoc());
				}
			  }
			  upto++;

			} while (termsEnum.next() != null);

			Assert.AreEqual(upto, field.Terms.Length);
		  }
		}
	  }

	  private void Write(FieldInfos fieldInfos, Directory dir, FieldData[] fields, bool allowPreFlex)
	  {

		int termIndexInterval = TestUtil.Next(random(), 13, 27);
		Codec codec = Codec.Default;
		SegmentInfo si = new SegmentInfo(dir, Constants.LUCENE_MAIN_VERSION, SEGMENT, 10000, false, codec, null);
		SegmentWriteState state = new SegmentWriteState(InfoStream.Default, dir, si, fieldInfos, termIndexInterval, null, newIOContext(random()));

		FieldsConsumer consumer = codec.postingsFormat().fieldsConsumer(state);
		Arrays.sort(fields);
		foreach (FieldData field in fields)
		{
		  if (!allowPreFlex && codec is Lucene3xCodec)
		  {
			// code below expects unicode sort order
			continue;
		  }
		  field.write(consumer);
		}
		consumer.close();
	  }

	  public virtual void TestDocsOnlyFreq()
	  {
		// tests that when fields are indexed with DOCS_ONLY, the Codec
		// returns 1 in docsEnum.freq()
		Directory dir = newDirectory();
		Random random = random();
		IndexWriter writer = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random)));
		// we don't need many documents to assert this, but don't use one document either
		int numDocs = atLeast(random, 50);
		for (int i = 0; i < numDocs; i++)
		{
		  Document doc = new Document();
		  doc.add(new StringField("f", "doc", Store.NO));
		  writer.addDocument(doc);
		}
		writer.close();

		Term term = new Term("f", new BytesRef("doc"));
		DirectoryReader reader = DirectoryReader.open(dir);
		foreach (AtomicReaderContext ctx in reader.leaves())
		{
		  DocsEnum de = ctx.reader().termDocsEnum(term);
		  while (de.nextDoc() != DocIdSetIterator.NO_MORE_DOCS)
		  {
			Assert.AreEqual("wrong freq for doc " + de.docID(), 1, de.freq());
		  }
		}
		reader.close();

		dir.close();
	  }

	  public virtual void TestDisableImpersonation()
	  {
		Codec[] oldCodecs = new Codec[] {new Lucene40RWCodec(), new Lucene41RWCodec(), new Lucene42RWCodec()};
		Directory dir = newDirectory();
		IndexWriterConfig conf = newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random()));
		conf.Codec = oldCodecs[random().Next(oldCodecs.Length)];
		IndexWriter writer = new IndexWriter(dir, conf);

		Document doc = new Document();
		doc.add(new StringField("f", "bar", Store.YES));
		doc.add(new NumericDocValuesField("n", 18L));
		writer.addDocument(doc);

		OLD_FORMAT_IMPERSONATION_IS_ACTIVE = false;
		try
		{
		  writer.close();
		  Assert.Fail("should not have succeeded to impersonate an old format!");
		}
		catch (System.NotSupportedException e)
		{
		  writer.rollback();
		}
		finally
		{
		  OLD_FORMAT_IMPERSONATION_IS_ACTIVE = true;
		}

		dir.close();
	  }

	}

}