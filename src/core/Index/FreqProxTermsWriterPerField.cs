using System;
using System.Diagnostics;
using System.Collections.Generic;

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
	using FieldsConsumer = Lucene.Net.Codecs.FieldsConsumer;
	using PostingsConsumer = Lucene.Net.Codecs.PostingsConsumer;
	using TermStats = Lucene.Net.Codecs.TermStats;
	using TermsConsumer = Lucene.Net.Codecs.TermsConsumer;
	using IndexOptions = Lucene.Net.Index.FieldInfo.IndexOptions_e;
	using BytesRef = Lucene.Net.Util.BytesRef;
	using FixedBitSet = Lucene.Net.Util.FixedBitSet;
	using RamUsageEstimator = Lucene.Net.Util.RamUsageEstimator;

	// TODO: break into separate freq and prox writers as
	// codecs; make separate container (tii/tis/skip/*) that can
	// be configured as any number of files 1..N
	internal sealed class FreqProxTermsWriterPerField : TermsHashConsumerPerField, IComparable<FreqProxTermsWriterPerField>
	{

	  internal readonly FreqProxTermsWriter Parent;
	  internal readonly TermsHashPerField TermsHashPerField;
	  internal readonly FieldInfo FieldInfo;
	  internal readonly DocumentsWriterPerThread.DocState DocState;
	  internal readonly FieldInvertState FieldState;
	  private bool HasFreq;
	  private bool HasProx;
	  private bool HasOffsets;
	  internal PayloadAttribute PayloadAttribute;
	  internal OffsetAttribute OffsetAttribute;

	  public FreqProxTermsWriterPerField(TermsHashPerField termsHashPerField, FreqProxTermsWriter parent, FieldInfo fieldInfo)
	  {
		this.TermsHashPerField = termsHashPerField;
		this.Parent = parent;
		this.FieldInfo = fieldInfo;
		DocState = termsHashPerField.DocState;
		FieldState = termsHashPerField.FieldState;
		IndexOptions = fieldInfo.IndexOptions_e;
	  }

	  internal override int StreamCount
	  {
		  get
		  {
			if (!HasProx)
			{
			  return 1;
			}
			else
			{
			  return 2;
			}
		  }
	  }

	  internal override void Finish()
	  {
		if (HasPayloads)
		{
		  FieldInfo.SetStorePayloads();
		}
	  }

	  internal bool HasPayloads;

	  internal override void SkippingLongTerm()
	  {
	  }
	  public int CompareTo(FreqProxTermsWriterPerField other)
	  {
		return FieldInfo.Name.CompareTo(other.FieldInfo.Name);
	  }

	  // Called after flush
	  internal void Reset()
	  {
		// Record, up front, whether our in-RAM format will be
		// with or without term freqs:
		IndexOptions = FieldInfo.IndexOptions_e;
		PayloadAttribute = null;
	  }

	  private IndexOptions IndexOptions
	  {
		  set
		  {
			if (value == null)
			{
			  // field could later be updated with indexed=true, so set everything on
			  HasFreq = HasProx = HasOffsets = true;
			}
			else
			{
			  HasFreq = value.compareTo(IndexOptions_e.DOCS_AND_FREQS) >= 0;
			  HasProx = value.compareTo(IndexOptions_e.DOCS_AND_FREQS_AND_POSITIONS) >= 0;
			  HasOffsets = value.compareTo(IndexOptions_e.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS) >= 0;
			}
		  }
	  }

	  internal override bool Start(IndexableField[] fields, int count)
	  {
		for (int i = 0;i < count;i++)
		{
		  if (fields[i].FieldType().indexed())
		  {
			return true;
		  }
		}
		return false;
	  }

	  internal override void Start(IndexableField f)
	  {
		if (FieldState.AttributeSource_Renamed.HasAttribute(typeof(PayloadAttribute)))
		{
		  PayloadAttribute = FieldState.AttributeSource_Renamed.getAttribute(typeof(PayloadAttribute));
		}
		else
		{
		  PayloadAttribute = null;
		}
		if (HasOffsets)
		{
		  OffsetAttribute = FieldState.AttributeSource_Renamed.addAttribute(typeof(OffsetAttribute));
		}
		else
		{
		  OffsetAttribute = null;
		}
	  }

	  internal void WriteProx(int termID, int proxCode)
	  {
		//System.out.println("writeProx termID=" + termID + " proxCode=" + proxCode);
		Debug.Assert(HasProx);
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

		if (payload != null && payload.Length > 0)
		{
		  TermsHashPerField.WriteVInt(1, (proxCode << 1) | 1);
		  TermsHashPerField.WriteVInt(1, payload.Length);
		  TermsHashPerField.WriteBytes(1, payload.Bytes, payload.Offset, payload.Length);
		  HasPayloads = true;
		}
		else
		{
		  TermsHashPerField.WriteVInt(1, proxCode << 1);
		}

		FreqProxPostingsArray postings = (FreqProxPostingsArray) TermsHashPerField.PostingsArray;
		postings.LastPositions[termID] = FieldState.Position_Renamed;
	  }

	  internal void WriteOffsets(int termID, int offsetAccum)
	  {
		Debug.Assert(HasOffsets);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int startOffset = offsetAccum + offsetAttribute.StartOffset();
		int startOffset = offsetAccum + OffsetAttribute.StartOffset();
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int endOffset = offsetAccum + offsetAttribute.EndOffset();
		int endOffset = offsetAccum + OffsetAttribute.EndOffset();
		//System.out.println("writeOffsets termID=" + termID + " prevOffset=" + prevOffset + " startOff=" + startOffset + " endOff=" + endOffset);
		FreqProxPostingsArray postings = (FreqProxPostingsArray) TermsHashPerField.PostingsArray;
		Debug.Assert(startOffset - postings.LastOffsets[termID] >= 0);
		TermsHashPerField.WriteVInt(1, startOffset - postings.LastOffsets[termID]);
		TermsHashPerField.WriteVInt(1, endOffset - startOffset);

		postings.LastOffsets[termID] = startOffset;
	  }

	  internal override void NewTerm(int termID)
	  {
		// First time we're seeing this term since the last
		// flush
		Debug.Assert(DocState.TestPoint("FreqProxTermsWriterPerField.newTerm start"));

		FreqProxPostingsArray postings = (FreqProxPostingsArray) TermsHashPerField.PostingsArray;
		postings.LastDocIDs[termID] = DocState.DocID;
		if (!HasFreq)
		{
		  postings.LastDocCodes[termID] = DocState.DocID;
		}
		else
		{
		  postings.LastDocCodes[termID] = DocState.DocID << 1;
		  postings.TermFreqs[termID] = 1;
		  if (HasProx)
		  {
			WriteProx(termID, FieldState.Position_Renamed);
			if (HasOffsets)
			{
			  WriteOffsets(termID, FieldState.Offset_Renamed);
			}
		  }
		  else
		  {
			Debug.Assert(!HasOffsets);
		  }
		}
		FieldState.MaxTermFrequency_Renamed = Math.Max(1, FieldState.MaxTermFrequency_Renamed);
		FieldState.UniqueTermCount_Renamed++;
	  }

	  internal override void AddTerm(int termID)
	  {

		Debug.Assert(DocState.TestPoint("FreqProxTermsWriterPerField.addTerm start"));

		FreqProxPostingsArray postings = (FreqProxPostingsArray) TermsHashPerField.PostingsArray;

		Debug.Assert(!HasFreq || postings.TermFreqs[termID] > 0);

		if (!HasFreq)
		{
		  Debug.Assert(postings.TermFreqs == null);
		  if (DocState.DocID != postings.LastDocIDs[termID])
		  {
			Debug.Assert(DocState.DocID > postings.LastDocIDs[termID]);
			TermsHashPerField.WriteVInt(0, postings.LastDocCodes[termID]);
			postings.LastDocCodes[termID] = DocState.DocID - postings.LastDocIDs[termID];
			postings.LastDocIDs[termID] = DocState.DocID;
			FieldState.UniqueTermCount_Renamed++;
		  }
		}
		else if (DocState.DocID != postings.LastDocIDs[termID])
		{
		  Debug.Assert(DocState.DocID > postings.LastDocIDs[termID], "id: " + DocState.DocID + " postings ID: " + postings.LastDocIDs[termID] + " termID: " + termID);
		  // Term not yet seen in the current doc but previously
		  // seen in other doc(s) since the last flush

		  // Now that we know doc freq for previous doc,
		  // write it & lastDocCode
		  if (1 == postings.TermFreqs[termID])
		  {
			TermsHashPerField.WriteVInt(0, postings.LastDocCodes[termID] | 1);
		  }
		  else
		  {
			TermsHashPerField.WriteVInt(0, postings.LastDocCodes[termID]);
			TermsHashPerField.WriteVInt(0, postings.TermFreqs[termID]);
		  }
		  postings.TermFreqs[termID] = 1;
		  FieldState.MaxTermFrequency_Renamed = Math.Max(1, FieldState.MaxTermFrequency_Renamed);
		  postings.LastDocCodes[termID] = (DocState.DocID - postings.LastDocIDs[termID]) << 1;
		  postings.LastDocIDs[termID] = DocState.DocID;
		  if (HasProx)
		  {
			WriteProx(termID, FieldState.Position_Renamed);
			if (HasOffsets)
			{
			  postings.LastOffsets[termID] = 0;
			  WriteOffsets(termID, FieldState.Offset_Renamed);
			}
		  }
		  else
		  {
			Debug.Assert(!HasOffsets);
		  }
		  FieldState.UniqueTermCount_Renamed++;
		}
		else
		{
		  FieldState.MaxTermFrequency_Renamed = Math.Max(FieldState.MaxTermFrequency_Renamed, ++postings.TermFreqs[termID]);
		  if (HasProx)
		  {
			WriteProx(termID, FieldState.Position_Renamed - postings.LastPositions[termID]);
		  }
		  if (HasOffsets)
		  {
			WriteOffsets(termID, FieldState.Offset_Renamed);
		  }
		}
	  }

	  internal override ParallelPostingsArray CreatePostingsArray(int size)
	  {
		return new FreqProxPostingsArray(size, HasFreq, HasProx, HasOffsets);
	  }

	  internal sealed class FreqProxPostingsArray : ParallelPostingsArray
	  {
		public FreqProxPostingsArray(int size, bool writeFreqs, bool writeProx, bool writeOffsets) : base(size)
		{
		  if (writeFreqs)
		  {
			TermFreqs = new int[size];
		  }
		  LastDocIDs = new int[size];
		  LastDocCodes = new int[size];
		  if (writeProx)
		  {
			LastPositions = new int[size];
			if (writeOffsets)
			{
			  LastOffsets = new int[size];
			}
		  }
		  else
		  {
			Debug.Assert(!writeOffsets);
		  }
		  //System.out.println("PA init freqs=" + writeFreqs + " pos=" + writeProx + " offs=" + writeOffsets);
		}

		internal int[] TermFreqs; // # times this term occurs in the current doc
		internal int[] LastDocIDs; // Last docID where this term occurred
		internal int[] LastDocCodes; // Code for prior doc
		internal int[] LastPositions; // Last position where this term occurred
		internal int[] LastOffsets; // Last endOffset where this term occurred

		internal override ParallelPostingsArray NewInstance(int size)
		{
		  return new FreqProxPostingsArray(size, TermFreqs != null, LastPositions != null, LastOffsets != null);
		}

		internal override void CopyTo(ParallelPostingsArray toArray, int numToCopy)
		{
		  Debug.Assert(toArray is FreqProxPostingsArray);
		  FreqProxPostingsArray to = (FreqProxPostingsArray) toArray;

		  base.CopyTo(toArray, numToCopy);

		  Array.Copy(LastDocIDs, 0, to.LastDocIDs, 0, numToCopy);
		  Array.Copy(LastDocCodes, 0, to.LastDocCodes, 0, numToCopy);
		  if (LastPositions != null)
		  {
			Debug.Assert(to.LastPositions != null);
			Array.Copy(LastPositions, 0, to.LastPositions, 0, numToCopy);
		  }
		  if (LastOffsets != null)
		  {
			Debug.Assert(to.LastOffsets != null);
			Array.Copy(LastOffsets, 0, to.LastOffsets, 0, numToCopy);
		  }
		  if (TermFreqs != null)
		  {
			Debug.Assert(to.TermFreqs != null);
			Array.Copy(TermFreqs, 0, to.TermFreqs, 0, numToCopy);
		  }
		}

		internal override int BytesPerPosting()
		{
		  int bytes = ParallelPostingsArray.BYTES_PER_POSTING + 2 * RamUsageEstimator.NUM_BYTES_INT;
		  if (LastPositions != null)
		  {
			bytes += RamUsageEstimator.NUM_BYTES_INT;
		  }
		  if (LastOffsets != null)
		  {
			bytes += RamUsageEstimator.NUM_BYTES_INT;
		  }
		  if (TermFreqs != null)
		  {
			bytes += RamUsageEstimator.NUM_BYTES_INT;
		  }

		  return bytes;
		}
	  }

	  public void Abort()
	  {
	  }

	  internal BytesRef Payload;

	  /* Walk through all unique text tokens (Posting
	   * instances) found in this field and serialize them
	   * into a single RAM segment. */
	  internal void Flush(string fieldName, FieldsConsumer consumer, SegmentWriteState state)
	  {

		if (!FieldInfo.Indexed)
		{
		  return; // nothing to flush, don't bother the codec with the unindexed field
		}

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final Lucene.Net.Codecs.TermsConsumer termsConsumer = consumer.addField(fieldInfo);
		TermsConsumer termsConsumer = consumer.AddField(FieldInfo);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final java.util.Comparator<Lucene.Net.Util.BytesRef> termComp = termsConsumer.getComparator();
		IComparer<BytesRef> termComp = termsConsumer.Comparator;

		// CONFUSING: this.indexOptions holds the index options
		// that were current when we first saw this field.  But
		// it's possible this has changed, eg when other
		// documents are indexed that cause a "downgrade" of the
		// IndexOptions.  So we must decode the in-RAM buffer
		// according to this.indexOptions, but then write the
		// new segment to the directory according to
		// currentFieldIndexOptions:
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final Lucene.Net.Index.FieldInfo.IndexOptions currentFieldIndexOptions = fieldInfo.getIndexOptions();
		IndexOptions currentFieldIndexOptions = FieldInfo.IndexOptions_e;
		Debug.Assert(currentFieldIndexOptions != null);

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final boolean writeTermFreq = currentFieldIndexOptions.compareTo(Lucene.Net.Index.FieldInfo.IndexOptions.DOCS_AND_FREQS) >= 0;
		bool writeTermFreq = currentFieldIndexOptions.compareTo(IndexOptions_e.DOCS_AND_FREQS) >= 0;
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final boolean writePositions = currentFieldIndexOptions.compareTo(Lucene.Net.Index.FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS) >= 0;
		bool writePositions = currentFieldIndexOptions.compareTo(IndexOptions_e.DOCS_AND_FREQS_AND_POSITIONS) >= 0;
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final boolean writeOffsets = currentFieldIndexOptions.compareTo(Lucene.Net.Index.FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS) >= 0;
		bool writeOffsets = currentFieldIndexOptions.compareTo(IndexOptions_e.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS) >= 0;

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final boolean readTermFreq = this.hasFreq;
		bool readTermFreq = this.HasFreq;
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final boolean readPositions = this.hasProx;
		bool readPositions = this.HasProx;
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final boolean readOffsets = this.hasOffsets;
		bool readOffsets = this.HasOffsets;

		//System.out.println("flush readTF=" + readTermFreq + " readPos=" + readPositions + " readOffs=" + readOffsets);

		// Make sure FieldInfo.update is working correctly!:
		Debug.Assert(!writeTermFreq || readTermFreq);
		Debug.Assert(!writePositions || readPositions);
		Debug.Assert(!writeOffsets || readOffsets);

		Debug.Assert(!writeOffsets || writePositions);

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final java.util.Map<Term,Integer> segDeletes;
		IDictionary<Term, int?> segDeletes;
		if (state.SegUpdates != null && state.SegUpdates.terms.size() > 0)
		{
		  segDeletes = state.SegUpdates.terms;
		}
		else
		{
		  segDeletes = null;
		}

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int[] termIDs = termsHashPerField.sortPostings(termComp);
		int[] termIDs = TermsHashPerField.SortPostings(termComp);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int numTerms = termsHashPerField.bytesHash.size();
		int numTerms = TermsHashPerField.BytesHash.size();
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final Lucene.Net.Util.BytesRef text = new Lucene.Net.Util.BytesRef();
		BytesRef text = new BytesRef();
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final FreqProxPostingsArray postings = (FreqProxPostingsArray) termsHashPerField.postingsArray;
		FreqProxPostingsArray postings = (FreqProxPostingsArray) TermsHashPerField.PostingsArray;
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final ByteSliceReader freq = new ByteSliceReader();
		ByteSliceReader freq = new ByteSliceReader();
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final ByteSliceReader prox = new ByteSliceReader();
		ByteSliceReader prox = new ByteSliceReader();

		FixedBitSet visitedDocs = new FixedBitSet(state.SegmentInfo.DocCount);
		long sumTotalTermFreq = 0;
		long sumDocFreq = 0;

		Term protoTerm = new Term(fieldName);
		for (int i = 0; i < numTerms; i++)
		{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int termID = termIDs[i];
		  int termID = termIDs[i];
		  //System.out.println("term=" + termID);
		  // Get BytesRef
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int textStart = postings.textStarts[termID];
		  int textStart = postings.TextStarts[termID];
		  TermsHashPerField.BytePool.setBytesRef(text, textStart);

		  TermsHashPerField.InitReader(freq, termID, 0);
		  if (readPositions || readOffsets)
		  {
			TermsHashPerField.InitReader(prox, termID, 1);
		  }

		  // TODO: really TermsHashPerField should take over most
		  // of this loop, including merge sort of terms from
		  // multiple threads and interacting with the
		  // TermsConsumer, only calling out to us (passing us the
		  // DocsConsumer) to handle delivery of docs/positions

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final Lucene.Net.Codecs.PostingsConsumer postingsConsumer = termsConsumer.startTerm(text);
		  PostingsConsumer postingsConsumer = termsConsumer.StartTerm(text);

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int delDocLimit;
		  int delDocLimit;
		  if (segDeletes != null)
		  {
			protoTerm.Bytes_Renamed = text;
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final Integer docIDUpto = segDeletes.get(protoTerm);
			int? docIDUpto = segDeletes[protoTerm];
			if (docIDUpto != null)
			{
			  delDocLimit = docIDUpto;
			}
			else
			{
			  delDocLimit = 0;
			}
		  }
		  else
		  {
			delDocLimit = 0;
		  }

		  // Now termStates has numToMerge FieldMergeStates
		  // which all share the same term.  Now we must
		  // interleave the docID streams.
		  int docFreq = 0;
		  long totalTermFreq = 0;
		  int docID = 0;

		  while (true)
		  {
			//System.out.println("  cycle");
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int termFreq;
			int termFreq;
			if (freq.Eof())
			{
			  if (postings.LastDocCodes[termID] != -1)
			  {
				// Return last doc
				docID = postings.LastDocIDs[termID];
				if (readTermFreq)
				{
				  termFreq = postings.TermFreqs[termID];
				}
				else
				{
				  termFreq = -1;
				}
				postings.LastDocCodes[termID] = -1;
			  }
			  else
			  {
				// EOF
				break;
			  }
			}
			else
			{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int code = freq.readVInt();
			  int code = freq.ReadVInt();
			  if (!readTermFreq)
			  {
				docID += code;
				termFreq = -1;
			  }
			  else
			  {
				docID += (int)((uint)code >> 1);
				if ((code & 1) != 0)
				{
				  termFreq = 1;
				}
				else
				{
				  termFreq = freq.ReadVInt();
				}
			  }

			  Debug.Assert(docID != postings.LastDocIDs[termID]);
			}

			docFreq++;
			Debug.Assert(docID < state.SegmentInfo.DocCount, "doc=" + docID + " maxDoc=" + state.SegmentInfo.DocCount);

			// NOTE: we could check here if the docID was
			// deleted, and skip it.  However, this is somewhat
			// dangerous because it can yield non-deterministic
			// behavior since we may see the docID before we see
			// the term that caused it to be deleted.  this
			// would mean some (but not all) of its postings may
			// make it into the index, which'd alter the docFreq
			// for those terms.  We could fix this by doing two
			// passes, ie first sweep marks all del docs, and
			// 2nd sweep does the real flush, but I suspect
			// that'd add too much time to flush.
			visitedDocs.Set(docID);
			postingsConsumer.StartDoc(docID, writeTermFreq ? termFreq : -1);
			if (docID < delDocLimit)
			{
			  // Mark it deleted.  TODO: we could also skip
			  // writing its postings; this would be
			  // deterministic (just for this Term's docs).

			  // TODO: can we do this reach-around in a cleaner way????
			  if (state.LiveDocs == null)
			  {
				state.LiveDocs = DocState.DocWriter.codec.liveDocsFormat().newLiveDocs(state.SegmentInfo.DocCount);
			  }
			  if (state.LiveDocs.get(docID))
			  {
				state.DelCountOnFlush++;
				state.LiveDocs.clear(docID);
			  }
			}

			totalTermFreq += termFreq;

			// Carefully copy over the prox + payload info,
			// changing the format to match Lucene's segment
			// format.

			if (readPositions || readOffsets)
			{
			  // we did record positions (& maybe payload) and/or offsets
			  int position = 0;
			  int offset = 0;
			  for (int j = 0;j < termFreq;j++)
			  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final Lucene.Net.Util.BytesRef thisPayload;
				BytesRef thisPayload;

				if (readPositions)
				{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int code = prox.readVInt();
				  int code = prox.ReadVInt();
				  position += (int)((uint)code >> 1);

				  if ((code & 1) != 0)
				  {

					// this position has a payload
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int payloadLength = prox.readVInt();
					int payloadLength = prox.ReadVInt();

					if (Payload == null)
					{
					  Payload = new BytesRef();
					  Payload.Bytes = new sbyte[payloadLength];
					}
					else if (Payload.Bytes.Length < payloadLength)
					{
					  Payload.Grow(payloadLength);
					}

					prox.ReadBytes(Payload.Bytes, 0, payloadLength);
					Payload.Length = payloadLength;
					thisPayload = Payload;

				  }
				  else
				  {
					thisPayload = null;
				  }

				  if (readOffsets)
				  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int startOffset = offset + prox.readVInt();
					int startOffset = offset + prox.ReadVInt();
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int endOffset = startOffset + prox.readVInt();
					int endOffset = startOffset + prox.ReadVInt();
					if (writePositions)
					{
					  if (writeOffsets)
					  {
						Debug.Assert(startOffset >= 0 && endOffset >= startOffset, "startOffset=" + startOffset + ",endOffset=" + endOffset + ",offset=" + offset);
						postingsConsumer.AddPosition(position, thisPayload, startOffset, endOffset);
					  }
					  else
					  {
						postingsConsumer.AddPosition(position, thisPayload, -1, -1);
					  }
					}
					offset = startOffset;
				  }
				  else if (writePositions)
				  {
					postingsConsumer.AddPosition(position, thisPayload, -1, -1);
				  }
				}
			  }
			}
			postingsConsumer.FinishDoc();
		  }
		  termsConsumer.FinishTerm(text, new TermStats(docFreq, writeTermFreq ? totalTermFreq : -1));
		  sumTotalTermFreq += totalTermFreq;
		  sumDocFreq += docFreq;
		}

		termsConsumer.Finish(writeTermFreq ? sumTotalTermFreq : -1, sumDocFreq, visitedDocs.Cardinality());
	  }
	}

}