using Lucene.Net.Diagnostics;
using Lucene.Net.Support;
using System;
using System.Data.Common;
using System.Runtime.CompilerServices;

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
    /// This abstract class reads skip lists with multiple levels.
    /// <para/>
    /// See <see cref="MultiLevelSkipListWriter"/> for the information about the encoding
    /// of the multi level skip lists.
    /// <para/>
    /// Subclasses must implement the abstract method <see cref="ReadSkipData(int, IndexInput)"/>
    /// which defines the actual format of the skip data.
    /// <para/>
    /// @lucene.experimental
    /// </summary>
    public abstract class MultiLevelSkipListReader : IDisposable
    {
        /// <summary>
        /// The maximum number of skip levels possible for this index. </summary>
        protected internal int m_maxNumberOfSkipLevels;

        // number of levels in this skip list
        private int numberOfSkipLevels;

        // Expert: defines the number of top skip levels to buffer in memory.
        // Reducing this number results in less memory usage, but possibly
        // slower performance due to more random I/Os.
        // Please notice that the space each level occupies is limited by
        // the skipInterval. The top level can not contain more than
        // skipLevel entries, the second top level can not contain more
        // than skipLevel^2 entries and so forth.
        private readonly int numberOfLevelsToBuffer = 1; // LUCENENET: marked readonly

        private int docCount;
        private bool haveSkipped;

        /// <summary>
        /// SkipStream for each level. </summary>
        private readonly IndexInput[] skipStream;

        /// <summary>
        /// The start pointer of each skip level. </summary>
        private readonly long[] skipPointer;

        /// <summary>
        /// SkipInterval of each level. </summary>
        private readonly int[] skipInterval;

        /// <summary>
        /// Number of docs skipped per level. </summary>
        private readonly int[] numSkipped;

        /// <summary>
        /// Doc id of current skip entry per level. </summary>
        protected internal int[] m_skipDoc;

        /// <summary>
        /// Doc id of last read skip entry with docId &lt;= target. </summary>
        private int lastDoc;

        /// <summary>
        /// Child pointer of current skip entry per level. </summary>
        private readonly long[] childPointer;

        /// <summary>
        /// childPointer of last read skip entry with docId &lt;=
        /// target.
        /// </summary>
        private long lastChildPointer;

        private readonly bool inputIsBuffered;
        private readonly int skipMultiplier;

        /// <summary>
        /// Creates a <see cref="MultiLevelSkipListReader"/>. </summary>
        protected MultiLevelSkipListReader(IndexInput skipStream, int maxSkipLevels, int skipInterval, int skipMultiplier)
        {
            this.skipStream = new IndexInput[maxSkipLevels];
            this.skipPointer = new long[maxSkipLevels];
            this.childPointer = new long[maxSkipLevels];
            this.numSkipped = new int[maxSkipLevels];
            this.m_maxNumberOfSkipLevels = maxSkipLevels;
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
            m_skipDoc = new int[maxSkipLevels];
        }

        /// <summary>
        /// Creates a <see cref="MultiLevelSkipListReader"/>, where
        /// <see cref="skipInterval"/> and <see cref="skipMultiplier"/> are
        /// the same.
        /// </summary>
        protected internal MultiLevelSkipListReader(IndexInput skipStream, int maxSkipLevels, int skipInterval)
            : this(skipStream, maxSkipLevels, skipInterval, skipInterval)
        {
        }

        /// <summary>
        /// Returns the id of the doc to which the last call of <see cref="SkipTo(int)"/>
        /// has skipped.
        /// </summary>
        public virtual int Doc => lastDoc;

        /// <summary>
        /// Skips entries to the first beyond the current whose document number is
        /// greater than or equal to <paramref name="target"/>. Returns the current doc count.
        /// </summary>
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
            while (level < numberOfSkipLevels - 1 && target > m_skipDoc[level + 1])
            {
                level++;
            }

            while (level >= 0)
            {
                if (target > m_skipDoc[level])
                {
                    if (!LoadNextSkip(level))
                    {
                        // continue; // LUCENENET: Removed redundant jump statements. https://rules.sonarsource.com/csharp/RSPEC-3626
                    }
                }
                else
                {
                    // no more skips on this level, go down one level
                    if (level > 0 && lastChildPointer > skipStream[level - 1].Position) // LUCENENET specific: Renamed from getFilePointer() to match FileStream
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
                m_skipDoc[level] = int.MaxValue;
                if (numberOfSkipLevels > level)
                {
                    numberOfSkipLevels = level;
                }
                return false;
            }

            // read next skip entry
            m_skipDoc[level] += ReadSkipData(level, skipStream[level]);

            if (level != 0)
            {
                // read the child pointer if we are not on the leaf level
                childPointer[level] = skipStream[level].ReadVInt64() + skipPointer[level - 1];
            }

            return true;
        }

        /// <summary>
        /// Seeks the skip entry on the given level. </summary>
        protected virtual void SeekChild(int level)
        {
            skipStream[level].Seek(lastChildPointer);
            numSkipped[level] = numSkipped[level + 1] - skipInterval[level + 1];
            m_skipDoc[level] = lastDoc;
            if (level > 0)
            {
                childPointer[level] = skipStream[level].ReadVInt64() + skipPointer[level - 1];
            }
        }

        /// <summary>
        /// Disposes all resources used by this object.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes all resources used by this object. Subclasses may override
        /// to dispose their own resources.
        /// </summary>
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
        }

        /// <summary>
        /// Initializes the reader, for reuse on a new term. </summary>
        public virtual void Init(long skipPointer, int df)
        {
            this.skipPointer[0] = skipPointer;
            this.docCount = df;
            if (Debugging.AssertsEnabled) Debugging.Assert(skipPointer >= 0 && skipPointer <= skipStream[0].Length,"invalid skip pointer: {0}, length={1}", skipPointer, skipStream[0].Length);
            Arrays.Fill(m_skipDoc, 0);
            Arrays.Fill(numSkipped, 0);
            Arrays.Fill(childPointer, 0);

            haveSkipped = false;
            for (int i = 1; i < numberOfSkipLevels; i++)
            {
                skipStream[i] = null;
            }
        }

        /// <summary>
        /// Loads the skip levels. </summary>
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

            if (numberOfSkipLevels > m_maxNumberOfSkipLevels)
            {
                numberOfSkipLevels = m_maxNumberOfSkipLevels;
            }

            skipStream[0].Seek(skipPointer[0]);

            int toBuffer = numberOfLevelsToBuffer;

            for (int i = numberOfSkipLevels - 1; i > 0; i--)
            {
                // the length of the current level
                long length = skipStream[0].ReadVInt64();

                // the start pointer of the current level
                skipPointer[i] = skipStream[0].Position; // LUCENENET specific: Renamed from getFilePointer() to match FileStream
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
                    skipStream[0].Seek(skipStream[0].Position + length); // LUCENENET specific: Renamed from getFilePointer() to match FileStream
                }
            }

            // use base stream for the lowest level
            skipPointer[0] = skipStream[0].Position; // LUCENENET specific: Renamed from getFilePointer() to match FileStream
        }

        /// <summary>
        /// Subclasses must implement the actual skip data encoding in this method.
        /// </summary>
        /// <param name="level"> The level skip data shall be read from. </param>
        /// <param name="skipStream"> The skip stream to read from. </param>
        protected abstract int ReadSkipData(int level, IndexInput skipStream);

        /// <summary>
        /// Copies the values of the last read skip entry on this <paramref name="level"/>. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected virtual void SetLastSkipData(int level)
        {
            lastDoc = m_skipDoc[level];
            lastChildPointer = childPointer[level];
        }

        /// <summary>
        /// Used to buffer the top skip levels. </summary>
        private sealed class SkipBuffer : IndexInput
        {
            private byte[] data;
            private readonly long pointer;
            private int pos;

            internal SkipBuffer(IndexInput input, int length)
                : base("SkipBuffer on " + input)
            {
                data = new byte[length];
                pointer = input.Position; // LUCENENET specific: Renamed from getFilePointer() to match FileStream
                input.ReadBytes(data, 0, length);
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    data = null;
                }
            }

            public override long Position => pointer + pos; // LUCENENET specific: Renamed from getFilePointer() to match FileStream

            public override long Length
            {
                get
                {
                    EnsureOpen(); // LUCENENET: Guard against disposed IndexInput
                    return data.Length;
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override byte ReadByte()
            {
                EnsureOpen(); // LUCENENET: Guard against disposed IndexInput
                return data[pos++];
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override void ReadBytes(byte[] b, int offset, int len)
            {
                EnsureOpen(); // LUCENENET: Guard against disposed IndexInput
                Arrays.Copy(data, pos, b, offset, len);
                pos += len;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override void Seek(long pos)
            {
                EnsureOpen(); // LUCENENET: Guard against disposed IndexInput
                this.pos = (int)(pos - pointer);
            }

            private void EnsureOpen() // LUCENENET: Guard against disposed IndexInput
            {
                if (data is null)
                {
                    throw AlreadyClosedException.Create(this.GetType().FullName, "this IndexInput is disposed.");
                }
            }
        }
    }
}