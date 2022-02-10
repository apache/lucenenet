using J2N.Numerics;
using Lucene.Net.Diagnostics;
using Lucene.Net.Index;
using System;
using System.Runtime.CompilerServices;
using BytesRef = Lucene.Net.Util.BytesRef;

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

    using FieldInfos = Lucene.Net.Index.FieldInfos;
    using IndexInput = Lucene.Net.Store.IndexInput;
    using Term = Lucene.Net.Index.Term;

    /// <summary>
    /// @lucene.experimental 
    /// </summary>
    [Obsolete("(4.0)")]
    internal sealed class SegmentTermPositions : SegmentTermDocs
    {
        private IndexInput proxStream;
        private readonly IndexInput proxStreamOrig; // LUCENENET: marked readonly
        private int proxCount;
        private int position;

        private BytesRef payload;

        // the current payload length
        private int payloadLength;

        // indicates whether the payload of the current position has
        // been read from the proxStream yet
        private bool needToLoadPayload;

        // these variables are being used to remember information
        // for a lazy skip
        private long lazySkipPointer = -1;

        private int lazySkipProxCount = 0;

        /*
        SegmentTermPositions(SegmentReader p) {
          super(p);
          this.proxStream = null;  // the proxStream will be cloned lazily when nextPosition() is called for the first time
        }
        */

        public SegmentTermPositions(IndexInput freqStream, IndexInput proxStream, TermInfosReader tis, FieldInfos fieldInfos)
            : base(freqStream, tis, fieldInfos)
        {
            this.proxStreamOrig = proxStream; // the proxStream will be cloned lazily when nextPosition() is called for the first time
        }

        internal override void Seek(TermInfo ti, Term term)
        {
            base.Seek(ti, term);
            if (ti != null)
            {
                lazySkipPointer = ti.ProxPointer;
            }

            lazySkipProxCount = 0;
            proxCount = 0;
            payloadLength = 0;
            needToLoadPayload = false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
                proxStream?.Dispose();
        }

        public int NextPosition()
        {
            if (m_indexOptions != IndexOptions.DOCS_AND_FREQS_AND_POSITIONS)
            // this field does not store positions, payloads
            {
                return 0;
            }
            // perform lazy skips if necessary
            LazySkip();
            proxCount--;
            return position += ReadDeltaPosition();
        }

        private int ReadDeltaPosition()
        {
            int delta = proxStream.ReadVInt32();
            if (m_currentFieldStoresPayloads)
            {
                // if the current field stores payloads then
                // the position delta is shifted one bit to the left.
                // if the LSB is set, then we have to read the current
                // payload length
                if ((delta & 1) != 0)
                {
                    payloadLength = proxStream.ReadVInt32();
                }
                delta = delta.TripleShift(1);
                needToLoadPayload = true;
            }
            else if (delta == -1)
            {
                delta = 0; // LUCENE-1542 correction
            }
            return delta;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected internal sealed override void SkippingDoc()
        {
            // we remember to skip a document lazily
            lazySkipProxCount += freq;
        }

        public sealed override bool Next()
        {
            // we remember to skip the remaining positions of the current
            // document lazily
            lazySkipProxCount += proxCount;

            if (base.Next()) // run super
            {
                proxCount = freq; // note frequency
                position = 0; // reset position
                return true;
            }
            return false;
        }

        public sealed override int Read(int[] docs, int[] freqs)
        {
            throw UnsupportedOperationException.Create("TermPositions does not support processing multiple documents in one call. Use TermDocs instead.");
        }

        /// <summary>
        /// Called by <c>base.SkipTo()</c>. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected internal override void SkipProx(long proxPointer, int payloadLength)
        {
            // we save the pointer, we might have to skip there lazily
            lazySkipPointer = proxPointer;
            lazySkipProxCount = 0;
            proxCount = 0;
            this.payloadLength = payloadLength;
            needToLoadPayload = false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SkipPositions(int n)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(m_indexOptions == IndexOptions.DOCS_AND_FREQS_AND_POSITIONS);
            for (int f = n; f > 0; f--) // skip unread positions
            {
                ReadDeltaPosition();
                SkipPayload();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SkipPayload()
        {
            if (needToLoadPayload && payloadLength > 0)
            {
                proxStream.Seek(proxStream.Position + payloadLength); // LUCENENET specific: Renamed from getFilePointer() to match FileStream
            }
            needToLoadPayload = false;
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
            if (proxStream is null)
            {
                // clone lazily
                proxStream = (IndexInput)proxStreamOrig.Clone();
            }

            // we might have to skip the current payload
            // if it was not read yet
            SkipPayload();

            if (lazySkipPointer != -1)
            {
                proxStream.Seek(lazySkipPointer);
                lazySkipPointer = -1;
            }

            if (lazySkipProxCount != 0)
            {
                SkipPositions(lazySkipProxCount);
                lazySkipProxCount = 0;
            }
        }

        public int PayloadLength => payloadLength;

        public BytesRef GetPayload()
        {
            if (payloadLength <= 0)
            {
                return null; // no payload
            }

            if (needToLoadPayload)
            {
                // read payloads lazily
                if (payload is null)
                {
                    payload = new BytesRef(payloadLength);
                }
                else
                {
                    payload.Grow(payloadLength);
                }

                proxStream.ReadBytes(payload.Bytes, payload.Offset, payloadLength);
                payload.Length = payloadLength;
                needToLoadPayload = false;
            }
            return payload;
        }

        public bool IsPayloadAvailable => needToLoadPayload && payloadLength > 0;
    }
}