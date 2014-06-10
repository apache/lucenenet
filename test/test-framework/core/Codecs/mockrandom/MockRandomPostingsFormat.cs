using System;
using System.Diagnostics;
using System.Collections.Generic;

namespace Lucene.Net.Codecs.mockrandom
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


	using BlockTermsReader = Lucene.Net.Codecs.blockterms.BlockTermsReader;
	using BlockTermsWriter = Lucene.Net.Codecs.blockterms.BlockTermsWriter;
	using FixedGapTermsIndexReader = Lucene.Net.Codecs.blockterms.FixedGapTermsIndexReader;
	using FixedGapTermsIndexWriter = Lucene.Net.Codecs.blockterms.FixedGapTermsIndexWriter;
	using TermsIndexReaderBase = Lucene.Net.Codecs.blockterms.TermsIndexReaderBase;
	using TermsIndexWriterBase = Lucene.Net.Codecs.blockterms.TermsIndexWriterBase;
	using VariableGapTermsIndexReader = Lucene.Net.Codecs.blockterms.VariableGapTermsIndexReader;
	using VariableGapTermsIndexWriter = Lucene.Net.Codecs.blockterms.VariableGapTermsIndexWriter;
	using Lucene41PostingsReader = Lucene.Net.Codecs.Lucene41.Lucene41PostingsReader;
	using Lucene41PostingsWriter = Lucene.Net.Codecs.Lucene41.Lucene41PostingsWriter;
	using MockFixedIntBlockPostingsFormat = Lucene.Net.Codecs.mockintblock.MockFixedIntBlockPostingsFormat;
	using MockVariableIntBlockPostingsFormat = Lucene.Net.Codecs.mockintblock.MockVariableIntBlockPostingsFormat;
	using MockSingleIntFactory = Lucene.Net.Codecs.mocksep.MockSingleIntFactory;
	using PulsingPostingsReader = Lucene.Net.Codecs.pulsing.PulsingPostingsReader;
	using PulsingPostingsWriter = Lucene.Net.Codecs.pulsing.PulsingPostingsWriter;
	using IntIndexInput = Lucene.Net.Codecs.sep.IntIndexInput;
	using IntIndexOutput = Lucene.Net.Codecs.sep.IntIndexOutput;
	using IntStreamFactory = Lucene.Net.Codecs.sep.IntStreamFactory;
	using SepPostingsReader = Lucene.Net.Codecs.sep.SepPostingsReader;
	using SepPostingsWriter = Lucene.Net.Codecs.sep.SepPostingsWriter;
	using FSTTermsWriter = Lucene.Net.Codecs.memory.FSTTermsWriter;
	using FSTTermsReader = Lucene.Net.Codecs.memory.FSTTermsReader;
	using FSTOrdTermsWriter = Lucene.Net.Codecs.memory.FSTOrdTermsWriter;
	using FSTOrdTermsReader = Lucene.Net.Codecs.memory.FSTOrdTermsReader;
	using FieldInfo = Lucene.Net.Index.FieldInfo;
	using IndexFileNames = Lucene.Net.Index.IndexFileNames;
	using SegmentReadState = Lucene.Net.Index.SegmentReadState;
	using SegmentWriteState = Lucene.Net.Index.SegmentWriteState;
	using Directory = Lucene.Net.Store.Directory;
	using IOContext = Lucene.Net.Store.IOContext;
	using IndexInput = Lucene.Net.Store.IndexInput;
	using IndexOutput = Lucene.Net.Store.IndexOutput;
	using BytesRef = Lucene.Net.Util.BytesRef;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
	using TestUtil = Lucene.Net.Util.TestUtil;

	/// <summary>
	/// Randomly combines terms index impl w/ postings impls.
	/// </summary>

	public sealed class MockRandomPostingsFormat : PostingsFormat
	{
	  private readonly Random SeedRandom;
	  private readonly string SEED_EXT = "sd";

	  public MockRandomPostingsFormat() : this(null)
	  {
		// this ctor should *only* be used at read-time: get NPE if you use it!
	  }

	  public MockRandomPostingsFormat(Random random) : base("MockRandom")
	  {
		if (random == null)
		{
		  this.SeedRandom = new RandomAnonymousInnerClassHelper(this);
		}
		else
		{
		  this.SeedRandom = new Random(random.nextLong());
		}
	  }

	  private class RandomAnonymousInnerClassHelper : Random
	  {
		  private readonly MockRandomPostingsFormat OuterInstance;

		  public RandomAnonymousInnerClassHelper(MockRandomPostingsFormat outerInstance) : base(0L)
		  {
			  this.OuterInstance = outerInstance;
		  }

		  protected internal override int Next(int arg0)
		  {
			throw new InvalidOperationException("Please use MockRandomPostingsFormat(Random)");
		  }
	  }

	  // Chooses random IntStreamFactory depending on file's extension
	  private class MockIntStreamFactory : IntStreamFactory
	  {
		internal readonly int Salt;
		internal readonly IList<IntStreamFactory> Delegates = new List<IntStreamFactory>();

		public MockIntStreamFactory(Random random)
		{
		  Salt = random.Next();
		  Delegates.Add(new MockSingleIntFactory());
		  int blockSize = TestUtil.NextInt(random, 1, 2000);
		  Delegates.Add(new MockFixedIntBlockPostingsFormat.MockIntFactory(blockSize));
		  int baseBlockSize = TestUtil.NextInt(random, 1, 127);
		  Delegates.Add(new MockVariableIntBlockPostingsFormat.MockIntFactory(baseBlockSize));
		  // TODO: others
		}

		internal static string GetExtension(string fileName)
		{
		  int idx = fileName.IndexOf('.');
		  Debug.Assert(idx != -1);
		  return fileName.Substring(idx);
		}

		public override IntIndexInput OpenInput(Directory dir, string fileName, IOContext Context)
		{
		  // Must only use extension, because IW.addIndexes can
		  // rename segment!
		  IntStreamFactory f = Delegates[(Math.Abs(Salt ^ GetExtension(fileName).GetHashCode())) % Delegates.Count];
		  if (LuceneTestCase.VERBOSE)
		  {
			Console.WriteLine("MockRandomCodec: read using int factory " + f + " from fileName=" + fileName);
		  }
		  return f.openInput(dir, fileName, Context);
		}

		public override IntIndexOutput CreateOutput(Directory dir, string fileName, IOContext Context)
		{
		  IntStreamFactory f = Delegates[(Math.Abs(Salt ^ GetExtension(fileName).GetHashCode())) % Delegates.Count];
		  if (LuceneTestCase.VERBOSE)
		  {
			Console.WriteLine("MockRandomCodec: write using int factory " + f + " to fileName=" + fileName);
		  }
		  return f.CreateOutput(dir, fileName, Context);
		}
	  }

	  public override FieldsConsumer FieldsConsumer(SegmentWriteState state)
	  {
		int minSkipInterval;
		if (state.SegmentInfo.DocCount > 1000000)
		{
		  // Test2BPostings can OOME otherwise:
		  minSkipInterval = 3;
		}
		else
		{
		  minSkipInterval = 2;
		}

		// we pull this before the seed intentionally: because its not consumed at runtime
		// (the skipInterval is written into postings header)
		int skipInterval = TestUtil.NextInt(SeedRandom, minSkipInterval, 10);

		if (LuceneTestCase.VERBOSE)
		{
		  Console.WriteLine("MockRandomCodec: skipInterval=" + skipInterval);
		}

		long seed = SeedRandom.nextLong();

		if (LuceneTestCase.VERBOSE)
		{
		  Console.WriteLine("MockRandomCodec: writing to seg=" + state.SegmentInfo.Name + " formatID=" + state.SegmentSuffix + " seed=" + seed);
		}

		string seedFileName = IndexFileNames.SegmentFileName(state.SegmentInfo.Name, state.SegmentSuffix, SEED_EXT);
		IndexOutput @out = state.Directory.CreateOutput(seedFileName, state.Context);
		try
		{
		  @out.WriteLong(seed);
		}
		finally
		{
		  @out.Close();
		}

		Random random = new Random((int)seed);

		random.Next(); // consume a random for buffersize

		PostingsWriterBase postingsWriter;
		if (random.nextBoolean())
		{
		  postingsWriter = new SepPostingsWriter(state, new MockIntStreamFactory(random), skipInterval);
		}
		else
		{
		  if (LuceneTestCase.VERBOSE)
		  {
			Console.WriteLine("MockRandomCodec: writing Standard postings");
		  }
		  // TODO: randomize variables like acceptibleOverHead?!
		  postingsWriter = new Lucene41PostingsWriter(state, skipInterval);
		}

		if (random.nextBoolean())
		{
		  int totTFCutoff = TestUtil.NextInt(random, 1, 20);
		  if (LuceneTestCase.VERBOSE)
		  {
			Console.WriteLine("MockRandomCodec: writing pulsing postings with totTFCutoff=" + totTFCutoff);
		  }
		  postingsWriter = new PulsingPostingsWriter(state, totTFCutoff, postingsWriter);
		}

		FieldsConsumer fields;
		int t1 = random.Next(4);

		if (t1 == 0)
		{
		  bool success = false;
		  try
		  {
			fields = new FSTTermsWriter(state, postingsWriter);
			success = true;
		  }
		  finally
		  {
			if (!success)
			{
			  postingsWriter.Close();
			}
		  }
		}
		else if (t1 == 1)
		{
		  bool success = false;
		  try
		  {
			fields = new FSTOrdTermsWriter(state, postingsWriter);
			success = true;
		  }
		  finally
		  {
			if (!success)
			{
			  postingsWriter.Close();
			}
		  }
		}
		else if (t1 == 2)
		{
		  // Use BlockTree terms dict

		  if (LuceneTestCase.VERBOSE)
		  {
			Console.WriteLine("MockRandomCodec: writing BlockTree terms dict");
		  }

		  // TODO: would be nice to allow 1 but this is very
		  // slow to write
		  int minTermsInBlock = TestUtil.NextInt(random, 2, 100);
		  int maxTermsInBlock = Math.Max(2, (minTermsInBlock - 1) * 2 + random.Next(100));

		  bool success = false;
		  try
		  {
			fields = new BlockTreeTermsWriter(state, postingsWriter, minTermsInBlock, maxTermsInBlock);
			success = true;
		  }
		  finally
		  {
			if (!success)
			{
			  postingsWriter.Close();
			}
		  }
		}
		else
		{

		  if (LuceneTestCase.VERBOSE)
		  {
			Console.WriteLine("MockRandomCodec: writing Block terms dict");
		  }

		  bool success = false;

		  TermsIndexWriterBase indexWriter;
		  try
		  {
			if (random.nextBoolean())
			{
			  state.TermIndexInterval = TestUtil.NextInt(random, 1, 100);
			  if (LuceneTestCase.VERBOSE)
			  {
				Console.WriteLine("MockRandomCodec: fixed-gap terms index (tii=" + state.TermIndexInterval + ")");
			  }
			  indexWriter = new FixedGapTermsIndexWriter(state);
			}
			else
			{
			  VariableGapTermsIndexWriter.IndexTermSelector selector;
			  int n2 = random.Next(3);
			  if (n2 == 0)
			  {
				int tii = TestUtil.NextInt(random, 1, 100);
				selector = new VariableGapTermsIndexWriter.EveryNTermSelector(tii);
			   if (LuceneTestCase.VERBOSE)
			   {
				  Console.WriteLine("MockRandomCodec: variable-gap terms index (tii=" + tii + ")");
			   }
			  }
			  else if (n2 == 1)
			  {
				int docFreqThresh = TestUtil.NextInt(random, 2, 100);
				int tii = TestUtil.NextInt(random, 1, 100);
				selector = new VariableGapTermsIndexWriter.EveryNOrDocFreqTermSelector(docFreqThresh, tii);
			  }
			  else
			  {
				long seed2 = random.nextLong();
				int gap = TestUtil.NextInt(random, 2, 40);
				if (LuceneTestCase.VERBOSE)
				{
				 Console.WriteLine("MockRandomCodec: random-gap terms index (max gap=" + gap + ")");
				}
			   selector = new IndexTermSelectorAnonymousInnerClassHelper(this, seed2, gap);
			  }
			  indexWriter = new VariableGapTermsIndexWriter(state, selector);
			}
			success = true;
		  }
		  finally
		  {
			if (!success)
			{
			  postingsWriter.Close();
			}
		  }

		  success = false;
		  try
		  {
			fields = new BlockTermsWriter(indexWriter, state, postingsWriter);
			success = true;
		  }
		  finally
		  {
			if (!success)
			{
			  try
			  {
				postingsWriter.Close();
			  }
			  finally
			  {
				indexWriter.close();
			  }
			}
		  }
		}

		return fields;
	  }

	  private class IndexTermSelectorAnonymousInnerClassHelper : VariableGapTermsIndexWriter.IndexTermSelector
	  {
		  private readonly MockRandomPostingsFormat OuterInstance;

		  private long Seed2;
		  private int Gap;

		  public IndexTermSelectorAnonymousInnerClassHelper(MockRandomPostingsFormat outerInstance, long seed2, int gap)
		  {
			  this.OuterInstance = outerInstance;
			  this.Seed2 = seed2;
			  this.Gap = gap;
			  rand = new Random((int)seed2);
		  }

		  internal readonly Random rand;

		  public override bool IsIndexTerm(BytesRef term, TermStats stats)
		  {
			return rand.Next(Gap) == Gap / 2;
		  }

		  public override void NewField(FieldInfo fieldInfo)
		  {
		  }
	  }

	  public override FieldsProducer FieldsProducer(SegmentReadState state)
	  {

		string seedFileName = IndexFileNames.SegmentFileName(state.SegmentInfo.Name, state.SegmentSuffix, SEED_EXT);
		IndexInput @in = state.Directory.OpenInput(seedFileName, state.Context);
		long seed = @in.ReadLong();
		if (LuceneTestCase.VERBOSE)
		{
		  Console.WriteLine("MockRandomCodec: reading from seg=" + state.SegmentInfo.Name + " formatID=" + state.SegmentSuffix + " seed=" + seed);
		}
		@in.Close();

		Random random = new Random((int)seed);

		int readBufferSize = TestUtil.NextInt(random, 1, 4096);
		if (LuceneTestCase.VERBOSE)
		{
		  Console.WriteLine("MockRandomCodec: readBufferSize=" + readBufferSize);
		}

		PostingsReaderBase postingsReader;

		if (random.nextBoolean())
		{
		  if (LuceneTestCase.VERBOSE)
		  {
			Console.WriteLine("MockRandomCodec: reading Sep postings");
		  }
		  postingsReader = new SepPostingsReader(state.Directory, state.FieldInfos, state.SegmentInfo, state.Context, new MockIntStreamFactory(random), state.SegmentSuffix);
		}
		else
		{
		  if (LuceneTestCase.VERBOSE)
		  {
			Console.WriteLine("MockRandomCodec: reading Standard postings");
		  }
		  postingsReader = new Lucene41PostingsReader(state.Directory, state.FieldInfos, state.SegmentInfo, state.Context, state.SegmentSuffix);
		}

		if (random.nextBoolean())
		{
		  int totTFCutoff = TestUtil.NextInt(random, 1, 20);
		  if (LuceneTestCase.VERBOSE)
		  {
			Console.WriteLine("MockRandomCodec: reading pulsing postings with totTFCutoff=" + totTFCutoff);
		  }
		  postingsReader = new PulsingPostingsReader(state, postingsReader);
		}

		FieldsProducer fields;
		int t1 = random.Next(4);
		if (t1 == 0)
		{
		  bool success = false;
		  try
		  {
			fields = new FSTTermsReader(state, postingsReader);
			success = true;
		  }
		  finally
		  {
			if (!success)
			{
			  postingsReader.Close();
			}
		  }
		}
		else if (t1 == 1)
		{
		  bool success = false;
		  try
		  {
			fields = new FSTOrdTermsReader(state, postingsReader);
			success = true;
		  }
		  finally
		  {
			if (!success)
			{
			  postingsReader.Close();
			}
		  }
		}
		else if (t1 == 2)
		{
		  // Use BlockTree terms dict
		  if (LuceneTestCase.VERBOSE)
		  {
			Console.WriteLine("MockRandomCodec: reading BlockTree terms dict");
		  }

		  bool success = false;
		  try
		  {
			fields = new BlockTreeTermsReader(state.Directory, state.FieldInfos, state.SegmentInfo, postingsReader, state.Context, state.SegmentSuffix, state.TermsIndexDivisor);
			success = true;
		  }
		  finally
		  {
			if (!success)
			{
			  postingsReader.Close();
			}
		  }
		}
		else
		{

		  if (LuceneTestCase.VERBOSE)
		  {
			Console.WriteLine("MockRandomCodec: reading Block terms dict");
		  }
		  TermsIndexReaderBase indexReader;
		  bool success = false;
		  try
		  {
			bool doFixedGap = random.nextBoolean();

			// randomness diverges from writer, here:
			if (state.TermsIndexDivisor != -1)
			{
			  state.TermsIndexDivisor = TestUtil.NextInt(random, 1, 10);
			}

			if (doFixedGap)
			{
			  // if TermsIndexDivisor is set to -1, we should not touch it. It means a
			  // test explicitly instructed not to load the terms index.
			  if (LuceneTestCase.VERBOSE)
			  {
				Console.WriteLine("MockRandomCodec: fixed-gap terms index (divisor=" + state.TermsIndexDivisor + ")");
			  }
			  indexReader = new FixedGapTermsIndexReader(state.Directory, state.FieldInfos, state.SegmentInfo.Name, state.TermsIndexDivisor, BytesRef.UTF8SortedAsUnicodeComparator, state.SegmentSuffix, state.Context);
			}
			else
			{
			  int n2 = random.Next(3);
			  if (n2 == 1)
			  {
				random.Next();
			  }
			  else if (n2 == 2)
			  {
				random.nextLong();
			  }
			  if (LuceneTestCase.VERBOSE)
			  {
				Console.WriteLine("MockRandomCodec: variable-gap terms index (divisor=" + state.TermsIndexDivisor + ")");
			  }
			  indexReader = new VariableGapTermsIndexReader(state.Directory, state.FieldInfos, state.SegmentInfo.Name, state.TermsIndexDivisor, state.SegmentSuffix, state.Context);

			}

			success = true;
		  }
		  finally
		  {
			if (!success)
			{
			  postingsReader.Close();
			}
		  }

		  success = false;
		  try
		  {
			fields = new BlockTermsReader(indexReader, state.Directory, state.FieldInfos, state.SegmentInfo, postingsReader, state.Context, state.SegmentSuffix);
			success = true;
		  }
		  finally
		  {
			if (!success)
			{
			  try
			  {
				postingsReader.Close();
			  }
			  finally
			  {
				indexReader.close();
			  }
			}
		  }
		}

		return fields;
	  }
	}

}