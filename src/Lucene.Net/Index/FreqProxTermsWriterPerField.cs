using J2N.Numerics;
using J2N.Text;
using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Diagnostics;
using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

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

    using BytesRef = Lucene.Net.Util.BytesRef;
    using FieldsConsumer = Lucene.Net.Codecs.FieldsConsumer;
    using FixedBitSet = Lucene.Net.Util.FixedBitSet;
    using PostingsConsumer = Lucene.Net.Codecs.PostingsConsumer;
    using RamUsageEstimator = Lucene.Net.Util.RamUsageEstimator;
    using TermsConsumer = Lucene.Net.Codecs.TermsConsumer;
    using TermStats = Lucene.Net.Codecs.TermStats;

    // TODO: break into separate freq and prox writers as
    // codecs; make separate container (tii/tis/skip/*) that can
    // be configured as any number of files 1..N
    internal sealed class FreqProxTermsWriterPerField : TermsHashConsumerPerField, IComparable<FreqProxTermsWriterPerField>
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
            SetIndexOptions(fieldInfo.IndexOptions);
        }

        internal override int StreamCount
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

        internal override void Finish()
        {
            if (hasPayloads)
            {
                fieldInfo.SetStorePayloads();
            }
        }

        internal bool hasPayloads;

        [ExceptionToNetNumericConvention]
        internal override void SkippingLongTerm()
        {
        }

        public int CompareTo(FreqProxTermsWriterPerField other)
        {
            return fieldInfo.Name.CompareToOrdinal(other.fieldInfo.Name);
        }

        // Called after flush
        internal void Reset()
        {
            // Record, up front, whether our in-RAM format will be
            // with or without term freqs:
            SetIndexOptions(fieldInfo.IndexOptions);
            payloadAttribute = null;
        }

        private void SetIndexOptions(IndexOptions indexOptions)
        {
            if (indexOptions == IndexOptions.NONE)
            {
                // field could later be updated with indexed=true, so set everything on
                hasFreq = hasProx = hasOffsets = true;
            }
            else
            {
                // LUCENENET specific - to avoid boxing, changed from CompareTo() to IndexOptionsComparer.Compare()
                hasFreq = IndexOptionsComparer.Default.Compare(indexOptions, IndexOptions.DOCS_AND_FREQS) >= 0;
                hasProx = IndexOptionsComparer.Default.Compare(indexOptions, IndexOptions.DOCS_AND_FREQS_AND_POSITIONS) >= 0;
                hasOffsets = IndexOptionsComparer.Default.Compare(indexOptions, IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS) >= 0;
            }
        }

        internal override bool Start(IIndexableField[] fields, int count)
        {
            for (int i = 0; i < count; i++)
            {
                if (fields[i].IndexableFieldType.IsIndexed)
                {
                    return true;
                }
            }
            return false;
        }

        internal override void Start(IIndexableField f)
        {
            if (fieldState.AttributeSource.HasAttribute<IPayloadAttribute>())
            {
                payloadAttribute = fieldState.AttributeSource.GetAttribute<IPayloadAttribute>();
            }
            else
            {
                payloadAttribute = null;
            }
            if (hasOffsets)
            {
                offsetAttribute = fieldState.AttributeSource.AddAttribute<IOffsetAttribute>();
            }
            else
            {
                offsetAttribute = null;
            }
        }

        internal void WriteProx(int termID, int proxCode)
        {
            //System.out.println("writeProx termID=" + termID + " proxCode=" + proxCode);
            if (Debugging.AssertsEnabled) Debugging.Assert(hasProx);
            BytesRef payload;
            if (payloadAttribute is null)
            {
                payload = null;
            }
            else
            {
                payload = payloadAttribute.Payload;
            }

            if (payload != null && payload.Length > 0)
            {
                termsHashPerField.WriteVInt32(1, (proxCode << 1) | 1);
                termsHashPerField.WriteVInt32(1, payload.Length);
                termsHashPerField.WriteBytes(1, payload.Bytes, payload.Offset, payload.Length);
                hasPayloads = true;
            }
            else
            {
                termsHashPerField.WriteVInt32(1, proxCode << 1);
            }

            FreqProxPostingsArray postings = (FreqProxPostingsArray)termsHashPerField.postingsArray;
            postings.lastPositions[termID] = fieldState.Position;
        }

        internal void WriteOffsets(int termID, int offsetAccum)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(hasOffsets);
            int startOffset = offsetAccum + offsetAttribute.StartOffset;
            int endOffset = offsetAccum + offsetAttribute.EndOffset;
            FreqProxPostingsArray postings = (FreqProxPostingsArray)termsHashPerField.postingsArray;
            if (Debugging.AssertsEnabled) Debugging.Assert(startOffset - postings.lastOffsets[termID] >= 0);
            termsHashPerField.WriteVInt32(1, startOffset - postings.lastOffsets[termID]);
            termsHashPerField.WriteVInt32(1, endOffset - startOffset);

            postings.lastOffsets[termID] = startOffset;
        }

        internal override void NewTerm(int termID)
        {
            // First time we're seeing this term since the last
            // flush
            if (Debugging.AssertsEnabled) Debugging.Assert(docState.TestPoint("FreqProxTermsWriterPerField.newTerm start"));

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
                    WriteProx(termID, fieldState.Position);
                    if (hasOffsets)
                    {
                        WriteOffsets(termID, fieldState.Offset);
                    }
                }
                else
                {
                    if (Debugging.AssertsEnabled) Debugging.Assert(!hasOffsets);
                }
            }
            fieldState.MaxTermFrequency = Math.Max(1, fieldState.MaxTermFrequency);
            fieldState.UniqueTermCount++;
        }

        internal override void AddTerm(int termID)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(docState.TestPoint("FreqProxTermsWriterPerField.addTerm start"));

            FreqProxPostingsArray postings = (FreqProxPostingsArray)termsHashPerField.postingsArray;

            if (Debugging.AssertsEnabled) Debugging.Assert(!hasFreq || postings.termFreqs[termID] > 0);

            if (!hasFreq)
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(postings.termFreqs is null);
                if (docState.docID != postings.lastDocIDs[termID])
                {
                    if (Debugging.AssertsEnabled) Debugging.Assert(docState.docID > postings.lastDocIDs[termID]);
                    termsHashPerField.WriteVInt32(0, postings.lastDocCodes[termID]);
                    postings.lastDocCodes[termID] = docState.docID - postings.lastDocIDs[termID];
                    postings.lastDocIDs[termID] = docState.docID;
                    fieldState.UniqueTermCount++;
                }
            }
            else if (docState.docID != postings.lastDocIDs[termID])
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(docState.docID > postings.lastDocIDs[termID], "id: {0} postings ID: {1} termID: {2}", docState.docID, postings.lastDocIDs[termID], termID);
                // Term not yet seen in the current doc but previously
                // seen in other doc(s) since the last flush

                // Now that we know doc freq for previous doc,
                // write it & lastDocCode
                if (1 == postings.termFreqs[termID])
                {
                    termsHashPerField.WriteVInt32(0, postings.lastDocCodes[termID] | 1);
                }
                else
                {
                    termsHashPerField.WriteVInt32(0, postings.lastDocCodes[termID]);
                    termsHashPerField.WriteVInt32(0, postings.termFreqs[termID]);
                }
                postings.termFreqs[termID] = 1;
                fieldState.MaxTermFrequency = Math.Max(1, fieldState.MaxTermFrequency);
                postings.lastDocCodes[termID] = (docState.docID - postings.lastDocIDs[termID]) << 1;
                postings.lastDocIDs[termID] = docState.docID;
                if (hasProx)
                {
                    WriteProx(termID, fieldState.Position);
                    if (hasOffsets)
                    {
                        postings.lastOffsets[termID] = 0;
                        WriteOffsets(termID, fieldState.Offset);
                    }
                }
                else
                {
                    if (Debugging.AssertsEnabled) Debugging.Assert(!hasOffsets);
                }
                fieldState.UniqueTermCount++;
            }
            else
            {
                fieldState.MaxTermFrequency = Math.Max(fieldState.MaxTermFrequency, ++postings.termFreqs[termID]);
                if (hasProx)
                {
                    WriteProx(termID, fieldState.Position - postings.lastPositions[termID]);
                }
                if (hasOffsets)
                {
                    WriteOffsets(termID, fieldState.Offset);
                }
            }
        }

        internal override ParallelPostingsArray CreatePostingsArray(int size)
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
                    if (Debugging.AssertsEnabled) Debugging.Assert(!writeOffsets);
                }
                //System.out.println("PA init freqs=" + writeFreqs + " pos=" + writeProx + " offs=" + writeOffsets);
            }

            internal int[] termFreqs; // # times this term occurs in the current doc
            internal int[] lastDocIDs; // Last docID where this term occurred
            internal int[] lastDocCodes; // Code for prior doc
            internal int[] lastPositions; // Last position where this term occurred
            internal int[] lastOffsets; // Last endOffset where this term occurred

            internal override ParallelPostingsArray NewInstance(int size)
            {
                return new FreqProxPostingsArray(size, termFreqs != null, lastPositions != null, lastOffsets != null);
            }

            internal override void CopyTo(ParallelPostingsArray toArray, int numToCopy)
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(toArray is FreqProxPostingsArray);
                FreqProxPostingsArray to = (FreqProxPostingsArray)toArray;

                base.CopyTo(toArray, numToCopy);

                Arrays.Copy(lastDocIDs, 0, to.lastDocIDs, 0, numToCopy);
                Arrays.Copy(lastDocCodes, 0, to.lastDocCodes, 0, numToCopy);
                if (lastPositions != null)
                {
                    if (Debugging.AssertsEnabled) Debugging.Assert(to.lastPositions != null);
                    Arrays.Copy(lastPositions, 0, to.lastPositions, 0, numToCopy);
                }
                if (lastOffsets != null)
                {
                    if (Debugging.AssertsEnabled) Debugging.Assert(to.lastOffsets != null);
                    Arrays.Copy(lastOffsets, 0, to.lastOffsets, 0, numToCopy);
                }
                if (termFreqs != null)
                {
                    if (Debugging.AssertsEnabled) Debugging.Assert(to.termFreqs != null);
                    Arrays.Copy(termFreqs, 0, to.termFreqs, 0, numToCopy);
                }
            }

            internal override int BytesPerPosting()
            {
                int bytes = ParallelPostingsArray.BYTES_PER_POSTING + 2 * RamUsageEstimator.NUM_BYTES_INT32;
                if (lastPositions != null)
                {
                    bytes += RamUsageEstimator.NUM_BYTES_INT32;
                }
                if (lastOffsets != null)
                {
                    bytes += RamUsageEstimator.NUM_BYTES_INT32;
                }
                if (termFreqs != null)
                {
                    bytes += RamUsageEstimator.NUM_BYTES_INT32;
                }

                return bytes;
            }
        }

        // LUCENENET: Removed Abort() method because it is not in use.

        internal BytesRef payload;

        /// <summary>
        /// Walk through all unique text tokens (Posting
        /// instances) found in this field and serialize them
        /// into a single RAM segment.
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal void Flush(string fieldName, FieldsConsumer consumer, SegmentWriteState state)
        {
            if (!fieldInfo.IsIndexed)
            {
                return; // nothing to flush, don't bother the codec with the unindexed field
            }

            TermsConsumer termsConsumer = consumer.AddField(fieldInfo);
            IComparer<BytesRef> termComp = termsConsumer.Comparer;

            // CONFUSING: this.indexOptions holds the index options
            // that were current when we first saw this field.  But
            // it's possible this has changed, eg when other
            // documents are indexed that cause a "downgrade" of the
            // IndexOptions.  So we must decode the in-RAM buffer
            // according to this.indexOptions, but then write the
            // new segment to the directory according to
            // currentFieldIndexOptions:
            IndexOptions currentFieldIndexOptions = fieldInfo.IndexOptions;
            if (Debugging.AssertsEnabled) Debugging.Assert(currentFieldIndexOptions != IndexOptions.NONE);

            // LUCENENET specific - to avoid boxing, changed from CompareTo() to IndexOptionsComparer.Compare()
            bool writeTermFreq = IndexOptionsComparer.Default.Compare(currentFieldIndexOptions, IndexOptions.DOCS_AND_FREQS) >= 0;
            bool writePositions = IndexOptionsComparer.Default.Compare(currentFieldIndexOptions, IndexOptions.DOCS_AND_FREQS_AND_POSITIONS) >= 0;
            bool writeOffsets = IndexOptionsComparer.Default.Compare(currentFieldIndexOptions, IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS) >= 0;

            bool readTermFreq = this.hasFreq;
            bool readPositions = this.hasProx;
            bool readOffsets = this.hasOffsets;

            //System.out.println("flush readTF=" + readTermFreq + " readPos=" + readPositions + " readOffs=" + readOffsets);

            // Make sure FieldInfo.update is working correctly!:
            if (Debugging.AssertsEnabled)
            {
                Debugging.Assert(!writeTermFreq || readTermFreq);
                Debugging.Assert(!writePositions || readPositions);
                Debugging.Assert(!writeOffsets || readOffsets);

                Debugging.Assert(!writeOffsets || writePositions);
            }

            IDictionary<Term, int> segDeletes;
            if (state.SegUpdates != null && state.SegUpdates.terms.Count > 0)
            {
                segDeletes = state.SegUpdates.terms;
            }
            else
            {
                segDeletes = null;
            }

            int[] termIDs = termsHashPerField.SortPostings(termComp);
            int numTerms = termsHashPerField.bytesHash.Count;
            BytesRef text = new BytesRef();
            FreqProxPostingsArray postings = (FreqProxPostingsArray)termsHashPerField.postingsArray;
            ByteSliceReader freq = new ByteSliceReader();
            ByteSliceReader prox = new ByteSliceReader();

            FixedBitSet visitedDocs = new FixedBitSet(state.SegmentInfo.DocCount);
            long sumTotalTermFreq = 0;
            long sumDocFreq = 0;

            Term protoTerm = new Term(fieldName);
            for (int i = 0; i < numTerms; i++)
            {
                int termID = termIDs[i];
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
                    protoTerm.Bytes = text;
                    if (segDeletes.TryGetValue(protoTerm, out int docIDUpto))
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
                        int code = freq.ReadVInt32();
                        if (!readTermFreq)
                        {
                            docID += code;
                            termFreq = -1;
                        }
                        else
                        {
                            docID += code.TripleShift(1);
                            if ((code & 1) != 0)
                            {
                                termFreq = 1;
                            }
                            else
                            {
                                termFreq = freq.ReadVInt32();
                            }
                        }

                        if (Debugging.AssertsEnabled) Debugging.Assert(docID != postings.lastDocIDs[termID]);
                    }

                    docFreq++;
                    if (Debugging.AssertsEnabled) Debugging.Assert(docID < state.SegmentInfo.DocCount,"doc={0} maxDoc={1}", docID, state.SegmentInfo.DocCount);

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
                        if (state.LiveDocs is null)
                        {
                            state.LiveDocs = docState.docWriter.codec.LiveDocsFormat.NewLiveDocs(state.SegmentInfo.DocCount);
                        }
                        if (state.LiveDocs.Get(docID))
                        {
                            state.DelCountOnFlush++;
                            state.LiveDocs.Clear(docID);
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
                        for (int j = 0; j < termFreq; j++)
                        {
                            BytesRef thisPayload;

                            if (readPositions)
                            {
                                int code = prox.ReadVInt32();
                                position += code.TripleShift(1);

                                if ((code & 1) != 0)
                                {
                                    // this position has a payload
                                    int payloadLength = prox.ReadVInt32();

                                    if (payload is null)
                                    {
                                        payload = new BytesRef();
                                        payload.Bytes = new byte[payloadLength];
                                    }
                                    else if (payload.Bytes.Length < payloadLength)
                                    {
                                        payload.Grow(payloadLength);
                                    }

                                    prox.ReadBytes(payload.Bytes, 0, payloadLength);
                                    payload.Length = payloadLength;
                                    thisPayload = payload;
                                }
                                else
                                {
                                    thisPayload = null;
                                }

                                if (readOffsets)
                                {
                                    int startOffset = offset + prox.ReadVInt32();
                                    int endOffset = startOffset + prox.ReadVInt32();
                                    if (writePositions)
                                    {
                                        if (writeOffsets)
                                        {
                                            if (Debugging.AssertsEnabled) Debugging.Assert(startOffset >= 0 && endOffset >= startOffset, "startOffset={0},endOffset={1},offset={2}", startOffset, endOffset, offset);
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

            termsConsumer.Finish(writeTermFreq ? sumTotalTermFreq : -1, sumDocFreq, visitedDocs.Cardinality);
        }
    }
}