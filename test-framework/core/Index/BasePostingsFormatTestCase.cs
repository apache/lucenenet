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


	using Codec = Lucene.Net.Codecs.Codec;
	using FieldsConsumer = Lucene.Net.Codecs.FieldsConsumer;
	using FieldsProducer = Lucene.Net.Codecs.FieldsProducer;
	using PostingsConsumer = Lucene.Net.Codecs.PostingsConsumer;
	using TermStats = Lucene.Net.Codecs.TermStats;
	using TermsConsumer = Lucene.Net.Codecs.TermsConsumer;
	using Document = Lucene.Net.Document.Document;
	using Field = Lucene.Net.Document.Field;
	using FieldType = Lucene.Net.Document.FieldType;
	using DocValuesType = Lucene.Net.Index.FieldInfo.DocValuesType_e;
	using IndexOptions = Lucene.Net.Index.FieldInfo.IndexOptions_e;
	using Directory = Lucene.Net.Store.Directory;
	using FlushInfo = Lucene.Net.Store.FlushInfo;
	using IOContext = Lucene.Net.Store.IOContext;
	using Bits = Lucene.Net.Util.Bits;
	using BytesRef = Lucene.Net.Util.BytesRef;
	using Constants = Lucene.Net.Util.Constants;
	using FixedBitSet = Lucene.Net.Util.FixedBitSet;
	using TestUtil = Lucene.Net.Util.TestUtil;
	using AfterClass = org.junit.AfterClass;
	using BeforeClass = org.junit.BeforeClass;

	/// <summary>
	/// Abstract class to do basic tests for a postings format.
	/// NOTE: this test focuses on the postings
	/// (docs/freqs/positions/payloads/offsets) impl, not the
	/// terms dict.  The [stretch] goal is for this test to be
	/// so thorough in testing a new PostingsFormat that if this
	/// test passes, then all Lucene/Solr tests should also pass.  Ie,
	/// if there is some bug in a given PostingsFormat that this
	/// test fails to catch then this test needs to be improved! 
	/// </summary>

	// TODO can we make it easy for testing to pair up a "random terms dict impl" with your postings base format...

	// TODO test when you reuse after skipping a term or two, eg the block reuse case

	/* TODO
	  - threads
	  - assert doc=-1 before any nextDoc
	  - if a PF passes this test but fails other tests then this
	    test has a bug!!
	  - test tricky reuse cases, eg across fields
	  - verify you get null if you pass needFreq/needOffset but
	    they weren't indexed
	*/

	public abstract class BasePostingsFormatTestCase : BaseIndexFileFormatTestCase
	{

	  private enum Option
	  {
		// Sometimes use .advance():
		SKIPPING,

		// Sometimes reuse the Docs/AndPositionsEnum across terms:
		REUSE_ENUMS,

		// Sometimes pass non-null live docs:
		LIVE_DOCS,

		// Sometimes seek to term using previously saved TermState:
		TERM_STATE,

		// Sometimes don't fully consume docs from the enum
		PARTIAL_DOC_CONSUME,

		// Sometimes don't fully consume positions at each doc
		PARTIAL_POS_CONSUME,

		// Sometimes check payloads
		PAYLOADS,

		// Test w/ multiple threads
		THREADS
	  }

	  /// <summary>
	  /// Given the same random seed this always enumerates the
	  ///  same random postings 
	  /// </summary>
	  private class SeedPostings : DocsAndPositionsEnum
	  {
		// Used only to generate docIDs; this way if you pull w/
		// or w/o positions you get the same docID sequence:
		internal readonly Random DocRandom;
		internal readonly Random Random;
		public int DocFreq;
		internal readonly int MaxDocSpacing;
		internal readonly int PayloadSize;
		internal readonly bool FixedPayloads;
		internal readonly Bits LiveDocs;
		internal readonly BytesRef Payload_Renamed;
		internal readonly IndexOptions Options;
		internal readonly bool DoPositions;

		internal int DocID_Renamed;
		internal int Freq_Renamed;
		public int Upto;

		internal int Pos;
		internal int Offset;
		internal int StartOffset_Renamed;
		internal int EndOffset_Renamed;
		internal int PosSpacing;
		internal int PosUpto;

		public SeedPostings(long seed, int minDocFreq, int maxDocFreq, Bits liveDocs, IndexOptions options)
		{
		  Random = new Random(seed);
		  DocRandom = new Random(Random.nextLong());
		  DocFreq = TestUtil.NextInt(Random, minDocFreq, maxDocFreq);
		  this.LiveDocs = liveDocs;

		  // TODO: more realistic to inversely tie this to numDocs:
		  MaxDocSpacing = TestUtil.NextInt(Random, 1, 100);

		  if (Random.Next(10) == 7)
		  {
			// 10% of the time create big payloads:
			PayloadSize = 1 + Random.Next(3);
		  }
		  else
		  {
			PayloadSize = 1 + Random.Next(1);
		  }

		  FixedPayloads = Random.nextBoolean();
		  sbyte[] payloadBytes = new sbyte[PayloadSize];
		  Payload_Renamed = new BytesRef(payloadBytes);
		  this.Options = options;
		  DoPositions = IndexOptions.DOCS_AND_FREQS_AND_POSITIONS.CompareTo(options) <= 0;
		}

		public override int NextDoc()
		{
		  while (true)
		  {
			_nextDoc();
			if (LiveDocs == null || DocID_Renamed == NO_MORE_DOCS || LiveDocs.get(DocID_Renamed))
			{
			  return DocID_Renamed;
			}
		  }
		}

		internal virtual int _nextDoc()
		{
		  // Must consume random:
		  while (PosUpto < Freq_Renamed)
		  {
			NextPosition();
		  }

		  if (Upto < DocFreq)
		  {
			if (Upto == 0 && DocRandom.nextBoolean())
			{
			  // Sometimes index docID = 0
			}
			else if (MaxDocSpacing == 1)
			{
			  DocID_Renamed++;
			}
			else
			{
			  // TODO: sometimes have a biggish gap here!
			  DocID_Renamed += TestUtil.NextInt(DocRandom, 1, MaxDocSpacing);
			}

			if (Random.Next(200) == 17)
			{
			  Freq_Renamed = TestUtil.NextInt(Random, 1, 1000);
			}
			else if (Random.Next(10) == 17)
			{
			  Freq_Renamed = TestUtil.NextInt(Random, 1, 20);
			}
			else
			{
			  Freq_Renamed = TestUtil.NextInt(Random, 1, 4);
			}

			Pos = 0;
			Offset = 0;
			PosUpto = 0;
			PosSpacing = TestUtil.NextInt(Random, 1, 100);

			Upto++;
			return DocID_Renamed;
		  }
		  else
		  {
			return DocID_Renamed = NO_MORE_DOCS;
		  }
		}

		public override int DocID()
		{
		  return DocID_Renamed;
		}

		public override int Freq()
		{
		  return Freq_Renamed;
		}

		public override int NextPosition()
		{
		  if (!DoPositions)
		  {
			PosUpto = Freq_Renamed;
			return 0;
		  }
		  Debug.Assert(PosUpto < Freq_Renamed);

		  if (PosUpto == 0 && Random.nextBoolean())
		  {
			// Sometimes index pos = 0
		  }
		  else if (PosSpacing == 1)
		  {
			Pos++;
		  }
		  else
		  {
			Pos += TestUtil.NextInt(Random, 1, PosSpacing);
		  }

		  if (PayloadSize != 0)
		  {
			if (FixedPayloads)
			{
			  Payload_Renamed.length = PayloadSize;
			  Random.nextBytes(Payload_Renamed.bytes);
			}
			else
			{
			  int thisPayloadSize = Random.Next(PayloadSize);
			  if (thisPayloadSize != 0)
			  {
				Payload_Renamed.length = PayloadSize;
				Random.nextBytes(Payload_Renamed.bytes);
			  }
			  else
			  {
				Payload_Renamed.length = 0;
			  }
			}
		  }
		  else
		  {
			Payload_Renamed.length = 0;
		  }

		  StartOffset_Renamed = Offset + Random.Next(5);
		  EndOffset_Renamed = StartOffset_Renamed + Random.Next(10);
		  Offset = EndOffset_Renamed;

		  PosUpto++;
		  return Pos;
		}

		public override int StartOffset()
		{
		  return StartOffset_Renamed;
		}

		public override int EndOffset()
		{
		  return EndOffset_Renamed;
		}

		public override BytesRef Payload
		{
			get
			{
			  return Payload_Renamed.length == 0 ? null : Payload_Renamed;
			}
		}

		public override int Advance(int target)
		{
		  return slowAdvance(target);
		}

		public override long Cost()
		{
		  return DocFreq;
		}
	  }

	  private class FieldAndTerm
	  {
		internal string Field;
		internal BytesRef Term;

		public FieldAndTerm(string field, BytesRef term)
		{
		  this.Field = field;
		  this.Term = BytesRef.deepCopyOf(term);
		}
	  }

	  // Holds all postings:
	  private static IDictionary<string, IDictionary<BytesRef, long?>> Fields;

	  private static FieldInfos FieldInfos;

	  private static FixedBitSet GlobalLiveDocs;

	  private static IList<FieldAndTerm> AllTerms;
	  private static int MaxDoc;

	  private static long TotalPostings;
	  private static long TotalPayloadBytes;

	  private static SeedPostings GetSeedPostings(string term, long seed, bool withLiveDocs, IndexOptions options)
	  {
		int minDocFreq, maxDocFreq;
		if (term.StartsWith("big_"))
		{
		  minDocFreq = RANDOM_MULTIPLIER * 50000;
		  maxDocFreq = RANDOM_MULTIPLIER * 70000;
		}
		else if (term.StartsWith("medium_"))
		{
		  minDocFreq = RANDOM_MULTIPLIER * 3000;
		  maxDocFreq = RANDOM_MULTIPLIER * 6000;
		}
		else if (term.StartsWith("low_"))
		{
		  minDocFreq = RANDOM_MULTIPLIER;
		  maxDocFreq = RANDOM_MULTIPLIER * 40;
		}
		else
		{
		  minDocFreq = 1;
		  maxDocFreq = 3;
		}

		return new SeedPostings(seed, minDocFreq, maxDocFreq, withLiveDocs ? GlobalLiveDocs : null, options);
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @BeforeClass public static void createPostings() throws java.io.IOException
	  public static void CreatePostings()
	  {
		TotalPostings = 0;
		TotalPayloadBytes = 0;
		Fields = new SortedDictionary<>();

		int numFields = TestUtil.NextInt(Random(), 1, 5);
		if (VERBOSE)
		{
		  Console.WriteLine("TEST: " + numFields + " fields");
		}
		MaxDoc = 0;

		FieldInfo[] fieldInfoArray = new FieldInfo[numFields];
		int fieldUpto = 0;
		while (fieldUpto < numFields)
		{
		  string field = TestUtil.RandomSimpleString(Random());
		  if (Fields.ContainsKey(field))
		  {
			continue;
		  }

		  fieldInfoArray[fieldUpto] = new FieldInfo(field, true, fieldUpto, false, false, true, IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS, null, DocValuesType.NUMERIC, null);
		  fieldUpto++;

		  IDictionary<BytesRef, long?> postings = new SortedDictionary<BytesRef, long?>();
		  Fields[field] = postings;
		  Set<string> seenTerms = new HashSet<string>();

		  int numTerms;
		  if (Random().Next(10) == 7)
		  {
			numTerms = AtLeast(50);
		  }
		  else
		  {
			numTerms = TestUtil.NextInt(Random(), 2, 20);
		  }

		  for (int termUpto = 0;termUpto < numTerms;termUpto++)
		  {
			string term = TestUtil.RandomSimpleString(Random());
			if (seenTerms.contains(term))
			{
			  continue;
			}
			seenTerms.add(term);

			if (TEST_NIGHTLY && termUpto == 0 && fieldUpto == 1)
			{
			  // Make 1 big term:
			  term = "big_" + term;
			}
			else if (termUpto == 1 && fieldUpto == 1)
			{
			  // Make 1 medium term:
			  term = "medium_" + term;
			}
			else if (Random().nextBoolean())
			{
			  // Low freq term:
			  term = "low_" + term;
			}
			else
			{
			  // Very low freq term (don't multiply by RANDOM_MULTIPLIER):
			  term = "verylow_" + term;
			}

			long termSeed = Random().nextLong();
			postings[new BytesRef(term)] = termSeed;

			// NOTE: sort of silly: we enum all the docs just to
			// get the maxDoc
			DocsEnum docsEnum = GetSeedPostings(term, termSeed, false, IndexOptions.DOCS_ONLY);
			int doc;
			int lastDoc = 0;
			while ((doc = docsEnum.nextDoc()) != DocsEnum.NO_MORE_DOCS)
			{
			  lastDoc = doc;
			}
			MaxDoc = Math.Max(lastDoc, MaxDoc);
		  }
		}

		FieldInfos = new FieldInfos(fieldInfoArray);

		// It's the count, not the last docID:
		MaxDoc++;

		GlobalLiveDocs = new FixedBitSet(MaxDoc);
		double liveRatio = Random().NextDouble();
		for (int i = 0;i < MaxDoc;i++)
		{
		  if (Random().NextDouble() <= liveRatio)
		  {
			GlobalLiveDocs.set(i);
		  }
		}

		AllTerms = new List<>();
		foreach (KeyValuePair<string, IDictionary<BytesRef, long?>> fieldEnt in Fields)
		{
		  string field = fieldEnt.Key;
		  foreach (KeyValuePair<BytesRef, long?> termEnt in fieldEnt.Value.entrySet())
		  {
			AllTerms.Add(new FieldAndTerm(field, termEnt.Key));
		  }
		}

		if (VERBOSE)
		{
		  Console.WriteLine("TEST: done init postings; " + AllTerms.Count + " total terms, across " + FieldInfos.size() + " fields");
		}
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @AfterClass public static void afterClass() throws Exception
	  public static void AfterClass()
	  {
		AllTerms = null;
		FieldInfos = null;
		Fields = null;
		GlobalLiveDocs = null;
	  }

	  // TODO maybe instead of @BeforeClass just make a single test run: build postings & index & test it?

	  private FieldInfos CurrentFieldInfos;

	  // maxAllowed = the "highest" we can index, but we will still
	  // randomly index at lower IndexOption
	  private FieldsProducer BuildIndex(Directory dir, IndexOptions maxAllowed, bool allowPayloads, bool alwaysTestMax)
	  {
		Codec codec = Codec;
		SegmentInfo segmentInfo = new SegmentInfo(dir, Constants.LUCENE_MAIN_VERSION, "_0", MaxDoc, false, codec, null);

		int maxIndexOption = Arrays.asList(IndexOptions.values()).IndexOf(maxAllowed);
		if (VERBOSE)
		{
		  Console.WriteLine("\nTEST: now build index");
		}

		int maxIndexOptionNoOffsets = Arrays.asList(IndexOptions.values()).IndexOf(IndexOptions.DOCS_AND_FREQS_AND_POSITIONS);

		// TODO use allowPayloads

		FieldInfo[] newFieldInfoArray = new FieldInfo[Fields.Count];
		for (int fieldUpto = 0;fieldUpto < Fields.Count;fieldUpto++)
		{
		  FieldInfo oldFieldInfo = FieldInfos.fieldInfo(fieldUpto);

		  string pf = TestUtil.GetPostingsFormat(codec, oldFieldInfo.name);
		  int fieldMaxIndexOption;
		  if (DoesntSupportOffsets.contains(pf))
		  {
			fieldMaxIndexOption = Math.Min(maxIndexOptionNoOffsets, maxIndexOption);
		  }
		  else
		  {
			fieldMaxIndexOption = maxIndexOption;
		  }

		  // Randomly picked the IndexOptions to index this
		  // field with:
		  IndexOptions_e indexOptions = IndexOptions.values()[alwaysTestMax ? fieldMaxIndexOption : Random().Next(1 + fieldMaxIndexOption)];
		  bool doPayloads = indexOptions.compareTo(IndexOptions.DOCS_AND_FREQS_AND_POSITIONS) >= 0 && allowPayloads;

		  newFieldInfoArray[fieldUpto] = new FieldInfo(oldFieldInfo.name, true, fieldUpto, false, false, doPayloads, indexOptions, null, DocValuesType.NUMERIC, null);
		}

		FieldInfos newFieldInfos = new FieldInfos(newFieldInfoArray);

		// Estimate that flushed segment size will be 25% of
		// what we use in RAM:
		long bytes = TotalPostings * 8 + TotalPayloadBytes;

		SegmentWriteState writeState = new SegmentWriteState(null, dir, segmentInfo, newFieldInfos, 32, null, new IOContext(new FlushInfo(MaxDoc, bytes)));
		FieldsConsumer fieldsConsumer = codec.postingsFormat().fieldsConsumer(writeState);

		foreach (KeyValuePair<string, IDictionary<BytesRef, long?>> fieldEnt in Fields)
		{
		  string field = fieldEnt.Key;
		  IDictionary<BytesRef, long?> terms = fieldEnt.Value;

		  FieldInfo fieldInfo = newFieldInfos.fieldInfo(field);

		  IndexOptions_e indexOptions = fieldInfo.IndexOptions_e;

		  if (VERBOSE)
		  {
			Console.WriteLine("field=" + field + " indexOtions=" + indexOptions);
		  }

		  bool doFreq = indexOptions.compareTo(IndexOptions.DOCS_AND_FREQS) >= 0;
		  bool doPos = indexOptions.compareTo(IndexOptions.DOCS_AND_FREQS_AND_POSITIONS) >= 0;
		  bool doPayloads = indexOptions.compareTo(IndexOptions.DOCS_AND_FREQS_AND_POSITIONS) >= 0 && allowPayloads;
		  bool doOffsets = indexOptions.compareTo(IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS) >= 0;

		  TermsConsumer termsConsumer = fieldsConsumer.addField(fieldInfo);
		  long sumTotalTF = 0;
		  long sumDF = 0;
		  FixedBitSet seenDocs = new FixedBitSet(MaxDoc);
		  foreach (KeyValuePair<BytesRef, long?> termEnt in terms)
		  {
			BytesRef term = termEnt.Key;
			SeedPostings postings = GetSeedPostings(term.utf8ToString(), termEnt.Value, false, maxAllowed);
			if (VERBOSE)
			{
			  Console.WriteLine("  term=" + field + ":" + term.utf8ToString() + " docFreq=" + postings.DocFreq + " seed=" + termEnt.Value);
			}

			PostingsConsumer postingsConsumer = termsConsumer.startTerm(term);
			long totalTF = 0;
			int docID = 0;
			while ((docID = postings.NextDoc()) != DocsEnum.NO_MORE_DOCS)
			{
			  int freq = postings.Freq();
			  if (VERBOSE)
			  {
				Console.WriteLine("    " + postings.Upto + ": docID=" + docID + " freq=" + postings.Freq_Renamed);
			  }
			  postingsConsumer.startDoc(docID, doFreq ? postings.Freq_Renamed : -1);
			  seenDocs.set(docID);
			  if (doPos)
			  {
				totalTF += postings.Freq_Renamed;
				for (int posUpto = 0;posUpto < freq;posUpto++)
				{
				  int pos = postings.NextPosition();
				  BytesRef payload = postings.Payload;

				  if (VERBOSE)
				  {
					if (doPayloads)
					{
					  Console.WriteLine("      pos=" + pos + " payload=" + (payload == null ? "null" : payload.length + " bytes"));
					}
					else
					{
					  Console.WriteLine("      pos=" + pos);
					}
				  }
				  postingsConsumer.addPosition(pos, doPayloads ? payload : null, doOffsets ? postings.StartOffset() : -1, doOffsets ? postings.EndOffset() : -1);
				}
			  }
			  else if (doFreq)
			  {
				totalTF += freq;
			  }
			  else
			  {
				totalTF++;
			  }
			  postingsConsumer.finishDoc();
			}
			termsConsumer.finishTerm(term, new TermStats(postings.DocFreq, doFreq ? totalTF : -1));
			sumTotalTF += totalTF;
			sumDF += postings.DocFreq;
		  }

		  termsConsumer.finish(doFreq ? sumTotalTF : -1, sumDF, seenDocs.cardinality());
		}

		fieldsConsumer.close();

		if (VERBOSE)
		{
		  Console.WriteLine("TEST: after indexing: files=");
		  foreach (string file in dir.listAll())
		  {
			Console.WriteLine("  " + file + ": " + dir.fileLength(file) + " bytes");
		  }
		}

		CurrentFieldInfos = newFieldInfos;

		SegmentReadState readState = new SegmentReadState(dir, segmentInfo, newFieldInfos, IOContext.READ, 1);

		return codec.postingsFormat().fieldsProducer(readState);
	  }

	  private class ThreadState
	  {
		// Only used with REUSE option:
		public DocsEnum ReuseDocsEnum;
		public DocsAndPositionsEnum ReuseDocsAndPositionsEnum;
	  }

	  private void VerifyEnum(ThreadState threadState, string field, BytesRef term, TermsEnum termsEnum, IndexOptions maxTestOptions, IndexOptions maxIndexOptions, EnumSet<Option> options, bool alwaysTestMax)
							  // Maximum options (docs/freqs/positions/offsets) to test:
	  {

		if (VERBOSE)
		{
		  Console.WriteLine("  verifyEnum: options=" + options + " maxTestOptions=" + maxTestOptions);
		}

		// Make sure TermsEnum really is positioned on the
		// expected term:
		Assert.AreEqual(term, termsEnum.term());

		// 50% of the time time pass liveDocs:
		bool useLiveDocs = options.contains(Option.LIVE_DOCS) && Random().nextBoolean();
		Bits liveDocs;
		if (useLiveDocs)
		{
		  liveDocs = GlobalLiveDocs;
		  if (VERBOSE)
		  {
			Console.WriteLine("  use liveDocs");
		  }
		}
		else
		{
		  liveDocs = null;
		  if (VERBOSE)
		  {
			Console.WriteLine("  no liveDocs");
		  }
		}

		FieldInfo fieldInfo = CurrentFieldInfos.fieldInfo(field);

		// NOTE: can be empty list if we are using liveDocs:
		SeedPostings expected = GetSeedPostings(term.utf8ToString(), Fields[field][term], useLiveDocs, maxIndexOptions);
		Assert.AreEqual(expected.DocFreq, termsEnum.docFreq());

		bool allowFreqs = fieldInfo.IndexOptions_e.compareTo(IndexOptions.DOCS_AND_FREQS) >= 0 && maxTestOptions.compareTo(IndexOptions.DOCS_AND_FREQS) >= 0;
		bool doCheckFreqs = allowFreqs && (alwaysTestMax || Random().Next(3) <= 2);

		bool allowPositions = fieldInfo.IndexOptions_e.compareTo(IndexOptions.DOCS_AND_FREQS_AND_POSITIONS) >= 0 && maxTestOptions.compareTo(IndexOptions.DOCS_AND_FREQS_AND_POSITIONS) >= 0;
		bool doCheckPositions = allowPositions && (alwaysTestMax || Random().Next(3) <= 2);

		bool allowOffsets = fieldInfo.IndexOptions_e.compareTo(IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS) >= 0 && maxTestOptions.compareTo(IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS) >= 0;
		bool doCheckOffsets = allowOffsets && (alwaysTestMax || Random().Next(3) <= 2);

		bool doCheckPayloads = options.contains(Option.PAYLOADS) && allowPositions && fieldInfo.hasPayloads() && (alwaysTestMax || Random().Next(3) <= 2);

		DocsEnum prevDocsEnum = null;

		DocsEnum docsEnum;
		DocsAndPositionsEnum docsAndPositionsEnum;

		if (!doCheckPositions)
		{
		  if (allowPositions && Random().Next(10) == 7)
		  {
			// 10% of the time, even though we will not check positions, pull a DocsAndPositions enum

			if (options.contains(Option.REUSE_ENUMS) && Random().Next(10) < 9)
			{
			  prevDocsEnum = threadState.ReuseDocsAndPositionsEnum;
			}

			int flags = 0;
			if (alwaysTestMax || Random().nextBoolean())
			{
			  flags |= DocsAndPositionsEnum.FLAG_OFFSETS;
			}
			if (alwaysTestMax || Random().nextBoolean())
			{
			  flags |= DocsAndPositionsEnum.FLAG_PAYLOADS;
			}

			if (VERBOSE)
			{
			  Console.WriteLine("  get DocsAndPositionsEnum (but we won't check positions) flags=" + flags);
			}

			threadState.ReuseDocsAndPositionsEnum = termsEnum.docsAndPositions(liveDocs, (DocsAndPositionsEnum) prevDocsEnum, flags);
			docsEnum = threadState.ReuseDocsAndPositionsEnum;
			docsAndPositionsEnum = threadState.ReuseDocsAndPositionsEnum;
		  }
		  else
		  {
			if (VERBOSE)
			{
			  Console.WriteLine("  get DocsEnum");
			}
			if (options.contains(Option.REUSE_ENUMS) && Random().Next(10) < 9)
			{
			  prevDocsEnum = threadState.ReuseDocsEnum;
			}
			threadState.ReuseDocsEnum = termsEnum.docs(liveDocs, prevDocsEnum, doCheckFreqs ? DocsEnum.FLAG_FREQS : DocsEnum.FLAG_NONE);
			docsEnum = threadState.ReuseDocsEnum;
			docsAndPositionsEnum = null;
		  }
		}
		else
		{
		  if (options.contains(Option.REUSE_ENUMS) && Random().Next(10) < 9)
		  {
			prevDocsEnum = threadState.ReuseDocsAndPositionsEnum;
		  }

		  int flags = 0;
		  if (alwaysTestMax || doCheckOffsets || Random().Next(3) == 1)
		  {
			flags |= DocsAndPositionsEnum.FLAG_OFFSETS;
		  }
		  if (alwaysTestMax || doCheckPayloads || Random().Next(3) == 1)
		  {
			flags |= DocsAndPositionsEnum.FLAG_PAYLOADS;
		  }

		  if (VERBOSE)
		  {
			Console.WriteLine("  get DocsAndPositionsEnum flags=" + flags);
		  }

		  threadState.ReuseDocsAndPositionsEnum = termsEnum.docsAndPositions(liveDocs, (DocsAndPositionsEnum) prevDocsEnum, flags);
		  docsEnum = threadState.ReuseDocsAndPositionsEnum;
		  docsAndPositionsEnum = threadState.ReuseDocsAndPositionsEnum;
		}

		Assert.IsNotNull("null DocsEnum", docsEnum);
		int initialDocID = docsEnum.docID();
		Assert.AreEqual("inital docID should be -1" + docsEnum, -1, initialDocID);

		if (VERBOSE)
		{
		  if (prevDocsEnum == null)
		  {
			Console.WriteLine("  got enum=" + docsEnum);
		  }
		  else if (prevDocsEnum == docsEnum)
		  {
			Console.WriteLine("  got reuse enum=" + docsEnum);
		  }
		  else
		  {
			Console.WriteLine("  got enum=" + docsEnum + " (reuse of " + prevDocsEnum + " failed)");
		  }
		}

		// 10% of the time don't consume all docs:
		int stopAt;
		if (!alwaysTestMax && options.contains(Option.PARTIAL_DOC_CONSUME) && expected.DocFreq > 1 && Random().Next(10) == 7)
		{
		  stopAt = Random().Next(expected.DocFreq - 1);
		  if (VERBOSE)
		  {
			Console.WriteLine("  will not consume all docs (" + stopAt + " vs " + expected.DocFreq + ")");
		  }
		}
		else
		{
		  stopAt = expected.DocFreq;
		  if (VERBOSE)
		  {
			Console.WriteLine("  consume all docs");
		  }
		}

		double skipChance = alwaysTestMax ? 0.5 : Random().NextDouble();
		int numSkips = expected.DocFreq < 3 ? 1 : TestUtil.NextInt(Random(), 1, Math.Min(20, expected.DocFreq / 3));
		int skipInc = expected.DocFreq / numSkips;
		int skipDocInc = MaxDoc / numSkips;

		// Sometimes do 100% skipping:
		bool doAllSkipping = options.contains(Option.SKIPPING) && Random().Next(7) == 1;

		double freqAskChance = alwaysTestMax ? 1.0 : Random().NextDouble();
		double payloadCheckChance = alwaysTestMax ? 1.0 : Random().NextDouble();
		double offsetCheckChance = alwaysTestMax ? 1.0 : Random().NextDouble();

		if (VERBOSE)
		{
		  if (options.contains(Option.SKIPPING))
		  {
			Console.WriteLine("  skipChance=" + skipChance + " numSkips=" + numSkips);
		  }
		  else
		  {
			Console.WriteLine("  no skipping");
		  }
		  if (doCheckFreqs)
		  {
			Console.WriteLine("  freqAskChance=" + freqAskChance);
		  }
		  if (doCheckPayloads)
		  {
			Console.WriteLine("  payloadCheckChance=" + payloadCheckChance);
		  }
		  if (doCheckOffsets)
		  {
			Console.WriteLine("  offsetCheckChance=" + offsetCheckChance);
		  }
		}

		while (expected.Upto <= stopAt)
		{
		  if (expected.Upto == stopAt)
		  {
			if (stopAt == expected.DocFreq)
			{
			  Assert.AreEqual("DocsEnum should have ended but didn't", DocsEnum.NO_MORE_DOCS, docsEnum.nextDoc());

			  // Common bug is to forget to set this.doc=NO_MORE_DOCS in the enum!:
			  Assert.AreEqual("DocsEnum should have ended but didn't", DocsEnum.NO_MORE_DOCS, docsEnum.docID());
			}
			break;
		  }

		  if (options.contains(Option.SKIPPING) && (doAllSkipping || Random().NextDouble() <= skipChance))
		  {
			int targetDocID = -1;
			if (expected.Upto < stopAt && Random().nextBoolean())
			{
			  // Pick target we know exists:
			  int skipCount = TestUtil.NextInt(Random(), 1, skipInc);
			  for (int skip = 0;skip < skipCount;skip++)
			  {
				if (expected.NextDoc() == DocsEnum.NO_MORE_DOCS)
				{
				  break;
				}
			  }
			}
			else
			{
			  // Pick random target (might not exist):
			  int skipDocIDs = TestUtil.NextInt(Random(), 1, skipDocInc);
			  if (skipDocIDs > 0)
			  {
				targetDocID = expected.DocID() + skipDocIDs;
				expected.Advance(targetDocID);
			  }
			}

			if (expected.Upto >= stopAt)
			{
			  int target = Random().nextBoolean() ? MaxDoc : DocsEnum.NO_MORE_DOCS;
			  if (VERBOSE)
			  {
				Console.WriteLine("  now advance to end (target=" + target + ")");
			  }
			  Assert.AreEqual("DocsEnum should have ended but didn't", DocsEnum.NO_MORE_DOCS, docsEnum.advance(target));
			  break;
			}
			else
			{
			  if (VERBOSE)
			  {
				if (targetDocID != -1)
				{
				  Console.WriteLine("  now advance to random target=" + targetDocID + " (" + expected.Upto + " of " + stopAt + ") current=" + docsEnum.docID());
				}
				else
				{
				  Console.WriteLine("  now advance to known-exists target=" + expected.DocID() + " (" + expected.Upto + " of " + stopAt + ") current=" + docsEnum.docID());
				}
			  }
			  int docID = docsEnum.advance(targetDocID != -1 ? targetDocID : expected.DocID());
			  Assert.AreEqual("docID is wrong", expected.DocID(), docID);
			}
		  }
		  else
		  {
			expected.NextDoc();
			if (VERBOSE)
			{
			  Console.WriteLine("  now nextDoc to " + expected.DocID() + " (" + expected.Upto + " of " + stopAt + ")");
			}
			int docID = docsEnum.nextDoc();
			Assert.AreEqual("docID is wrong", expected.DocID(), docID);
			if (docID == DocsEnum.NO_MORE_DOCS)
			{
			  break;
			}
		  }

		  if (doCheckFreqs && Random().NextDouble() <= freqAskChance)
		  {
			if (VERBOSE)
			{
			  Console.WriteLine("    now freq()=" + expected.Freq());
			}
			int freq = docsEnum.freq();
			Assert.AreEqual("freq is wrong", expected.Freq(), freq);
		  }

		  if (doCheckPositions)
		  {
			int freq = docsEnum.freq();
			int numPosToConsume;
			if (!alwaysTestMax && options.contains(Option.PARTIAL_POS_CONSUME) && Random().Next(5) == 1)
			{
			  numPosToConsume = Random().Next(freq);
			}
			else
			{
			  numPosToConsume = freq;
			}

			for (int i = 0;i < numPosToConsume;i++)
			{
			  int pos = expected.NextPosition();
			  if (VERBOSE)
			  {
				Console.WriteLine("    now nextPosition to " + pos);
			  }
			  Assert.AreEqual("position is wrong", pos, docsAndPositionsEnum.nextPosition());

			  if (doCheckPayloads)
			  {
				BytesRef expectedPayload = expected.Payload;
				if (Random().NextDouble() <= payloadCheckChance)
				{
				  if (VERBOSE)
				  {
					Console.WriteLine("      now check expectedPayload length=" + (expectedPayload == null ? 0 : expectedPayload.length));
				  }
				  if (expectedPayload == null || expectedPayload.length == 0)
				  {
					assertNull("should not have payload", docsAndPositionsEnum.Payload);
				  }
				  else
				  {
					BytesRef payload = docsAndPositionsEnum.Payload;
					Assert.IsNotNull("should have payload but doesn't", payload);

					Assert.AreEqual("payload length is wrong", expectedPayload.length, payload.length);
					for (int byteUpto = 0;byteUpto < expectedPayload.length;byteUpto++)
					{
					  Assert.AreEqual("payload bytes are wrong", expectedPayload.bytes[expectedPayload.offset + byteUpto], payload.bytes[payload.offset + byteUpto]);
					}

					// make a deep copy
					payload = BytesRef.deepCopyOf(payload);
					Assert.AreEqual("2nd call to getPayload returns something different!", payload, docsAndPositionsEnum.Payload);
				  }
				}
				else
				{
				  if (VERBOSE)
				  {
					Console.WriteLine("      skip check payload length=" + (expectedPayload == null ? 0 : expectedPayload.length));
				  }
				}
			  }

			  if (doCheckOffsets)
			  {
				if (Random().NextDouble() <= offsetCheckChance)
				{
				  if (VERBOSE)
				  {
					Console.WriteLine("      now check offsets: startOff=" + expected.StartOffset() + " endOffset=" + expected.EndOffset());
				  }
				  Assert.AreEqual("startOffset is wrong", expected.StartOffset(), docsAndPositionsEnum.StartOffset());
				  Assert.AreEqual("endOffset is wrong", expected.EndOffset(), docsAndPositionsEnum.EndOffset());
				}
				else
				{
				  if (VERBOSE)
				  {
					Console.WriteLine("      skip check offsets");
				  }
				}
			  }
			  else if (fieldInfo.IndexOptions_e.compareTo(IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS) < 0)
			  {
				if (VERBOSE)
				{
				  Console.WriteLine("      now check offsets are -1");
				}
				Assert.AreEqual("startOffset isn't -1", -1, docsAndPositionsEnum.StartOffset());
				Assert.AreEqual("endOffset isn't -1", -1, docsAndPositionsEnum.EndOffset());
			  }
			}
		  }
		}
	  }

	  private class TestThread : System.Threading.Thread
	  {
		internal Fields FieldsSource;
		internal EnumSet<Option> Options;
		internal IndexOptions MaxIndexOptions;
		internal IndexOptions MaxTestOptions;
		internal bool AlwaysTestMax;
		internal BasePostingsFormatTestCase TestCase;

		public TestThread(BasePostingsFormatTestCase testCase, Fields fieldsSource, EnumSet<Option> options, IndexOptions maxTestOptions, IndexOptions maxIndexOptions, bool alwaysTestMax)
		{
		  this.FieldsSource = fieldsSource;
		  this.Options = options;
		  this.MaxTestOptions = maxTestOptions;
		  this.MaxIndexOptions = maxIndexOptions;
		  this.AlwaysTestMax = alwaysTestMax;
		  this.TestCase = testCase;
		}

		public override void Run()
		{
		  try
		  {
			try
			{
			  TestCase.TestTermsOneThread(FieldsSource, Options, MaxTestOptions, MaxIndexOptions, AlwaysTestMax);
			}
			catch (Exception t)
			{
			  throw new Exception(t);
			}
		  }
		  finally
		  {
			FieldsSource = null;
			TestCase = null;
		  }
		}
	  }

	  private void TestTerms(Fields fieldsSource, EnumSet<Option> options, IndexOptions maxTestOptions, IndexOptions maxIndexOptions, bool alwaysTestMax)
	  {

		if (options.contains(Option.THREADS))
		{
		  int numThreads = TestUtil.NextInt(Random(), 2, 5);
		  Thread[] threads = new Thread[numThreads];
		  for (int threadUpto = 0;threadUpto < numThreads;threadUpto++)
		  {
			threads[threadUpto] = new TestThread(this, fieldsSource, options, maxTestOptions, maxIndexOptions, alwaysTestMax);
			threads[threadUpto].Start();
		  }
		  for (int threadUpto = 0;threadUpto < numThreads;threadUpto++)
		  {
			threads[threadUpto].Join();
		  }
		}
		else
		{
		  TestTermsOneThread(fieldsSource, options, maxTestOptions, maxIndexOptions, alwaysTestMax);
		}
	  }

	  private void TestTermsOneThread(Fields fieldsSource, EnumSet<Option> options, IndexOptions maxTestOptions, IndexOptions maxIndexOptions, bool alwaysTestMax)
	  {

		ThreadState threadState = new ThreadState();

		// Test random terms/fields:
		IList<TermState> termStates = new List<TermState>();
		IList<FieldAndTerm> termStateTerms = new List<FieldAndTerm>();

		Collections.shuffle(AllTerms, Random());
		int upto = 0;
		while (upto < AllTerms.Count)
		{

		  bool useTermState = termStates.Count != 0 && Random().Next(5) == 1;
		  FieldAndTerm fieldAndTerm;
		  TermsEnum termsEnum;

		  TermState termState = null;

		  if (!useTermState)
		  {
			// Seek by random field+term:
			fieldAndTerm = AllTerms[upto++];
			if (VERBOSE)
			{
			  Console.WriteLine("\nTEST: seek to term=" + fieldAndTerm.Field + ":" + fieldAndTerm.Term.utf8ToString());
			}
		  }
		  else
		  {
			// Seek by previous saved TermState
			int idx = Random().Next(termStates.Count);
			fieldAndTerm = termStateTerms[idx];
			if (VERBOSE)
			{
			  Console.WriteLine("\nTEST: seek using TermState to term=" + fieldAndTerm.Field + ":" + fieldAndTerm.Term.utf8ToString());
			}
			termState = termStates[idx];
		  }

		  Terms terms = fieldsSource.terms(fieldAndTerm.Field);
		  Assert.IsNotNull(terms);
		  termsEnum = terms.iterator(null);

		  if (!useTermState)
		  {
			Assert.IsTrue(termsEnum.seekExact(fieldAndTerm.Term));
		  }
		  else
		  {
			termsEnum.seekExact(fieldAndTerm.Term, termState);
		  }

		  bool savedTermState = false;

		  if (options.contains(Option.TERM_STATE) && !useTermState && Random().Next(5) == 1)
		  {
			// Save away this TermState:
			termStates.Add(termsEnum.termState());
			termStateTerms.Add(fieldAndTerm);
			savedTermState = true;
		  }

		  VerifyEnum(threadState, fieldAndTerm.Field, fieldAndTerm.Term, termsEnum, maxTestOptions, maxIndexOptions, options, alwaysTestMax);

		  // Sometimes save term state after pulling the enum:
		  if (options.contains(Option.TERM_STATE) && !useTermState && !savedTermState && Random().Next(5) == 1)
		  {
			// Save away this TermState:
			termStates.Add(termsEnum.termState());
			termStateTerms.Add(fieldAndTerm);
			useTermState = true;
		  }

		  // 10% of the time make sure you can pull another enum
		  // from the same term:
		  if (alwaysTestMax || Random().Next(10) == 7)
		  {
			// Try same term again
			if (VERBOSE)
			{
			  Console.WriteLine("TEST: try enum again on same term");
			}

			VerifyEnum(threadState, fieldAndTerm.Field, fieldAndTerm.Term, termsEnum, maxTestOptions, maxIndexOptions, options, alwaysTestMax);
		  }
		}
	  }

	  private void TestFields(Fields fields)
	  {
		IEnumerator<string> iterator = fields.GetEnumerator();
		while (iterator.MoveNext())
		{
		  iterator.Current;
		  try
		  {
			iterator.remove();
			Assert.Fail("Fields.iterator() allows for removal");
		  }
		  catch (System.NotSupportedException expected)
		  {
			// expected;
		  }
		}
//JAVA TO C# CONVERTER TODO TASK: Java iterators are only converted within the context of 'while' and 'for' loops:
		Assert.IsFalse(iterator.hasNext());
		try
		{
//JAVA TO C# CONVERTER TODO TASK: Java iterators are only converted within the context of 'while' and 'for' loops:
		  iterator.next();
		  Assert.Fail("Fields.iterator() doesn't throw NoSuchElementException when past the end");
		}
		catch (NoSuchElementException expected)
		{
		  // expected
		}
	  }

	  /// <summary>
	  /// Indexes all fields/terms at the specified
	  ///  IndexOptions, and fully tests at that IndexOptions. 
	  /// </summary>
	  private void TestFull(IndexOptions options, bool withPayloads)
	  {
		File path = CreateTempDir("testPostingsFormat.testExact");
		Directory dir = NewFSDirectory(path);

		// TODO test thread safety of buildIndex too
		FieldsProducer fieldsProducer = BuildIndex(dir, options, withPayloads, true);

		TestFields(fieldsProducer);

		IndexOptions_e[] allOptions = IndexOptions.values();
		int maxIndexOption = Arrays.asList(allOptions).IndexOf(options);

		for (int i = 0;i <= maxIndexOption;i++)
		{
		  TestTerms(fieldsProducer, EnumSet.allOf(typeof(Option)), allOptions[i], options, true);
		  if (withPayloads)
		  {
			// If we indexed w/ payloads, also test enums w/o accessing payloads:
			TestTerms(fieldsProducer, EnumSet.complementOf(EnumSet.of(Option.PAYLOADS)), allOptions[i], options, true);
		  }
		}

		fieldsProducer.close();
		dir.close();
		TestUtil.Rm(path);
	  }

	  public virtual void TestDocsOnly()
	  {
		TestFull(IndexOptions.DOCS_ONLY, false);
	  }

	  public virtual void TestDocsAndFreqs()
	  {
		TestFull(IndexOptions.DOCS_AND_FREQS, false);
	  }

	  public virtual void TestDocsAndFreqsAndPositions()
	  {
		TestFull(IndexOptions.DOCS_AND_FREQS_AND_POSITIONS, false);
	  }

	  public virtual void TestDocsAndFreqsAndPositionsAndPayloads()
	  {
		TestFull(IndexOptions.DOCS_AND_FREQS_AND_POSITIONS, true);
	  }

	  public virtual void TestDocsAndFreqsAndPositionsAndOffsets()
	  {
		TestFull(IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS, false);
	  }

	  public virtual void TestDocsAndFreqsAndPositionsAndOffsetsAndPayloads()
	  {
		TestFull(IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS, true);
	  }

	  public virtual void TestRandom()
	  {

		int iters = 5;

		for (int iter = 0;iter < iters;iter++)
		{
		  File path = CreateTempDir("testPostingsFormat");
		  Directory dir = NewFSDirectory(path);

		  bool indexPayloads = Random().nextBoolean();
		  // TODO test thread safety of buildIndex too
		  FieldsProducer fieldsProducer = BuildIndex(dir, IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS, indexPayloads, false);

		  TestFields(fieldsProducer);

		  // NOTE: you can also test "weaker" index options than
		  // you indexed with:
		  TestTerms(fieldsProducer, EnumSet.allOf(typeof(Option)), IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS, IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS, false);

		  fieldsProducer.close();
		  fieldsProducer = null;

		  dir.close();
		  TestUtil.Rm(path);
		}
	  }

	  protected internal override void AddRandomFields(Document doc)
	  {
		foreach (IndexOptions_e opts in IndexOptions.values())
		{
		  string field = "f_" + opts;
		  string pf = TestUtil.GetPostingsFormat(Codec.Default, field);
		  if (opts == IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS && DoesntSupportOffsets.contains(pf))
		  {
			continue;
		  }
		  FieldType ft = new FieldType();
		  ft.IndexOptions = opts;
		  ft.Indexed = true;
		  ft.OmitNorms = true;
		  ft.freeze();
		  int numFields = Random().Next(5);
		  for (int j = 0; j < numFields; ++j)
		  {
			doc.add(new Field("f_" + opts, TestUtil.RandomSimpleString(Random(), 2), ft));
		  }
		}
	  }
	}

}