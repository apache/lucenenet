using Lucene.Net.Store;
using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Codecs.Lucene41
{
    internal sealed class Lucene41SkipReader : MultiLevelSkipListReader
    {
        // private boolean DEBUG = Lucene41PostingsReader.DEBUG;
        private readonly int blockSize;

        private long[] docPointer;
        private long[] posPointer;
        private long[] payPointer;
        private int[] posBufferUpto;
        private int[] payloadByteUpto;

        private long lastPosPointer;
        private long lastPayPointer;
        private int lastPayloadByteUpto;
        private long lastDocPointer;
        private int lastPosBufferUpto;

        public Lucene41SkipReader(IndexInput skipStream, int maxSkipLevels, int blockSize, bool hasPos, bool hasOffsets, bool hasPayloads)
            : base(skipStream, maxSkipLevels, blockSize, 8)
        {
            this.blockSize = blockSize;
            docPointer = new long[maxSkipLevels];
            if (hasPos)
            {
                posPointer = new long[maxSkipLevels];
                posBufferUpto = new int[maxSkipLevels];
                if (hasPayloads)
                {
                    payloadByteUpto = new int[maxSkipLevels];
                }
                else
                {
                    payloadByteUpto = null;
                }
                if (hasOffsets || hasPayloads)
                {
                    payPointer = new long[maxSkipLevels];
                }
                else
                {
                    payPointer = null;
                }
            }
            else
            {
                posPointer = null;
            }
        }

        protected int Trim(int df)
        {
            return df % blockSize == 0 ? df - 1 : df;
        }

        public void Init(long skipPointer, long docBasePointer, long posBasePointer, long payBasePointer, int df)
        {
            base.Init(skipPointer, Trim(df));
            lastDocPointer = docBasePointer;
            lastPosPointer = posBasePointer;
            lastPayPointer = payBasePointer;

            Arrays.Fill(docPointer, docBasePointer);
            if (posPointer != null)
            {
                Arrays.Fill(posPointer, posBasePointer);
                if (payPointer != null)
                {
                    Arrays.Fill(payPointer, payBasePointer);
                }
            }
            else
            {
                //assert posBasePointer == 0;
            }
        }

        public long DocPointer
        {
            get
            {
                return lastDocPointer;
            }
        }

        public long PosPointer
        {
            get
            {
                return lastPosPointer;
            }
        }

        public int PosBufferUpto
        {
            get
            {
                return lastPosBufferUpto;
            }
        }

        public long PayPointer
        {
            get
            {
                return lastPayPointer;
            }
        }

        public int PayloadByteUpto
        {
            get
            {
                return lastPayloadByteUpto;
            }
        }

        public int NextSkipDoc
        {
            get
            {
                return skipDoc[0];
            }
        }

        protected override void SeekChild(int level)
        {
            base.SeekChild(level);
            // if (DEBUG) {
            //   System.out.println("seekChild level=" + level);
            // }
            docPointer[level] = lastDocPointer;
            if (posPointer != null)
            {
                posPointer[level] = lastPosPointer;
                posBufferUpto[level] = lastPosBufferUpto;
                if (payloadByteUpto != null)
                {
                    payloadByteUpto[level] = lastPayloadByteUpto;
                }
                if (payPointer != null)
                {
                    payPointer[level] = lastPayPointer;
                }
            }
        }

        protected override void SetLastSkipData(int level)
        {
            base.SetLastSkipData(level);
            lastDocPointer = docPointer[level];
            // if (DEBUG) {
            //   System.out.println("setLastSkipData level=" + level);
            //   System.out.println("  lastDocPointer=" + lastDocPointer);
            // }
            if (posPointer != null)
            {
                lastPosPointer = posPointer[level];
                lastPosBufferUpto = posBufferUpto[level];
                // if (DEBUG) {
                //   System.out.println("  lastPosPointer=" + lastPosPointer + " lastPosBUfferUpto=" + lastPosBufferUpto);
                // }
                if (payPointer != null)
                {
                    lastPayPointer = payPointer[level];
                }
                if (payloadByteUpto != null)
                {
                    lastPayloadByteUpto = payloadByteUpto[level];
                }
            }
        }

        protected override int ReadSkipData(int level, IndexInput skipStream)
        {
            // if (DEBUG) {
            //   System.out.println("readSkipData level=" + level);
            // }
            int delta = skipStream.ReadVInt();
            // if (DEBUG) {
            //   System.out.println("  delta=" + delta);
            // }
            docPointer[level] += skipStream.ReadVInt();
            // if (DEBUG) {
            //   System.out.println("  docFP=" + docPointer[level]);
            // }

            if (posPointer != null)
            {
                posPointer[level] += skipStream.ReadVInt();
                // if (DEBUG) {
                //   System.out.println("  posFP=" + posPointer[level]);
                // }
                posBufferUpto[level] = skipStream.ReadVInt();
                // if (DEBUG) {
                //   System.out.println("  posBufferUpto=" + posBufferUpto[level]);
                // }

                if (payloadByteUpto != null)
                {
                    payloadByteUpto[level] = skipStream.ReadVInt();
                }

                if (payPointer != null)
                {
                    payPointer[level] += skipStream.ReadVInt();
                }
            }
            return delta;
        }
    }
}
