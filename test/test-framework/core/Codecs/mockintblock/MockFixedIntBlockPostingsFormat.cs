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
	using FixedIntBlockIndexInput = Lucene.Net.Codecs.intblock.FixedIntBlockIndexInput;
	using FixedIntBlockIndexOutput = Lucene.Net.Codecs.intblock.FixedIntBlockIndexOutput;
	using IntIndexInput = Lucene.Net.Codecs.sep.IntIndexInput;
	using IntIndexOutput = Lucene.Net.Codecs.sep.IntIndexOutput;
	using IntStreamFactory = Lucene.Net.Codecs.sep.IntStreamFactory;
	using SepPostingsReader = Lucene.Net.Codecs.sep.SepPostingsReader;
	using SepPostingsWriter = Lucene.Net.Codecs.sep.SepPostingsWriter;
	using SegmentWriteState = Lucene.Net.Index.SegmentWriteState;
	using SegmentReadState = Lucene.Net.Index.SegmentReadState;
	using Lucene.Net.Store;
	using BytesRef = Lucene.Net.Util.BytesRef;
	using IOUtils = Lucene.Net.Util.IOUtils;

	/// <summary>
	/// A silly test codec to verify core support for fixed
	/// sized int block encoders is working.  The int encoder
	/// used here just writes each block as a series of vInt.
	/// </summary>

	public sealed class MockFixedIntBlockPostingsFormat : PostingsFormat
	{

	  private readonly int BlockSize;

	  public MockFixedIntBlockPostingsFormat() : this(1)
	  {
	  }

	  public MockFixedIntBlockPostingsFormat(int blockSize) : base("MockFixedIntBlock")
	  {
		this.BlockSize = blockSize;
	  }

	  public override string ToString()
	  {
		return Name + "(blockSize=" + BlockSize + ")";
	  }

	  // only for testing
	  public IntStreamFactory IntFactory
	  {
		  get
		  {
			return new MockIntFactory(BlockSize);
		  }
	  }

	  /// <summary>
	  /// Encodes blocks as vInts of a fixed block size.
	  /// </summary>
	  public class MockIntFactory : IntStreamFactory
	  {
		internal readonly int BlockSize;

		public MockIntFactory(int blockSize)
		{
		  this.BlockSize = blockSize;
		}

		public override IntIndexInput OpenInput(Directory dir, string fileName, IOContext context)
		{
		  return new FixedIntBlockIndexInputAnonymousInnerClassHelper(this, dir.OpenInput(fileName, context));
		}

		private class FixedIntBlockIndexInputAnonymousInnerClassHelper : FixedIntBlockIndexInput
		{
			private readonly MockIntFactory OuterInstance;

			public FixedIntBlockIndexInputAnonymousInnerClassHelper(MockIntFactory outerInstance, UnknownType openInput) : base(openInput)
			{
				this.OuterInstance = outerInstance;
			}


			protected internal override BlockReader GetBlockReader(IndexInput @in, int[] buffer)
			{
			  return new BlockReaderAnonymousInnerClassHelper(this, @in, buffer);
			}

			private class BlockReaderAnonymousInnerClassHelper : BlockReader
			{
				private readonly FixedIntBlockIndexInputAnonymousInnerClassHelper OuterInstance;

				private IndexInput @in;
				private int[] Buffer;

				public BlockReaderAnonymousInnerClassHelper(FixedIntBlockIndexInputAnonymousInnerClassHelper outerInstance, IndexInput @in, int[] buffer)
				{
					this.OuterInstance = outerInstance;
					this.@in = @in;
					this.Buffer = buffer;
				}

				public virtual void Seek(long pos)
				{
				}
				public override void ReadBlock()
				{
				  for (int i = 0;i < Buffer.Length;i++)
				  {
					Buffer[i] = @in.ReadVInt();
				  }
				}
			}
		}

		public override IntIndexOutput CreateOutput(Directory dir, string fileName, IOContext context)
		{
		  IndexOutput @out = dir.CreateOutput(fileName, context);
		  bool success = false;
		  try
		  {
			FixedIntBlockIndexOutput ret = new FixedIntBlockIndexOutputAnonymousInnerClassHelper(this, @out, BlockSize);
			success = true;
			return ret;
		  }
		  finally
		  {
			if (!success)
			{
			  IOUtils.CloseWhileHandlingException(@out);
			}
		  }
		}

		private class FixedIntBlockIndexOutputAnonymousInnerClassHelper : FixedIntBlockIndexOutput
		{
			private readonly MockIntFactory OuterInstance;

			private IndexOutput @out;

			public FixedIntBlockIndexOutputAnonymousInnerClassHelper(MockIntFactory outerInstance, IndexOutput @out, int blockSize) : base(@out, blockSize)
			{
				this.OuterInstance = outerInstance;
				this.@out = @out;
			}

			protected internal override void FlushBlock()
			{
			  for (int i = 0;i < buffer.length;i++)
			  {
				@out.WriteVInt(buffer[i]);
			  }
			}
		}
	  }

	  public override FieldsConsumer FieldsConsumer(SegmentWriteState state)
	  {
		PostingsWriterBase postingsWriter = new SepPostingsWriter(state, new MockIntFactory(BlockSize));

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
			postingsWriter.Close();
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
			  postingsWriter.Close();
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
		PostingsReaderBase postingsReader = new SepPostingsReader(state.Directory, state.FieldInfos, state.SegmentInfo, state.Context, new MockIntFactory(BlockSize), state.SegmentSuffix);

		TermsIndexReaderBase indexReader;
		bool success = false;
		try
		{
		  indexReader = new FixedGapTermsIndexReader(state.Directory, state.FieldInfos, state.SegmentInfo.Name, state.TermsIndexDivisor, BytesRef.UTF8SortedAsUnicodeComparator, state.SegmentSuffix, IOContext.DEFAULT);
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
		  FieldsProducer ret = new BlockTermsReader(indexReader, state.Directory, state.FieldInfos, state.SegmentInfo, postingsReader, state.Context, state.SegmentSuffix);
		  success = true;
		  return ret;
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
	}

}