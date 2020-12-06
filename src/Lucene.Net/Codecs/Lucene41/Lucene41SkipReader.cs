using Lucene.Net.Diagnostics;
using Lucene.Net.Support;
using System.Runtime.CompilerServices;

namespace Lucene.Net.Codecs.Lucene41
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

    using IndexInput = Lucene.Net.Store.IndexInput;

    /// <summary>
    /// Implements the skip list reader for block postings format
    /// that stores positions and payloads.
    /// <para/>
    /// Although this skipper uses MultiLevelSkipListReader as an interface,
    /// its definition of skip position will be a little different.
    /// <para/>
    /// For example, when skipInterval = blockSize = 3, df = 2*skipInterval = 6,
    /// <para/>
    /// 0 1 2 3 4 5
    /// d d d d d d    (posting list)
    ///     ^     ^    (skip point in MultiLeveSkipWriter)
    ///       ^        (skip point in Lucene41SkipWriter)
    /// <para/>
    /// In this case, MultiLevelSkipListReader will use the last document as a skip point,
    /// while Lucene41SkipReader should assume no skip point will comes.
    /// <para/>
    /// If we use the interface directly in Lucene41SkipReader, it may silly try to read
    /// another skip data after the only skip point is loaded.
    /// <para/>
    /// To illustrate this, we can call skipTo(d[5]), since skip point d[3] has smaller docId,
    /// and numSkipped+blockSize== df, the MultiLevelSkipListReader will assume the skip list
    /// isn't exhausted yet, and try to load a non-existed skip point
    /// <para/>
    /// Therefore, we'll trim df before passing it to the interface. see <see cref="Trim(int)"/>.
    /// </summary>
    internal sealed class Lucene41SkipReader : MultiLevelSkipListReader
    {
        // private boolean DEBUG = Lucene41PostingsReader.DEBUG;
        private readonly int blockSize;

        private readonly long[] docPointer; // LUCENENET: marked readonly
        private readonly long[] posPointer; // LUCENENET: marked readonly
        private readonly long[] payPointer; // LUCENENET: marked readonly
        private readonly int[] posBufferUpto; // LUCENENET: marked readonly
        private readonly int[] payloadByteUpto; // LUCENENET: marked readonly

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

        /// <summary>
        /// Trim original docFreq to tell skipReader read proper number of skip points.
        /// <para/>
        /// Since our definition in Lucene41Skip* is a little different from MultiLevelSkip*
        /// this trimmed docFreq will prevent skipReader from:
        /// 1. silly reading a non-existed skip point after the last block boundary
        /// 2. moving into the vInt block
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal int Trim(int df)
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
                if (Debugging.AssertsEnabled) Debugging.Assert(posBasePointer == 0);
            }
        }

        /// <summary>
        /// Returns the doc pointer of the doc to which the last call of
        /// <seealso cref="MultiLevelSkipListReader.SkipTo(int)"/> has skipped.
        /// </summary>
        public long DocPointer => lastDocPointer;

        public long PosPointer => lastPosPointer;

        public int PosBufferUpto => lastPosBufferUpto;

        public long PayPointer => lastPayPointer;

        public int PayloadByteUpto => lastPayloadByteUpto;

        public int NextSkipDoc => m_skipDoc[0];

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
            //   System.out.println("setLastSkipData level=" + value);
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
            int delta = skipStream.ReadVInt32();
            // if (DEBUG) {
            //   System.out.println("  delta=" + delta);
            // }
            docPointer[level] += skipStream.ReadVInt32();
            // if (DEBUG) {
            //   System.out.println("  docFP=" + docPointer[level]);
            // }

            if (posPointer != null)
            {
                posPointer[level] += skipStream.ReadVInt32();
                // if (DEBUG) {
                //   System.out.println("  posFP=" + posPointer[level]);
                // }
                posBufferUpto[level] = skipStream.ReadVInt32();
                // if (DEBUG) {
                //   System.out.println("  posBufferUpto=" + posBufferUpto[level]);
                // }

                if (payloadByteUpto != null)
                {
                    payloadByteUpto[level] = skipStream.ReadVInt32();
                }

                if (payPointer != null)
                {
                    payPointer[level] += skipStream.ReadVInt32();
                }
            }
            return delta;
        }
    }
}