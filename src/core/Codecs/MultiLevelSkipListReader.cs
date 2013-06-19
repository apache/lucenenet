using Lucene.Net.Store;
using Lucene.Net.Support;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Codecs
{
    public abstract class MultiLevelSkipListReader : IDisposable
    {
        protected int maxNumberOfSkipLevels;
        private int numberOfSkipLevels;
        private int numberOfLevelsToBuffer = 1;
        private int docCount;
        private bool haveSkipped;

        private IndexInput[] skipStream;

        private long[] skipPointer;

        private int[] skipInterval;

        private int[] numSkipped;

        protected int[] skipDoc;

        private int lastDoc;

        private long[] childPointer;

        private long lastChildPointer;

        private bool inputIsBuffered;
        private readonly int skipMultiplier;

        protected MultiLevelSkipListReader(IndexInput skipStream, int maxSkipLevels, int skipInterval, int skipMultiplier)
        {
            this.skipStream = new IndexInput[maxSkipLevels];
            this.skipPointer = new long[maxSkipLevels];
            this.childPointer = new long[maxSkipLevels];
            this.numSkipped = new int[maxSkipLevels];
            this.maxNumberOfSkipLevels = maxSkipLevels;
            this.skipInterval = new int[maxSkipLevels];
            this.skipMultiplier = skipMultiplier;
            this.skipStream[0] = skipStream;
            this.inputIsBuffered = (skipStream is BufferedIndexInput);
            this.skipInterval[0] = skipInterval;
            for (int i = 1; i < maxSkipLevels; i++)
            {
                // cache skip intervals
                this.skipInterval[i] = this.skipInterval[i - 1] * skipMultiplier;
            }
            skipDoc = new int[maxSkipLevels];
        }

        protected MultiLevelSkipListReader(IndexInput skipStream, int maxSkipLevels, int skipInterval)
            : this(skipStream, maxSkipLevels, skipInterval, skipInterval)
        {
        }

        public virtual int Doc
        {
            get { return lastDoc; }
        }

        public virtual int SkipTo(int target)
        {
            if (!haveSkipped)
            {
                // first time, load skip levels
                LoadSkipLevels();
                haveSkipped = true;
            }

            // walk up the levels until highest level is found that has a skip
            // for this target
            int level = 0;
            while (level < numberOfSkipLevels - 1 && target > skipDoc[level + 1])
            {
                level++;
            }

            while (level >= 0)
            {
                if (target > skipDoc[level])
                {
                    if (!LoadNextSkip(level))
                    {
                        continue;
                    }
                }
                else
                {
                    // no more skips on this level, go down one level
                    if (level > 0 && lastChildPointer > skipStream[level - 1].FilePointer)
                    {
                        SeekChild(level - 1);
                    }
                    level--;
                }
            }

            return numSkipped[0] - skipInterval[0] - 1;
        }

        private bool LoadNextSkip(int level)
        {
            // we have to skip, the target document is greater than the current
            // skip list entry        
            SetLastSkipData(level);

            numSkipped[level] += skipInterval[level];

            if (numSkipped[level] > docCount)
            {
                // this skip list is exhausted
                skipDoc[level] = int.MaxValue;
                if (numberOfSkipLevels > level) numberOfSkipLevels = level;
                return false;
            }

            // read next skip entry
            skipDoc[level] += ReadSkipData(level, skipStream[level]);

            if (level != 0)
            {
                // read the child pointer if we are not on the leaf level
                childPointer[level] = skipStream[level].ReadVLong() + skipPointer[level - 1];
            }

            return true;
        }

        protected void SeekChild(int level)
        {
            skipStream[level].Seek(lastChildPointer);
            numSkipped[level] = numSkipped[level + 1] - skipInterval[level + 1];
            skipDoc[level] = lastDoc;
            if (level > 0)
            {
                childPointer[level] = skipStream[level].ReadVLong() + skipPointer[level - 1];
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                for (int i = 1; i < skipStream.Length; i++)
                {
                    if (skipStream[i] != null)
                    {
                        skipStream[i].Dispose();
                    }
                }
            }

            skipStream = null;
        }

        public virtual void Init(long skipPointer, int df)
        {
            this.skipPointer[0] = skipPointer;
            this.docCount = df;
            //assert skipPointer >= 0 && skipPointer <= skipStream[0].length() 
            //: "invalid skip pointer: " + skipPointer + ", length=" + skipStream[0].length();
            Arrays.Fill(skipDoc, 0);
            Arrays.Fill(numSkipped, 0);
            Arrays.Fill(childPointer, 0);

            haveSkipped = false;
            for (int i = 1; i < numberOfSkipLevels; i++)
            {
                skipStream[i] = null;
            }
        }

        private void LoadSkipLevels()
        {
            if (docCount <= skipInterval[0])
            {
                numberOfSkipLevels = 1;
            }
            else
            {
                numberOfSkipLevels = 1 + MathUtil.Log(docCount / skipInterval[0], skipMultiplier);
            }

            if (numberOfSkipLevels > maxNumberOfSkipLevels)
            {
                numberOfSkipLevels = maxNumberOfSkipLevels;
            }

            skipStream[0].Seek(skipPointer[0]);

            int toBuffer = numberOfLevelsToBuffer;

            for (int i = numberOfSkipLevels - 1; i > 0; i--)
            {
                // the length of the current level
                long length = skipStream[0].ReadVLong();

                // the start pointer of the current level
                skipPointer[i] = skipStream[0].FilePointer;
                if (toBuffer > 0)
                {
                    // buffer this level
                    skipStream[i] = new SkipBuffer(skipStream[0], (int)length);
                    toBuffer--;
                }
                else
                {
                    // clone this stream, it is already at the start of the current level
                    skipStream[i] = (IndexInput)skipStream[0].Clone();
                    if (inputIsBuffered && length < BufferedIndexInput.BUFFER_SIZE)
                    {
                        ((BufferedIndexInput)skipStream[i]).SetBufferSize((int)length);
                    }

                    // move base stream beyond the current level
                    skipStream[0].Seek(skipStream[0].FilePointer + length);
                }
            }

            // use base stream for the lowest level
            skipPointer[0] = skipStream[0].FilePointer;
        }

        protected abstract int ReadSkipData(int level, IndexInput skipStream);

        protected void SetLastSkipData(int level)
        {
            lastDoc = skipDoc[level];
            lastChildPointer = childPointer[level];
        }

        private sealed class SkipBuffer : IndexInput
        {
            private byte[] data;
            private long pointer;
            private int pos;

            internal SkipBuffer(IndexInput input, int length)
                : base("SkipBuffer on " + input)
            {
                data = new byte[length];
                pointer = input.FilePointer;
                input.ReadBytes(data, 0, length);
            }

            protected override void Dispose(bool disposing)
            {
                data = null;
            }

            public override long FilePointer
            {
                get { return pointer + pos; }
            }

            public override long Length
            {
                get { return data.Length; }
            }

            public override byte ReadByte()
            {
                return data[pos++];
            }

            public override void ReadBytes(byte[] b, int offset, int len, bool useBuffer)
            {
                Array.Copy(data, pos, b, offset, len);
                pos += len;
            }

            public override void Seek(long pos)
            {
                this.pos = (int)(pos - pointer);
            }
        }
    }
}
