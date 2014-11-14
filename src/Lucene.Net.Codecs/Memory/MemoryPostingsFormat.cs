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

using System;
using System.Diagnostics;
using System.Collections.Generic;
using Lucene.Net.Util.Packed;

namespace Lucene.Net.Codecs.Memory
{

	using DocsAndPositionsEnum = Index.DocsAndPositionsEnum;
	using DocsEnum = Index.DocsEnum;
	using IndexOptions = Index.FieldInfo.IndexOptions;
	using FieldInfo = Index.FieldInfo;
	using FieldInfos = Index.FieldInfos;
	using IndexFileNames = Index.IndexFileNames;
	using SegmentReadState = Index.SegmentReadState;
	using SegmentWriteState = Index.SegmentWriteState;
	using Terms = Index.Terms;
	using TermsEnum = Index.TermsEnum;
	using ByteArrayDataInput = Store.ByteArrayDataInput;
	using ChecksumIndexInput = Store.ChecksumIndexInput;
	using IOContext = Store.IOContext;
	using IndexInput = Store.IndexInput;
	using IndexOutput = Store.IndexOutput;
	using RAMOutputStream = Store.RAMOutputStream;
	using ArrayUtil = Util.ArrayUtil;
	using Bits = Util.Bits;
	using BytesRef = Util.BytesRef;
	using IOUtils = Util.IOUtils;
	using IntsRef = Util.IntsRef;
	using RamUsageEstimator = Util.RamUsageEstimator;
	using Builder = Util.Fst.Builder;
	using ByteSequenceOutputs = Util.Fst.ByteSequenceOutputs;
	using BytesRefFSTEnum = Util.Fst.BytesRefFSTEnum;
	using FST = Util.Fst.FST;
	using Util = Util.Fst.Util;
	using PackedInts = Util.Packed.PackedInts;

	// TODO: would be nice to somehow allow this to act like
	// InstantiatedIndex, by never writing to disk; ie you write
	// to this Codec in RAM only and then when you open a reader
	// it pulls the FST directly from what you wrote w/o going
	// to disk.

	/// <summary>
	/// Stores terms & postings (docs, positions, payloads) in
	///  RAM, using an FST.
	/// 
	/// <para>Note that this codec implements advance as a linear
	/// scan!  This means if you store large fields in here,
	/// queries that rely on advance will (AND BooleanQuery,
	/// PhraseQuery) will be relatively slow!
	/// 
	/// @lucene.experimental 
	/// </para>
	/// </summary>

	// TODO: Maybe name this 'Cached' or something to reflect
	// the reality that it is actually written to disk, but
	// loads itself in ram?
	public sealed class MemoryPostingsFormat : PostingsFormat
	{

	  private readonly bool doPackFST;
	  private readonly float acceptableOverheadRatio;

	  public MemoryPostingsFormat() : this(false, PackedInts.DEFAULT)
	  {
	  }

	  /// <summary>
	  /// Create MemoryPostingsFormat, specifying advanced FST options. </summary>
	  /// <param name="doPackFST"> true if a packed FST should be built.
	  ///        NOTE: packed FSTs are limited to ~2.1 GB of postings. </param>
	  /// <param name="acceptableOverheadRatio"> allowable overhead for packed ints
	  ///        during FST construction. </param>
	  public MemoryPostingsFormat(bool doPackFST, float acceptableOverheadRatio) : base("Memory")
	  {
		this.doPackFST = doPackFST;
		this.acceptableOverheadRatio = acceptableOverheadRatio;
	  }

	  public override string ToString()
	  {
		return "PostingsFormat(name=" + Name + " doPackFST= " + doPackFST + ")";
	  }

	  private sealed class TermsWriter : TermsConsumer
	  {
		  internal bool InstanceFieldsInitialized = false;

		  internal void InitializeInstanceFields()
		  {
			  postingsWriter = new PostingsWriter(this);
		  }

		internal readonly IndexOutput @out;
		internal readonly FieldInfo field;
		internal readonly Builder<BytesRef> builder;
		internal readonly ByteSequenceOutputs outputs = ByteSequenceOutputs.Singleton;
		internal readonly bool doPackFST;
		internal readonly float acceptableOverheadRatio;
		internal int termCount;

		public TermsWriter(IndexOutput @out, FieldInfo field, bool doPackFST, float acceptableOverheadRatio)
		{
			if (!InstanceFieldsInitialized)
			{
				InitializeInstanceFields();
				InstanceFieldsInitialized = true;
			}
		  this.@out = @out;
		  this.field = field;
		  this.doPackFST = doPackFST;
		  this.acceptableOverheadRatio = acceptableOverheadRatio;
		  builder = new Builder<BytesRef>(FST.INPUT_TYPE.BYTE1, 0, 0, true, true, int.MaxValue, outputs, null, doPackFST, acceptableOverheadRatio, true, 15);
		}

		private class PostingsWriter : PostingsConsumer
		{
			private readonly MemoryPostingsFormat.TermsWriter outerInstance;

			public PostingsWriter(MemoryPostingsFormat.TermsWriter outerInstance)
			{
				this.outerInstance = outerInstance;
			}

		  internal int lastDocID;
		  internal int lastPos;
		  internal int lastPayloadLen;

		  // NOTE: not private so we don't pay access check at runtime:
		  internal int docCount;
		  internal RAMOutputStream buffer = new RAMOutputStream();

		  internal int lastOffsetLength;
		  internal int lastOffset;

            public override void StartDoc(int docID, int termDocFreq)
		  {
			int delta = docID - lastDocID;
			Debug.Assert(docID == 0 || delta > 0);
			lastDocID = docID;
			docCount++;

			if (outerInstance.field.FieldIndexOptions == IndexOptions.DOCS_ONLY)
			{
			  buffer.WriteVInt(delta);
			}
			else if (termDocFreq == 1)
			{
			  buffer.WriteVInt((delta << 1) | 1);
			}
			else
			{
			  buffer.WriteVInt(delta << 1);
			  Debug.Assert(termDocFreq > 0);
			  buffer.WriteVInt(termDocFreq);
			}

			lastPos = 0;
			lastOffset = 0;
		  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public void addPosition(int pos, util.BytesRef payload, int startOffset, int endOffset) throws java.io.IOException
		  public override void addPosition(int pos, BytesRef payload, int startOffset, int endOffset)
		  {
			Debug.Assert(payload == null || outerInstance.field.hasPayloads());

			//System.out.println("      addPos pos=" + pos + " payload=" + payload);

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int delta = pos - lastPos;
			int delta = pos - lastPos;
			Debug.Assert(delta >= 0);
			lastPos = pos;

			int payloadLen = 0;

			if (outerInstance.field.hasPayloads())
			{
			  payloadLen = payload == null ? 0 : payload.length;
			  if (payloadLen != lastPayloadLen)
			  {
				lastPayloadLen = payloadLen;
				buffer.WriteVInt((delta << 1) | 1);
				buffer.WriteVInt(payloadLen);
			  }
			  else
			  {
				buffer.WriteVInt(delta << 1);
			  }
			}
			else
			{
			  buffer.WriteVInt(delta);
			}

			if (outerInstance.field.IndexOptions.compareTo(IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS) >= 0)
			{
			  // don't use startOffset - lastEndOffset, because this creates lots of negative vints for synonyms,
			  // and the numbers aren't that much smaller anyways.
			  int offsetDelta = startOffset - lastOffset;
			  int offsetLength = endOffset - startOffset;
			  if (offsetLength != lastOffsetLength)
			  {
				buffer.WriteVInt(offsetDelta << 1 | 1);
				buffer.WriteVInt(offsetLength);
			  }
			  else
			  {
				buffer.WriteVInt(offsetDelta << 1);
			  }
			  lastOffset = startOffset;
			  lastOffsetLength = offsetLength;
			}

			if (payloadLen > 0)
			{
			  buffer.WriteBytes(payload.bytes, payload.offset, payloadLen);
			}
		  }

		  public override void finishDoc()
		  {
		  }

		  public virtual PostingsWriter reset()
		  {
			Debug.Assert(buffer.FilePointer == 0);
			lastDocID = 0;
			docCount = 0;
			lastPayloadLen = 0;
			// force first offset to write its length
			lastOffsetLength = -1;
			return this;
		  }
		}

		internal PostingsWriter postingsWriter;

		public override PostingsConsumer startTerm(BytesRef text)
		{
		  //System.out.println("  startTerm term=" + text.utf8ToString());
		  return postingsWriter.reset();
		}

		internal readonly RAMOutputStream buffer2 = new RAMOutputStream();
		internal readonly BytesRef spare = new BytesRef();
		internal sbyte[] finalBuffer = new sbyte[128];

		internal readonly IntsRef scratchIntsRef = new IntsRef();

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public void finishTerm(util.BytesRef text, codecs.TermStats stats) throws java.io.IOException
		public override void finishTerm(BytesRef text, TermStats stats)
		{

		  Debug.Assert(postingsWriter.docCount == stats.docFreq);

		  Debug.Assert(buffer2.FilePointer == 0);

		  buffer2.WriteVInt(stats.docFreq);
		  if (field.IndexOptions != IndexOptions.DOCS_ONLY)
		  {
			buffer2.WriteVLong(stats.totalTermFreq - stats.docFreq);
		  }
		  int pos = (int) buffer2.FilePointer;
		  buffer2.WriteTo(finalBuffer, 0);
		  buffer2.reset();

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int totalBytes = pos + (int) postingsWriter.buffer.getFilePointer();
		  int totalBytes = pos + (int) postingsWriter.buffer.FilePointer;
		  if (totalBytes > finalBuffer.Length)
		  {
			finalBuffer = ArrayUtil.grow(finalBuffer, totalBytes);
		  }
		  postingsWriter.buffer.WriteTo(finalBuffer, pos);
		  postingsWriter.buffer.reset();

		  spare.bytes = finalBuffer;
		  spare.length = totalBytes;

		  //System.out.println("    finishTerm term=" + text.utf8ToString() + " " + totalBytes + " bytes totalTF=" + stats.totalTermFreq);
		  //for(int i=0;i<totalBytes;i++) {
		  //  System.out.println("      " + Integer.toHexString(finalBuffer[i]&0xFF));
		  //}

		  builder.add(Util.toIntsRef(text, scratchIntsRef), BytesRef.deepCopyOf(spare));
		  termCount++;
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public void finish(long sumTotalTermFreq, long sumDocFreq, int docCount) throws java.io.IOException
		public override void finish(long sumTotalTermFreq, long sumDocFreq, int docCount)
		{
		  if (termCount > 0)
		  {
			@out.WriteVInt(termCount);
			@out.WriteVInt(field.number);
			if (field.IndexOptions != IndexOptions.DOCS_ONLY)
			{
			  @out.WriteVLong(sumTotalTermFreq);
			}
			@out.WriteVLong(sumDocFreq);
			@out.WriteVInt(docCount);
			FST<BytesRef> fst = builder.finish();
			fst.save(@out);
			//System.out.println("finish field=" + field.name + " fp=" + out.getFilePointer());
		  }
		}

		public override IComparer<BytesRef> Comparator
		{
			get
			{
			  return BytesRef.UTF8SortedAsUnicodeComparator;
			}
		}
	  }

	  private static string EXTENSION = "ram";
	  private const string CODEC_NAME = "MemoryPostings";
	  private const int VERSION_START = 0;
	  private const int VERSION_CURRENT = VERSION_START;

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public codecs.FieldsConsumer fieldsConsumer(index.SegmentWriteState state) throws java.io.IOException
	  public override FieldsConsumer fieldsConsumer(SegmentWriteState state)
	  {

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final String fileName = index.IndexFileNames.segmentFileName(state.segmentInfo.name, state.segmentSuffix, EXTENSION);
		string fileName = IndexFileNames.segmentFileName(state.segmentInfo.name, state.segmentSuffix, EXTENSION);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final store.IndexOutput out = state.directory.createOutput(fileName, state.context);
		IndexOutput @out = state.directory.createOutput(fileName, state.context);
		bool success = false;
		try
		{
		  CodecUtil.WriteHeader(@out, CODEC_NAME, VERSION_CURRENT);
		  success = true;
		}
		finally
		{
		  if (!success)
		  {
			IOUtils.CloseWhileHandlingException(@out);
		  }
		}

		return new FieldsConsumerAnonymousInnerClassHelper(this, @out);
	  }

	  private class FieldsConsumerAnonymousInnerClassHelper : FieldsConsumer
	  {
		  private readonly MemoryPostingsFormat outerInstance;

		  private IndexOutput @out;

		  public FieldsConsumerAnonymousInnerClassHelper(MemoryPostingsFormat outerInstance, IndexOutput @out)
		  {
			  this.outerInstance = outerInstance;
			  this.@out = @out;
		  }

		  public override TermsConsumer addField(FieldInfo field)
		  {
			//System.out.println("\naddField field=" + field.name);
			return new TermsWriter(@out, field, outerInstance.doPackFST, outerInstance.acceptableOverheadRatio);
		  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public void close() throws java.io.IOException
		  public override void close()
		  {
			// EOF marker:
			try
			{
			  @out.WriteVInt(0);
			  CodecUtil.WriteFooter(@out);
			}
			finally
			{
			  @out.close();
			}
		  }
	  }

	  private sealed class FSTDocsEnum : DocsEnum
	  {
		  internal bool InstanceFieldsInitialized = false;

		  internal void InitializeInstanceFields()
		  {
			  @in = new ByteArrayDataInput(buffer);
		  }

		internal readonly IndexOptions indexOptions;
		internal readonly bool storePayloads;
		internal sbyte[] buffer = new sbyte[16];
		internal ByteArrayDataInput @in;

		internal Bits liveDocs;
		internal int docUpto;
		internal int docID_Renamed = -1;
		internal int accum;
		internal int freq_Renamed;
		internal int payloadLen;
		internal int numDocs;

		public FSTDocsEnum(IndexOptions indexOptions, bool storePayloads)
		{
			if (!InstanceFieldsInitialized)
			{
				InitializeInstanceFields();
				InstanceFieldsInitialized = true;
			}
		  this.indexOptions = indexOptions;
		  this.storePayloads = storePayloads;
		}

		public bool canReuse(IndexOptions indexOptions, bool storePayloads)
		{
		  return indexOptions == this.indexOptions && storePayloads == this.storePayloads;
		}

		public FSTDocsEnum reset(BytesRef bufferIn, Bits liveDocs, int numDocs)
		{
		  Debug.Assert(numDocs > 0);
		  if (buffer.Length < bufferIn.length)
		  {
			buffer = ArrayUtil.grow(buffer, bufferIn.length);
		  }
		  @in.reset(buffer, 0, bufferIn.length);
		  Array.Copy(bufferIn.bytes, bufferIn.offset, buffer, 0, bufferIn.length);
		  this.liveDocs = liveDocs;
		  docID_Renamed = -1;
		  accum = 0;
		  docUpto = 0;
		  freq_Renamed = 1;
		  payloadLen = 0;
		  this.numDocs = numDocs;
		  return this;
		}

		public override int nextDoc()
		{
		  while (true)
		  {
			//System.out.println("  nextDoc cycle docUpto=" + docUpto + " numDocs=" + numDocs + " fp=" + in.getPosition() + " this=" + this);
			if (docUpto == numDocs)
			{
			  // System.out.println("    END");
			  return docID_Renamed = NO_MORE_DOCS;
			}
			docUpto++;
			if (indexOptions == IndexOptions.DOCS_ONLY)
			{
			  accum += @in.readVInt();
			}
			else
			{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int code = in.readVInt();
			  int code = @in.readVInt();
			  accum += (int)((uint)code >> 1);
			  //System.out.println("  docID=" + accum + " code=" + code);
			  if ((code & 1) != 0)
			  {
				freq_Renamed = 1;
			  }
			  else
			  {
				freq_Renamed = @in.readVInt();
				Debug.Assert(freq_Renamed > 0);
			  }

			  if (indexOptions == IndexOptions.DOCS_AND_FREQS_AND_POSITIONS)
			  {
				// Skip positions/payloads
				for (int posUpto = 0;posUpto < freq_Renamed;posUpto++)
				{
				  if (!storePayloads)
				  {
					@in.readVInt();
				  }
				  else
				  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int posCode = in.readVInt();
					int posCode = @in.readVInt();
					if ((posCode & 1) != 0)
					{
					  payloadLen = @in.readVInt();
					}
					@in.skipBytes(payloadLen);
				  }
				}
			  }
			  else if (indexOptions == IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS)
			  {
				// Skip positions/offsets/payloads
				for (int posUpto = 0;posUpto < freq_Renamed;posUpto++)
				{
				  int posCode = @in.readVInt();
				  if (storePayloads && ((posCode & 1) != 0))
				  {
					payloadLen = @in.readVInt();
				  }
				  if ((@in.readVInt() & 1) != 0)
				  {
					// new offset length
					@in.readVInt();
				  }
				  if (storePayloads)
				  {
					@in.skipBytes(payloadLen);
				  }
				}
			  }
			}

			if (liveDocs == null || liveDocs.get(accum))
			{
			  //System.out.println("    return docID=" + accum + " freq=" + freq);
			  return (docID_Renamed = accum);
			}
		  }
		}

		public override int docID()
		{
		  return docID_Renamed;
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public int advance(int target) throws java.io.IOException
		public override int advance(int target)
		{
		  // TODO: we could make more efficient version, but, it
		  // should be rare that this will matter in practice
		  // since usually apps will not store "big" fields in
		  // this codec!
		  return slowAdvance(target);
		}

		public override int freq()
		{
		  return freq_Renamed;
		}

		public override long cost()
		{
		  return numDocs;
		}
	  }

	  private sealed class FSTDocsAndPositionsEnum : DocsAndPositionsEnum
	  {
		  internal bool InstanceFieldsInitialized = false;

		  internal void InitializeInstanceFields()
		  {
			  @in = new ByteArrayDataInput(buffer);
		  }

		internal readonly bool storePayloads;
		internal sbyte[] buffer = new sbyte[16];
		internal ByteArrayDataInput @in;

		internal Bits liveDocs;
		internal int docUpto;
		internal int docID_Renamed = -1;
		internal int accum;
		internal int freq_Renamed;
		internal int numDocs;
		internal int posPending;
		internal int payloadLength;
		internal readonly bool storeOffsets;
		internal int offsetLength;
		internal int startOffset_Renamed;

		internal int pos;
		internal readonly BytesRef payload = new BytesRef();

		public FSTDocsAndPositionsEnum(bool storePayloads, bool storeOffsets)
		{
			if (!InstanceFieldsInitialized)
			{
				InitializeInstanceFields();
				InstanceFieldsInitialized = true;
			}
		  this.storePayloads = storePayloads;
		  this.storeOffsets = storeOffsets;
		}

		public bool canReuse(bool storePayloads, bool storeOffsets)
		{
		  return storePayloads == this.storePayloads && storeOffsets == this.storeOffsets;
		}

		public FSTDocsAndPositionsEnum reset(BytesRef bufferIn, Bits liveDocs, int numDocs)
		{
		  Debug.Assert(numDocs > 0);

		  // System.out.println("D&P reset bytes this=" + this);
		  // for(int i=bufferIn.offset;i<bufferIn.length;i++) {
		  //   System.out.println("  " + Integer.toHexString(bufferIn.bytes[i]&0xFF));
		  // }

		  if (buffer.Length < bufferIn.length)
		  {
			buffer = ArrayUtil.grow(buffer, bufferIn.length);
		  }
		  @in.reset(buffer, 0, bufferIn.length - bufferIn.offset);
		  Array.Copy(bufferIn.bytes, bufferIn.offset, buffer, 0, bufferIn.length);
		  this.liveDocs = liveDocs;
		  docID_Renamed = -1;
		  accum = 0;
		  docUpto = 0;
		  payload.bytes = buffer;
		  payloadLength = 0;
		  this.numDocs = numDocs;
		  posPending = 0;
		  startOffset_Renamed = storeOffsets ? 0 : -1; // always return -1 if no offsets are stored
		  offsetLength = 0;
		  return this;
		}

		public override int nextDoc()
		{
		  while (posPending > 0)
		  {
			nextPosition();
		  }
		  while (true)
		  {
			//System.out.println("  nextDoc cycle docUpto=" + docUpto + " numDocs=" + numDocs + " fp=" + in.getPosition() + " this=" + this);
			if (docUpto == numDocs)
			{
			  //System.out.println("    END");
			  return docID_Renamed = NO_MORE_DOCS;
			}
			docUpto++;

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int code = in.readVInt();
			int code = @in.readVInt();
			accum += (int)((uint)code >> 1);
			if ((code & 1) != 0)
			{
			  freq_Renamed = 1;
			}
			else
			{
			  freq_Renamed = @in.readVInt();
			  Debug.Assert(freq_Renamed > 0);
			}

			if (liveDocs == null || liveDocs.get(accum))
			{
			  pos = 0;
			  startOffset_Renamed = storeOffsets ? 0 : -1;
			  posPending = freq_Renamed;
			  //System.out.println("    return docID=" + accum + " freq=" + freq);
			  return (docID_Renamed = accum);
			}

			// Skip positions
			for (int posUpto = 0;posUpto < freq_Renamed;posUpto++)
			{
			  if (!storePayloads)
			  {
				@in.readVInt();
			  }
			  else
			  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int skipCode = in.readVInt();
				int skipCode = @in.readVInt();
				if ((skipCode & 1) != 0)
				{
				  payloadLength = @in.readVInt();
				  //System.out.println("    new payloadLen=" + payloadLength);
				}
			  }

			  if (storeOffsets)
			  {
				if ((@in.readVInt() & 1) != 0)
				{
				  // new offset length
				  offsetLength = @in.readVInt();
				}
			  }

			  if (storePayloads)
			  {
				@in.skipBytes(payloadLength);
			  }
			}
		  }
		}

		public override int nextPosition()
		{
		  //System.out.println("    nextPos storePayloads=" + storePayloads + " this=" + this);
		  Debug.Assert(posPending > 0);
		  posPending--;
		  if (!storePayloads)
		  {
			pos += @in.readVInt();
		  }
		  else
		  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int code = in.readVInt();
			int code = @in.readVInt();
			pos += (int)((uint)code >> 1);
			if ((code & 1) != 0)
			{
			  payloadLength = @in.readVInt();
			  //System.out.println("      new payloadLen=" + payloadLength);
			  //} else {
			  //System.out.println("      same payloadLen=" + payloadLength);
			}
		  }

		  if (storeOffsets)
		  {
			int offsetCode = @in.readVInt();
			if ((offsetCode & 1) != 0)
			{
			  // new offset length
			  offsetLength = @in.readVInt();
			}
			startOffset_Renamed += (int)((uint)offsetCode >> 1);
		  }

		  if (storePayloads)
		  {
			payload.offset = @in.Position;
			@in.skipBytes(payloadLength);
			payload.length = payloadLength;
		  }

		  //System.out.println("      pos=" + pos + " payload=" + payload + " fp=" + in.getPosition());
		  return pos;
		}

		public override int startOffset()
		{
		  return startOffset_Renamed;
		}

		public override int endOffset()
		{
		  return startOffset_Renamed + offsetLength;
		}

		public override BytesRef Payload
		{
			get
			{
			  return payload.length > 0 ? payload : null;
			}
		}

		public override int docID()
		{
		  return docID_Renamed;
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public int advance(int target) throws java.io.IOException
		public override int advance(int target)
		{
		  // TODO: we could make more efficient version, but, it
		  // should be rare that this will matter in practice
		  // since usually apps will not store "big" fields in
		  // this codec!
		  return slowAdvance(target);
		}

		public override int freq()
		{
		  return freq_Renamed;
		}

		public override long cost()
		{
		  return numDocs;
		}
	  }

	  private sealed class FSTTermsEnum : TermsEnum
	  {
		internal readonly FieldInfo field;
		internal readonly BytesRefFSTEnum<BytesRef> fstEnum;
		internal readonly ByteArrayDataInput buffer = new ByteArrayDataInput();
		internal bool didDecode;

		internal int docFreq_Renamed;
		internal long totalTermFreq_Renamed;
		internal BytesRefFSTEnum.InputOutput<BytesRef> current;
		internal BytesRef postingsSpare = new BytesRef();

		public FSTTermsEnum(FieldInfo field, FST<BytesRef> fst)
		{
		  this.field = field;
		  fstEnum = new BytesRefFSTEnum<>(fst);
		}

		internal void decodeMetaData()
		{
		  if (!didDecode)
		  {
			buffer.reset(current.output.bytes, current.output.offset, current.output.length);
			docFreq_Renamed = buffer.readVInt();
			if (field.IndexOptions != IndexOptions.DOCS_ONLY)
			{
			  totalTermFreq_Renamed = docFreq_Renamed + buffer.readVLong();
			}
			else
			{
			  totalTermFreq_Renamed = -1;
			}
			postingsSpare.bytes = current.output.bytes;
			postingsSpare.offset = buffer.Position;
			postingsSpare.length = current.output.length - (buffer.Position - current.output.offset);
			//System.out.println("  df=" + docFreq + " totTF=" + totalTermFreq + " offset=" + buffer.getPosition() + " len=" + current.output.length);
			didDecode = true;
		  }
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public boolean seekExact(util.BytesRef text) throws java.io.IOException
		public override bool seekExact(BytesRef text)
		{
		  //System.out.println("te.seekExact text=" + field.name + ":" + text.utf8ToString() + " this=" + this);
		  current = fstEnum.seekExact(text);
		  didDecode = false;
		  return current != null;
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public SeekStatus seekCeil(util.BytesRef text) throws java.io.IOException
		public override SeekStatus seekCeil(BytesRef text)
		{
		  //System.out.println("te.seek text=" + field.name + ":" + text.utf8ToString() + " this=" + this);
		  current = fstEnum.seekCeil(text);
		  if (current == null)
		  {
			return SeekStatus.END;
		  }
		  else
		  {

			// System.out.println("  got term=" + current.input.utf8ToString());
			// for(int i=0;i<current.output.length;i++) {
			//   System.out.println("    " + Integer.toHexString(current.output.bytes[i]&0xFF));
			// }

			didDecode = false;

			if (text.Equals(current.input))
			{
			  //System.out.println("  found!");
			  return SeekStatus.FOUND;
			}
			else
			{
			  //System.out.println("  not found: " + current.input.utf8ToString());
			  return SeekStatus.NOT_FOUND;
			}
		  }
		}

		public override DocsEnum docs(Bits liveDocs, DocsEnum reuse, int flags)
		{
		  decodeMetaData();
		  FSTDocsEnum docsEnum;

		  if (reuse == null || !(reuse is FSTDocsEnum))
		  {
			docsEnum = new FSTDocsEnum(field.IndexOptions, field.hasPayloads());
		  }
		  else
		  {
			docsEnum = (FSTDocsEnum) reuse;
			if (!docsEnum.canReuse(field.IndexOptions, field.hasPayloads()))
			{
			  docsEnum = new FSTDocsEnum(field.IndexOptions, field.hasPayloads());
			}
		  }
		  return docsEnum.reset(this.postingsSpare, liveDocs, docFreq_Renamed);
		}

		public override DocsAndPositionsEnum docsAndPositions(Bits liveDocs, DocsAndPositionsEnum reuse, int flags)
		{

		  bool hasOffsets = field.IndexOptions.compareTo(IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS) >= 0;
		  if (field.IndexOptions.compareTo(IndexOptions.DOCS_AND_FREQS_AND_POSITIONS) < 0)
		  {
			return null;
		  }
		  decodeMetaData();
		  FSTDocsAndPositionsEnum docsAndPositionsEnum;
		  if (reuse == null || !(reuse is FSTDocsAndPositionsEnum))
		  {
			docsAndPositionsEnum = new FSTDocsAndPositionsEnum(field.hasPayloads(), hasOffsets);
		  }
		  else
		  {
			docsAndPositionsEnum = (FSTDocsAndPositionsEnum) reuse;
			if (!docsAndPositionsEnum.canReuse(field.hasPayloads(), hasOffsets))
			{
			  docsAndPositionsEnum = new FSTDocsAndPositionsEnum(field.hasPayloads(), hasOffsets);
			}
		  }
		  //System.out.println("D&P reset this=" + this);
		  return docsAndPositionsEnum.reset(postingsSpare, liveDocs, docFreq_Renamed);
		}

		public override BytesRef term()
		{
		  return current.input;
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public util.BytesRef next() throws java.io.IOException
		public override BytesRef next()
		{
		  //System.out.println("te.next");
		  current = fstEnum.next();
		  if (current == null)
		  {
			//System.out.println("  END");
			return null;
		  }
		  didDecode = false;
		  //System.out.println("  term=" + field.name + ":" + current.input.utf8ToString());
		  return current.input;
		}

		public override int docFreq()
		{
		  decodeMetaData();
		  return docFreq_Renamed;
		}

		public override long totalTermFreq()
		{
		  decodeMetaData();
		  return totalTermFreq_Renamed;
		}

		public override IComparer<BytesRef> Comparator
		{
			get
			{
			  return BytesRef.UTF8SortedAsUnicodeComparator;
			}
		}

		public override void seekExact(long ord)
		{
		  // NOTE: we could add this...
		  throw new System.NotSupportedException();
		}

		public override long ord()
		{
		  // NOTE: we could add this...
		  throw new System.NotSupportedException();
		}
	  }

	  private sealed class TermsReader : Terms
	  {

		internal readonly long sumTotalTermFreq;
		internal readonly long sumDocFreq;
		internal readonly int docCount;
		internal readonly int termCount;
		internal FST<BytesRef> fst;
		internal readonly ByteSequenceOutputs outputs = ByteSequenceOutputs.Singleton;
		internal readonly FieldInfo field;

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public TermsReader(index.FieldInfos fieldInfos, store.IndexInput in, int termCount) throws java.io.IOException
		public TermsReader(FieldInfos fieldInfos, IndexInput @in, int termCount)
		{
		  this.termCount = termCount;
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int fieldNumber = in.readVInt();
		  int fieldNumber = @in.readVInt();
		  field = fieldInfos.fieldInfo(fieldNumber);
		  if (field.IndexOptions != IndexOptions.DOCS_ONLY)
		  {
			sumTotalTermFreq = @in.readVLong();
		  }
		  else
		  {
			sumTotalTermFreq = -1;
		  }
		  sumDocFreq = @in.readVLong();
		  docCount = @in.readVInt();

		  fst = new FST<>(@in, outputs);
		}

		public override long SumTotalTermFreq
		{
			get
			{
			  return sumTotalTermFreq;
			}
		}

		public override long SumDocFreq
		{
			get
			{
			  return sumDocFreq;
			}
		}

		public override int DocCount
		{
			get
			{
			  return docCount;
			}
		}

		public override long size()
		{
		  return termCount;
		}

		public override TermsEnum iterator(TermsEnum reuse)
		{
		  return new FSTTermsEnum(field, fst);
		}

		public override IComparer<BytesRef> Comparator
		{
			get
			{
			  return BytesRef.UTF8SortedAsUnicodeComparator;
			}
		}

		public override bool hasFreqs()
		{
		  return field.IndexOptions.compareTo(IndexOptions.DOCS_AND_FREQS) >= 0;
		}

		public override bool hasOffsets()
		{
		  return field.IndexOptions.compareTo(IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS) >= 0;
		}

		public override bool hasPositions()
		{
		  return field.IndexOptions.compareTo(IndexOptions.DOCS_AND_FREQS_AND_POSITIONS) >= 0;
		}

		public override bool hasPayloads()
		{
		  return field.hasPayloads();
		}

		public long ramBytesUsed()
		{
		  return ((fst != null) ? fst.sizeInBytes() : 0);
		}
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public codecs.FieldsProducer fieldsProducer(index.SegmentReadState state) throws java.io.IOException
	  public override FieldsProducer fieldsProducer(SegmentReadState state)
	  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final String fileName = index.IndexFileNames.segmentFileName(state.segmentInfo.name, state.segmentSuffix, EXTENSION);
		string fileName = IndexFileNames.segmentFileName(state.segmentInfo.name, state.segmentSuffix, EXTENSION);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final store.ChecksumIndexInput in = state.directory.openChecksumInput(fileName, store.IOContext.READONCE);
		ChecksumIndexInput @in = state.directory.openChecksumInput(fileName, IOContext.READONCE);

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final java.util.SortedMap<String,TermsReader> fields = new java.util.TreeMap<>();
		SortedMap<string, TermsReader> fields = new SortedDictionary<string, TermsReader>();

		try
		{
		  CodecUtil.checkHeader(@in, CODEC_NAME, VERSION_START, VERSION_CURRENT);
		  while (true)
		  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int termCount = in.readVInt();
			int termCount = @in.readVInt();
			if (termCount == 0)
			{
			  break;
			}
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final TermsReader termsReader = new TermsReader(state.fieldInfos, in, termCount);
			TermsReader termsReader = new TermsReader(state.fieldInfos, @in, termCount);
			// System.out.println("load field=" + termsReader.field.name);
			fields.put(termsReader.field.name, termsReader);
		  }
		  CodecUtil.checkFooter(@in);
		}
		finally
		{
		  @in.close();
		}

		return new FieldsProducerAnonymousInnerClassHelper(this, fields);
	  }

	  private class FieldsProducerAnonymousInnerClassHelper : FieldsProducer
	  {
	      private readonly Dictionary<string, TermsReader> _fields;

		  public FieldsProducerAnonymousInnerClassHelper(MemoryPostingsFormat outerInstance, Dictionary<string, TermsReader> fields)
		  {
		      _fields = fields;
		  }

		  public override IEnumerator<string> GetEnumerator()
		  {
			return Collections.unmodifiableSet(_fields.Keys).GetEnumerator();
		  }

		  public override Terms Terms(string field)
		  {
			return _fields.Get(field);
		  }

		  public override int Size
		  {
		      get
		      {
		          return _fields.Size ;
		      }
		  }

		  public override void Dispose()
		  {
			// Drop ref to FST:
			foreach (TermsReader termsReader in _fields)
			{
			  termsReader.fst = null;
			}
		  }

		  public override long RamBytesUsed()
		  {
			long sizeInBytes = 0;
			foreach (KeyValuePair<string, TermsReader> entry in _fields.EntrySet())
			{
			  sizeInBytes += (entry.Key.Length * RamUsageEstimator.NUM_BYTES_CHAR);
			  sizeInBytes += entry.Value.ramBytesUsed();
			}
			return sizeInBytes;
		  }

		  public override void CheckIntegrity()
		  {
		  }
	  }
	}

}