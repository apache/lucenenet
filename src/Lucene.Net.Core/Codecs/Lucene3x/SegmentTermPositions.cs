using System;
using System.Diagnostics;

namespace Lucene.Net.Codecs.Lucene3x
{
    using Lucene.Net.Index;
    using BytesRef = Lucene.Net.Util.BytesRef;

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

    using FieldInfos = Lucene.Net.Index.FieldInfos;
    using IndexInput = Lucene.Net.Store.IndexInput;
    using Term = Lucene.Net.Index.Term;

    /// <summary>
    /// @lucene.experimental </summary>
    /// @deprecated (4.0)
    [Obsolete("(4.0)")]
    internal sealed class SegmentTermPositions : SegmentTermDocs
    {
        private IndexInput ProxStream;
        private IndexInput ProxStreamOrig;
        private int ProxCount;
        private int Position;

        private BytesRef Payload_Renamed;

        // the current payload length
        private int PayloadLength_Renamed;

        // indicates whether the payload of the current position has
        // been read from the proxStream yet
        private bool NeedToLoadPayload;

        // these variables are being used to remember information
        // for a lazy skip
        private long LazySkipPointer = -1;

        private int LazySkipProxCount = 0;

        /*
        SegmentTermPositions(SegmentReader p) {
          super(p);
          this.proxStream = null;  // the proxStream will be cloned lazily when nextPosition() is called for the first time
        }
        */

        public SegmentTermPositions(IndexInput freqStream, IndexInput proxStream, TermInfosReader tis, FieldInfos fieldInfos)
            : base(freqStream, tis, fieldInfos)
        {
            this.ProxStreamOrig = proxStream; // the proxStream will be cloned lazily when nextPosition() is called for the first time
        }

        internal override void Seek(TermInfo ti, Term term)
        {
            base.Seek(ti, term);
            if (ti != null)
            {
                LazySkipPointer = ti.ProxPointer;
            }

            LazySkipProxCount = 0;
            ProxCount = 0;
            PayloadLength_Renamed = 0;
            NeedToLoadPayload = false;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                if (ProxStream != null)
                {
                    ProxStream.Dispose();
                }
            }
        }

        public int NextPosition()
        {
            if (IndexOptions != FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS)
            // this field does not store positions, payloads
            {
                return 0;
            }
            // perform lazy skips if necessary
            LazySkip();
            ProxCount--;
            return Position += ReadDeltaPosition();
        }

        private int ReadDeltaPosition()
        {
            int delta = ProxStream.ReadVInt();
            if (CurrentFieldStoresPayloads)
            {
                // if the current field stores payloads then
                // the position delta is shifted one bit to the left.
                // if the LSB is set, then we have to read the current
                // payload length
                if ((delta & 1) != 0)
                {
                    PayloadLength_Renamed = ProxStream.ReadVInt();
                }
                delta = (int)((uint)delta >> 1);
                NeedToLoadPayload = true;
            }
            else if (delta == -1)
            {
                delta = 0; // LUCENE-1542 correction
            }
            return delta;
        }

        protected internal sealed override void SkippingDoc()
        {
            // we remember to skip a document lazily
            LazySkipProxCount += Freq_Renamed;
        }

        public sealed override bool Next()
        {
            // we remember to skip the remaining positions of the current
            // document lazily
            LazySkipProxCount += ProxCount;

            if (base.Next()) // run super
            {
                ProxCount = Freq_Renamed; // note frequency
                Position = 0; // reset position
                return true;
            }
            return false;
        }

        public sealed override int Read(int[] docs, int[] freqs)
        {
            throw new System.NotSupportedException("TermPositions does not support processing multiple documents in one call. Use TermDocs instead.");
        }

        /// <summary>
        /// Called by super.skipTo(). </summary>
        protected internal override void SkipProx(long proxPointer, int payloadLength)
        {
            // we save the pointer, we might have to skip there lazily
            LazySkipPointer = proxPointer;
            LazySkipProxCount = 0;
            ProxCount = 0;
            this.PayloadLength_Renamed = payloadLength;
            NeedToLoadPayload = false;
        }

        private void SkipPositions(int n)
        {
            Debug.Assert(IndexOptions == FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS);
            for (int f = n; f > 0; f--) // skip unread positions
            {
                ReadDeltaPosition();
                SkipPayload();
            }
        }

        private void SkipPayload()
        {
            if (NeedToLoadPayload && PayloadLength_Renamed > 0)
            {
                ProxStream.Seek(ProxStream.FilePointer + PayloadLength_Renamed);
            }
            NeedToLoadPayload = false;
        }

        // It is not always necessary to move the prox pointer
        // to a new document after the freq pointer has been moved.
        // Consider for example a phrase query with two terms:
        // the freq pointer for term 1 has to move to document x
        // to answer the question if the term occurs in that document. But
        // only if term 2 also matches document x, the positions have to be
        // read to figure out if term 1 and term 2 appear next
        // to each other in document x and thus satisfy the query.
        // So we move the prox pointer lazily to the document
        // as soon as positions are requested.
        private void LazySkip()
        {
            if (ProxStream == null)
            {
                // clone lazily
                ProxStream = (IndexInput)ProxStreamOrig.Clone();
            }

            // we might have to skip the current payload
            // if it was not read yet
            SkipPayload();

            if (LazySkipPointer != -1)
            {
                ProxStream.Seek(LazySkipPointer);
                LazySkipPointer = -1;
            }

            if (LazySkipProxCount != 0)
            {
                SkipPositions(LazySkipProxCount);
                LazySkipProxCount = 0;
            }
        }

        public int PayloadLength
        {
            get
            {
                return PayloadLength_Renamed;
            }
        }

        public BytesRef Payload
        {
            get
            {
                if (PayloadLength_Renamed <= 0)
                {
                    return null; // no payload
                }

                if (NeedToLoadPayload)
                {
                    // read payloads lazily
                    if (Payload_Renamed == null)
                    {
                        Payload_Renamed = new BytesRef(PayloadLength_Renamed);
                    }
                    else
                    {
                        Payload_Renamed.Grow(PayloadLength_Renamed);
                    }

                    ProxStream.ReadBytes(Payload_Renamed.Bytes, Payload_Renamed.Offset, PayloadLength_Renamed);
                    Payload_Renamed.Length = PayloadLength_Renamed;
                    NeedToLoadPayload = false;
                }
                return Payload_Renamed;
            }
        }

        public bool IsPayloadAvailable
        {
            get
            {
                return NeedToLoadPayload && PayloadLength_Renamed > 0;
            }
        }
    }
}