using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Support;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Codecs.Lucene3x
{
    [Obsolete]
    internal sealed class SegmentTermPositions : SegmentTermDocs
    {
        private IndexInput proxStream;
        private IndexInput proxStreamOrig;
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

        public SegmentTermPositions(IndexInput freqStream, IndexInput proxStream, TermInfosReader tis, FieldInfos fieldInfos)
            : base(freqStream, tis, fieldInfos)
        {
            this.proxStreamOrig = proxStream;  // the proxStream will be cloned lazily when nextPosition() is called for the first time
        }

        internal override void Seek(TermInfo ti, Term term)
        {
            base.Seek(ti, term);
            if (ti != null)
                lazySkipPointer = ti.proxPointer;

            lazySkipProxCount = 0;
            proxCount = 0;
            payloadLength = 0;
            needToLoadPayload = false;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                if (proxStream != null) proxStream.Dispose();
            }
        }

        public int NextPosition()
        {
            if (indexOptions != FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS)
                // This field does not store positions, payloads
                return 0;
            // perform lazy skips if necessary
            LazySkip();
            proxCount--;
            return position += ReadDeltaPosition();
        }

        private int ReadDeltaPosition()
        {
            int delta = proxStream.ReadVInt();
            if (currentFieldStoresPayloads)
            {
                // if the current field stores payloads then
                // the position delta is shifted one bit to the left.
                // if the LSB is set, then we have to read the current
                // payload length
                if ((delta & 1) != 0)
                {
                    payloadLength = proxStream.ReadVInt();
                }
                delta = Number.URShift(delta, 1);
                needToLoadPayload = true;
            }
            else if (delta == -1)
            {
                delta = 0; // LUCENE-1542 correction
            }
            return delta;
        }

        protected override void SkippingDoc()
        {
            // we remember to skip a document lazily
            lazySkipProxCount += freq;
        }

        public override bool Next()
        {
            // we remember to skip the remaining positions of the current
            // document lazily
            lazySkipProxCount += proxCount;

            if (base.Next())
            {               // run super
                proxCount = freq;               // note frequency
                position = 0;               // reset position
                return true;
            }
            return false;
        }

        public override int Read(int[] docs, int[] freqs)
        {
            throw new NotSupportedException("TermPositions does not support processing multiple documents in one call. Use TermDocs instead.");
        }

        protected override void SkipProx(long proxPointer, int payloadLength)
        {
            // we save the pointer, we might have to skip there lazily
            lazySkipPointer = proxPointer;
            lazySkipProxCount = 0;
            proxCount = 0;
            this.payloadLength = payloadLength;
            needToLoadPayload = false;
        }

        private void SkipPositions(int n)
        {
            //assert indexOptions == IndexOptions.DOCS_AND_FREQS_AND_POSITIONS;
            for (int f = n; f > 0; f--)
            {        // skip unread positions
                ReadDeltaPosition();
                SkipPayload();
            }
        }

        private void SkipPayload()
        {
            if (needToLoadPayload && payloadLength > 0)
            {
                proxStream.Seek(proxStream.FilePointer + payloadLength);
            }
            needToLoadPayload = false;
        }

        private void LazySkip()
        {
            if (proxStream == null)
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

        public int PayloadLength
        {
            get
            {
                return payloadLength;
            }
        }

        public BytesRef Payload
        {
            get
            {
                if (payloadLength <= 0)
                {
                    return null; // no payload
                }

                if (needToLoadPayload)
                {
                    // read payloads lazily
                    if (payload == null)
                    {
                        payload = new BytesRef(payloadLength);
                    }
                    else
                    {
                        payload.Grow(payloadLength);
                    }

                    proxStream.ReadBytes(payload.bytes, payload.offset, payloadLength);
                    payload.length = payloadLength;
                    needToLoadPayload = false;
                }
                return payload;
            }
        }

        public bool IsPayloadAvailable
        {
            get
            {
                return needToLoadPayload && payloadLength > 0;
            }
        }
    }
}
