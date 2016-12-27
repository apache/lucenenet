using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Lucene.Net.Util
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

    using DataInput = Lucene.Net.Store.DataInput;
    using DataOutput = Lucene.Net.Store.DataOutput;
    using IndexInput = Lucene.Net.Store.IndexInput;

    /// <summary>
    /// Represents a logical byte[] as a series of pages.  You
    ///  can write-once into the logical byte[] (append only),
    ///  using copy, and then retrieve slices (BytesRef) into it
    ///  using fill.
    ///
    /// @lucene.internal
    ///
    /// </summary>
    // TODO: refactor this, byteblockpool, fst.bytestore, and any
    // other "shift/mask big arrays". there are too many of these classes!
    public sealed class PagedBytes
    {
        private readonly IList<byte[]> Blocks = new List<byte[]>();

        // TODO: these are unused?
        private readonly IList<int> BlockEnd = new List<int>();

        private readonly int BlockSize;
        private readonly int BlockBits;
        private readonly int BlockMask;
        private bool DidSkipBytes;
        private bool Frozen;
        private int Upto;
        private byte[] CurrentBlock;
        private readonly long BytesUsedPerBlock;

        private static readonly byte[] EMPTY_BYTES = new byte[0];

        /// <summary>
        /// Provides methods to read BytesRefs from a frozen
        ///  PagedBytes.
        /// </summary>
        /// <seealso cref= #freeze  </seealso>
        public sealed class Reader
        {
            private readonly byte[][] Blocks;
            private readonly int[] BlockEnds;
            private readonly int BlockBits;
            private readonly int BlockMask;
            private readonly int BlockSize;

            internal Reader(PagedBytes pagedBytes)
            {
                Blocks = new byte[pagedBytes.Blocks.Count][];
                for (var i = 0; i < Blocks.Length; i++)
                {
                    Blocks[i] = pagedBytes.Blocks[i];
                }
                BlockEnds = new int[Blocks.Length];
                for (int i = 0; i < BlockEnds.Length; i++)
                {
                    BlockEnds[i] = pagedBytes.BlockEnd[i];
                }
                BlockBits = pagedBytes.BlockBits;
                BlockMask = pagedBytes.BlockMask;
                BlockSize = pagedBytes.BlockSize;
            }

            /// <summary>
            /// Gets a slice out of <seealso cref="PagedBytes"/> starting at <i>start</i> with a
            /// given length. Iff the slice spans across a block border this method will
            /// allocate sufficient resources and copy the paged data.
            /// <p>
            /// Slices spanning more than two blocks are not supported.
            /// </p>
            /// @lucene.internal
            ///
            /// </summary>
            public void FillSlice(BytesRef b, long start, int length)
            {
                Debug.Assert(length >= 0, "length=" + length);
                Debug.Assert(length <= BlockSize + 1, "length=" + length);
                b.Length = length;
                if (length == 0)
                {
                    return;
                }
                var index = (int)(start >> BlockBits);
                var offset = (int)(start & BlockMask);
                if (BlockSize - offset >= length)
                {
                    // Within block
                    b.Bytes = Blocks[index];
                    b.Offset = offset;
                }
                else
                {
                    // Split
                    b.Bytes = new byte[length];
                    b.Offset = 0;
                    Array.Copy(Blocks[index], offset, b.Bytes, 0, BlockSize - offset);
                    Array.Copy(Blocks[1 + index], 0, b.Bytes, BlockSize - offset, length - (BlockSize - offset));
                }
            }

            /// <summary>
            /// Reads length as 1 or 2 byte vInt prefix, starting at <i>start</i>.
            /// <p>
            /// <b>Note:</b> this method does not support slices spanning across block
            /// borders.
            /// </p>
            ///
            /// @lucene.internal
            ///
            /// </summary>
            // TODO: this really needs to be refactored into fieldcacheimpl
            public void Fill(BytesRef b, long start)
            {
                var index = (int)(start >> BlockBits);
                var offset = (int)(start & BlockMask);
                var block = b.Bytes = Blocks[index];

                if ((block[offset] & 128) == 0)
                {
                    b.Length = block[offset];
                    b.Offset = offset + 1;
                }
                else
                {
                    b.Length = ((block[offset] & 0x7f) << 8) | (block[1 + offset] & 0xff);
                    b.Offset = offset + 2;
                    Debug.Assert(b.Length > 0);
                }
            }

            /// <summary>
            /// Returns approximate RAM bytes used </summary>
            public long RamBytesUsed()
            {
                return ((Blocks != null) ? (BlockSize * Blocks.Length) : 0);
            }
        }

        /// <summary>
        /// 1&lt;&lt;blockBits must be bigger than biggest single
        ///  BytesRef slice that will be pulled
        /// </summary>
        public PagedBytes(int blockBits)
        {
            Debug.Assert(blockBits > 0 && blockBits <= 31, blockBits.ToString());
            this.BlockSize = 1 << blockBits;
            this.BlockBits = blockBits;
            BlockMask = BlockSize - 1;
            Upto = BlockSize;
            BytesUsedPerBlock = BlockSize + RamUsageEstimator.NUM_BYTES_ARRAY_HEADER + RamUsageEstimator.NUM_BYTES_OBJECT_REF;
        }

        /// <summary>
        /// Read this many bytes from in </summary>
        public void Copy(IndexInput @in, long byteCount)
        {
            while (byteCount > 0)
            {
                int left = BlockSize - Upto;
                if (left == 0)
                {
                    if (CurrentBlock != null)
                    {
                        Blocks.Add(CurrentBlock);
                        BlockEnd.Add(Upto);
                    }
                    CurrentBlock = new byte[BlockSize];
                    Upto = 0;
                    left = BlockSize;
                }
                if (left < byteCount)
                {
                    @in.ReadBytes(CurrentBlock, Upto, left, false);
                    Upto = BlockSize;
                    byteCount -= left;
                }
                else
                {
                    @in.ReadBytes(CurrentBlock, Upto, (int)byteCount, false);
                    Upto += (int)byteCount;
                    break;
                }
            }
        }

        /// <summary>
        /// Copy BytesRef in, setting BytesRef out to the result.
        /// Do not use this if you will use freeze(true).
        /// this only supports bytes.length <= blockSize
        /// </summary>
        public void Copy(BytesRef bytes, BytesRef @out)
        {
            int left = BlockSize - Upto;
            if (bytes.Length > left || CurrentBlock == null)
            {
                if (CurrentBlock != null)
                {
                    Blocks.Add(CurrentBlock);
                    BlockEnd.Add(Upto);
                    DidSkipBytes = true;
                }
                CurrentBlock = new byte[BlockSize];
                Upto = 0;
                left = BlockSize;
                Debug.Assert(bytes.Length <= BlockSize);
                // TODO: we could also support variable block sizes
            }

            @out.Bytes = CurrentBlock;
            @out.Offset = Upto;
            @out.Length = bytes.Length;

            Array.Copy(bytes.Bytes, bytes.Offset, CurrentBlock, Upto, bytes.Length);
            Upto += bytes.Length;
        }

        /// <summary>
        /// Commits final byte[], trimming it if necessary and if trim=true </summary>
        public Reader Freeze(bool trim)
        {
            if (Frozen)
            {
                throw new InvalidOperationException("already frozen");
            }
            if (DidSkipBytes)
            {
                throw new InvalidOperationException("cannot freeze when copy(BytesRef, BytesRef) was used");
            }
            if (trim && Upto < BlockSize)
            {
                var newBlock = new byte[Upto];
                Array.Copy(CurrentBlock, 0, newBlock, 0, Upto);
                CurrentBlock = newBlock;
            }
            if (CurrentBlock == null)
            {
                CurrentBlock = EMPTY_BYTES;
            }
            Blocks.Add(CurrentBlock);
            BlockEnd.Add(Upto);
            Frozen = true;
            CurrentBlock = null;
            return new PagedBytes.Reader(this);
        }

        public long Pointer
        {
            get
            {
                if (CurrentBlock == null)
                {
                    return 0;
                }
                else
                {
                    return (Blocks.Count * ((long)BlockSize)) + Upto;
                }
            }
        }

        /// <summary>
        /// Return approx RAM usage in bytes. </summary>
        public long RamBytesUsed()
        {
            return (Blocks.Count + (CurrentBlock != null ? 1 : 0)) * BytesUsedPerBlock;
        }

        /// <summary>
        /// Copy bytes in, writing the length as a 1 or 2 byte
        ///  vInt prefix.
        /// </summary>
        // TODO: this really needs to be refactored into fieldcacheimpl
        public long CopyUsingLengthPrefix(BytesRef bytes)
        {
            if (bytes.Length >= 32768)
            {
                throw new System.ArgumentException("max length is 32767 (got " + bytes.Length + ")");
            }

            if (Upto + bytes.Length + 2 > BlockSize)
            {
                if (bytes.Length + 2 > BlockSize)
                {
                    throw new System.ArgumentException("block size " + BlockSize + " is too small to store length " + bytes.Length + " bytes");
                }
                if (CurrentBlock != null)
                {
                    Blocks.Add(CurrentBlock);
                    BlockEnd.Add(Upto);
                }
                CurrentBlock = new byte[BlockSize];
                Upto = 0;
            }

            long pointer = Pointer;

            if (bytes.Length < 128)
            {
                CurrentBlock[Upto++] = (byte)bytes.Length;
            }
            else
            {
                CurrentBlock[Upto++] = unchecked((byte)(0x80 | (bytes.Length >> 8)));
                CurrentBlock[Upto++] = unchecked((byte)(bytes.Length & 0xff));
            }
            Array.Copy(bytes.Bytes, bytes.Offset, CurrentBlock, Upto, bytes.Length);
            Upto += bytes.Length;

            return pointer;
        }

        public sealed class PagedBytesDataInput : DataInput
        {
            private readonly PagedBytes OuterInstance;

            private int CurrentBlockIndex;
            private int CurrentBlockUpto;
            private byte[] CurrentBlock;

            internal PagedBytesDataInput(PagedBytes outerInstance)
            {
                this.OuterInstance = outerInstance;
                CurrentBlock = outerInstance.Blocks[0];
            }

            public override object Clone()
            {
                PagedBytesDataInput clone = OuterInstance.DataInput;
                clone.Position = Position;
                return clone;
            }

            /// <summary>
            /// Returns the current byte position. </summary>
            public long Position
            {
                get
                {
                    return (long)CurrentBlockIndex * OuterInstance.BlockSize + CurrentBlockUpto;
                }
                set // LUCENENET TODO: Change to SetPosition(long position) (has side effect)
                {
                    CurrentBlockIndex = (int)(value >> OuterInstance.BlockBits);
                    CurrentBlock = OuterInstance.Blocks[CurrentBlockIndex];
                    CurrentBlockUpto = (int)(value & OuterInstance.BlockMask);
                }
            }

            public override byte ReadByte()
            {
                if (CurrentBlockUpto == OuterInstance.BlockSize)
                {
                    NextBlock();
                }
                return (byte)CurrentBlock[CurrentBlockUpto++];
            }

            public override void ReadBytes(byte[] b, int offset, int len)
            {
                Debug.Assert(b.Length >= offset + len);
                int offsetEnd = offset + len;
                while (true)
                {
                    int blockLeft = OuterInstance.BlockSize - CurrentBlockUpto;
                    int left = offsetEnd - offset;
                    if (blockLeft < left)
                    {
                        System.Buffer.BlockCopy(CurrentBlock, CurrentBlockUpto, b, offset, blockLeft);
                        NextBlock();
                        offset += blockLeft;
                    }
                    else
                    {
                        // Last block
                        System.Buffer.BlockCopy(CurrentBlock, CurrentBlockUpto, b, offset, left);
                        CurrentBlockUpto += left;
                        break;
                    }
                }
            }

            private void NextBlock()
            {
                CurrentBlockIndex++;
                CurrentBlockUpto = 0;
                CurrentBlock = OuterInstance.Blocks[CurrentBlockIndex];
            }
        }

        public sealed class PagedBytesDataOutput : DataOutput
        {
            private readonly PagedBytes OuterInstance;

            public PagedBytesDataOutput(PagedBytes outerInstance)
            {
                this.OuterInstance = outerInstance;
            }

            public override void WriteByte(byte b)
            {
                if (OuterInstance.Upto == OuterInstance.BlockSize)
                {
                    if (OuterInstance.CurrentBlock != null)
                    {
                        OuterInstance.Blocks.Add(OuterInstance.CurrentBlock);
                        OuterInstance.BlockEnd.Add(OuterInstance.Upto);
                    }
                    OuterInstance.CurrentBlock = new byte[OuterInstance.BlockSize];
                    OuterInstance.Upto = 0;
                }
                OuterInstance.CurrentBlock[OuterInstance.Upto++] = (byte)b;
            }

            public override void WriteBytes(byte[] b, int offset, int length)
            {
                Debug.Assert(b.Length >= offset + length);
                if (length == 0)
                {
                    return;
                }

                if (OuterInstance.Upto == OuterInstance.BlockSize)
                {
                    if (OuterInstance.CurrentBlock != null)
                    {
                        OuterInstance.Blocks.Add(OuterInstance.CurrentBlock);
                        OuterInstance.BlockEnd.Add(OuterInstance.Upto);
                    }
                    OuterInstance.CurrentBlock = new byte[OuterInstance.BlockSize];
                    OuterInstance.Upto = 0;
                }

                int offsetEnd = offset + length;
                while (true)
                {
                    int left = offsetEnd - offset;
                    int blockLeft = OuterInstance.BlockSize - OuterInstance.Upto;
                    if (blockLeft < left)
                    {
                        System.Buffer.BlockCopy(b, offset, OuterInstance.CurrentBlock, OuterInstance.Upto, blockLeft);
                        OuterInstance.Blocks.Add(OuterInstance.CurrentBlock);
                        OuterInstance.BlockEnd.Add(OuterInstance.BlockSize);
                        OuterInstance.CurrentBlock = new byte[OuterInstance.BlockSize];
                        OuterInstance.Upto = 0;
                        offset += blockLeft;
                    }
                    else
                    {
                        // Last block
                        System.Buffer.BlockCopy(b, offset, OuterInstance.CurrentBlock, OuterInstance.Upto, left);
                        OuterInstance.Upto += left;
                        break;
                    }
                }
            }

            /// <summary>
            /// Return the current byte position. </summary>
            public long Position
            {
                get
                {
                    return OuterInstance.Pointer;
                }
            }
        }

        /// <summary>
        /// Returns a DataInput to read values from this
        ///  PagedBytes instance.
        /// </summary>
        public PagedBytesDataInput DataInput // LUCENENET TODO: change to GetDataInput() (returns new instance)
        {
            get
            {
                if (!Frozen)
                {
                    throw new InvalidOperationException("must call freeze() before getDataInput");
                }
                return new PagedBytesDataInput(this);
            }
        }

        /// <summary>
        /// Returns a DataOutput that you may use to write into
        ///  this PagedBytes instance.  If you do this, you should
        ///  not call the other writing methods (eg, copy);
        ///  results are undefined.
        /// </summary>
        public PagedBytesDataOutput DataOutput // LUCENENET TODO: change to GetDataOutput() (returns new instance)
        {
            get
            {
                if (Frozen)
                {
                    throw new InvalidOperationException("cannot get DataOutput after freeze()");
                }
                return new PagedBytesDataOutput(this);
            }
        }
    }
}