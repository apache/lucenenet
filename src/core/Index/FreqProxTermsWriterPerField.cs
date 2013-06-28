/* 
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 * 
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using Lucene.Net.Analysis.Tokenattributes;
using Lucene.Net.Documents;
using Lucene.Net.Util;
using Lucene.Net.Codecs;
using System.Collections.Generic;
using Lucene.Net.Support;

namespace Lucene.Net.Index
{

    // TODO: break into separate freq and prox writers as
    // codecs; make separate container (tii/tis/skip/*) that can
    // be configured as any number of files 1..N
    sealed class FreqProxTermsWriterPerField : TermsHashConsumerPerField, IComparable<FreqProxTermsWriterPerField>
    {
        internal readonly FreqProxTermsWriter parent;
        internal readonly TermsHashPerField termsHashPerField;
        internal readonly FieldInfo fieldInfo;
        internal readonly DocumentsWriterPerThread.DocState docState;
        internal readonly FieldInvertState fieldState;
        private bool hasFreq;
        private bool hasProx;
        private bool hasOffsets;
        internal IPayloadAttribute payloadAttribute;
        internal IOffsetAttribute offsetAttribute;

        public FreqProxTermsWriterPerField(TermsHashPerField termsHashPerField, FreqProxTermsWriter parent, FieldInfo fieldInfo)
        {
            this.termsHashPerField = termsHashPerField;
            this.parent = parent;
            this.fieldInfo = fieldInfo;
            docState = termsHashPerField.docState;
            fieldState = termsHashPerField.fieldState;
            SetIndexOptions(fieldInfo.IndexOptionsValue);
        }

        public override int StreamCount
        {
            get
            {
                if (!hasProx)
                {
                    return 1;
                }
                else
                {
                    return 2;
                }
            }
        }

        public override void Finish()
        {
            if (hasPayloads)
            {
                fieldInfo.SetStorePayloads();
            }
        }

        internal bool hasPayloads;

        internal override void SkippingLongTerm()
        {
        }

        public int CompareTo(FreqProxTermsWriterPerField other)
        {
            return String.CompareOrdinal(fieldInfo.name, other.fieldInfo.name);
        }

        internal void Reset()
        {
            // Record, up front, whether our in-RAM format will be
            // with or without term freqs:
            SetIndexOptions(fieldInfo.IndexOptionsValue);
            payloadAttribute = null;
        }

        private void SetIndexOptions(FieldInfo.IndexOptions? indexOptions)
        {
            if (indexOptions == null)
            {
                // field could later be updated with indexed=true, so set everything on
                hasFreq = hasProx = hasOffsets = true;
            }
            else
            {
                hasFreq = indexOptions >= FieldInfo.IndexOptions.DOCS_AND_FREQS;
                hasProx = indexOptions >= FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS;
                hasOffsets = indexOptions >= FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS;
            }
        }

        internal override bool Start(IIndexableField[] fields, int count)
        {
            for (int i = 0; i < count; i++)
            {
                if (fields[i].FieldType.Indexed)
                {
                    return true;
                }
            }
            return false;
        }

        internal override void Start(IIndexableField f)
        {
            if (fieldState.attributeSource.HasAttribute<IPayloadAttribute>())
            {
                payloadAttribute = fieldState.attributeSource.GetAttribute<IPayloadAttribute>();
            }
            else
            {
                payloadAttribute = null;
            }

            if (hasOffsets)
            {
                offsetAttribute = fieldState.attributeSource.AddAttribute<IOffsetAttribute>();
            }
            else
            {
                offsetAttribute = null;
            }
        }

        internal void WriteProx(int termID, int proxCode)
        {
            //System.out.println("writeProx termID=" + termID + " proxCode=" + proxCode);
            ////assert hasProx;
            BytesRef payload;
            if (payloadAttribute == null)
            {
                payload = null;
            }
            else
            {
                payload = payloadAttribute.Payload;
            }

            if (payload != null && payload.length > 0)
            {
                termsHashPerField.WriteVInt(1, (proxCode << 1) | 1);
                termsHashPerField.WriteVInt(1, payload.length);
                termsHashPerField.WriteBytes(1, (byte[])(Array)payload.bytes, payload.offset, payload.length);
                hasPayloads = true;
            }
            else
            {
                termsHashPerField.WriteVInt(1, proxCode << 1);
            }

            FreqProxPostingsArray postings = (FreqProxPostingsArray)termsHashPerField.postingsArray;
            postings.lastPositions[termID] = fieldState.position;
        }

        internal void WriteOffsets(int termID, int offsetAccum)
        {
            ////assert hasOffsets;
            int startOffset = offsetAccum + offsetAttribute.StartOffset;
            int endOffset = offsetAccum + offsetAttribute.EndOffset;
            //System.out.println("writeOffsets termID=" + termID + " prevOffset=" + prevOffset + " startOff=" + startOffset + " endOff=" + endOffset);
            FreqProxPostingsArray postings = (FreqProxPostingsArray)termsHashPerField.postingsArray;
            ////assert startOffset - postings.lastOffsets[termID] >= 0;
            termsHashPerField.WriteVInt(1, startOffset - postings.lastOffsets[termID]);
            termsHashPerField.WriteVInt(1, endOffset - startOffset);

            postings.lastOffsets[termID] = startOffset;
        }

        internal override void NewTerm(int termID)
        {
            // First time we're seeing this term since the last
            // flush
            ////assert docState.testPoint("FreqProxTermsWriterPerField.newTerm start");

            FreqProxPostingsArray postings = (FreqProxPostingsArray)termsHashPerField.postingsArray;
            postings.lastDocIDs[termID] = docState.docID;
            if (!hasFreq)
            {
                postings.lastDocCodes[termID] = docState.docID;
            }
            else
            {
                postings.lastDocCodes[termID] = docState.docID << 1;
                postings.termFreqs[termID] = 1;
                if (hasProx)
                {
                    WriteProx(termID, fieldState.position);
                    if (hasOffsets)
                    {
                        WriteOffsets(termID, fieldState.offset);
                    }
                }
                else
                {
                    ////assert !hasOffsets;
                }
            }
            fieldState.maxTermFrequency = Math.Max(1, fieldState.maxTermFrequency);
            fieldState.uniqueTermCount++;
        }

        internal override void AddTerm(int termID)
        {
            ////assert docState.testPoint("FreqProxTermsWriterPerField.addTerm start");

            FreqProxPostingsArray postings = (FreqProxPostingsArray)termsHashPerField.postingsArray;

            ////assert !hasFreq || postings.termFreqs[termID] > 0;

            if (!hasFreq)
            {
                ////assert postings.termFreqs == null;
                if (docState.docID != postings.lastDocIDs[termID])
                {
                    ////assert docState.docID > postings.lastDocIDs[termID];
                    termsHashPerField.WriteVInt(0, postings.lastDocCodes[termID]);
                    postings.lastDocCodes[termID] = docState.docID - postings.lastDocIDs[termID];
                    postings.lastDocIDs[termID] = docState.docID;
                    fieldState.uniqueTermCount++;
                }
            }
            else if (docState.docID != postings.lastDocIDs[termID])
            {
                ////assert docState.docID > postings.lastDocIDs[termID]:"id: "+docState.docID + " postings ID: "+ postings.lastDocIDs[termID] + " termID: "+termID;
                // Term not yet seen in the current doc but previously
                // seen in other doc(s) since the last flush

                // Now that we know doc freq for previous doc,
                // write it & lastDocCode
                if (1 == postings.termFreqs[termID])
                {
                    termsHashPerField.WriteVInt(0, postings.lastDocCodes[termID] | 1);
                }
                else
                {
                    termsHashPerField.WriteVInt(0, postings.lastDocCodes[termID]);
                    termsHashPerField.WriteVInt(0, postings.termFreqs[termID]);
                }
                postings.termFreqs[termID] = 1;
                fieldState.maxTermFrequency = Math.Max(1, fieldState.maxTermFrequency);
                postings.lastDocCodes[termID] = (docState.docID - postings.lastDocIDs[termID]) << 1;
                postings.lastDocIDs[termID] = docState.docID;
                if (hasProx)
                {
                    WriteProx(termID, fieldState.position);
                    if (hasOffsets)
                    {
                        postings.lastOffsets[termID] = 0;
                        WriteOffsets(termID, fieldState.offset);
                    }
                }
                else
                {
                    ////assert !hasOffsets;
                }
                fieldState.uniqueTermCount++;
            }
            else
            {
                fieldState.maxTermFrequency = Math.Max(fieldState.maxTermFrequency, ++postings.termFreqs[termID]);
                if (hasProx)
                {
                    WriteProx(termID, fieldState.position - postings.lastPositions[termID]);
                }
                if (hasOffsets)
                {
                    WriteOffsets(termID, fieldState.offset);
                }
            }
        }

        public override ParallelPostingsArray CreatePostingsArray(int size)
        {
            return new FreqProxPostingsArray(size, hasFreq, hasProx, hasOffsets);
        }

        internal sealed class FreqProxPostingsArray : ParallelPostingsArray
        {
            public FreqProxPostingsArray(int size, bool writeFreqs, bool writeProx, bool writeOffsets)
                : base(size)
            {
                if (writeFreqs)
                {
                    termFreqs = new int[size];
                }
                lastDocIDs = new int[size];
                lastDocCodes = new int[size];
                if (writeProx)
                {
                    lastPositions = new int[size];
                    if (writeOffsets)
                    {
                        lastOffsets = new int[size];
                    }
                }
                else
                {
                    ////assert !writeOffsets;
                }
                //System.out.println("PA init freqs=" + writeFreqs + " pos=" + writeProx + " offs=" + writeOffsets);
            }

            internal int[] termFreqs;                                   // # times this term occurs in the current doc
            internal int[] lastDocIDs;                                  // Last docID where this term occurred
            internal int[] lastDocCodes;                                // Code for prior doc
            internal int[] lastPositions;                               // Last position where this term occurred
            internal int[] lastOffsets;                                 // Last endOffset where this term occurred

            internal override ParallelPostingsArray NewInstance(int size)
            {
                return new FreqProxPostingsArray(size, termFreqs != null, lastPositions != null, lastOffsets != null);
            }

            internal override void CopyTo(ParallelPostingsArray toArray, int numToCopy)
            {
                ////assert toArray instanceof FreqProxPostingsArray;
                FreqProxPostingsArray to = (FreqProxPostingsArray)toArray;

                base.CopyTo(toArray, numToCopy);

                Array.Copy(lastDocIDs, 0, to.lastDocIDs, 0, numToCopy);
                Array.Copy(lastDocCodes, 0, to.lastDocCodes, 0, numToCopy);
                if (lastPositions != null)
                {
                    ////assert to.lastPositions != null;
                    Array.Copy(lastPositions, 0, to.lastPositions, 0, numToCopy);
                }
                if (lastOffsets != null)
                {
                    ////assert to.lastOffsets != null;
                    Array.Copy(lastOffsets, 0, to.lastOffsets, 0, numToCopy);
                }
                if (termFreqs != null)
                {
                    ////assert to.termFreqs != null;
                    Array.Copy(termFreqs, 0, to.termFreqs, 0, numToCopy);
                }
            }

            internal override int BytesPerPosting
            {
                get
                {
                    int bytes = ParallelPostingsArray.BYTES_PER_POSTING + 2 * RamUsageEstimator.NUM_BYTES_INT;
                    if (lastPositions != null)
                    {
                        bytes += RamUsageEstimator.NUM_BYTES_INT;
                    }
                    if (lastOffsets != null)
                    {
                        bytes += RamUsageEstimator.NUM_BYTES_INT;
                    }
                    if (termFreqs != null)
                    {
                        bytes += RamUsageEstimator.NUM_BYTES_INT;
                    }

                    return bytes;
                }
            }
        }

        public void Abort() { }

        BytesRef payload;

        internal void Flush(String fieldName, FieldsConsumer consumer, SegmentWriteState state)
        {

            if (!fieldInfo.IsIndexed)
            {
                return; // nothing to flush, don't bother the codec with the unindexed field
            }

            TermsConsumer termsConsumer = consumer.AddField(fieldInfo);
            IComparer<BytesRef> termComp = termsConsumer.Comparator;

            // CONFUSING: this.indexOptions holds the index options
            // that were current when we first saw this field.  But
            // it's possible this has changed, eg when other
            // documents are indexed that cause a "downgrade" of the
            // IndexOptions.  So we must decode the in-RAM buffer
            // according to this.indexOptions, but then write the
            // new segment to the directory according to
            // currentFieldIndexOptions:
            FieldInfo.IndexOptions currentFieldIndexOptions = fieldInfo.IndexOptionsValue.GetValueOrDefault();
            //assert currentFieldIndexOptions != null;

            bool writeTermFreq = currentFieldIndexOptions >= FieldInfo.IndexOptions.DOCS_AND_FREQS;
            bool writePositions = currentFieldIndexOptions >= FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS;
            bool writeOffsets = currentFieldIndexOptions >= FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS;

            bool readTermFreq = this.hasFreq;
            bool readPositions = this.hasProx;
            bool readOffsets = this.hasOffsets;

            //System.out.println("flush readTF=" + readTermFreq + " readPos=" + readPositions + " readOffs=" + readOffsets);

            // Make sure FieldInfo.update is working correctly!:
            //assert !writeTermFreq || readTermFreq;
            //assert !writePositions || readPositions;
            //assert !writeOffsets || readOffsets;

            //assert !writeOffsets || writePositions;

            IDictionary<Term, int?> segDeletes;
            if (state.segDeletes != null && state.segDeletes.terms.Count > 0)
            {
                segDeletes = state.segDeletes.terms;
            }
            else
            {
                segDeletes = null;
            }

            int[] termIDs = termsHashPerField.SortPostings(termComp);
            int numTerms = termsHashPerField.bytesHash.Size;
            BytesRef text = new BytesRef();
            FreqProxPostingsArray postings = (FreqProxPostingsArray)termsHashPerField.postingsArray;
            ByteSliceReader freq = new ByteSliceReader();
            ByteSliceReader prox = new ByteSliceReader();

            FixedBitSet visitedDocs = new FixedBitSet(state.segmentInfo.DocCount);
            long sumTotalTermFreq = 0;
            long sumDocFreq = 0;

            Term protoTerm = new Term(fieldName);
            for (int i = 0; i < numTerms; i++)
            {
                int termID = termIDs[i];
                //System.out.println("term=" + termID);
                // Get BytesRef
                int textStart = postings.textStarts[termID];
                termsHashPerField.bytePool.SetBytesRef(text, textStart);

                termsHashPerField.InitReader(freq, termID, 0);
                if (readPositions || readOffsets)
                {
                    termsHashPerField.InitReader(prox, termID, 1);
                }

                // TODO: really TermsHashPerField should take over most
                // of this loop, including merge sort of terms from
                // multiple threads and interacting with the
                // TermsConsumer, only calling out to us (passing us the
                // DocsConsumer) to handle delivery of docs/positions

                PostingsConsumer postingsConsumer = termsConsumer.StartTerm(text);

                int delDocLimit;
                if (segDeletes != null)
                {
                    protoTerm.bytes = text;
                    int? docIDUpto = segDeletes[protoTerm];
                    if (docIDUpto != null)
                    {
                        delDocLimit = docIDUpto.GetValueOrDefault();
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
                long totTF = 0;
                int docID = 0;

                while (true)
                {
                    //System.out.println("  cycle");
                    int termFreq;
                    if (freq.Eof())
                    {
                        if (postings.lastDocCodes[termID] != -1)
                        {
                            // Return last doc
                            docID = postings.lastDocIDs[termID];
                            if (readTermFreq)
                            {
                                termFreq = postings.termFreqs[termID];
                            }
                            else
                            {
                                termFreq = -1;
                            }
                            postings.lastDocCodes[termID] = -1;
                        }
                        else
                        {
                            // EOF
                            break;
                        }
                    }
                    else
                    {
                        int code = freq.ReadVInt();
                        if (!readTermFreq)
                        {
                            docID += code;
                            termFreq = -1;
                        }
                        else
                        {
                            docID += Number.URShift(code, 1);
                            if ((code & 1) != 0)
                            {
                                termFreq = 1;
                            }
                            else
                            {
                                termFreq = freq.ReadVInt();
                            }
                        }

                        //assert docID != postings.lastDocIDs[termID];
                    }

                    docFreq++;
                    //assert docID < state.segmentInfo.getDocCount(): "doc=" + docID + " maxDoc=" + state.segmentInfo.getDocCount();

                    // NOTE: we could check here if the docID was
                    // deleted, and skip it.  However, this is somewhat
                    // dangerous because it can yield non-deterministic
                    // behavior since we may see the docID before we see
                    // the term that caused it to be deleted.  This
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
                        if (state.liveDocs == null)
                        {
                            state.liveDocs = docState.docWriter.codec.LiveDocsFormat.NewLiveDocs(state.segmentInfo.DocCount);
                        }
                        if (state.liveDocs[docID])
                        {
                            state.delCountOnFlush++;
                            state.liveDocs.Clear(docID);
                        }
                    }

                    totTF += termFreq;

                    // Carefully copy over the prox + payload info,
                    // changing the format to match Lucene's segment
                    // format.

                    if (readPositions || readOffsets)
                    {
                        // we did record positions (& maybe payload) and/or offsets
                        int position = 0;
                        int offset = 0;
                        for (int j = 0; j < termFreq; j++)
                        {
                            BytesRef thisPayload;

                            if (readPositions)
                            {
                                int code = prox.ReadVInt();
                                position += Number.URShift(code, 1);

                                if ((code & 1) != 0)
                                {

                                    // This position has a payload
                                    int payloadLength = prox.ReadVInt();

                                    if (payload == null)
                                    {
                                        payload = new BytesRef();
                                        payload.bytes = new sbyte[payloadLength];
                                    }
                                    else if (payload.bytes.Length < payloadLength)
                                    {
                                        payload.Grow(payloadLength);
                                    }

                                    prox.ReadBytes(payload.bytes, 0, payloadLength);
                                    payload.length = payloadLength;
                                    thisPayload = payload;

                                }
                                else
                                {
                                    thisPayload = null;
                                }

                                if (readOffsets)
                                {
                                    int startOffset = offset + prox.ReadVInt();
                                    int endOffset = startOffset + prox.ReadVInt();
                                    if (writePositions)
                                    {
                                        if (writeOffsets)
                                        {
                                            //assert startOffset >=0 && endOffset >= startOffset : "startOffset=" + startOffset + ",endOffset=" + endOffset + ",offset=" + offset;
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
                termsConsumer.FinishTerm(text, new TermStats(docFreq, writeTermFreq ? totTF : -1));
                sumTotalTermFreq += totTF;
                sumDocFreq += docFreq;
            }

            termsConsumer.Finish(writeTermFreq ? sumTotalTermFreq : -1, sumDocFreq, visitedDocs.Cardinality());
        }
    }
}