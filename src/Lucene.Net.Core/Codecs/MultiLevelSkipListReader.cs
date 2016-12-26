using System;
using System.Diagnostics;

namespace Lucene.Net.Codecs
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

    using BufferedIndexInput = Lucene.Net.Store.BufferedIndexInput;
    using IndexInput = Lucene.Net.Store.IndexInput;
    using MathUtil = Lucene.Net.Util.MathUtil;

    /// <summary>
    /// this abstract class reads skip lists with multiple levels.
    ///
    /// See <seealso cref="MultiLevelSkipListWriter"/> for the information about the encoding
    /// of the multi level skip lists.
    ///
    /// Subclasses must implement the abstract method <seealso cref="#readSkipData(int, IndexInput)"/>
    /// which defines the actual format of the skip data.
    /// @lucene.experimental
    /// </summary>

    public abstract class MultiLevelSkipListReader : IDisposable
    {
        /// <summary>
        /// the maximum number of skip levels possible for this index </summary>
        protected internal int MaxNumberOfSkipLevels;

        // number of levels in this skip list
        private int NumberOfSkipLevels;

        // Expert: defines the number of top skip levels to buffer in memory.
        // Reducing this number results in less memory usage, but possibly
        // slower performance due to more random I/Os.
        // Please notice that the space each level occupies is limited by
        // the skipInterval. The top level can not contain more than
        // skipLevel entries, the second top level can not contain more
        // than skipLevel^2 entries and so forth.
        private int NumberOfLevelsToBuffer = 1;

        private int DocCount;
        private bool HaveSkipped;

        /// <summary>
        /// skipStream for each level. </summary>
        private IndexInput[] SkipStream;

        /// <summary>
        /// The start pointer of each skip level. </summary>
        private long[] SkipPointer;

        /// <summary>
        ///  skipInterval of each level. </summary>
        private int[] SkipInterval;

        /// <summary>
        /// Number of docs skipped per level. </summary>
        private int[] NumSkipped;

        /// <summary>
        /// Doc id of current skip entry per level. </summary>
        protected internal int[] SkipDoc;

        /// <summary>
        /// Doc id of last read skip entry with docId &lt;= target. </summary>
        private int LastDoc;

        /// <summary>
        /// Child pointer of current skip entry per level. </summary>
        private long[] ChildPointer;

        /// <summary>
        /// childPointer of last read skip entry with docId &lt;=
        ///  target.
        /// </summary>
        private long LastChildPointer;

        private bool InputIsBuffered;
        private readonly int SkipMultiplier;

        /// <summary>
        /// Creates a {@code MultiLevelSkipListReader}. </summary>
        protected internal MultiLevelSkipListReader(IndexInput skipStream, int maxSkipLevels, int skipInterval, int skipMultiplier)
        {
            this.SkipStream = new IndexInput[maxSkipLevels];
            this.SkipPointer = new long[maxSkipLevels];
            this.ChildPointer = new long[maxSkipLevels];
            this.NumSkipped = new int[maxSkipLevels];
            this.MaxNumberOfSkipLevels = maxSkipLevels;
            this.SkipInterval = new int[maxSkipLevels];
            this.SkipMultiplier = skipMultiplier;
            this.SkipStream[0] = skipStream;
            this.InputIsBuffered = (skipStream is BufferedIndexInput);
            this.SkipInterval[0] = skipInterval;
            for (int i = 1; i < maxSkipLevels; i++)
            {
                // cache skip intervals
                this.SkipInterval[i] = this.SkipInterval[i - 1] * skipMultiplier;
            }
            SkipDoc = new int[maxSkipLevels];
        }

        /// <summary>
        /// Creates a {@code MultiLevelSkipListReader}, where
        ///  {@code skipInterval} and {@code skipMultiplier} are
        ///  the same.
        /// </summary>
        protected internal MultiLevelSkipListReader(IndexInput skipStream, int maxSkipLevels, int skipInterval)
            : this(skipStream, maxSkipLevels, skipInterval, skipInterval)
        {
        }

        /// <summary>
        /// Returns the id of the doc to which the last call of <seealso cref="#skipTo(int)"/>
        ///  has skipped.
        /// </summary>
        public virtual int Doc
        {
            get
            {
                return LastDoc;
            }
        }

        /// <summary>
        /// Skips entries to the first beyond the current whose document number is
        ///  greater than or equal to <i>target</i>. Returns the current doc count.
        /// </summary>
        public virtual int SkipTo(int target)
        {
            if (!HaveSkipped)
            {
                // first time, load skip levels
                LoadSkipLevels();
                HaveSkipped = true;
            }

            // walk up the levels until highest level is found that has a skip
            // for this target
            int level = 0;
            while (level < NumberOfSkipLevels - 1 && target > SkipDoc[level + 1])
            {
                level++;
            }

            while (level >= 0)
            {
                if (target > SkipDoc[level])
                {
                    if (!LoadNextSkip(level))
                    {
                        continue;
                    }
                }
                else
                {
                    // no more skips on this level, go down one level
                    if (level > 0 && LastChildPointer > SkipStream[level - 1].FilePointer)
                    {
                        SeekChild(level - 1);
                    }
                    level--;
                }
            }

            return NumSkipped[0] - SkipInterval[0] - 1;
        }

        private bool LoadNextSkip(int level)
        {
            // we have to skip, the target document is greater than the current
            // skip list entry
            SetLastSkipData(level);

            NumSkipped[level] += SkipInterval[level];

            if (NumSkipped[level] > DocCount)
            {
                // this skip list is exhausted
                SkipDoc[level] = int.MaxValue;
                if (NumberOfSkipLevels > level)
                {
                    NumberOfSkipLevels = level;
                }
                return false;
            }

            // read next skip entry
            SkipDoc[level] += ReadSkipData(level, SkipStream[level]);

            if (level != 0)
            {
                // read the child pointer if we are not on the leaf level
                ChildPointer[level] = SkipStream[level].ReadVLong() + SkipPointer[level - 1];
            }

            return true;
        }

        /// <summary>
        /// Seeks the skip entry on the given level </summary>
        protected virtual void SeekChild(int level)
        {
            SkipStream[level].Seek(LastChildPointer);
            NumSkipped[level] = NumSkipped[level + 1] - SkipInterval[level + 1];
            SkipDoc[level] = LastDoc;
            if (level > 0)
            {
                ChildPointer[level] = SkipStream[level].ReadVLong() + SkipPointer[level - 1];
            }
        }

        public void Dispose()
        {
            for (int i = 1; i < SkipStream.Length; i++)
            {
                if (SkipStream[i] != null)
                {
                    SkipStream[i].Dispose();
                }
            }
        }

        /// <summary>
        /// Initializes the reader, for reuse on a new term. </summary>
        public virtual void Init(long skipPointer, int df)
        {
            this.SkipPointer[0] = skipPointer;
            this.DocCount = df;
            Debug.Assert(skipPointer >= 0 && skipPointer <= SkipStream[0].Length, "invalid skip pointer: " + skipPointer + ", length=" + SkipStream[0].Length);
            Array.Clear(SkipDoc, 0, SkipDoc.Length);
            Array.Clear(NumSkipped, 0, NumSkipped.Length);
            Array.Clear(ChildPointer, 0, ChildPointer.Length);

            HaveSkipped = false;
            for (int i = 1; i < NumberOfSkipLevels; i++)
            {
                SkipStream[i] = null;
            }
        }

        /// <summary>
        /// Loads the skip levels </summary>
        private void LoadSkipLevels()
        {
            if (DocCount <= SkipInterval[0])
            {
                NumberOfSkipLevels = 1;
            }
            else
            {
                NumberOfSkipLevels = 1 + MathUtil.Log(DocCount / SkipInterval[0], SkipMultiplier);
            }

            if (NumberOfSkipLevels > MaxNumberOfSkipLevels)
            {
                NumberOfSkipLevels = MaxNumberOfSkipLevels;
            }

            SkipStream[0].Seek(SkipPointer[0]);

            int toBuffer = NumberOfLevelsToBuffer;

            for (int i = NumberOfSkipLevels - 1; i > 0; i--)
            {
                // the length of the current level
                long length = SkipStream[0].ReadVLong();

                // the start pointer of the current level
                SkipPointer[i] = SkipStream[0].FilePointer;
                if (toBuffer > 0)
                {
                    // buffer this level
                    SkipStream[i] = new SkipBuffer(SkipStream[0], (int)length);
                    toBuffer--;
                }
                else
                {
                    // clone this stream, it is already at the start of the current level
                    SkipStream[i] = (IndexInput)SkipStream[0].Clone();
                    if (InputIsBuffered && length < BufferedIndexInput.BUFFER_SIZE)
                    {
                        ((BufferedIndexInput)SkipStream[i]).SetBufferSize((int)length);
                    }

                    // move base stream beyond the current level
                    SkipStream[0].Seek(SkipStream[0].FilePointer + length);
                }
            }

            // use base stream for the lowest level
            SkipPointer[0] = SkipStream[0].FilePointer;
        }

        /// <summary>
        /// Subclasses must implement the actual skip data encoding in this method.
        /// </summary>
        /// <param name="level"> the level skip data shall be read from </param>
        /// <param name="skipStream"> the skip stream to read from </param>
        protected abstract int ReadSkipData(int level, IndexInput skipStream);

        /// <summary>
        /// Copies the values of the last read skip entry on this <paramref name="level"/> </summary>
        protected virtual void SetLastSkipData(int level)
        {
            LastDoc = SkipDoc[level];
            LastChildPointer = ChildPointer[level];
        }

        /// <summary>
        /// used to buffer the top skip levels </summary>
        private sealed class SkipBuffer : IndexInput
        {
            private byte[] Data;
            private long Pointer;
            private int Pos;

            internal SkipBuffer(IndexInput input, int length)
                : base("SkipBuffer on " + input)
            {
                Data = new byte[length];
                Pointer = input.FilePointer;
                input.ReadBytes(Data, 0, length);
            }

            public override void Dispose()
            {
                Data = null;
            }

            public override long FilePointer
            {
                get
                {
                    return Pointer + Pos;
                }
            }

            public override long Length
            {
                get { return Data.Length; }
            }

            public override byte ReadByte()
            {
                return Data[Pos++];
            }

            public override void ReadBytes(byte[] b, int offset, int len)
            {
                Array.Copy(Data, Pos, b, offset, len);
                Pos += len;
            }

            public override void Seek(long pos)
            {
                this.Pos = (int)(pos - Pointer);
            }
        }
    }
}