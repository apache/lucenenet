using Lucene.Net.Store;
using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Codecs.Lucene40
{
    [Obsolete]
    public class Lucene40SkipListReader : MultiLevelSkipListReader
    {
        private bool currentFieldStoresPayloads;
        private bool currentFieldStoresOffsets;
        private long[] freqPointer;
        private long[] proxPointer;
        private int[] payloadLength;
        private int[] offsetLength;

        private long lastFreqPointer;
        private long lastProxPointer;
        private int lastPayloadLength;
        private int lastOffsetLength;

        public Lucene40SkipListReader(IndexInput skipStream, int maxSkipLevels, int skipInterval)
            : base(skipStream, maxSkipLevels, skipInterval)
        {
            freqPointer = new long[maxSkipLevels];
            proxPointer = new long[maxSkipLevels];
            payloadLength = new int[maxSkipLevels];
            offsetLength = new int[maxSkipLevels];
        }

        public void Init(long skipPointer, long freqBasePointer, long proxBasePointer, int df, bool storesPayloads, bool storesOffsets)
        {
            base.Init(skipPointer, df);
            this.currentFieldStoresPayloads = storesPayloads;
            this.currentFieldStoresOffsets = storesOffsets;
            lastFreqPointer = freqBasePointer;
            lastProxPointer = proxBasePointer;

            Arrays.Fill(freqPointer, freqBasePointer);
            Arrays.Fill(proxPointer, proxBasePointer);
            Arrays.Fill(payloadLength, 0);
            Arrays.Fill(offsetLength, 0);
        }

        public long FreqPointer
        {
            get
            {
                return lastFreqPointer;
            }
        }

        public long ProxPointer
        {
            get
            {
                return lastProxPointer;
            }
        }

        public int PayloadLength
        {
            get
            {
                return lastPayloadLength;
            }
        }

        public int OffsetLength
        {
            get
            {
                return lastOffsetLength;
            }
        }

        protected override void SeekChild(int level)
        {
            base.SeekChild(level);
            freqPointer[level] = lastFreqPointer;
            proxPointer[level] = lastProxPointer;
            payloadLength[level] = lastPayloadLength;
            offsetLength[level] = lastOffsetLength;
        }

        protected override void SetLastSkipData(int level)
        {
            base.SetLastSkipData(level);
            lastFreqPointer = freqPointer[level];
            lastProxPointer = proxPointer[level];
            lastPayloadLength = payloadLength[level];
            lastOffsetLength = offsetLength[level];
        }

        protected override int ReadSkipData(int level, IndexInput skipStream)
        {
            int delta;
            if (currentFieldStoresPayloads || currentFieldStoresOffsets)
            {
                // the current field stores payloads and/or offsets.
                // if the doc delta is odd then we have
                // to read the current payload/offset lengths
                // because it differs from the lengths of the
                // previous payload/offset
                delta = skipStream.ReadVInt();
                if ((delta & 1) != 0)
                {
                    if (currentFieldStoresPayloads)
                    {
                        payloadLength[level] = skipStream.ReadVInt();
                    }
                    if (currentFieldStoresOffsets)
                    {
                        offsetLength[level] = skipStream.ReadVInt();
                    }
                }
                delta = Number.URShift(delta, 1);
            }
            else
            {
                delta = skipStream.ReadVInt();
            }

            freqPointer[level] += skipStream.ReadVInt();
            proxPointer[level] += skipStream.ReadVInt();

            return delta;
        }
    }
}
