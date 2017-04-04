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
        private readonly IList<byte[]> blocks = new List<byte[]>();

        // TODO: these are unused?
        private readonly IList<int> blockEnd = new List<int>();

        private readonly int blockSize;
        private readonly int blockBits;
        private readonly int blockMask;
        private bool didSkipBytes;
        private bool frozen;
        private int upto;
        private byte[] currentBlock;
        private readonly long bytesUsedPerBlock;

        private static readonly byte[] EMPTY_BYTES = new byte[0];

        /// <summary>
        /// Provides methods to read BytesRefs from a frozen
        ///  PagedBytes.
        /// </summary>
        /// <seealso cref= #freeze  </seealso>
        public sealed class Reader
        {
            private readonly byte[][] blocks;
            private readonly int[] blockEnds;
            private readonly int blockBits;
            private readonly int blockMask;
            private readonly int blockSize;

            internal Reader(PagedBytes pagedBytes)
            {
                blocks = new byte[pagedBytes.blocks.Count][];
                for (var i = 0; i < blocks.Length; i++)
                {
                    blocks[i] = pagedBytes.blocks[i];
                }
                blockEnds = new int[blocks.Length];
                for (int i = 0; i < blockEnds.Length; i++)
                {
                    blockEnds[i] = pagedBytes.blockEnd[i];
                }
                blockBits = pagedBytes.blockBits;
                blockMask = pagedBytes.blockMask;
                blockSize = pagedBytes.blockSize;
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
                Debug.Assert(length <= blockSize + 1, "length=" + length);
                b.Length = length;
                if (length == 0)
                {
                    return;
                }
                var index = (int)(start >> blockBits);
                var offset = (int)(start & blockMask);
                if (blockSize - offset >= length)
                {
                    // Within block
                    b.Bytes = blocks[index];
                    b.Offset = offset;
                }
                else
                {
                    // Split
                    b.Bytes = new byte[length];
                    b.Offset = 0;
                    Array.Copy(blocks[index], offset, b.Bytes, 0, blockSize - offset);
                    Array.Copy(blocks[1 + index], 0, b.Bytes, blockSize - offset, length - (blockSize - offset));
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
                var index = (int)(start >> blockBits);
                var offset = (int)(start & blockMask);
                var block = b.Bytes = blocks[index];

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
                return ((blocks != null) ? (blockSize * blocks.Length) : 0);
            }
        }

        /// <summary>
        /// 1&lt;&lt;blockBits must be bigger than biggest single
        ///  BytesRef slice that will be pulled
        /// </summary>
        public PagedBytes(int blockBits)
        {
            Debug.Assert(blockBits > 0 && blockBits <= 31, blockBits.ToString());
            this.blockSize = 1 << blockBits;
            this.blockBits = blockBits;
            blockMask = blockSize - 1;
            upto = blockSize;
            bytesUsedPerBlock = blockSize + RamUsageEstimator.NUM_BYTES_ARRAY_HEADER + RamUsageEstimator.NUM_BYTES_OBJECT_REF;
        }

        /// <summary>
        /// Read this many bytes from in </summary>
        public void Copy(IndexInput @in, long byteCount)
        {
            while (byteCount > 0)
            {
                int left = blockSize - upto;
                if (left == 0)
                {
                    if (currentBlock != null)
                    {
                        blocks.Add(currentBlock);
                        blockEnd.Add(upto);
                    }
                    currentBlock = new byte[blockSize];
                    upto = 0;
                    left = blockSize;
                }
                if (left < byteCount)
                {
                    @in.ReadBytes(currentBlock, upto, left, false);
                    upto = blockSize;
                    byteCount -= left;
                }
                else
                {
                    @in.ReadBytes(currentBlock, upto, (int)byteCount, false);
                    upto += (int)byteCount;
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
            int left = blockSize - upto;
            if (bytes.Length > left || currentBlock == null)
            {
                if (currentBlock != null)
                {
                    blocks.Add(currentBlock);
                    blockEnd.Add(upto);
                    didSkipBytes = true;
                }
                currentBlock = new byte[blockSize];
                upto = 0;
                left = blockSize;
                Debug.Assert(bytes.Length <= blockSize);
                // TODO: we could also support variable block sizes
            }

            @out.Bytes = currentBlock;
            @out.Offset = upto;
            @out.Length = bytes.Length;

            Array.Copy(bytes.Bytes, bytes.Offset, currentBlock, upto, bytes.Length);
            upto += bytes.Length;
        }

        /// <summary>
        /// Commits final byte[], trimming it if necessary and if trim=true </summary>
        public Reader Freeze(bool trim)
        {
            if (frozen)
            {
                throw new InvalidOperationException("already frozen");
            }
            if (didSkipBytes)
            {
                throw new InvalidOperationException("cannot freeze when copy(BytesRef, BytesRef) was used");
            }
            if (trim && upto < blockSize)
            {
                var newBlock = new byte[upto];
                Array.Copy(currentBlock, 0, newBlock, 0, upto);
                currentBlock = newBlock;
            }
            if (currentBlock == null)
            {
                currentBlock = EMPTY_BYTES;
            }
            blocks.Add(currentBlock);
            blockEnd.Add(upto);
            frozen = true;
            currentBlock = null;
            return new PagedBytes.Reader(this);
        }

        public long Pointer
        {
            get
            {
                if (currentBlock == null)
                {
                    return 0;
                }
                else
                {
                    return (blocks.Count * ((long)blockSize)) + upto;
                }
            }
        }

        /// <summary>
        /// Return approx RAM usage in bytes. </summary>
        public long RamBytesUsed()
        {
            return (blocks.Count + (currentBlock != null ? 1 : 0)) * bytesUsedPerBlock;
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

            if (upto + bytes.Length + 2 > blockSize)
            {
                if (bytes.Length + 2 > blockSize)
                {
                    throw new System.ArgumentException("block size " + blockSize + " is too small to store length " + bytes.Length + " bytes");
                }
                if (currentBlock != null)
                {
                    blocks.Add(currentBlock);
                    blockEnd.Add(upto);
                }
                currentBlock = new byte[blockSize];
                upto = 0;
            }

            long pointer = Pointer;

            if (bytes.Length < 128)
            {
                currentBlock[upto++] = (byte)bytes.Length;
            }
            else
            {
                currentBlock[upto++] = unchecked((byte)(0x80 | (bytes.Length >> 8)));
                currentBlock[upto++] = unchecked((byte)(bytes.Length & 0xff));
            }
            Array.Copy(bytes.Bytes, bytes.Offset, currentBlock, upto, bytes.Length);
            upto += bytes.Length;

            return pointer;
        }

        public sealed class PagedBytesDataInput : DataInput
        {
            private readonly PagedBytes outerInstance;

            private int currentBlockIndex;
            private int currentBlockUpto;
            private byte[] currentBlock;

            internal PagedBytesDataInput(PagedBytes outerInstance)
            {
                this.outerInstance = outerInstance;
                currentBlock = outerInstance.blocks[0];
            }

            public override object Clone()
            {
                PagedBytesDataInput clone = outerInstance.GetDataInput();
                clone.SetPosition(Position);
                return clone;
            }

            /// <summary>
            /// Returns the current byte position. </summary>
            public long Position
            {
                get
                {
                    return (long)currentBlockIndex * outerInstance.blockSize + currentBlockUpto;
                }
            }

            /// <summary>
            /// Seek to a position previously obtained from <see cref="Position"/>.
            /// </summary>
            /// <param name="position"></param>
            public void SetPosition(long position)
            {
                currentBlockIndex = (int)(position >> outerInstance.blockBits);
                currentBlock = outerInstance.blocks[currentBlockIndex];
                currentBlockUpto = (int)(position & outerInstance.blockMask);
            }

            public override byte ReadByte()
            {
                if (currentBlockUpto == outerInstance.blockSize)
                {
                    NextBlock();
                }
                return (byte)currentBlock[currentBlockUpto++];
            }

            public override void ReadBytes(byte[] b, int offset, int len)
            {
                Debug.Assert(b.Length >= offset + len);
                int offsetEnd = offset + len;
                while (true)
                {
                    int blockLeft = outerInstance.blockSize - currentBlockUpto;
                    int left = offsetEnd - offset;
                    if (blockLeft < left)
                    {
                        System.Buffer.BlockCopy(currentBlock, currentBlockUpto, b, offset, blockLeft);
                        NextBlock();
                        offset += blockLeft;
                    }
                    else
                    {
                        // Last block
                        System.Buffer.BlockCopy(currentBlock, currentBlockUpto, b, offset, left);
                        currentBlockUpto += left;
                        break;
                    }
                }
            }

            private void NextBlock()
            {
                currentBlockIndex++;
                currentBlockUpto = 0;
                currentBlock = outerInstance.blocks[currentBlockIndex];
            }
        }

        public sealed class PagedBytesDataOutput : DataOutput
        {
            private readonly PagedBytes outerInstance;

            public PagedBytesDataOutput(PagedBytes outerInstance)
            {
                this.outerInstance = outerInstance;
            }

            public override void WriteByte(byte b)
            {
                if (outerInstance.upto == outerInstance.blockSize)
                {
                    if (outerInstance.currentBlock != null)
                    {
                        outerInstance.blocks.Add(outerInstance.currentBlock);
                        outerInstance.blockEnd.Add(outerInstance.upto);
                    }
                    outerInstance.currentBlock = new byte[outerInstance.blockSize];
                    outerInstance.upto = 0;
                }
                outerInstance.currentBlock[outerInstance.upto++] = (byte)b;
            }

            public override void WriteBytes(byte[] b, int offset, int length)
            {
                Debug.Assert(b.Length >= offset + length);
                if (length == 0)
                {
                    return;
                }

                if (outerInstance.upto == outerInstance.blockSize)
                {
                    if (outerInstance.currentBlock != null)
                    {
                        outerInstance.blocks.Add(outerInstance.currentBlock);
                        outerInstance.blockEnd.Add(outerInstance.upto);
                    }
                    outerInstance.currentBlock = new byte[outerInstance.blockSize];
                    outerInstance.upto = 0;
                }

                int offsetEnd = offset + length;
                while (true)
                {
                    int left = offsetEnd - offset;
                    int blockLeft = outerInstance.blockSize - outerInstance.upto;
                    if (blockLeft < left)
                    {
                        System.Buffer.BlockCopy(b, offset, outerInstance.currentBlock, outerInstance.upto, blockLeft);
                        outerInstance.blocks.Add(outerInstance.currentBlock);
                        outerInstance.blockEnd.Add(outerInstance.blockSize);
                        outerInstance.currentBlock = new byte[outerInstance.blockSize];
                        outerInstance.upto = 0;
                        offset += blockLeft;
                    }
                    else
                    {
                        // Last block
                        System.Buffer.BlockCopy(b, offset, outerInstance.currentBlock, outerInstance.upto, left);
                        outerInstance.upto += left;
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
                    return outerInstance.Pointer;
                }
            }
        }

        /// <summary>
        /// Returns a DataInput to read values from this
        ///  PagedBytes instance.
        /// </summary>
        public PagedBytesDataInput GetDataInput()
        {
            if (!frozen)
            {
                throw new InvalidOperationException("must call Freeze() before GetDataInput()");
            }
            return new PagedBytesDataInput(this);
        }

        /// <summary>
        /// Returns a DataOutput that you may use to write into
        ///  this PagedBytes instance.  If you do this, you should
        ///  not call the other writing methods (eg, copy);
        ///  results are undefined.
        /// </summary>
        public PagedBytesDataOutput GetDataOutput()
        {
            if (frozen)
            {
                throw new InvalidOperationException("cannot get DataOutput after Freeze()");
            }
            return new PagedBytesDataOutput(this);
        }
    }
}