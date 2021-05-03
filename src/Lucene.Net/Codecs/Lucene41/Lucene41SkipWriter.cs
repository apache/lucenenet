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

    using IndexOutput = Lucene.Net.Store.IndexOutput;

    /// <summary>
    /// Write skip lists with multiple levels, and support skip within block ints.
    /// <para/>
    /// Assume that docFreq = 28, skipInterval = blockSize = 12
    ///
    ///  |       block#0       | |      block#1        | |vInts|
    ///  d d d d d d d d d d d d d d d d d d d d d d d d d d d d (posting list)
    ///                          ^                       ^       (level 0 skip point)
    /// <para/>
    /// Note that skipWriter will ignore first document in block#0, since
    /// it is useless as a skip point.  Also, we'll never skip into the vInts
    /// block, only record skip data at the start its start point(if it exist).
    /// <para/>
    /// For each skip point, we will record:
    /// 1. docID in former position, i.e. for position 12, record docID[11], etc.
    /// 2. its related file points(position, payload),
    /// 3. related numbers or uptos(position, payload).
    /// 4. start offset.
    /// </summary>
    internal sealed class Lucene41SkipWriter : MultiLevelSkipListWriter
    {
        // private boolean DEBUG = Lucene41PostingsReader.DEBUG;

        private readonly int[] lastSkipDoc; // LUCENENET: marked readonly
        private readonly long[] lastSkipDocPointer; // LUCENENET: marked readonly
        private readonly long[] lastSkipPosPointer; // LUCENENET: marked readonly
        private readonly long[] lastSkipPayPointer; // LUCENENET: marked readonly
        private readonly int[] lastPayloadByteUpto; // LUCENENET: marked readonly

        private readonly IndexOutput docOut;
        private readonly IndexOutput posOut;
        private readonly IndexOutput payOut;

        private int curDoc;
        private long curDocPointer;
        private long curPosPointer;
        private long curPayPointer;
        private int curPosBufferUpto;
        private int curPayloadByteUpto;
        private bool fieldHasPositions;
        private bool fieldHasOffsets;
        private bool fieldHasPayloads;

        public Lucene41SkipWriter(int maxSkipLevels, int blockSize, int docCount, IndexOutput docOut, IndexOutput posOut, IndexOutput payOut)
            : base(blockSize, 8, maxSkipLevels, docCount)
        {
            this.docOut = docOut;
            this.posOut = posOut;
            this.payOut = payOut;

            lastSkipDoc = new int[maxSkipLevels];
            lastSkipDocPointer = new long[maxSkipLevels];
            if (posOut != null)
            {
                lastSkipPosPointer = new long[maxSkipLevels];
                if (payOut != null)
                {
                    lastSkipPayPointer = new long[maxSkipLevels];
                }
                lastPayloadByteUpto = new int[maxSkipLevels];
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetField(bool fieldHasPositions, bool fieldHasOffsets, bool fieldHasPayloads)
        {
            this.fieldHasPositions = fieldHasPositions;
            this.fieldHasOffsets = fieldHasOffsets;
            this.fieldHasPayloads = fieldHasPayloads;
        }

        public override void ResetSkip()
        {
            base.ResetSkip();
            Arrays.Fill(lastSkipDoc, 0);
            Arrays.Fill(lastSkipDocPointer, docOut.Position); // LUCENENET specific: Renamed from getFilePointer() to match FileStream
            if (fieldHasPositions)
            {
                Arrays.Fill(lastSkipPosPointer, posOut.Position); // LUCENENET specific: Renamed from getFilePointer() to match FileStream
                if (fieldHasPayloads)
                {
                    Arrays.Fill(lastPayloadByteUpto, 0);
                }
                if (fieldHasOffsets || fieldHasPayloads)
                {
                    Arrays.Fill(lastSkipPayPointer, payOut.Position); // LUCENENET specific: Renamed from getFilePointer() to match FileStream
                }
            }
        }

        /// <summary>
        /// Sets the values for the current skip data.
        /// </summary>
        public void BufferSkip(int doc, int numDocs, long posFP, long payFP, int posBufferUpto, int payloadByteUpto)
        {
            this.curDoc = doc;
            this.curDocPointer = docOut.Position; // LUCENENET specific: Renamed from getFilePointer() to match FileStream
            this.curPosPointer = posFP;
            this.curPayPointer = payFP;
            this.curPosBufferUpto = posBufferUpto;
            this.curPayloadByteUpto = payloadByteUpto;
            BufferSkip(numDocs);
        }

        protected override void WriteSkipData(int level, IndexOutput skipBuffer)
        {
            int delta = curDoc - lastSkipDoc[level];
            // if (DEBUG) {
            //   System.out.println("writeSkipData level=" + level + " lastDoc=" + curDoc + " delta=" + delta + " curDocPointer=" + curDocPointer);
            // }
            skipBuffer.WriteVInt32(delta);
            lastSkipDoc[level] = curDoc;

            skipBuffer.WriteVInt32((int)(curDocPointer - lastSkipDocPointer[level]));
            lastSkipDocPointer[level] = curDocPointer;

            if (fieldHasPositions)
            {
                // if (DEBUG) {
                //   System.out.println("  curPosPointer=" + curPosPointer + " curPosBufferUpto=" + curPosBufferUpto);
                // }
                skipBuffer.WriteVInt32((int)(curPosPointer - lastSkipPosPointer[level]));
                lastSkipPosPointer[level] = curPosPointer;
                skipBuffer.WriteVInt32(curPosBufferUpto);

                if (fieldHasPayloads)
                {
                    skipBuffer.WriteVInt32(curPayloadByteUpto);
                }

                if (fieldHasOffsets || fieldHasPayloads)
                {
                    skipBuffer.WriteVInt32((int)(curPayPointer - lastSkipPayPointer[level]));
                    lastSkipPayPointer[level] = curPayPointer;
                }
            }
        }
    }
}