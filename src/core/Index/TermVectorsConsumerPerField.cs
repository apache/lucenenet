using System;
using System.Diagnostics;

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

	using OffsetAttribute = Lucene.Net.Analysis.tokenattributes.OffsetAttribute;
	using PayloadAttribute = Lucene.Net.Analysis.tokenattributes.PayloadAttribute;
	using TermVectorsWriter = Lucene.Net.Codecs.TermVectorsWriter;
	using ByteBlockPool = Lucene.Net.Util.ByteBlockPool;
	using BytesRef = Lucene.Net.Util.BytesRef;
	using RamUsageEstimator = Lucene.Net.Util.RamUsageEstimator;

	internal sealed class TermVectorsConsumerPerField : TermsHashConsumerPerField
	{

	  internal readonly TermsHashPerField TermsHashPerField;
	  internal readonly TermVectorsConsumer TermsWriter;
	  internal readonly FieldInfo FieldInfo;
	  internal readonly DocumentsWriterPerThread.DocState DocState;
	  internal readonly FieldInvertState FieldState;

	  internal bool DoVectors;
	  internal bool DoVectorPositions;
	  internal bool DoVectorOffsets;
	  internal bool DoVectorPayloads;

	  internal int MaxNumPostings;
	  internal OffsetAttribute OffsetAttribute;
	  internal PayloadAttribute PayloadAttribute;
	  internal bool HasPayloads; // if enabled, and we actually saw any for this field

	  public TermVectorsConsumerPerField(TermsHashPerField termsHashPerField, TermVectorsConsumer termsWriter, FieldInfo fieldInfo)
	  {
		this.TermsHashPerField = termsHashPerField;
		this.TermsWriter = termsWriter;
		this.FieldInfo = fieldInfo;
		DocState = termsHashPerField.DocState;
		FieldState = termsHashPerField.FieldState;
	  }

	  internal override int StreamCount
	  {
		  get
		  {
			return 2;
		  }
	  }

	  internal override bool Start(IndexableField[] fields, int count)
	  {
		DoVectors = false;
		DoVectorPositions = false;
		DoVectorOffsets = false;
		DoVectorPayloads = false;
		HasPayloads = false;

		for (int i = 0;i < count;i++)
		{
		  IndexableField field = fields[i];
		  if (field.FieldType().indexed())
		  {
			if (field.FieldType().storeTermVectors())
			{
			  DoVectors = true;
			  DoVectorPositions |= field.FieldType().storeTermVectorPositions();
			  DoVectorOffsets |= field.FieldType().storeTermVectorOffsets();
			  if (DoVectorPositions)
			  {
				DoVectorPayloads |= field.FieldType().storeTermVectorPayloads();
			  }
			  else if (field.FieldType().storeTermVectorPayloads())
			  {
				// TODO: move this check somewhere else, and impl the other missing ones
				throw new System.ArgumentException("cannot index term vector payloads without term vector positions (field=\"" + field.Name() + "\")");
			  }
			}
			else
			{
			  if (field.FieldType().storeTermVectorOffsets())
			  {
				throw new System.ArgumentException("cannot index term vector offsets when term vectors are not indexed (field=\"" + field.Name() + "\")");
			  }
			  if (field.FieldType().storeTermVectorPositions())
			  {
				throw new System.ArgumentException("cannot index term vector positions when term vectors are not indexed (field=\"" + field.Name() + "\")");
			  }
			  if (field.FieldType().storeTermVectorPayloads())
			  {
				throw new System.ArgumentException("cannot index term vector payloads when term vectors are not indexed (field=\"" + field.Name() + "\")");
			  }
			}
		  }
		  else
		  {
			if (field.FieldType().storeTermVectors())
			{
			  throw new System.ArgumentException("cannot index term vectors when field is not indexed (field=\"" + field.Name() + "\")");
			}
			if (field.FieldType().storeTermVectorOffsets())
			{
			  throw new System.ArgumentException("cannot index term vector offsets when field is not indexed (field=\"" + field.Name() + "\")");
			}
			if (field.FieldType().storeTermVectorPositions())
			{
			  throw new System.ArgumentException("cannot index term vector positions when field is not indexed (field=\"" + field.Name() + "\")");
			}
			if (field.FieldType().storeTermVectorPayloads())
			{
			  throw new System.ArgumentException("cannot index term vector payloads when field is not indexed (field=\"" + field.Name() + "\")");
			}
		  }
		}

		if (DoVectors)
		{
		  TermsWriter.HasVectors = true;
		  if (TermsHashPerField.BytesHash.size() != 0)
		  {
			// Only necessary if previous doc hit a
			// non-aborting exception while writing vectors in
			// this field:
			TermsHashPerField.Reset();
		  }
		}

		// TODO: only if needed for performance
		//perThread.postingsCount = 0;

		return DoVectors;
	  }

	  public void Abort()
	  {
	  }

	  /// <summary>
	  /// Called once per field per document if term vectors
	  ///  are enabled, to write the vectors to
	  ///  RAMOutputStream, which is then quickly flushed to
	  ///  the real term vectors files in the Directory. 	  /// </summary>
	  internal override void Finish()
	  {
		if (!DoVectors || TermsHashPerField.BytesHash.size() == 0)
		{
		  return;
		}

		TermsWriter.AddFieldToFlush(this);
	  }

	  internal void FinishDocument()
	  {
		Debug.Assert(DocState.TestPoint("TermVectorsTermsWriterPerField.finish start"));

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int numPostings = termsHashPerField.bytesHash.size();
		int numPostings = TermsHashPerField.BytesHash.size();

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final Lucene.Net.Util.BytesRef flushTerm = termsWriter.flushTerm;
		BytesRef flushTerm = TermsWriter.FlushTerm;

		Debug.Assert(numPostings >= 0);

		if (numPostings > MaxNumPostings)
		{
		  MaxNumPostings = numPostings;
		}

		// this is called once, after inverting all occurrences
		// of a given field in the doc.  At this point we flush
		// our hash into the DocWriter.

		Debug.Assert(TermsWriter.VectorFieldsInOrder(FieldInfo));

		TermVectorsPostingsArray postings = (TermVectorsPostingsArray) TermsHashPerField.PostingsArray;
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final Lucene.Net.Codecs.TermVectorsWriter tv = termsWriter.writer;
		TermVectorsWriter tv = TermsWriter.Writer;

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int[] termIDs = termsHashPerField.sortPostings(tv.getComparator());
		int[] termIDs = TermsHashPerField.SortPostings(tv.Comparator);

		tv.StartField(FieldInfo, numPostings, DoVectorPositions, DoVectorOffsets, HasPayloads);

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final ByteSliceReader posReader = doVectorPositions ? termsWriter.vectorSliceReaderPos : null;
		ByteSliceReader posReader = DoVectorPositions ? TermsWriter.VectorSliceReaderPos : null;
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final ByteSliceReader offReader = doVectorOffsets ? termsWriter.vectorSliceReaderOff : null;
		ByteSliceReader offReader = DoVectorOffsets ? TermsWriter.VectorSliceReaderOff : null;

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final Lucene.Net.Util.ByteBlockPool termBytePool = termsHashPerField.termBytePool;
		ByteBlockPool termBytePool = TermsHashPerField.TermBytePool;

		for (int j = 0;j < numPostings;j++)
		{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int termID = termIDs[j];
		  int termID = termIDs[j];
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int freq = postings.freqs[termID];
		  int freq = postings.Freqs[termID];

		  // Get BytesRef
		  termBytePool.SetBytesRef(flushTerm, postings.TextStarts[termID]);
		  tv.StartTerm(flushTerm, freq);

		  if (DoVectorPositions || DoVectorOffsets)
		  {
			if (posReader != null)
			{
			  TermsHashPerField.InitReader(posReader, termID, 0);
			}
			if (offReader != null)
			{
			  TermsHashPerField.InitReader(offReader, termID, 1);
			}
			tv.AddProx(freq, posReader, offReader);
		  }
		  tv.FinishTerm();
		}
		tv.FinishField();

		TermsHashPerField.Reset();

		FieldInfo.SetStoreTermVectors();
	  }

	  internal void ShrinkHash()
	  {
		TermsHashPerField.ShrinkHash(MaxNumPostings);
		MaxNumPostings = 0;
	  }

	  internal override void Start(IndexableField f)
	  {
		if (DoVectorOffsets)
		{
		  OffsetAttribute = FieldState.AttributeSource_Renamed.addAttribute(typeof(OffsetAttribute));
		}
		else
		{
		  OffsetAttribute = null;
		}
		if (DoVectorPayloads && FieldState.AttributeSource_Renamed.HasAttribute(typeof(PayloadAttribute)))
		{
		  PayloadAttribute = FieldState.AttributeSource_Renamed.getAttribute(typeof(PayloadAttribute));
		}
		else
		{
		  PayloadAttribute = null;
		}
	  }

	  internal void WriteProx(TermVectorsPostingsArray postings, int termID)
	  {
		if (DoVectorOffsets)
		{
		  int startOffset = FieldState.Offset_Renamed + OffsetAttribute.StartOffset();
		  int endOffset = FieldState.Offset_Renamed + OffsetAttribute.EndOffset();

		  TermsHashPerField.WriteVInt(1, startOffset - postings.LastOffsets[termID]);
		  TermsHashPerField.WriteVInt(1, endOffset - startOffset);
		  postings.LastOffsets[termID] = endOffset;
		}

		if (DoVectorPositions)
		{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final Lucene.Net.Util.BytesRef payload;
		  BytesRef payload;
		  if (PayloadAttribute == null)
		  {
			payload = null;
		  }
		  else
		  {
			payload = PayloadAttribute.Payload;
		  }

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int pos = fieldState.position - postings.lastPositions[termID];
		  int pos = FieldState.Position_Renamed - postings.LastPositions[termID];
		  if (payload != null && payload.Length > 0)
		  {
			TermsHashPerField.WriteVInt(0, (pos << 1) | 1);
			TermsHashPerField.WriteVInt(0, payload.Length);
			TermsHashPerField.WriteBytes(0, payload.Bytes, payload.Offset, payload.Length);
			HasPayloads = true;
		  }
		  else
		  {
			TermsHashPerField.WriteVInt(0, pos << 1);
		  }
		  postings.LastPositions[termID] = FieldState.Position_Renamed;
		}
	  }

	  internal override void NewTerm(int termID)
	  {
		Debug.Assert(DocState.TestPoint("TermVectorsTermsWriterPerField.newTerm start"));
		TermVectorsPostingsArray postings = (TermVectorsPostingsArray) TermsHashPerField.PostingsArray;

		postings.Freqs[termID] = 1;
		postings.LastOffsets[termID] = 0;
		postings.LastPositions[termID] = 0;

		WriteProx(postings, termID);
	  }

	  internal override void AddTerm(int termID)
	  {
		Debug.Assert(DocState.TestPoint("TermVectorsTermsWriterPerField.addTerm start"));
		TermVectorsPostingsArray postings = (TermVectorsPostingsArray) TermsHashPerField.PostingsArray;

		postings.Freqs[termID]++;

		WriteProx(postings, termID);
	  }

	  internal override void SkippingLongTerm()
	  {
	  }
	  internal override ParallelPostingsArray CreatePostingsArray(int size)
	  {
		return new TermVectorsPostingsArray(size);
	  }

	  internal sealed class TermVectorsPostingsArray : ParallelPostingsArray
	  {
		public TermVectorsPostingsArray(int size) : base(size)
		{
		  Freqs = new int[size];
		  LastOffsets = new int[size];
		  LastPositions = new int[size];
		}

		internal int[] Freqs; // How many times this term occurred in the current doc
		internal int[] LastOffsets; // Last offset we saw
		internal int[] LastPositions; // Last position where this term occurred

		internal override ParallelPostingsArray NewInstance(int size)
		{
		  return new TermVectorsPostingsArray(size);
		}

		internal override void CopyTo(ParallelPostingsArray toArray, int numToCopy)
		{
		  Debug.Assert(toArray is TermVectorsPostingsArray);
		  TermVectorsPostingsArray to = (TermVectorsPostingsArray) toArray;

		  base.CopyTo(toArray, numToCopy);

		  Array.Copy(Freqs, 0, to.Freqs, 0, Size);
		  Array.Copy(LastOffsets, 0, to.LastOffsets, 0, Size);
		  Array.Copy(LastPositions, 0, to.LastPositions, 0, Size);
		}

		internal override int BytesPerPosting()
		{
		  return base.BytesPerPosting() + 3 * RamUsageEstimator.NUM_BYTES_INT;
		}
	  }
	}

}