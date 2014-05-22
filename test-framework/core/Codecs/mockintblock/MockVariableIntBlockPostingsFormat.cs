using System.Diagnostics;

namespace Lucene.Net.Codecs.mockintblock
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
	using VariableIntBlockIndexInput = Lucene.Net.Codecs.intblock.VariableIntBlockIndexInput;
	using VariableIntBlockIndexOutput = Lucene.Net.Codecs.intblock.VariableIntBlockIndexOutput;
	using IntIndexInput = Lucene.Net.Codecs.sep.IntIndexInput;
	using IntIndexOutput = Lucene.Net.Codecs.sep.IntIndexOutput;
	using IntStreamFactory = Lucene.Net.Codecs.sep.IntStreamFactory;
	using SepPostingsReader = Lucene.Net.Codecs.sep.SepPostingsReader;
	using SepPostingsWriter = Lucene.Net.Codecs.sep.SepPostingsWriter;
	using SegmentWriteState = Lucene.Net.Index.SegmentWriteState;
	using SegmentReadState = Lucene.Net.Index.SegmentReadState;
	using Directory = Lucene.Net.Store.Directory;
	using IOContext = Lucene.Net.Store.IOContext;
	using IndexInput = Lucene.Net.Store.IndexInput;
	using IndexOutput = Lucene.Net.Store.IndexOutput;
	using BytesRef = Lucene.Net.Util.BytesRef;
	using IOUtils = Lucene.Net.Util.IOUtils;

	/// <summary>
	/// A silly test codec to verify core support for variable
	/// sized int block encoders is working.  The int encoder
	/// used here writes baseBlockSize ints at once, if the first
	/// int is <= 3, else 2*baseBlockSize.
	/// </summary>

	public sealed class MockVariableIntBlockPostingsFormat : PostingsFormat
	{
	  private readonly int BaseBlockSize;

	  public MockVariableIntBlockPostingsFormat() : this(1)
	  {
	  }

	  public MockVariableIntBlockPostingsFormat(int baseBlockSize) : base("MockVariableIntBlock")
	  {
		this.BaseBlockSize = baseBlockSize;
	  }

	  public override string ToString()
	  {
		return Name + "(baseBlockSize=" + BaseBlockSize + ")";
	  }

	  /// <summary>
	  /// If the first value is <= 3, writes baseBlockSize vInts at once,
	  /// otherwise writes 2*baseBlockSize vInts.
	  /// </summary>
	  public class MockIntFactory : IntStreamFactory
	  {

		internal readonly int BaseBlockSize;

		public MockIntFactory(int baseBlockSize)
		{
		  this.BaseBlockSize = baseBlockSize;
		}

		public override IntIndexInput OpenInput(Directory dir, string fileName, IOContext context)
		{
		  IndexInput @in = dir.openInput(fileName, context);
		  int baseBlockSize = @in.readInt();
		  return new VariableIntBlockIndexInputAnonymousInnerClassHelper(this, @in, baseBlockSize);
		}

		private class VariableIntBlockIndexInputAnonymousInnerClassHelper : VariableIntBlockIndexInput
		{
			private readonly MockIntFactory OuterInstance;

			private IndexInput @in;
			private int BaseBlockSize;

			public VariableIntBlockIndexInputAnonymousInnerClassHelper(MockIntFactory outerInstance, IndexInput @in, int baseBlockSize) : base(@in)
			{
				this.OuterInstance = outerInstance;
				this.@in = @in;
				this.BaseBlockSize = baseBlockSize;
			}


			protected internal override BlockReader GetBlockReader(IndexInput @in, int[] buffer)
			{
			  return new BlockReaderAnonymousInnerClassHelper(this, @in, buffer);
			}

			private class BlockReaderAnonymousInnerClassHelper : BlockReader
			{
				private readonly VariableIntBlockIndexInputAnonymousInnerClassHelper OuterInstance;

				private IndexInput @in;
				private int[] Buffer;

				public BlockReaderAnonymousInnerClassHelper(VariableIntBlockIndexInputAnonymousInnerClassHelper outerInstance, IndexInput @in, int[] buffer)
				{
					this.outerInstance = outerInstance;
					this.@in = @in;
					this.Buffer = buffer;
				}

				public override void Seek(long pos)
				{
				}
				public override int ReadBlock()
				{
				  Buffer[0] = @in.readVInt();
				  int count = Buffer[0] <= 3 ? OuterInstance.BaseBlockSize-1 : 2 * OuterInstance.BaseBlockSize-1;
				  Debug.Assert(Buffer.Length >= count, "buffer.length=" + Buffer.Length + " count=" + count);
				  for (int i = 0;i < count;i++)
				  {
					Buffer[i + 1] = @in.readVInt();
				  }
				  return 1 + count;
				}
			}
		}

		public override IntIndexOutput CreateOutput(Directory dir, string fileName, IOContext context)
		{
		  IndexOutput @out = dir.createOutput(fileName, context);
		  bool success = false;
		  try
		  {
			@out.writeInt(BaseBlockSize);
			VariableIntBlockIndexOutput ret = new VariableIntBlockIndexOutputAnonymousInnerClassHelper(this, @out, 2 * BaseBlockSize);
			success = true;
			return ret;
		  }
		  finally
		  {
			if (!success)
			{
			  IOUtils.closeWhileHandlingException(@out);
			}
		  }
		}

		private class VariableIntBlockIndexOutputAnonymousInnerClassHelper : VariableIntBlockIndexOutput
		{
			private readonly MockIntFactory OuterInstance;

			private IndexOutput @out;

			public VariableIntBlockIndexOutputAnonymousInnerClassHelper(MockIntFactory outerInstance, IndexOutput @out, int 2 * baseBlockSize) : base(@out, 2 * outerInstance.BaseBlockSize)
			{
				this.OuterInstance = outerInstance;
				this.@out = @out;
				buffer = new int[2 + 2 * outerInstance.BaseBlockSize];
			}

			internal int pendingCount;
			internal readonly int[] buffer;

			protected internal override int Add(int value)
			{
			  buffer[pendingCount++] = value;
			  // silly variable block length int encoder: if
			  // first value <= 3, we write N vints at once;
			  // else, 2*N
			  int flushAt = buffer[0] <= 3 ? OuterInstance.BaseBlockSize : 2 * OuterInstance.BaseBlockSize;

			  // intentionally be non-causal here:
			  if (pendingCount == flushAt + 1)
			  {
				for (int i = 0;i < flushAt;i++)
				{
				  @out.writeVInt(buffer[i]);
				}
				buffer[0] = buffer[flushAt];
				pendingCount = 1;
				return flushAt;
			  }
			  else
			  {
				return 0;
			  }
			}
		}
	  }

	  public override FieldsConsumer FieldsConsumer(SegmentWriteState state)
	  {
		PostingsWriterBase postingsWriter = new SepPostingsWriter(state, new MockIntFactory(BaseBlockSize));

		bool success = false;
		TermsIndexWriterBase indexWriter;
		try
		{
		  indexWriter = new FixedGapTermsIndexWriter(state);
		  success = true;
		}
		finally
		{
		  if (!success)
		  {
			postingsWriter.close();
		  }
		}

		success = false;
		try
		{
		  FieldsConsumer ret = new BlockTermsWriter(indexWriter, state, postingsWriter);
		  success = true;
		  return ret;
		}
		finally
		{
		  if (!success)
		  {
			try
			{
			  postingsWriter.close();
			}
			finally
			{
			  indexWriter.close();
			}
		  }
		}
	  }

	  public override FieldsProducer FieldsProducer(SegmentReadState state)
	  {
		PostingsReaderBase postingsReader = new SepPostingsReader(state.directory, state.fieldInfos, state.segmentInfo, state.context, new MockIntFactory(BaseBlockSize), state.segmentSuffix);

		TermsIndexReaderBase indexReader;
		bool success = false;
		try
		{
		  indexReader = new FixedGapTermsIndexReader(state.directory, state.fieldInfos, state.segmentInfo.name, state.termsIndexDivisor, BytesRef.UTF8SortedAsUnicodeComparator, state.segmentSuffix, state.context);
		  success = true;
		}
		finally
		{
		  if (!success)
		  {
			postingsReader.close();
		  }
		}

		success = false;
		try
		{
		  FieldsProducer ret = new BlockTermsReader(indexReader, state.directory, state.fieldInfos, state.segmentInfo, postingsReader, state.context, state.segmentSuffix);
		  success = true;
		  return ret;
		}
		finally
		{
		  if (!success)
		  {
			try
			{
			  postingsReader.close();
			}
			finally
			{
			  indexReader.close();
			}
		  }
		}
	  }
	}

}