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

namespace Lucene.Net.Codecs.Pulsing
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using Index;
    using Store;
    using Util;

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

        private readonly SegmentWriteState _segmentState;
        private IndexOutput _termsOut;
        private readonly List<FieldMetaData> _fields;
        private IndexOptions? _indexOptions;
        private bool _storePayloads;

        // information for wrapped PF, in current field
        private int _longsSize;
        private long[] _longs;
        private bool _absolute;

        private class PulsingTermState : BlockTermState
        {
            internal byte[] BYTES;
            internal BlockTermState WRAPPED_STATE;

            public override String ToString()
            {
                if (BYTES != null)
                {
                    return "inlined";
                }
                return "not inlined wrapped=" + WRAPPED_STATE;
            }
        }

        // one entry per position
        private readonly Position[] _pending;
        private int _pendingCount = 0;   // -1 once we've hit too many positions
        private Position _currentDoc;    // first Position entry of current doc

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
        /// for this term) is less than or equal maxPositions, then the postings are
        /// inlined into terms dict
        /// </summary>
        public PulsingPostingsWriter(SegmentWriteState state, int maxPositions, PostingsWriterBase wrappedPostingsWriter)
        {

            _pending = new Position[maxPositions];
            for (var i = 0; i < maxPositions; i++)
            {
                _pending[i] = new Position();
            }
            _fields = new List<FieldMetaData>();

            // We simply wrap another postings writer, but only call
            // on it when tot positions is >= the cutoff:
            _wrappedPostingsWriter = wrappedPostingsWriter;
            _segmentState = state;
        }

        public override void Init(IndexOutput termsOut)
        {
            _termsOut = termsOut;
            CodecUtil.WriteHeader(termsOut, CODEC, VERSION_CURRENT);
            termsOut.WriteVInt(_pending.Length); // encode maxPositions in header
            _wrappedPostingsWriter.Init(termsOut);
        }

        public override BlockTermState NewTermState()
        {
            var state = new PulsingTermState {WRAPPED_STATE = _wrappedPostingsWriter.NewTermState()};
            return state;
        }

        public override void StartTerm()
        {
            Debug.Assert(_pendingCount == 0);
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
            _indexOptions = fieldInfo.IndexOptions;
            _storePayloads = fieldInfo.HasPayloads;
            _absolute = false;
            _longsSize = _wrappedPostingsWriter.SetField(fieldInfo);
            _longs = new long[_longsSize];
            _fields.Add(new FieldMetaData(fieldInfo.Number, _longsSize));
            return 0;
        }

        public override void StartDoc(int docId, int termDocFreq)
        {
            Debug.Assert(docId >= 0, "Got DocID=" + docId);

            if (_pendingCount == _pending.Length)
            {
                Push();
                _wrappedPostingsWriter.FinishDoc();
            }

            if (_pendingCount != -1)
            {
                Debug.Assert(_pendingCount < _pending.Length);
                _currentDoc = _pending[_pendingCount];
                _currentDoc.docID = docId;
                if (_indexOptions == IndexOptions.DOCS_ONLY)
                {
                    _pendingCount++;
                }
                else if (_indexOptions == IndexOptions.DOCS_AND_FREQS)
                {
                    _pendingCount++;
                    _currentDoc.termFreq = termDocFreq;
                }
                else
                {
                    _currentDoc.termFreq = termDocFreq;
                }
            }
            else
            {
                // We've already seen too many docs for this term --
                // just forward to our fallback writer
                _wrappedPostingsWriter.StartDoc(docId, termDocFreq);
            }
        }

        public override void AddPosition(int position, BytesRef payload, int startOffset, int endOffset)
        {

            if (_pendingCount == _pending.Length)
            {
                Push();
            }

            if (_pendingCount == -1)
            {
                // We've already seen too many docs for this term --
                // just forward to our fallback writer
                _wrappedPostingsWriter.AddPosition(position, payload, startOffset, endOffset);
            }
            else
            {
                // buffer up
                Position pos = _pending[_pendingCount++];
                pos.pos = position;
                pos.startOffset = startOffset;
                pos.endOffset = endOffset;
                pos.docID = _currentDoc.docID;
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
            if (_pendingCount == -1)
            {
                _wrappedPostingsWriter.FinishDoc();
            }
        }

        private readonly RAMOutputStream _buffer = new RAMOutputStream();

        /// <summary>
        /// Called when we are done adding docs to this term
        /// </summary>
        /// <param name="_state"></param>
        public override void FinishTerm(BlockTermState _state)
        {
            var state = (PulsingTermState) _state;

            Debug.Assert(_pendingCount > 0 || _pendingCount == -1);

            if (_pendingCount == -1)
            {
                state.WRAPPED_STATE.DocFreq = state.DocFreq;
                state.WRAPPED_STATE.TotalTermFreq = state.TotalTermFreq;
                state.BYTES = null;
                _wrappedPostingsWriter.FinishTerm(state.WRAPPED_STATE);
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

                if (_indexOptions >= IndexOptions.DOCS_AND_FREQS_AND_POSITIONS)
                {
                    var lastDocID = 0;
                    var pendingIDX = 0;
                    var lastPayloadLength = -1;
                    var lastOffsetLength = -1;
                    while (pendingIDX < _pendingCount)
                    {
                        var doc = _pending[pendingIDX];

                        var delta = doc.docID - lastDocID;
                        lastDocID = doc.docID;

                        // if (DEBUG) System.out.println("  write doc=" + doc.docID + " freq=" + doc.termFreq);

                        if (doc.termFreq == 1)
                        {
                            _buffer.WriteVInt((delta << 1) | 1);
                        }
                        else
                        {
                            _buffer.WriteVInt(delta << 1);
                            _buffer.WriteVInt(doc.termFreq);
                        }

                        var lastPos = 0;
                        var lastOffset = 0;
                        for (var posIDX = 0; posIDX < doc.termFreq; posIDX++)
                        {
                            var pos = _pending[pendingIDX++];
                            Debug.Assert(pos.docID == doc.docID);
                            var posDelta = pos.pos - lastPos;
                            lastPos = pos.pos;
                            
                            var payloadLength = pos.payload == null ? 0 : pos.payload.Length;
                            if (_storePayloads)
                            {
                                if (payloadLength != lastPayloadLength)
                                {
                                    _buffer.WriteVInt((posDelta << 1) | 1);
                                    _buffer.WriteVInt(payloadLength);
                                    lastPayloadLength = payloadLength;
                                }
                                else
                                {
                                    _buffer.WriteVInt(posDelta << 1);
                                }
                            }
                            else
                            {
                                _buffer.WriteVInt(posDelta);
                            }

                            if (_indexOptions >= IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS)
                            {
                                //System.out.println("write=" + pos.startOffset + "," + pos.endOffset);
                                var offsetDelta = pos.startOffset - lastOffset;
                                var offsetLength = pos.endOffset - pos.startOffset;
                                if (offsetLength != lastOffsetLength)
                                {
                                    _buffer.WriteVInt(offsetDelta << 1 | 1);
                                    _buffer.WriteVInt(offsetLength);
                                }
                                else
                                {
                                    _buffer.WriteVInt(offsetDelta << 1);
                                }
                                lastOffset = pos.startOffset;
                                lastOffsetLength = offsetLength;
                            }

                            if (payloadLength > 0)
                            {
                                Debug.Assert(_storePayloads);
                                _buffer.WriteBytes(pos.payload.Bytes, 0, pos.payload.Length);
                            }
                        }
                    }
                }
                else switch (_indexOptions)
                {
                    case IndexOptions.DOCS_AND_FREQS:
                    {
                        var lastDocId = 0;
                        for (var posIdx = 0; posIdx < _pendingCount; posIdx++)
                        {
                            var doc = _pending[posIdx];
                            var delta = doc.docID - lastDocId;

                            Debug.Assert(doc.termFreq != 0);

                            if (doc.termFreq == 1)
                            {
                                _buffer.WriteVInt((delta << 1) | 1);
                            }
                            else
                            {
                                _buffer.WriteVInt(delta << 1);
                                _buffer.WriteVInt(doc.termFreq);
                            }
                            lastDocId = doc.docID;
                        }
                    }
                        break;
                    case IndexOptions.DOCS_ONLY:
                    {
                        var lastDocId = 0;
                        for (var posIdx = 0; posIdx < _pendingCount; posIdx++)
                        {
                            var doc = _pending[posIdx];
                            _buffer.WriteVInt(doc.docID - lastDocId);
                            lastDocId = doc.docID;
                        }
                    }
                        break;
                }

                state.BYTES = new byte[(int) _buffer.FilePointer];
                _buffer.WriteTo(state.BYTES, 0);
                _buffer.Reset();
            }
            _pendingCount = 0;
        }

        public override void EncodeTerm(long[] empty, DataOutput output, FieldInfo fieldInfo, BlockTermState state,
            bool abs)
        {
            var _state = (PulsingTermState) state;
            Debug.Assert(empty.Length == 0);
            _absolute = _absolute || abs;
            if (_state.BYTES == null)
            {
                _wrappedPostingsWriter.EncodeTerm(_longs, _buffer, fieldInfo, _state.WRAPPED_STATE, _absolute);
                for (var i = 0; i < _longsSize; i++)
                {
                    output.WriteVLong(_longs[i]);
                }
                _buffer.WriteTo(output);
                _buffer.Reset();
                _absolute = false;
            }
            else
            {
                output.WriteVInt(_state.BYTES.Length);
                output.WriteBytes(_state.BYTES, 0, _state.BYTES.Length);
                _absolute = _absolute || abs;
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

            var summaryFileName = IndexFileNames.SegmentFileName(_segmentState.SegmentInfo.Name,
                _segmentState.SegmentSuffix, SUMMARY_EXTENSION);
            IndexOutput output = null;
            try
            {
                output =
                    _segmentState.Directory.CreateOutput(summaryFileName, _segmentState.Context);
                CodecUtil.WriteHeader(output, CODEC, VERSION_CURRENT);
                output.WriteVInt(_fields.Count);
                foreach (var field in _fields)
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
        private void Push()
        {
            Debug.Assert(_pendingCount == _pending.Length);

            _wrappedPostingsWriter.StartTerm();

            // Flush all buffered docs
            if (_indexOptions >= IndexOptions.DOCS_AND_FREQS_AND_POSITIONS)
            {
                Position doc = null;

                foreach(var pos in _pending)
                {
                    if (doc == null)
                    {
                        doc = pos;
                        _wrappedPostingsWriter.StartDoc(doc.docID, doc.termFreq);
                    }
                    else if (doc.docID != pos.docID)
                    {
                        Debug.Assert(pos.docID > doc.docID);
                        _wrappedPostingsWriter.FinishDoc();
                        doc = pos;
                        _wrappedPostingsWriter.StartDoc(doc.docID, doc.termFreq);
                    }
                    _wrappedPostingsWriter.AddPosition(pos.pos, pos.payload, pos.startOffset, pos.endOffset);
                }
                //wrappedPostingsWriter.finishDoc();
            }
            else
            {
                foreach(var doc in _pending)
                {
                    _wrappedPostingsWriter.StartDoc(doc.docID, _indexOptions == IndexOptions.DOCS_ONLY ? 0 : doc.termFreq);
                }
            }
            _pendingCount = -1;
        }
    }
}
