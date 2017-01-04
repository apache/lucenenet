using Lucene.Net.Support;

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
    ///
    /// Assume that docFreq = 28, skipInterval = blockSize = 12
    ///
    ///  |       block#0       | |      block#1        | |vInts|
    ///  d d d d d d d d d d d d d d d d d d d d d d d d d d d d (posting list)
    ///                          ^                       ^       (level 0 skip point)
    ///
    /// Note that skipWriter will ignore first document in block#0, since
    /// it is useless as a skip point.  Also, we'll never skip into the vInts
    /// block, only record skip data at the start its start point(if it exist).
    ///
    /// For each skip point, we will record:
    /// 1. docID in former position, i.e. for position 12, record docID[11], etc.
    /// 2. its related file points(position, payload),
    /// 3. related numbers or uptos(position, payload).
    /// 4. start offset.
    ///
    /// </summary>
    internal sealed class Lucene41SkipWriter : MultiLevelSkipListWriter
    {
        // private boolean DEBUG = Lucene41PostingsReader.DEBUG;

        private int[] lastSkipDoc;
        private long[] lastSkipDocPointer;
        private long[] lastSkipPosPointer;
        private long[] lastSkipPayPointer;
        private int[] lastPayloadByteUpto;

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

        public void SetField(bool fieldHasPositions, bool fieldHasOffsets, bool fieldHasPayloads)
        {
            this.fieldHasPositions = fieldHasPositions;
            this.fieldHasOffsets = fieldHasOffsets;
            this.fieldHasPayloads = fieldHasPayloads;
        }

        public override void ResetSkip()
        {
            base.ResetSkip();
            CollectionsHelper.Fill(lastSkipDoc, 0);
            CollectionsHelper.Fill(lastSkipDocPointer, docOut.FilePointer);
            if (fieldHasPositions)
            {
                CollectionsHelper.Fill(lastSkipPosPointer, posOut.FilePointer);
                if (fieldHasPayloads)
                {
                    CollectionsHelper.Fill(lastPayloadByteUpto, 0);
                }
                if (fieldHasOffsets || fieldHasPayloads)
                {
                    CollectionsHelper.Fill(lastSkipPayPointer, payOut.FilePointer);
                }
            }
        }

        /// <summary>
        /// Sets the values for the current skip data.
        /// </summary>
        public void BufferSkip(int doc, int numDocs, long posFP, long payFP, int posBufferUpto, int payloadByteUpto)
        {
            this.curDoc = doc;
            this.curDocPointer = docOut.FilePointer;
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
            skipBuffer.WriteVInt(delta);
            lastSkipDoc[level] = curDoc;

            skipBuffer.WriteVInt((int)(curDocPointer - lastSkipDocPointer[level]));
            lastSkipDocPointer[level] = curDocPointer;

            if (fieldHasPositions)
            {
                // if (DEBUG) {
                //   System.out.println("  curPosPointer=" + curPosPointer + " curPosBufferUpto=" + curPosBufferUpto);
                // }
                skipBuffer.WriteVInt((int)(curPosPointer - lastSkipPosPointer[level]));
                lastSkipPosPointer[level] = curPosPointer;
                skipBuffer.WriteVInt(curPosBufferUpto);

                if (fieldHasPayloads)
                {
                    skipBuffer.WriteVInt(curPayloadByteUpto);
                }

                if (fieldHasOffsets || fieldHasPayloads)
                {
                    skipBuffer.WriteVInt((int)(curPayPointer - lastSkipPayPointer[level]));
                    lastSkipPayPointer[level] = curPayPointer;
                }
            }
        }
    }
}