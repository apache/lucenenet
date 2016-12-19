using Lucene.Net.Analysis.TokenAttributes;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Lucene.Net.Index
{
    using BytesRef = Lucene.Net.Util.BytesRef;
    using FieldsConsumer = Lucene.Net.Codecs.FieldsConsumer;
    using FixedBitSet = Lucene.Net.Util.FixedBitSet;
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

    using OffsetAttribute = Lucene.Net.Analysis.TokenAttributes.OffsetAttribute;
    using PayloadAttribute = Lucene.Net.Analysis.TokenAttributes.PayloadAttribute;
    using PostingsConsumer = Lucene.Net.Codecs.PostingsConsumer;
    using RamUsageEstimator = Lucene.Net.Util.RamUsageEstimator;
    using TermsConsumer = Lucene.Net.Codecs.TermsConsumer;
    using TermStats = Lucene.Net.Codecs.TermStats;

    // TODO: break into separate freq and prox writers as
    // codecs; make separate container (tii/tis/skip/*) that can
    // be configured as any number of files 1..N
    public sealed class FreqProxTermsWriterPerField : TermsHashConsumerPerField, IComparable<FreqProxTermsWriterPerField>
    {
        internal readonly FreqProxTermsWriter Parent;
        internal readonly TermsHashPerField TermsHashPerField;
        internal readonly FieldInfo fieldInfo;
        internal readonly DocumentsWriterPerThread.DocState DocState;
        internal readonly FieldInvertState FieldState;
        private bool HasFreq;
        private bool HasProx;
        private bool HasOffsets;
        internal IPayloadAttribute PayloadAttribute;
        internal IOffsetAttribute OffsetAttribute;

        public FreqProxTermsWriterPerField(TermsHashPerField termsHashPerField, FreqProxTermsWriter parent, FieldInfo fieldInfo)
        {
            this.TermsHashPerField = termsHashPerField;
            this.Parent = parent;
            this.fieldInfo = fieldInfo;
            DocState = termsHashPerField.DocState;
            FieldState = termsHashPerField.FieldState;
            IndexOptions = fieldInfo.FieldIndexOptions;
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
                fieldInfo.SetStorePayloads();
            }
        }

        internal bool HasPayloads;

        internal override void SkippingLongTerm()
        {
        }

        public int CompareTo(FreqProxTermsWriterPerField other)
        {
            return fieldInfo.Name.CompareTo(other.fieldInfo.Name);
        }

        // Called after flush
        internal void Reset()
        {
            // Record, up front, whether our in-RAM format will be
            // with or without term freqs:
            IndexOptions = fieldInfo.FieldIndexOptions;
            PayloadAttribute = null;
        }

        private FieldInfo.IndexOptions? IndexOptions
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
                    HasFreq = value >= FieldInfo.IndexOptions.DOCS_AND_FREQS;
                    HasProx = value >= FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS;
                    HasOffsets = value >= FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS;
                }
            }
        }

        internal override bool Start(IndexableField[] fields, int count)
        {
            for (int i = 0; i < count; i++)
            {
                if (fields[i].FieldType.IsIndexed)
                {
                    return true;
                }
            }
            return false;
        }

        internal override void Start(IndexableField f)
        {
            if (FieldState.AttributeSource_Renamed.HasAttribute<IPayloadAttribute>())
            {
                PayloadAttribute = FieldState.AttributeSource_Renamed.GetAttribute<IPayloadAttribute>();
            }
            else
            {
                PayloadAttribute = null;
            }
            if (HasOffsets)
            {
                OffsetAttribute = FieldState.AttributeSource_Renamed.AddAttribute<IOffsetAttribute>();
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

            FreqProxPostingsArray postings = (FreqProxPostingsArray)TermsHashPerField.PostingsArray;
            postings.LastPositions[termID] = FieldState.Position_Renamed;
        }

        internal void WriteOffsets(int termID, int offsetAccum)
        {
            Debug.Assert(HasOffsets);
            int startOffset = offsetAccum + OffsetAttribute.StartOffset;
            int endOffset = offsetAccum + OffsetAttribute.EndOffset;
            FreqProxPostingsArray postings = (FreqProxPostingsArray)TermsHashPerField.PostingsArray;
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

            FreqProxPostingsArray postings = (FreqProxPostingsArray)TermsHashPerField.PostingsArray;
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

            FreqProxPostingsArray postings = (FreqProxPostingsArray)TermsHashPerField.PostingsArray;

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
            public FreqProxPostingsArray(int size, bool writeFreqs, bool writeProx, bool writeOffsets)
                : base(size)
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
                FreqProxPostingsArray to = (FreqProxPostingsArray)toArray;

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
            if (!fieldInfo.Indexed)
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
            FieldInfo.IndexOptions? currentFieldIndexOptions = fieldInfo.FieldIndexOptions;
            Debug.Assert(currentFieldIndexOptions != null);

            bool writeTermFreq = currentFieldIndexOptions >= FieldInfo.IndexOptions.DOCS_AND_FREQS;
            bool writePositions = currentFieldIndexOptions >= FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS;
            bool writeOffsets = currentFieldIndexOptions >= FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS;

            bool readTermFreq = this.HasFreq;
            bool readPositions = this.HasProx;
            bool readOffsets = this.HasOffsets;

            //System.out.println("flush readTF=" + readTermFreq + " readPos=" + readPositions + " readOffs=" + readOffsets);

            // Make sure FieldInfo.update is working correctly!:
            Debug.Assert(!writeTermFreq || readTermFreq);
            Debug.Assert(!writePositions || readPositions);
            Debug.Assert(!writeOffsets || readOffsets);

            Debug.Assert(!writeOffsets || writePositions);

            IDictionary<Term, int?> segDeletes;
            if (state.SegUpdates != null && state.SegUpdates.Terms.Count > 0)
            {
                segDeletes = state.SegUpdates.Terms;
            }
            else
            {
                segDeletes = null;
            }

            int[] termIDs = TermsHashPerField.SortPostings(termComp);
            int numTerms = TermsHashPerField.BytesHash.Size();
            BytesRef text = new BytesRef();
            FreqProxPostingsArray postings = (FreqProxPostingsArray)TermsHashPerField.PostingsArray;
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
                int textStart = postings.TextStarts[termID];
                TermsHashPerField.BytePool.SetBytesRef(text, textStart);

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

                PostingsConsumer postingsConsumer = termsConsumer.StartTerm(text);

                int? delDocLimit;
                if (segDeletes != null)
                {
                    protoTerm.Bytes = text;
                    int? docIDUpto;
                    segDeletes.TryGetValue(protoTerm, out docIDUpto);
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
                            state.LiveDocs = DocState.DocWriter.Codec.LiveDocsFormat.NewLiveDocs(state.SegmentInfo.DocCount);
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
                                int code = prox.ReadVInt();
                                position += (int)((uint)code >> 1);

                                if ((code & 1) != 0)
                                {
                                    // this position has a payload
                                    int payloadLength = prox.ReadVInt();

                                    if (Payload == null)
                                    {
                                        Payload = new BytesRef();
                                        Payload.Bytes = new byte[payloadLength];
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
                                    int startOffset = offset + prox.ReadVInt();
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