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
using System.Collections.Generic;
using System.Diagnostics;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Util;

namespace Lucene.Net.Codecs.Pulsing
{

    /// <summary>
    /// TODO: we now inline based on total TF of the term,
    /// but it might be better to inline by "net bytes used"
    /// so that a term that has only 1 posting but a huge
    /// payload would not be inlined.  Though this is
    /// presumably rare in practice...
    /// 
    /// Writer for the pulsing format. 
    ///
    /// Wraps another postings implementation and decides 
    /// (based on total number of occurrences), whether a terms 
    /// postings should be inlined into the term dictionary,
    /// or passed through to the wrapped writer.
    ///
    /// @lucene.experimental
    /// </summary>
    public sealed class PulsingPostingsWriter : PostingsWriterBase
    {

        internal static readonly String CODEC = "PulsedPostingsWriter";
        internal static readonly String SUMMARY_EXTENSION = "smy";         // recording field summary
        
        // To add a new version, increment from the last one, and
        // change VERSION_CURRENT to point to your new version:
        internal static readonly int VERSION_START = 0;
        internal static readonly int VERSION_META_ARRAY = 1;
        internal static readonly int VERSION_CURRENT = VERSION_META_ARRAY;

        private SegmentWriteState segmentState;
        private IndexOutput termsOut;
        private List<FieldMetaData> fields;
        private FieldInfo.IndexOptions_e? indexOptions;
        private bool storePayloads;

        // information for wrapped PF, in current field
        private int longsSize;
        private long[] longs;
        private bool absolute;

        private class PulsingTermState : BlockTermState
        {
            internal byte[] bytes;
            internal BlockTermState wrappedState;

            public override String ToString()
            {
                if (bytes != null)
                {
                    return "inlined";
                }
                return "not inlined wrapped=" + wrappedState;
            }
        }

        // one entry per position
        private Position[] pending;
        private int pendingCount = 0;   // -1 once we've hit too many positions
        private Position currentDoc;    // first Position entry of current doc

        private sealed class Position
        {
            internal BytesRef payload;
            internal int termFreq; // only incremented on first position for a given doc
            internal int pos;
            internal int docID;
            internal int startOffset;
            internal int endOffset;
        }

        private class FieldMetaData
        {
            public int FieldNumber { get; private set; }
            public int LongsSize { get; private set; }

            public FieldMetaData(int number, int size)
            {
                FieldNumber = number;
                LongsSize = size;
            }
        }

        // TODO: -- lazy init this?  ie, if every single term
        // was inlined (eg for a "primary key" field) then we
        // never need to use this fallback?  Fallback writer for
        // non-inlined terms:
        private readonly PostingsWriterBase _wrappedPostingsWriter;

        /// <summary>
        /// If the total number of positions (summed across all docs
        /// for this term) is <= maxPositions, then the postings are
        /// inlined into terms dict
        /// </summary>
        public PulsingPostingsWriter(SegmentWriteState state, int maxPositions, PostingsWriterBase wrappedPostingsWriter)
        {

            pending = new Position[maxPositions];
            for (int i = 0; i < maxPositions; i++)
            {
                pending[i] = new Position();
            }
            fields = new List<FieldMetaData>();

            // We simply wrap another postings writer, but only call
            // on it when tot positions is >= the cutoff:
            this._wrappedPostingsWriter = wrappedPostingsWriter;
            this.segmentState = state;
        }

        public override void Init(IndexOutput termsOut)
        {
            this.termsOut = termsOut;
            CodecUtil.WriteHeader(termsOut, CODEC, VERSION_CURRENT);
            termsOut.WriteVInt(pending.Length); // encode maxPositions in header
            _wrappedPostingsWriter.Init(termsOut);
        }

        public override BlockTermState NewTermState()
        {
            PulsingTermState state = new PulsingTermState();
            state.wrappedState = _wrappedPostingsWriter.NewTermState();
            return state;
        }

        public override void StartTerm()
        {
            Debug.Debug.Assert((pendingCount == 0);
        }

        /// <summary>
        /// TODO: -- should we NOT reuse across fields?  would
        /// be cleaner
        /// Currently, this instance is re-used across fields, so
        /// our parent calls setField whenever the field changes
        /// </summary>
        /// <param name="fieldInfo"></param>
        /// <returns></returns>
        public override int SetField(FieldInfo fieldInfo)
        {
            this.indexOptions = fieldInfo.IndexOptions;
            storePayloads = fieldInfo.HasPayloads();
            absolute = false;
            longsSize = _wrappedPostingsWriter.SetField(fieldInfo);
            longs = new long[longsSize];
            fields.Add(new FieldMetaData(fieldInfo.Number, longsSize));
            return 0;
        }

        public override void StartDoc(int docID, int termDocFreq)
        {
            Debug.Debug.Assert((docID >= 0, "Got DocID=" + docID);

            if (pendingCount == pending.Length)
            {
                push();
                _wrappedPostingsWriter.FinishDoc();
            }

            if (pendingCount != -1)
            {
                Debug.Debug.Assert((pendingCount < pending.Length);
                currentDoc = pending[pendingCount];
                currentDoc.docID = docID;
                if (indexOptions == FieldInfo.IndexOptions_e.DOCS_ONLY)
                {
                    pendingCount++;
                }
                else if (indexOptions == FieldInfo.IndexOptions_e.DOCS_AND_FREQS)
                {
                    pendingCount++;
                    currentDoc.termFreq = termDocFreq;
                }
                else
                {
                    currentDoc.termFreq = termDocFreq;
                }
            }
            else
            {
                // We've already seen too many docs for this term --
                // just forward to our fallback writer
                _wrappedPostingsWriter.StartDoc(docID, termDocFreq);
            }
        }

        public override void AddPosition(int position, BytesRef payload, int startOffset, int endOffset)
        {

            if (pendingCount == pending.Length)
            {
                push();
            }

            if (pendingCount == -1)
            {
                // We've already seen too many docs for this term --
                // just forward to our fallback writer
                _wrappedPostingsWriter.AddPosition(position, payload, startOffset, endOffset);
            }
            else
            {
                // buffer up
                Position pos = pending[pendingCount++];
                pos.pos = position;
                pos.startOffset = startOffset;
                pos.endOffset = endOffset;
                pos.docID = currentDoc.docID;
                if (payload != null && payload.Length > 0)
                {
                    if (pos.payload == null)
                    {
                        pos.payload = BytesRef.DeepCopyOf(payload);
                    }
                    else
                    {
                        pos.payload.CopyBytes(payload);
                    }
                }
                else if (pos.payload != null)
                {
                    pos.payload.Length = 0;
                }
            }
        }

        public override void FinishDoc()
        {
            if (pendingCount == -1)
            {
                _wrappedPostingsWriter.FinishDoc();
            }
        }

        private readonly RAMOutputStream buffer = new RAMOutputStream();

        /// <summary>
        /// Called when we are done adding docs to this term
        /// </summary>
        /// <param name="_state"></param>
        public override void FinishTerm(BlockTermState _state)
        {
            PulsingTermState state = (PulsingTermState) _state;

            Debug.Debug.Assert((pendingCount > 0 || pendingCount == -1);

            if (pendingCount == -1)
            {
                state.wrappedState.DocFreq = state.DocFreq;
                state.wrappedState.TotalTermFreq = state.TotalTermFreq;
                state.bytes = null;
                _wrappedPostingsWriter.FinishTerm(state.wrappedState);
            }
            else
            {
                // There were few enough total occurrences for this
                // term, so we fully inline our postings data into
                // terms dict, now:

                // TODO: it'd be better to share this encoding logic
                // in some inner codec that knows how to write a
                // single doc / single position, etc.  This way if a
                // given codec wants to store other interesting
                // stuff, it could use this pulsing codec to do so

                if (indexOptions.Value.CompareTo(FieldInfo.IndexOptions_e.DOCS_AND_FREQS_AND_POSITIONS) >= 0)
                {
                    int lastDocID = 0;
                    int pendingIDX = 0;
                    int lastPayloadLength = -1;
                    int lastOffsetLength = -1;
                    while (pendingIDX < pendingCount)
                    {
                        Position doc = pending[pendingIDX];

                        int delta = doc.docID - lastDocID;
                        lastDocID = doc.docID;

                        // if (DEBUG) System.out.println("  write doc=" + doc.docID + " freq=" + doc.termFreq);

                        if (doc.termFreq == 1)
                        {
                            buffer.WriteVInt((delta << 1) | 1);
                        }
                        else
                        {
                            buffer.WriteVInt(delta << 1);
                            buffer.WriteVInt(doc.termFreq);
                        }

                        int lastPos = 0;
                        int lastOffset = 0;
                        for (int posIDX = 0; posIDX < doc.termFreq; posIDX++)
                        {
                            Position pos = pending[pendingIDX++];
                            Debug.Debug.Assert((pos.docID == doc.docID);
                            int posDelta = pos.pos - lastPos;
                            lastPos = pos.pos;
                            
                            int payloadLength = pos.payload == null ? 0 : pos.payload.Length;
                            if (storePayloads)
                            {
                                if (payloadLength != lastPayloadLength)
                                {
                                    buffer.WriteVInt((posDelta << 1) | 1);
                                    buffer.WriteVInt(payloadLength);
                                    lastPayloadLength = payloadLength;
                                }
                                else
                                {
                                    buffer.WriteVInt(posDelta << 1);
                                }
                            }
                            else
                            {
                                buffer.WriteVInt(posDelta);
                            }

                            if (indexOptions.Value.CompareTo(FieldInfo.IndexOptions_e.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS) >= 0)
                            {
                                //System.out.println("write=" + pos.startOffset + "," + pos.endOffset);
                                int offsetDelta = pos.startOffset - lastOffset;
                                int offsetLength = pos.endOffset - pos.startOffset;
                                if (offsetLength != lastOffsetLength)
                                {
                                    buffer.WriteVInt(offsetDelta << 1 | 1);
                                    buffer.WriteVInt(offsetLength);
                                }
                                else
                                {
                                    buffer.WriteVInt(offsetDelta << 1);
                                }
                                lastOffset = pos.startOffset;
                                lastOffsetLength = offsetLength;
                            }

                            if (payloadLength > 0)
                            {
                                Debug.Debug.Assert((storePayloads);
                                buffer.WriteBytes(pos.payload.Bytes, 0, pos.payload.Length);
                            }
                        }
                    }
                }
                else if (indexOptions == FieldInfo.IndexOptions_e.DOCS_AND_FREQS)
                {
                    int lastDocID = 0;
                    for (int posIDX = 0; posIDX < pendingCount; posIDX++)
                    {
                        Position doc = pending[posIDX];
                        int delta = doc.docID - lastDocID;
                        Debug.Debug.Assert((doc.termFreq != 0);

                        if (doc.termFreq == 1)
                        {
                            buffer.WriteVInt((delta << 1) | 1);
                        }
                        else
                        {
                            buffer.WriteVInt(delta << 1);
                            buffer.WriteVInt(doc.termFreq);
                        }
                        lastDocID = doc.docID;
                    }
                }
                else if (indexOptions == FieldInfo.IndexOptions_e.DOCS_ONLY)
                {
                    int lastDocID = 0;
                    for (int posIDX = 0; posIDX < pendingCount; posIDX++)
                    {
                        Position doc = pending[posIDX];
                        buffer.WriteVInt(doc.docID - lastDocID);
                        lastDocID = doc.docID;
                    }
                }

                state.bytes = new byte[(int) buffer.FilePointer];
                buffer.WriteTo((sbyte[])(Array)state.bytes, 0);
                buffer.Reset();
            }
            pendingCount = 0;
        }

        public override void EncodeTerm(long[] empty, DataOutput output, FieldInfo fieldInfo, BlockTermState _state,
            bool absolute)
        {
            PulsingTermState state = (PulsingTermState) _state;
            Debug.Debug.Assert((empty.Length == 0);
            this.absolute = this.absolute || absolute;
            if (state.bytes == null)
            {
                _wrappedPostingsWriter.EncodeTerm(longs, buffer, fieldInfo, state.wrappedState, this.absolute);
                for (int i = 0; i < longsSize; i++)
                {
                    output.WriteVLong(longs[i]);
                }
                buffer.WriteTo(output);
                buffer.Reset();
                this.absolute = false;
            }
            else
            {
                output.WriteVInt(state.bytes.Length);
                output.WriteBytes(state.bytes, 0, state.bytes.Length);
                this.absolute = this.absolute || absolute;
            }
        }

        protected override void Dispose(bool disposing)
        {
            _wrappedPostingsWriter.Dispose();

            if (_wrappedPostingsWriter is PulsingPostingsWriter ||
                VERSION_CURRENT < VERSION_META_ARRAY)
            {
                return;
            }

            String summaryFileName = IndexFileNames.SegmentFileName(segmentState.SegmentInfo.Name,
                segmentState.SegmentSuffix, SUMMARY_EXTENSION);
            IndexOutput output = null;
            try
            {
                output =
                    segmentState.Directory.CreateOutput(summaryFileName, segmentState.Context);
                CodecUtil.WriteHeader(output, CODEC, VERSION_CURRENT);
                output.WriteVInt(fields.Count);
                foreach (FieldMetaData field in fields)
                {
                    output.WriteVInt(field.FieldNumber);
                    output.WriteVInt(field.LongsSize);
                }
                output.Dispose();
            }
            finally
            {
                IOUtils.CloseWhileHandlingException(output);
            }
        }

        // Pushes pending positions to the wrapped codec
        private void push()
        {
            // if (DEBUG) System.out.println("PW now push @ " + pendingCount + " wrapped=" + wrappedPostingsWriter);
            Debug.Debug.Assert((pendingCount == pending.Length);

            _wrappedPostingsWriter.StartTerm();

            // Flush all buffered docs
            if (indexOptions.Value.CompareTo(FieldInfo.IndexOptions_e.DOCS_AND_FREQS_AND_POSITIONS) >= 0)
            {
                Position doc = null;

                foreach(Position pos in pending)
                {
                    if (doc == null)
                    {
                        doc = pos;
                        // if (DEBUG) System.out.println("PW: wrapped.startDoc docID=" + doc.docID + " tf=" + doc.termFreq);
                        _wrappedPostingsWriter.StartDoc(doc.docID, doc.termFreq);
                    }
                    else if (doc.docID != pos.docID)
                    {
                        Debug.Debug.Assert((pos.docID > doc.docID);
                        // if (DEBUG) System.out.println("PW: wrapped.finishDoc");
                        _wrappedPostingsWriter.FinishDoc();
                        doc = pos;
                        // if (DEBUG) System.out.println("PW: wrapped.startDoc docID=" + doc.docID + " tf=" + doc.termFreq);
                        _wrappedPostingsWriter.StartDoc(doc.docID, doc.termFreq);
                    }
                    // if (DEBUG) System.out.println("PW:   wrapped.addPos pos=" + pos.pos);
                    _wrappedPostingsWriter.AddPosition(pos.pos, pos.payload, pos.startOffset, pos.endOffset);
                }
                //wrappedPostingsWriter.finishDoc();
            }
            else
            {
                foreach(Position doc in pending)
                {
                    _wrappedPostingsWriter.StartDoc(doc.docID, indexOptions == FieldInfo.IndexOptions_e.DOCS_ONLY ? 0 : doc.termFreq);
                }
            }
            pendingCount = -1;
        }
    }
}
