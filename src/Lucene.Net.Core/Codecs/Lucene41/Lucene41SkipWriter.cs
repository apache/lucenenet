namespace Lucene.Net.Codecs.Lucene41
{
    using Lucene.Net.Support;

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

        private int[] LastSkipDoc;
        private long[] LastSkipDocPointer;
        private long[] LastSkipPosPointer;
        private long[] LastSkipPayPointer;
        private int[] LastPayloadByteUpto;

        private readonly IndexOutput DocOut;
        private readonly IndexOutput PosOut;
        private readonly IndexOutput PayOut;

        private int CurDoc;
        private long CurDocPointer;
        private long CurPosPointer;
        private long CurPayPointer;
        private int CurPosBufferUpto;
        private int CurPayloadByteUpto;
        private bool FieldHasPositions;
        private bool FieldHasOffsets;
        private bool FieldHasPayloads;

        public Lucene41SkipWriter(int maxSkipLevels, int blockSize, int docCount, IndexOutput docOut, IndexOutput posOut, IndexOutput payOut)
            : base(blockSize, 8, maxSkipLevels, docCount)
        {
            this.DocOut = docOut;
            this.PosOut = posOut;
            this.PayOut = payOut;

            LastSkipDoc = new int[maxSkipLevels];
            LastSkipDocPointer = new long[maxSkipLevels];
            if (posOut != null)
            {
                LastSkipPosPointer = new long[maxSkipLevels];
                if (payOut != null)
                {
                    LastSkipPayPointer = new long[maxSkipLevels];
                }
                LastPayloadByteUpto = new int[maxSkipLevels];
            }
        }

        public void SetField(bool fieldHasPositions, bool fieldHasOffsets, bool fieldHasPayloads)
        {
            this.FieldHasPositions = fieldHasPositions;
            this.FieldHasOffsets = fieldHasOffsets;
            this.FieldHasPayloads = fieldHasPayloads;
        }

        public override void ResetSkip()
        {
            base.ResetSkip();
            CollectionsHelper.Fill(LastSkipDoc, 0);
            CollectionsHelper.Fill(LastSkipDocPointer, DocOut.FilePointer);
            if (FieldHasPositions)
            {
                CollectionsHelper.Fill(LastSkipPosPointer, PosOut.FilePointer);
                if (FieldHasPayloads)
                {
                    CollectionsHelper.Fill(LastPayloadByteUpto, 0);
                }
                if (FieldHasOffsets || FieldHasPayloads)
                {
                    CollectionsHelper.Fill(LastSkipPayPointer, PayOut.FilePointer);
                }
            }
        }

        /// <summary>
        /// Sets the values for the current skip data.
        /// </summary>
        public void BufferSkip(int doc, int numDocs, long posFP, long payFP, int posBufferUpto, int payloadByteUpto)
        {
            this.CurDoc = doc;
            this.CurDocPointer = DocOut.FilePointer;
            this.CurPosPointer = posFP;
            this.CurPayPointer = payFP;
            this.CurPosBufferUpto = posBufferUpto;
            this.CurPayloadByteUpto = payloadByteUpto;
            BufferSkip(numDocs);
        }

        protected override void WriteSkipData(int level, IndexOutput skipBuffer)
        {
            int delta = CurDoc - LastSkipDoc[level];
            // if (DEBUG) {
            //   System.out.println("writeSkipData level=" + level + " lastDoc=" + curDoc + " delta=" + delta + " curDocPointer=" + curDocPointer);
            // }
            skipBuffer.WriteVInt(delta);
            LastSkipDoc[level] = CurDoc;

            skipBuffer.WriteVInt((int)(CurDocPointer - LastSkipDocPointer[level]));
            LastSkipDocPointer[level] = CurDocPointer;

            if (FieldHasPositions)
            {
                // if (DEBUG) {
                //   System.out.println("  curPosPointer=" + curPosPointer + " curPosBufferUpto=" + curPosBufferUpto);
                // }
                skipBuffer.WriteVInt((int)(CurPosPointer - LastSkipPosPointer[level]));
                LastSkipPosPointer[level] = CurPosPointer;
                skipBuffer.WriteVInt(CurPosBufferUpto);

                if (FieldHasPayloads)
                {
                    skipBuffer.WriteVInt(CurPayloadByteUpto);
                }

                if (FieldHasOffsets || FieldHasPayloads)
                {
                    skipBuffer.WriteVInt((int)(CurPayPointer - LastSkipPayPointer[level]));
                    LastSkipPayPointer[level] = CurPayPointer;
                }
            }
        }
    }
}