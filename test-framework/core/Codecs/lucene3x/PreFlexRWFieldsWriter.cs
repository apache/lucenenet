using System.Diagnostics;
using System.Collections.Generic;

namespace Lucene.Net.Codecs.Lucene3x
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


	using CorruptIndexException = Lucene.Net.Index.CorruptIndexException;
	using FieldInfo = Lucene.Net.Index.FieldInfo;
	using IndexOptions = Lucene.Net.Index.FieldInfo.IndexOptions;
	using IndexFileNames = Lucene.Net.Index.IndexFileNames;
	using SegmentWriteState = Lucene.Net.Index.SegmentWriteState;
	using IndexOutput = Lucene.Net.Store.IndexOutput;
	using BytesRef = Lucene.Net.Util.BytesRef;
	using IOUtils = Lucene.Net.Util.IOUtils;

	internal class PreFlexRWFieldsWriter : FieldsConsumer
	{

	  private readonly TermInfosWriter TermsOut;
	  private readonly IndexOutput FreqOut;
	  private readonly IndexOutput ProxOut;
	  private readonly PreFlexRWSkipListWriter SkipListWriter;
	  private readonly int TotalNumDocs;

	  public PreFlexRWFieldsWriter(SegmentWriteState state)
	  {
		TermsOut = new TermInfosWriter(state.directory, state.segmentInfo.name, state.fieldInfos, state.termIndexInterval);

		bool success = false;
		try
		{
		  string freqFile = IndexFileNames.segmentFileName(state.segmentInfo.name, "", Lucene3xPostingsFormat.FREQ_EXTENSION);
		  FreqOut = state.directory.createOutput(freqFile, state.context);
		  TotalNumDocs = state.segmentInfo.DocCount;
		  success = true;
		}
		finally
		{
		  if (!success)
		  {
			IOUtils.closeWhileHandlingException(TermsOut);
		  }
		}

		success = false;
		try
		{
		  if (state.fieldInfos.hasProx())
		  {
			string proxFile = IndexFileNames.segmentFileName(state.segmentInfo.name, "", Lucene3xPostingsFormat.PROX_EXTENSION);
			ProxOut = state.directory.createOutput(proxFile, state.context);
		  }
		  else
		  {
			ProxOut = null;
		  }
		  success = true;
		}
		finally
		{
		  if (!success)
		  {
			IOUtils.closeWhileHandlingException(TermsOut, FreqOut);
		  }
		}

		SkipListWriter = new PreFlexRWSkipListWriter(TermsOut.SkipInterval, TermsOut.MaxSkipLevels, TotalNumDocs, FreqOut, ProxOut);
		//System.out.println("\nw start seg=" + segment);
	  }

	  public override TermsConsumer AddField(FieldInfo field)
	  {
		Debug.Assert(field.number != -1);
		if (field.IndexOptions.compareTo(FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS) >= 0)
		{
		  throw new System.NotSupportedException("this codec cannot index offsets");
		}
		//System.out.println("w field=" + field.name + " storePayload=" + field.storePayloads + " number=" + field.number);
		return new PreFlexTermsWriter(this, field);
	  }

	  public override void Close()
	  {
		IOUtils.close(TermsOut, FreqOut, ProxOut);
	  }

	  private class PreFlexTermsWriter : TermsConsumer
	  {
		  internal bool InstanceFieldsInitialized = false;

		  internal virtual void InitializeInstanceFields()
		  {
			  PostingsWriter = new PostingsWriter(this);
		  }

		  private readonly PreFlexRWFieldsWriter OuterInstance;

		internal readonly FieldInfo FieldInfo;
		internal readonly bool OmitTF;
		internal readonly bool StorePayloads;

		internal readonly TermInfo TermInfo = new TermInfo();
		internal PostingsWriter PostingsWriter;

		public PreFlexTermsWriter(PreFlexRWFieldsWriter outerInstance, FieldInfo fieldInfo)
		{
			this.OuterInstance = outerInstance;

			if (!InstanceFieldsInitialized)
			{
				InitializeInstanceFields();
				InstanceFieldsInitialized = true;
			}
		  this.FieldInfo = fieldInfo;
		  OmitTF = fieldInfo.IndexOptions == FieldInfo.IndexOptions.DOCS_ONLY;
		  StorePayloads = fieldInfo.hasPayloads();
		}

		private class PostingsWriter : PostingsConsumer
		{
			private readonly PreFlexRWFieldsWriter.PreFlexTermsWriter OuterInstance;

			public PostingsWriter(PreFlexRWFieldsWriter.PreFlexTermsWriter outerInstance)
			{
				this.OuterInstance = outerInstance;
			}

		  internal int LastDocID;
		  internal int LastPayloadLength = -1;
		  internal int LastPosition;
		  internal int Df;

		  public virtual PostingsWriter Reset()
		  {
			Df = 0;
			LastDocID = 0;
			LastPayloadLength = -1;
			return this;
		  }

		  public override void StartDoc(int docID, int termDocFreq)
		  {
			//System.out.println("    w doc=" + docID);

			int delta = docID - LastDocID;
			if (docID < 0 || (Df > 0 && delta <= 0))
			{
			  throw new CorruptIndexException("docs out of order (" + docID + " <= " + LastDocID + " )");
			}

			if ((++Df % outerInstance.OuterInstance.TermsOut.skipInterval) == 0)
			{
			  outerInstance.OuterInstance.SkipListWriter.setSkipData(LastDocID, outerInstance.StorePayloads, LastPayloadLength);
			  outerInstance.OuterInstance.SkipListWriter.bufferSkip(Df);
			}

			LastDocID = docID;

			Debug.Assert(docID < outerInstance.OuterInstance.TotalNumDocs, "docID=" + docID + " totalNumDocs=" + outerInstance.OuterInstance.TotalNumDocs);

			if (outerInstance.OmitTF)
			{
			  outerInstance.OuterInstance.FreqOut.writeVInt(delta);
			}
			else
			{
			  int code = delta << 1;
			  if (termDocFreq == 1)
			  {
				outerInstance.OuterInstance.FreqOut.writeVInt(code | 1);
			  }
			  else
			  {
				outerInstance.OuterInstance.FreqOut.writeVInt(code);
				outerInstance.OuterInstance.FreqOut.writeVInt(termDocFreq);
			  }
			}
			LastPosition = 0;
		  }

		  public override void AddPosition(int position, BytesRef payload, int startOffset, int endOffset)
		  {
			Debug.Assert(outerInstance.OuterInstance.ProxOut != null);
			Debug.Assert(startOffset == -1);
			Debug.Assert(endOffset == -1);
			//System.out.println("      w pos=" + position + " payl=" + payload);
			int delta = position - LastPosition;
			LastPosition = position;

			if (outerInstance.StorePayloads)
			{
			  int payloadLength = payload == null ? 0 : payload.length;
			  if (payloadLength != LastPayloadLength)
			  {
				//System.out.println("        write payload len=" + payloadLength);
				LastPayloadLength = payloadLength;
				outerInstance.OuterInstance.ProxOut.writeVInt((delta << 1) | 1);
				outerInstance.OuterInstance.ProxOut.writeVInt(payloadLength);
			  }
			  else
			  {
				outerInstance.OuterInstance.ProxOut.writeVInt(delta << 1);
			  }
			  if (payloadLength > 0)
			  {
				outerInstance.OuterInstance.ProxOut.writeBytes(payload.bytes, payload.offset, payload.length);
			  }
			}
			else
			{
			  outerInstance.OuterInstance.ProxOut.writeVInt(delta);
			}
		  }

		  public override void FinishDoc()
		  {
		  }
		}

		public override PostingsConsumer StartTerm(BytesRef text)
		{
		  //System.out.println("  w term=" + text.utf8ToString());
		  outerInstance.SkipListWriter.ResetSkip();
		  TermInfo.freqPointer = outerInstance.FreqOut.FilePointer;
		  if (outerInstance.ProxOut != null)
		  {
			TermInfo.proxPointer = outerInstance.ProxOut.FilePointer;
		  }
		  return PostingsWriter.Reset();
		}

		public override void FinishTerm(BytesRef text, TermStats stats)
		{
		  if (stats.docFreq > 0)
		  {
			long skipPointer = outerInstance.SkipListWriter.writeSkip(outerInstance.FreqOut);
			TermInfo.docFreq = stats.docFreq;
			TermInfo.skipOffset = (int)(skipPointer - TermInfo.freqPointer);
			//System.out.println("  w finish term=" + text.utf8ToString() + " fnum=" + fieldInfo.number);
			outerInstance.TermsOut.Add(FieldInfo.number, text, TermInfo);
		  }
		}

		public override void Finish(long sumTotalTermCount, long sumDocFreq, int docCount)
		{
		}

		public override IComparer<BytesRef> Comparator
		{
			get
			{
			  return BytesRef.UTF8SortedAsUTF16Comparator;
			}
		}
	  }
	}
}