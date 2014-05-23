using System;
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


	using TokenStream = Lucene.Net.Analysis.TokenStream;
	using CharTermAttribute = Lucene.Net.Analysis.Tokenattributes.CharTermAttribute;
	using OffsetAttribute = Lucene.Net.Analysis.Tokenattributes.OffsetAttribute;
	using PayloadAttribute = Lucene.Net.Analysis.Tokenattributes.PayloadAttribute;
	using PositionIncrementAttribute = Lucene.Net.Analysis.Tokenattributes.PositionIncrementAttribute;
	using Codec = Lucene.Net.Codecs.Codec;
	using TermVectorsFormat = Lucene.Net.Codecs.TermVectorsFormat;
	using Document = Lucene.Net.Document.Document;
	using Field = Lucene.Net.Document.Field;
	using Store = Lucene.Net.Document.Field.Store;
	using FieldType = Lucene.Net.Document.FieldType;
	using StringField = Lucene.Net.Document.StringField;
	using TextField = Lucene.Net.Document.TextField;
	using IndexOptions = Lucene.Net.Index.FieldInfo.IndexOptions_e;
	using SeekStatus = Lucene.Net.Index.TermsEnum.SeekStatus;
	using IndexSearcher = Lucene.Net.Search.IndexSearcher;
	using TermQuery = Lucene.Net.Search.TermQuery;
	using Directory = Lucene.Net.Store.Directory;
	using AttributeImpl = Lucene.Net.Util.AttributeImpl;
	using BytesRef = Lucene.Net.Util.BytesRef;
	using FixedBitSet = Lucene.Net.Util.FixedBitSet;
	using TestUtil = Lucene.Net.Util.TestUtil;

	using RandomPicks = com.carrotsearch.randomizedtesting.generators.RandomPicks;

	/// <summary>
	/// Base class aiming at testing <seealso cref="TermVectorsFormat term vectors formats"/>.
	/// To test a new format, all you need is to register a new <seealso cref="Codec"/> which
	/// uses it and extend this class and override <seealso cref="#getCodec()"/>.
	/// @lucene.experimental
	/// </summary>
	public abstract class BaseTermVectorsFormatTestCase : BaseIndexFileFormatTestCase
	{

	  /// <summary>
	  /// A combination of term vectors options.
	  /// </summary>
	  protected internal enum Options
	  {
//JAVA TO C# CONVERTER TODO TASK: Enum values must be single integer values in .NET:
		NONE(false, false, false),
//JAVA TO C# CONVERTER TODO TASK: Enum values must be single integer values in .NET:
		POSITIONS(true, false, false),
//JAVA TO C# CONVERTER TODO TASK: Enum values must be single integer values in .NET:
		OFFSETS(false, true, false),
//JAVA TO C# CONVERTER TODO TASK: Enum values must be single integer values in .NET:
		POSITIONS_AND_OFFSETS(true, true, false),
//JAVA TO C# CONVERTER TODO TASK: Enum values must be single integer values in .NET:
		POSITIONS_AND_PAYLOADS(true, false, true),
//JAVA TO C# CONVERTER TODO TASK: Enum values must be single integer values in .NET:
		POSITIONS_AND_OFFSETS_AND_PAYLOADS(true, true, true);
//JAVA TO C# CONVERTER TODO TASK: Enums cannot contain fields in .NET:
//		final boolean positions, offsets, payloads;
//JAVA TO C# CONVERTER TODO TASK: Enums cannot contain methods in .NET:
//		private Options(boolean positions, boolean offsets, boolean payloads)
	//	{
	//	  this.positions = positions;
	//	  this.offsets = offsets;
	//	  this.payloads = payloads;
	//	}
	  }

	  protected internal virtual Set<Options> ValidOptions()
	  {
		return EnumSet.allOf(typeof(Options));
	  }

	  protected internal virtual Options RandomOptions()
	  {
		return RandomPicks.randomFrom(Random(), new List<>(ValidOptions()));
	  }

	  protected internal virtual FieldType FieldType(Options options)
	  {
		FieldType ft = new FieldType(TextField.TYPE_NOT_STORED);
		ft.StoreTermVectors = true;
		ft.StoreTermVectorPositions = options.positions;
		ft.StoreTermVectorOffsets = options.offsets;
		ft.StoreTermVectorPayloads = options.payloads;
		ft.freeze();
		return ft;
	  }

	  protected internal virtual BytesRef RandomPayload()
	  {
		int len = Random().Next(5);
		if (len == 0)
		{
		  return null;
		}
		BytesRef payload = new BytesRef(len);
		Random().nextBytes(payload.bytes);
		payload.length = len;
		return payload;
	  }

	  protected internal override void AddRandomFields(Document doc)
	  {
		foreach (Options opts in ValidOptions())
		{
		  FieldType ft = FieldType(opts);
		  int numFields = Random().Next(5);
		  for (int j = 0; j < numFields; ++j)
		  {
			doc.add(new Field("f_" + opts, TestUtil.RandomSimpleString(Random(), 2), ft));
		  }
		}
	  }

	  // custom impl to test cases that are forbidden by the default OffsetAttribute impl
	  private class PermissiveOffsetAttributeImpl : AttributeImpl, OffsetAttribute
	  {

		internal int Start, End;

		public override int StartOffset()
		{
		  return Start;
		}

		public override int EndOffset()
		{
		  return End;
		}

		public override void SetOffset(int startOffset, int endOffset)
		{
		  // no check!
		  Start = startOffset;
		  End = endOffset;
		}

		public override void Clear()
		{
		  Start = End = 0;
		}

		public override bool Equals(object other)
		{
		  if (other == this)
		  {
			return true;
		  }

		  if (other is PermissiveOffsetAttributeImpl)
		  {
			PermissiveOffsetAttributeImpl o = (PermissiveOffsetAttributeImpl) other;
			return o.Start == Start && o.End == End;
		  }

		  return false;
		}

		public override int HashCode()
		{
		  return Start + 31 * End;
		}

		public override void CopyTo(AttributeImpl target)
		{
		  OffsetAttribute t = (OffsetAttribute) target;
		  t.SetOffset(Start, End);
		}

	  }

	  // TODO: use CannedTokenStream?
	  protected internal class RandomTokenStream : TokenStream
	  {
		  private readonly BaseTermVectorsFormatTestCase OuterInstance;


		internal readonly string[] Terms;
		internal readonly BytesRef[] TermBytes;
		internal readonly int[] PositionsIncrements;
		internal readonly int[] Positions;
		internal readonly int[] StartOffsets, EndOffsets;
		internal readonly BytesRef[] Payloads;

		internal readonly IDictionary<string, int?> Freqs;
		internal readonly IDictionary<int?, Set<int?>> PositionToTerms;
		internal readonly IDictionary<int?, Set<int?>> StartOffsetToTerms;

		internal readonly CharTermAttribute TermAtt;
		internal readonly PositionIncrementAttribute PiAtt;
		internal readonly OffsetAttribute OAtt;
		internal readonly PayloadAttribute PAtt;
		internal int i = 0;

		protected internal RandomTokenStream(BaseTermVectorsFormatTestCase outerInstance, int len, string[] sampleTerms, BytesRef[] sampleTermBytes) : this(outerInstance, len, sampleTerms, sampleTermBytes, Rarely())
		{
			this.OuterInstance = outerInstance;
		}

		protected internal RandomTokenStream(BaseTermVectorsFormatTestCase outerInstance, int len, string[] sampleTerms, BytesRef[] sampleTermBytes, bool offsetsGoBackwards)
		{
			this.OuterInstance = outerInstance;
		  Terms = new string[len];
		  TermBytes = new BytesRef[len];
		  PositionsIncrements = new int[len];
		  Positions = new int[len];
		  StartOffsets = new int[len];
		  EndOffsets = new int[len];
		  Payloads = new BytesRef[len];
		  for (int i = 0; i < len; ++i)
		  {
			int o = Random().Next(sampleTerms.Length);
			Terms[i] = sampleTerms[o];
			TermBytes[i] = sampleTermBytes[o];
			PositionsIncrements[i] = TestUtil.NextInt(Random(), i == 0 ? 1 : 0, 10);
			if (offsetsGoBackwards)
			{
			  StartOffsets[i] = Random().Next();
			  EndOffsets[i] = Random().Next();
			}
			else
			{
			  if (i == 0)
			  {
				StartOffsets[i] = TestUtil.NextInt(Random(), 0, 1 << 16);
			  }
			  else
			  {
				StartOffsets[i] = StartOffsets[i - 1] + TestUtil.NextInt(Random(), 0, Rarely() ? 1 << 16 : 20);
			  }
			  EndOffsets[i] = StartOffsets[i] + TestUtil.NextInt(Random(), 0, Rarely() ? 1 << 10 : 20);
			}
		  }

		  for (int i = 0; i < len; ++i)
		  {
			if (i == 0)
			{
			  Positions[i] = PositionsIncrements[i] - 1;
			}
			else
			{
			  Positions[i] = Positions[i - 1] + PositionsIncrements[i];
			}
		  }
		  if (Rarely())
		  {
			Arrays.fill(Payloads, outerInstance.RandomPayload());
		  }
		  else
		  {
			for (int i = 0; i < len; ++i)
			{
			  Payloads[i] = outerInstance.RandomPayload();
			}
		  }

		  PositionToTerms = new Dictionary<>(len);
		  StartOffsetToTerms = new Dictionary<>(len);
		  for (int i = 0; i < len; ++i)
		  {
			if (!PositionToTerms.ContainsKey(Positions[i]))
			{
			  PositionToTerms[Positions[i]] = new HashSet<int?>(1);
			}
			PositionToTerms[Positions[i]].add(i);
			if (!StartOffsetToTerms.ContainsKey(StartOffsets[i]))
			{
			  StartOffsetToTerms[StartOffsets[i]] = new HashSet<int?>(1);
			}
			StartOffsetToTerms[StartOffsets[i]].add(i);
		  }

		  Freqs = new Dictionary<>();
		  foreach (string term in Terms)
		  {
			if (Freqs.ContainsKey(term))
			{
			  Freqs[term] = Freqs[term] + 1;
			}
			else
			{
			  Freqs[term] = 1;
			}
		  }

		  addAttributeImpl(new PermissiveOffsetAttributeImpl());

		  TermAtt = addAttribute(typeof(CharTermAttribute));
		  PiAtt = addAttribute(typeof(PositionIncrementAttribute));
		  OAtt = addAttribute(typeof(OffsetAttribute));
		  PAtt = addAttribute(typeof(PayloadAttribute));
		}

		public virtual bool HasPayloads()
		{
		  foreach (BytesRef payload in Payloads)
		  {
			if (payload != null && payload.length > 0)
			{
			  return true;
			}
		  }
		  return false;
		}

		public override bool IncrementToken()
		{
		  if (i < Terms.Length)
		  {
			TermAtt.setLength(0).append(Terms[i]);
			PiAtt.PositionIncrement = PositionsIncrements[i];
			OAtt.SetOffset(StartOffsets[i], EndOffsets[i]);
			PAtt.Payload = Payloads[i];
			++i;
			return true;
		  }
		  else
		  {
			return false;
		  }
		}

	  }

	  protected internal class RandomDocument
	  {
		  private readonly BaseTermVectorsFormatTestCase OuterInstance;


		internal readonly string[] FieldNames;
		internal readonly FieldType[] FieldTypes;
		internal readonly RandomTokenStream[] TokenStreams;

		protected internal RandomDocument(BaseTermVectorsFormatTestCase outerInstance, int fieldCount, int maxTermCount, Options options, string[] fieldNames, string[] sampleTerms, BytesRef[] sampleTermBytes)
		{
			this.OuterInstance = outerInstance;
		  if (fieldCount > fieldNames.Length)
		  {
			throw new System.ArgumentException();
		  }
		  this.FieldNames = new string[fieldCount];
		  FieldTypes = new FieldType[fieldCount];
		  TokenStreams = new RandomTokenStream[fieldCount];
		  Arrays.fill(FieldTypes, outerInstance.FieldType(options));
		  Set<string> usedFileNames = new HashSet<string>();
		  for (int i = 0; i < fieldCount; ++i)
		  {
			do
			{
			  this.FieldNames[i] = RandomPicks.randomFrom(Random(), fieldNames);
			} while (usedFileNames.contains(this.FieldNames[i]));
			usedFileNames.add(this.FieldNames[i]);
			TokenStreams[i] = new RandomTokenStream(outerInstance, TestUtil.NextInt(Random(), 1, maxTermCount), sampleTerms, sampleTermBytes);
		  }
		}

		public virtual Document ToDocument()
		{
		  Document doc = new Document();
		  for (int i = 0; i < FieldNames.Length; ++i)
		  {
			doc.add(new Field(FieldNames[i], TokenStreams[i], FieldTypes[i]));
		  }
		  return doc;
		}

	  }

	  protected internal class RandomDocumentFactory
	  {
		  private readonly BaseTermVectorsFormatTestCase OuterInstance;


		internal readonly string[] FieldNames;
		internal readonly string[] Terms;
		internal readonly BytesRef[] TermBytes;

		protected internal RandomDocumentFactory(BaseTermVectorsFormatTestCase outerInstance, int distinctFieldNames, int disctinctTerms)
		{
			this.OuterInstance = outerInstance;
		  Set<string> fieldNames = new HashSet<string>();
		  while (fieldNames.size() < distinctFieldNames)
		  {
			fieldNames.add(TestUtil.RandomSimpleString(Random()));
			fieldNames.remove("id");
		  }
		  this.FieldNames = fieldNames.toArray(new string[0]);
		  Terms = new string[disctinctTerms];
		  TermBytes = new BytesRef[disctinctTerms];
		  for (int i = 0; i < disctinctTerms; ++i)
		  {
			Terms[i] = TestUtil.RandomRealisticUnicodeString(Random());
			TermBytes[i] = new BytesRef(Terms[i]);
		  }
		}

		public virtual RandomDocument NewDocument(int fieldCount, int maxTermCount, Options options)
		{
		  return new RandomDocument(OuterInstance, fieldCount, maxTermCount, options, FieldNames, Terms, TermBytes);
		}

	  }

	  protected internal virtual void AssertEquals(RandomDocument doc, Fields fields)
	  {
		// compare field names
		AssertEquals(doc == null, fields == null);
		AssertEquals(doc.FieldNames.Length, fields.size());
		Set<string> fields1 = new HashSet<string>();
		Set<string> fields2 = new HashSet<string>();
		for (int i = 0; i < doc.FieldNames.Length; ++i)
		{
		  fields1.add(doc.FieldNames[i]);
		}
		foreach (string field in fields)
		{
		  fields2.add(field);
		}
		AssertEquals(fields1, fields2);

		for (int i = 0; i < doc.FieldNames.Length; ++i)
		{
		  AssertEquals(doc.TokenStreams[i], doc.FieldTypes[i], fields.terms(doc.FieldNames[i]));
		}
	  }

	  protected internal static bool Equals(object o1, object o2)
	  {
		if (o1 == null)
		{
		  return o2 == null;
		}
		else
		{
		  return o1.Equals(o2);
		}
	  }

	  // to test reuse
	  private readonly ThreadLocal<TermsEnum> TermsEnum = new ThreadLocal<TermsEnum>();
	  private readonly ThreadLocal<DocsEnum> DocsEnum = new ThreadLocal<DocsEnum>();
	  private readonly ThreadLocal<DocsAndPositionsEnum> DocsAndPositionsEnum = new ThreadLocal<DocsAndPositionsEnum>();

	  protected internal virtual void AssertEquals(RandomTokenStream tk, FieldType ft, Terms terms)
	  {
		AssertEquals(1, terms.DocCount);
		int termCount = (new HashSet<>(Arrays.asList(tk.Terms))).Count;
		AssertEquals(termCount, terms.size());
		AssertEquals(termCount, terms.SumDocFreq);
		AssertEquals(ft.storeTermVectorPositions(), terms.hasPositions());
		AssertEquals(ft.storeTermVectorOffsets(), terms.hasOffsets());
		AssertEquals(ft.storeTermVectorPayloads() && tk.HasPayloads(), terms.hasPayloads());
		Set<BytesRef> uniqueTerms = new HashSet<BytesRef>();
		foreach (string term in tk.Freqs.Keys)
		{
		  uniqueTerms.add(new BytesRef(term));
		}
		BytesRef[] sortedTerms = uniqueTerms.toArray(new BytesRef[0]);
		Arrays.sort(sortedTerms, terms.Comparator);
		TermsEnum termsEnum = terms.iterator(Random().nextBoolean() ? null : this.TermsEnum.get());
		this.TermsEnum.set(termsEnum);
		for (int i = 0; i < sortedTerms.Length; ++i)
		{
		  BytesRef nextTerm = termsEnum.next();
		  AssertEquals(sortedTerms[i], nextTerm);
		  AssertEquals(sortedTerms[i], termsEnum.term());
		  AssertEquals(1, termsEnum.docFreq());

		  FixedBitSet bits = new FixedBitSet(1);
		  DocsEnum docsEnum = termsEnum.docs(bits, Random().nextBoolean() ? null : this.DocsEnum.get());
		  AssertEquals(DocsEnum.NO_MORE_DOCS, docsEnum.nextDoc());
		  bits.set(0);

		  docsEnum = termsEnum.docs(Random().nextBoolean() ? bits : null, Random().nextBoolean() ? null : docsEnum);
		  Assert.IsNotNull(docsEnum);
		  AssertEquals(0, docsEnum.nextDoc());
		  AssertEquals(0, docsEnum.docID());
		  AssertEquals(tk.Freqs[termsEnum.term().utf8ToString()], (int?) docsEnum.freq());
		  AssertEquals(DocsEnum.NO_MORE_DOCS, docsEnum.nextDoc());
		  this.DocsEnum.set(docsEnum);

		  bits.clear(0);
		  DocsAndPositionsEnum docsAndPositionsEnum = termsEnum.docsAndPositions(bits, Random().nextBoolean() ? null : this.DocsAndPositionsEnum.get());
		  AssertEquals(ft.storeTermVectorOffsets() || ft.storeTermVectorPositions(), docsAndPositionsEnum != null);
		  if (docsAndPositionsEnum != null)
		  {
			AssertEquals(DocsEnum.NO_MORE_DOCS, docsAndPositionsEnum.nextDoc());
		  }
		  bits.set(0);

		  docsAndPositionsEnum = termsEnum.docsAndPositions(Random().nextBoolean() ? bits : null, Random().nextBoolean() ? null : docsAndPositionsEnum);
		  AssertEquals(ft.storeTermVectorOffsets() || ft.storeTermVectorPositions(), docsAndPositionsEnum != null);
		  if (terms.hasPositions() || terms.hasOffsets())
		  {
			AssertEquals(0, docsAndPositionsEnum.nextDoc());
			int freq = docsAndPositionsEnum.freq();
			AssertEquals(tk.Freqs[termsEnum.term().utf8ToString()], (int?) freq);
			if (docsAndPositionsEnum != null)
			{
			  for (int k = 0; k < freq; ++k)
			  {
				int position = docsAndPositionsEnum.nextPosition();
				Set<int?> indexes;
				if (terms.hasPositions())
				{
				  indexes = tk.PositionToTerms[position];
				  Assert.IsNotNull(indexes);
				}
				else
				{
				  indexes = tk.StartOffsetToTerms[docsAndPositionsEnum.StartOffset()];
				  Assert.IsNotNull(indexes);
				}
				if (terms.hasPositions())
				{
				  bool foundPosition = false;
				  foreach (int index in indexes)
				  {
					if (tk.TermBytes[index].Equals(termsEnum.term()) && tk.Positions[index] == position)
					{
					  foundPosition = true;
					  break;
					}
				  }
				  Assert.IsTrue(foundPosition);
				}
				if (terms.hasOffsets())
				{
				  bool foundOffset = false;
				  foreach (int index in indexes)
				  {
					if (tk.TermBytes[index].Equals(termsEnum.term()) && tk.StartOffsets[index] == docsAndPositionsEnum.StartOffset() && tk.EndOffsets[index] == docsAndPositionsEnum.EndOffset())
					{
					  foundOffset = true;
					  break;
					}
				  }
				  Assert.IsTrue(foundOffset);
				}
				if (terms.hasPayloads())
				{
				  bool foundPayload = false;
				  foreach (int index in indexes)
				  {
					if (tk.TermBytes[index].Equals(termsEnum.term()) && Equals(tk.Payloads[index], docsAndPositionsEnum.Payload))
					{
					  foundPayload = true;
					  break;
					}
				  }
				  Assert.IsTrue(foundPayload);
				}
			  }
			  try
			  {
				docsAndPositionsEnum.nextPosition();
				Assert.Fail();
			  }
			  catch (Exception e)
			  {
				// ok
			  }
			  catch (AssertionError e)
			  {
				// ok
			  }
			}
			AssertEquals(DocsEnum.NO_MORE_DOCS, docsAndPositionsEnum.nextDoc());
		  }
		  this.DocsAndPositionsEnum.set(docsAndPositionsEnum);
		}
		assertNull(termsEnum.next());
		for (int i = 0; i < 5; ++i)
		{
		  if (Random().nextBoolean())
		  {
			Assert.IsTrue(termsEnum.seekExact(RandomPicks.randomFrom(Random(), tk.TermBytes)));
		  }
		  else
		  {
			AssertEquals(SeekStatus.FOUND, termsEnum.seekCeil(RandomPicks.randomFrom(Random(), tk.TermBytes)));
		  }
		}
	  }

	  protected internal virtual Document AddId(Document doc, string id)
	  {
		doc.add(new StringField("id", id, Field.Store.NO));
		return doc;
	  }

	  protected internal virtual int DocID(IndexReader reader, string id)
	  {
		return (new IndexSearcher(reader)).search(new TermQuery(new Term("id", id)), 1).scoreDocs[0].doc;
	  }

	  // only one doc with vectors
	  public virtual void TestRareVectors()
	  {
		RandomDocumentFactory docFactory = new RandomDocumentFactory(this, 10, 20);
		foreach (Options options in ValidOptions())
		{
		  int numDocs = AtLeast(200);
		  int docWithVectors = Random().Next(numDocs);
		  Document emptyDoc = new Document();
		  Directory dir = NewDirectory();
		  RandomIndexWriter writer = new RandomIndexWriter(Random(), dir);
		  RandomDocument doc = docFactory.NewDocument(TestUtil.NextInt(Random(), 1, 3), 20, options);
		  for (int i = 0; i < numDocs; ++i)
		  {
			if (i == docWithVectors)
			{
			  writer.AddDocument(AddId(doc.ToDocument(), "42"));
			}
			else
			{
			  writer.AddDocument(emptyDoc);
			}
		  }
		  IndexReader reader = writer.Reader;
		  int docWithVectorsID = DocID(reader, "42");
		  for (int i = 0; i < 10; ++i)
		  {
			int docID = Random().Next(numDocs);
			Fields fields = reader.getTermVectors(docID);
			if (docID == docWithVectorsID)
			{
			  AssertEquals(doc, fields);
			}
			else
			{
			  assertNull(fields);
			}
		  }
		  Fields fields = reader.getTermVectors(docWithVectorsID);
		  AssertEquals(doc, fields);
		  reader.close();
		  writer.Close();
		  dir.close();
		}
	  }

	  public virtual void TestHighFreqs()
	  {
		RandomDocumentFactory docFactory = new RandomDocumentFactory(this, 3, 5);
		foreach (Options options in ValidOptions())
		{
		  if (options == Options.NONE)
		  {
			continue;
		  }
		  Directory dir = NewDirectory();
		  RandomIndexWriter writer = new RandomIndexWriter(Random(), dir);
		  RandomDocument doc = docFactory.NewDocument(TestUtil.NextInt(Random(), 1, 2), AtLeast(20000), options);
		  writer.AddDocument(doc.ToDocument());
		  IndexReader reader = writer.Reader;
		  AssertEquals(doc, reader.getTermVectors(0));
		  reader.close();
		  writer.Close();
		  dir.close();
		}
	  }

	  public virtual void TestLotsOfFields()
	  {
		RandomDocumentFactory docFactory = new RandomDocumentFactory(this, 5000, 10);
		foreach (Options options in ValidOptions())
		{
		  Directory dir = NewDirectory();
		  RandomIndexWriter writer = new RandomIndexWriter(Random(), dir);
		  RandomDocument doc = docFactory.NewDocument(AtLeast(100), 5, options);
		  writer.AddDocument(doc.ToDocument());
		  IndexReader reader = writer.Reader;
		  AssertEquals(doc, reader.getTermVectors(0));
		  reader.close();
		  writer.Close();
		  dir.close();
		}
	  }

	  // different options for the same field
	  public virtual void TestMixedOptions()
	  {
		int numFields = TestUtil.NextInt(Random(), 1, 3);
		RandomDocumentFactory docFactory = new RandomDocumentFactory(this, numFields, 10);
		foreach (Options options1 in ValidOptions())
		{
		  foreach (Options options2 in ValidOptions())
		  {
			if (options1 == options2)
			{
			  continue;
			}
			Directory dir = NewDirectory();
			RandomIndexWriter writer = new RandomIndexWriter(Random(), dir);
			RandomDocument doc1 = docFactory.NewDocument(numFields, 20, options1);
			RandomDocument doc2 = docFactory.NewDocument(numFields, 20, options2);
			writer.AddDocument(AddId(doc1.ToDocument(), "1"));
			writer.AddDocument(AddId(doc2.ToDocument(), "2"));
			IndexReader reader = writer.Reader;
			int doc1ID = DocID(reader, "1");
			AssertEquals(doc1, reader.getTermVectors(doc1ID));
			int doc2ID = DocID(reader, "2");
			AssertEquals(doc2, reader.getTermVectors(doc2ID));
			reader.close();
			writer.Close();
			dir.close();
		  }
		}
	  }

	  public virtual void TestRandom()
	  {
		RandomDocumentFactory docFactory = new RandomDocumentFactory(this, 5, 20);
		int numDocs = AtLeast(100);
		RandomDocument[] docs = new RandomDocument[numDocs];
		for (int i = 0; i < numDocs; ++i)
		{
		  docs[i] = docFactory.NewDocument(TestUtil.NextInt(Random(), 1, 3), TestUtil.NextInt(Random(), 10, 50), RandomOptions());
		}
		Directory dir = NewDirectory();
		RandomIndexWriter writer = new RandomIndexWriter(Random(), dir);
		for (int i = 0; i < numDocs; ++i)
		{
		  writer.AddDocument(AddId(docs[i].ToDocument(), "" + i));
		}
		IndexReader reader = writer.Reader;
		for (int i = 0; i < numDocs; ++i)
		{
		  int docID = DocID(reader, "" + i);
		  AssertEquals(docs[i], reader.getTermVectors(docID));
		}
		reader.close();
		writer.Close();
		dir.close();
	  }

	  public virtual void TestMerge()
	  {
		RandomDocumentFactory docFactory = new RandomDocumentFactory(this, 5, 20);
		int numDocs = AtLeast(100);
		int numDeletes = Random().Next(numDocs);
		Set<int?> deletes = new HashSet<int?>();
		while (deletes.size() < numDeletes)
		{
		  deletes.add(Random().Next(numDocs));
		}
		foreach (Options options in ValidOptions())
		{
		  RandomDocument[] docs = new RandomDocument[numDocs];
		  for (int i = 0; i < numDocs; ++i)
		  {
			docs[i] = docFactory.NewDocument(TestUtil.NextInt(Random(), 1, 3), AtLeast(10), options);
		  }
		  Directory dir = NewDirectory();
		  RandomIndexWriter writer = new RandomIndexWriter(Random(), dir);
		  for (int i = 0; i < numDocs; ++i)
		  {
			writer.AddDocument(AddId(docs[i].ToDocument(), "" + i));
			if (Rarely())
			{
			  writer.Commit();
			}
		  }
		  foreach (int delete in deletes)
		  {
			writer.DeleteDocuments(new Term("id", "" + delete));
		  }
		  // merge with deletes
		  writer.ForceMerge(1);
		  IndexReader reader = writer.Reader;
		  for (int i = 0; i < numDocs; ++i)
		  {
			if (!deletes.contains(i))
			{
			  int docID = DocID(reader, "" + i);
			  AssertEquals(docs[i], reader.getTermVectors(docID));
			}
		  }
		  reader.close();
		  writer.Close();
		  dir.close();
		}
	  }

	  // run random tests from different threads to make sure the per-thread clones
	  // don't share mutable data
	  public virtual void TestClone()
	  {
		RandomDocumentFactory docFactory = new RandomDocumentFactory(this, 5, 20);
		int numDocs = AtLeast(100);
		foreach (Options options in ValidOptions())
		{
		  RandomDocument[] docs = new RandomDocument[numDocs];
		  for (int i = 0; i < numDocs; ++i)
		  {
			docs[i] = docFactory.NewDocument(TestUtil.NextInt(Random(), 1, 3), AtLeast(10), options);
		  }
		  Directory dir = NewDirectory();
		  RandomIndexWriter writer = new RandomIndexWriter(Random(), dir);
		  for (int i = 0; i < numDocs; ++i)
		  {
			writer.AddDocument(AddId(docs[i].ToDocument(), "" + i));
		  }
		  IndexReader reader = writer.Reader;
		  for (int i = 0; i < numDocs; ++i)
		  {
			int docID = DocID(reader, "" + i);
			AssertEquals(docs[i], reader.getTermVectors(docID));
		  }

		  AtomicReference<Exception> exception = new AtomicReference<Exception>();
		  Thread[] threads = new Thread[2];
		  for (int i = 0; i < threads.Length; ++i)
		  {
			threads[i] = new ThreadAnonymousInnerClassHelper(this, numDocs, docs, reader, exception, i);
		  }
		  foreach (Thread thread in threads)
		  {
			thread.Start();
		  }
		  foreach (Thread thread in threads)
		  {
			thread.Join();
		  }
		  reader.close();
		  writer.Close();
		  dir.close();
		  assertNull("One thread threw an exception", exception.get());
		}
	  }

	  private class ThreadAnonymousInnerClassHelper : System.Threading.Thread
	  {
		  private readonly BaseTermVectorsFormatTestCase OuterInstance;

		  private int NumDocs;
		  private Lucene.Net.Index.BaseTermVectorsFormatTestCase.RandomDocument[] Docs;
		  private IndexReader Reader;
		  private AtomicReference<Exception> Exception;
		  private int i;

		  public ThreadAnonymousInnerClassHelper(BaseTermVectorsFormatTestCase outerInstance, int numDocs, Lucene.Net.Index.BaseTermVectorsFormatTestCase.RandomDocument[] docs, IndexReader reader, AtomicReference<Exception> exception, int i)
		  {
			  this.OuterInstance = outerInstance;
			  this.NumDocs = numDocs;
			  this.Docs = docs;
			  this.Reader = reader;
			  this.Exception = exception;
			  this.i = i;
		  }

		  public override void Run()
		  {
			try
			{
			  for (int i = 0; i < AtLeast(100); ++i)
			  {
				int idx = Random().Next(NumDocs);
				int docID = outerInstance.DocID(Reader, "" + idx);
				outerInstance.AssertEquals(Docs[idx], Reader.getTermVectors(docID));
			  }
			}
			catch (Exception t)
			{
			  Exception.set(t);
			}
		  }
	  }

	}

}