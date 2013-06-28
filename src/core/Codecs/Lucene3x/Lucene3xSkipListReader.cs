using Lucene.Net.Store;
using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Codecs.Lucene3x
{
    [Obsolete]
    internal sealed class Lucene3xSkipListReader : MultiLevelSkipListReader
    {
        private bool currentFieldStoresPayloads;
        private long[] freqPointer;
        private long[] proxPointer;
        private int[] payloadLength;

        private long lastFreqPointer;
        private long lastProxPointer;
        private int lastPayloadLength;

        public Lucene3xSkipListReader(IndexInput skipStream, int maxSkipLevels, int skipInterval)
            : base(skipStream, maxSkipLevels, skipInterval)
        {
            freqPointer = new long[maxSkipLevels];
            proxPointer = new long[maxSkipLevels];
            payloadLength = new int[maxSkipLevels];
        }

        public void Init(long skipPointer, long freqBasePointer, long proxBasePointer, int df, bool storesPayloads)
        {
            base.Init(skipPointer, df);
            this.currentFieldStoresPayloads = storesPayloads;
            lastFreqPointer = freqBasePointer;
            lastProxPointer = proxBasePointer;

            Arrays.Fill(freqPointer, freqBasePointer);
            Arrays.Fill(proxPointer, proxBasePointer);
            Arrays.Fill(payloadLength, 0);
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

        protected override void SeekChild(int level)
        {
            base.SeekChild(level);
            freqPointer[level] = lastFreqPointer;
            proxPointer[level] = lastProxPointer;
            payloadLength[level] = lastPayloadLength;
        }

        protected override void SetLastSkipData(int level)
        {
            base.SetLastSkipData(level);
            lastFreqPointer = freqPointer[level];
            lastProxPointer = proxPointer[level];
            lastPayloadLength = payloadLength[level];
        }

        protected override int ReadSkipData(int level, IndexInput skipStream)
        {
            int delta;
            if (currentFieldStoresPayloads)
            {
                // the current field stores payloads.
                // if the doc delta is odd then we have
                // to read the current payload length
                // because it differs from the length of the
                // previous payload
                delta = skipStream.ReadVInt();
                if ((delta & 1) != 0)
                {
                    payloadLength[level] = skipStream.ReadVInt();
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
