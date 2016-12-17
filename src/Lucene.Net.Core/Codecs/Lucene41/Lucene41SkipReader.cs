using System.Diagnostics;

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

    using IndexInput = Lucene.Net.Store.IndexInput;

    /// <summary>
    /// Implements the skip list reader for block postings format
    /// that stores positions and payloads.
    ///
    /// Although this skipper uses MultiLevelSkipListReader as an interface,
    /// its definition of skip position will be a little different.
    ///
    /// For example, when skipInterval = blockSize = 3, df = 2*skipInterval = 6,
    ///
    /// 0 1 2 3 4 5
    /// d d d d d d    (posting list)
    ///     ^     ^    (skip point in MultiLeveSkipWriter)
    ///       ^        (skip point in Lucene41SkipWriter)
    ///
    /// In this case, MultiLevelSkipListReader will use the last document as a skip point,
    /// while Lucene41SkipReader should assume no skip point will comes.
    ///
    /// If we use the interface directly in Lucene41SkipReader, it may silly try to read
    /// another skip data after the only skip point is loaded.
    ///
    /// To illustrate this, we can call skipTo(d[5]), since skip point d[3] has smaller docId,
    /// and numSkipped+blockSize== df, the MultiLevelSkipListReader will assume the skip list
    /// isn't exhausted yet, and try to load a non-existed skip point
    ///
    /// Therefore, we'll trim df before passing it to the interface. see trim(int)
    ///
    /// </summary>
    internal sealed class Lucene41SkipReader : MultiLevelSkipListReader
    {
        // private boolean DEBUG = Lucene41PostingsReader.DEBUG;
        private readonly int BlockSize;

        private long[] DocPointer_Renamed;
        private long[] PosPointer_Renamed;
        private long[] PayPointer_Renamed;
        private int[] PosBufferUpto_Renamed;
        private int[] PayloadByteUpto_Renamed;

        private long LastPosPointer;
        private long LastPayPointer;
        private int LastPayloadByteUpto;
        private long LastDocPointer;
        private int LastPosBufferUpto;

        public Lucene41SkipReader(IndexInput skipStream, int maxSkipLevels, int blockSize, bool hasPos, bool hasOffsets, bool hasPayloads)
            : base(skipStream, maxSkipLevels, blockSize, 8)
        {
            this.BlockSize = blockSize;
            DocPointer_Renamed = new long[maxSkipLevels];
            if (hasPos)
            {
                PosPointer_Renamed = new long[maxSkipLevels];
                PosBufferUpto_Renamed = new int[maxSkipLevels];
                if (hasPayloads)
                {
                    PayloadByteUpto_Renamed = new int[maxSkipLevels];
                }
                else
                {
                    PayloadByteUpto_Renamed = null;
                }
                if (hasOffsets || hasPayloads)
                {
                    PayPointer_Renamed = new long[maxSkipLevels];
                }
                else
                {
                    PayPointer_Renamed = null;
                }
            }
            else
            {
                PosPointer_Renamed = null;
            }
        }

        /// <summary>
        /// Trim original docFreq to tell skipReader read proper number of skip points.
        ///
        /// Since our definition in Lucene41Skip* is a little different from MultiLevelSkip*
        /// this trimmed docFreq will prevent skipReader from:
        /// 1. silly reading a non-existed skip point after the last block boundary
        /// 2. moving into the vInt block
        ///
        /// </summary>
        internal int Trim(int df)
        {
            return df % BlockSize == 0 ? df - 1 : df;
        }

        public void Init(long skipPointer, long docBasePointer, long posBasePointer, long payBasePointer, int df)
        {
            base.Init(skipPointer, Trim(df));
            LastDocPointer = docBasePointer;
            LastPosPointer = posBasePointer;
            LastPayPointer = payBasePointer;

            CollectionsHelper.Fill(DocPointer_Renamed, docBasePointer);
            if (PosPointer_Renamed != null)
            {
                CollectionsHelper.Fill(PosPointer_Renamed, posBasePointer);
                if (PayPointer_Renamed != null)
                {
                    CollectionsHelper.Fill(PayPointer_Renamed, payBasePointer);
                }
            }
            else
            {
                Debug.Assert(posBasePointer == 0);
            }
        }

        /// <summary>
        /// Returns the doc pointer of the doc to which the last call of
        /// <seealso cref="MultiLevelSkipListReader#skipTo(int)"/> has skipped.
        /// </summary>
        public long DocPointer
        {
            get
            {
                return LastDocPointer;
            }
        }

        public long PosPointer
        {
            get
            {
                return LastPosPointer;
            }
        }

        public int PosBufferUpto
        {
            get
            {
                return LastPosBufferUpto;
            }
        }

        public long PayPointer
        {
            get
            {
                return LastPayPointer;
            }
        }

        public int PayloadByteUpto
        {
            get
            {
                return LastPayloadByteUpto;
            }
        }

        public int NextSkipDoc
        {
            get
            {
                return SkipDoc[0];
            }
        }

        protected override void SeekChild(int level)
        {
            base.SeekChild(level);
            // if (DEBUG) {
            //   System.out.println("seekChild level=" + level);
            // }
            DocPointer_Renamed[level] = LastDocPointer;
            if (PosPointer_Renamed != null)
            {
                PosPointer_Renamed[level] = LastPosPointer;
                PosBufferUpto_Renamed[level] = LastPosBufferUpto;
                if (PayloadByteUpto_Renamed != null)
                {
                    PayloadByteUpto_Renamed[level] = LastPayloadByteUpto;
                }
                if (PayPointer_Renamed != null)
                {
                    PayPointer_Renamed[level] = LastPayPointer;
                }
            }
        }

        protected override int LastSkipData
        {
            set
            {
                base.LastSkipData = value;
                LastDocPointer = DocPointer_Renamed[value];
                // if (DEBUG) {
                //   System.out.println("setLastSkipData level=" + value);
                //   System.out.println("  lastDocPointer=" + lastDocPointer);
                // }
                if (PosPointer_Renamed != null)
                {
                    LastPosPointer = PosPointer_Renamed[value];
                    LastPosBufferUpto = PosBufferUpto_Renamed[value];
                    // if (DEBUG) {
                    //   System.out.println("  lastPosPointer=" + lastPosPointer + " lastPosBUfferUpto=" + lastPosBufferUpto);
                    // }
                    if (PayPointer_Renamed != null)
                    {
                        LastPayPointer = PayPointer_Renamed[value];
                    }
                    if (PayloadByteUpto_Renamed != null)
                    {
                        LastPayloadByteUpto = PayloadByteUpto_Renamed[value];
                    }
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
            DocPointer_Renamed[level] += skipStream.ReadVInt();
            // if (DEBUG) {
            //   System.out.println("  docFP=" + docPointer[level]);
            // }

            if (PosPointer_Renamed != null)
            {
                PosPointer_Renamed[level] += skipStream.ReadVInt();
                // if (DEBUG) {
                //   System.out.println("  posFP=" + posPointer[level]);
                // }
                PosBufferUpto_Renamed[level] = skipStream.ReadVInt();
                // if (DEBUG) {
                //   System.out.println("  posBufferUpto=" + posBufferUpto[level]);
                // }

                if (PayloadByteUpto_Renamed != null)
                {
                    PayloadByteUpto_Renamed[level] = skipStream.ReadVInt();
                }

                if (PayPointer_Renamed != null)
                {
                    PayPointer_Renamed[level] += skipStream.ReadVInt();
                }
            }
            return delta;
        }
    }
}