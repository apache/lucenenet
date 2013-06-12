using Lucene.Net.Store;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Util
{
    public sealed class PagedBytes
    {
        private readonly List<sbyte[]> blocks = new List<sbyte[]>();
        private readonly List<int> blockEnd = new List<int>();
        private readonly int blockSize;
        private readonly int blockBits;
        private readonly int blockMask;
        private bool didSkipBytes;
        private bool frozen;
        private int upto;
        private sbyte[] currentBlock;

        private static readonly sbyte[] EMPTY_BYTES = new sbyte[0];

        public sealed class Reader
        {
            private readonly sbyte[][] blocks;
            private readonly int[] blockEnds;
            private readonly int blockBits;
            private readonly int blockMask;
            private readonly int blockSize;

            internal Reader(PagedBytes pagedBytes)
            {
                blocks = new sbyte[pagedBytes.blocks.Count][];
                for (int i = 0; i < blocks.Length; i++)
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

            public void FillSlice(BytesRef b, long start, int length)
            {
                //assert length >= 0: "length=" + length;
                //assert length <= blockSize+1;
                int index = (int)(start >> blockBits);
                int offset = (int)(start & blockMask);
                b.length = length;
                if (blockSize - offset >= length)
                {
                    // Within block
                    b.bytes = blocks[index];
                    b.offset = offset;
                }
                else
                {
                    // Split
                    b.bytes = new sbyte[length];
                    b.offset = 0;
                    Array.Copy(blocks[index], offset, b.bytes, 0, blockSize - offset);
                    Array.Copy(blocks[1 + index], 0, b.bytes, blockSize - offset, length - (blockSize - offset));
                }
            }

            public void Fill(BytesRef b, long start)
            {
                int index = (int)(start >> blockBits);
                int offset = (int)(start & blockMask);
                sbyte[] block = b.bytes = blocks[index];

                if ((block[offset] & 128) == 0)
                {
                    b.length = block[offset];
                    b.offset = offset + 1;
                }
                else
                {
                    b.length = ((block[offset] & 0x7f) << 8) | (block[1 + offset] & 0xff);
                    b.offset = offset + 2;
                    //assert b.length > 0;
                }
            }
        }

        public PagedBytes(int blockBits)
        {
            //assert blockBits > 0 && blockBits <= 31 : blockBits;
            this.blockSize = 1 << blockBits;
            this.blockBits = blockBits;
            blockMask = blockSize - 1;
            upto = blockSize;
        }

        public void Copy(IndexInput input, long byteCount)
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
                    currentBlock = new sbyte[blockSize];
                    upto = 0;
                    left = blockSize;
                }
                if (left < byteCount)
                {
                    input.ReadBytes(currentBlock, upto, left, false);
                    upto = blockSize;
                    byteCount -= left;
                }
                else
                {
                    input.ReadBytes(currentBlock, upto, (int)byteCount, false);
                    upto += (int)byteCount; // overflow warning!
                    break;
                }
            }
        }

        public void Copy(BytesRef bytes, BytesRef output)
        {
            int left = blockSize - upto;
            if (bytes.length > left || currentBlock == null)
            {
                if (currentBlock != null)
                {
                    blocks.Add(currentBlock);
                    blockEnd.Add(upto);
                    didSkipBytes = true;
                }
                currentBlock = new sbyte[blockSize];
                upto = 0;
                left = blockSize;
                //assert bytes.length <= blockSize;
                // TODO: we could also support variable block sizes
            }

            output.bytes = currentBlock;
            output.offset = upto;
            output.length = bytes.length;

            Array.Copy(bytes.bytes, bytes.offset, currentBlock, upto, bytes.length);
            upto += bytes.length;
        }

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
                sbyte[] newBlock = new sbyte[upto];
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

        public long CopyUsingLengthPrefix(BytesRef bytes)
        {
            if (bytes.length >= 32768)
            {
                throw new ArgumentException("max length is 32767 (got " + bytes.length + ")");
            }

            if (upto + bytes.length + 2 > blockSize)
            {
                if (bytes.length + 2 > blockSize)
                {
                    throw new ArgumentException("block size " + blockSize + " is too small to store length " + bytes.length + " bytes");
                }
                if (currentBlock != null)
                {
                    blocks.Add(currentBlock);
                    blockEnd.Add(upto);
                }
                currentBlock = new sbyte[blockSize];
                upto = 0;
            }

            long pointer = Pointer;

            if (bytes.length < 128)
            {
                currentBlock[upto++] = (sbyte)bytes.length;
            }
            else
            {
                currentBlock[upto++] = (sbyte)(0x80 | (bytes.length >> 8));
                currentBlock[upto++] = (sbyte)(bytes.length & 0xff);
            }
            Array.Copy(bytes.bytes, bytes.offset, currentBlock, upto, bytes.length);
            upto += bytes.length;

            return pointer;
        }

        public sealed class PagedBytesDataInput : DataInput
        {
            private int currentBlockIndex;
            private int currentBlockUpto;
            private sbyte[] currentBlock;
            private readonly PagedBytes parent;

            public PagedBytesDataInput(PagedBytes parent)
            {
                currentBlock = parent.blocks[0];
                this.parent = parent;
            }

            public override object Clone()
            {
                PagedBytesDataInput clone = parent.GetDataInput();
                clone.Position = clone.Position; // intentionally call getter and setter
                return clone;
            }

            public long Position
            {
                get { return (long)currentBlockIndex * parent.blockSize + currentBlockUpto; }
                set
                {
                    currentBlockIndex = (int)(value >> parent.blockBits);
                    currentBlock = parent.blocks[currentBlockIndex];
                    currentBlockUpto = (int)(value & parent.blockMask);
                }
            }

            public override byte ReadByte()
            {
                if (currentBlockUpto == parent.blockSize)
                {
                    NextBlock();
                }
                return (byte)currentBlock[currentBlockUpto++];
            }

            public override void ReadBytes(byte[] b, int offset, int len)
            {
                //assert b.length >= offset + len;
                int offsetEnd = offset + len;
                while (true)
                {
                    int blockLeft = parent.blockSize - currentBlockUpto;
                    int left = offsetEnd - offset;
                    if (blockLeft < left)
                    {
                        Array.Copy(currentBlock, currentBlockUpto,
                                         b, offset,
                                         blockLeft);
                        NextBlock();
                        offset += blockLeft;
                    }
                    else
                    {
                        // Last block
                        Array.Copy(currentBlock, currentBlockUpto,
                                         b, offset,
                                         left);
                        currentBlockUpto += left;
                        break;
                    }
                }
            }

            private void NextBlock()
            {
                currentBlockIndex++;
                currentBlockUpto = 0;
                currentBlock = parent.blocks[currentBlockIndex];
            }
        }

        public sealed class PagedBytesDataOutput : DataOutput
        {
            private readonly PagedBytes parent;

            public PagedBytesDataOutput(PagedBytes parent)
            {
                this.parent = parent;
            }

            public override void WriteByte(byte b)
            {
                if (parent.upto == parent.blockSize)
                {
                    if (parent.currentBlock != null)
                    {
                        parent.blocks.Add(parent.currentBlock);
                        parent.blockEnd.Add(parent.upto);
                    }
                    parent.currentBlock = new sbyte[parent.blockSize];
                    parent.upto = 0;
                }
                parent.currentBlock[parent.upto++] = (sbyte)b;
            }
            
            public override void WriteBytes(byte[] b, int offset, int length)
            {
                //assert b.length >= offset + length;
                if (length == 0)
                {
                    return;
                }

                if (parent.upto == parent.blockSize)
                {
                    if (parent.currentBlock != null)
                    {
                        parent.blocks.Add(parent.currentBlock);
                        parent.blockEnd.Add(parent.upto);
                    }
                    parent.currentBlock = new sbyte[parent.blockSize];
                    parent.upto = 0;
                }

                int offsetEnd = offset + length;
                while (true)
                {
                    int left = offsetEnd - offset;
                    int blockLeft = parent.blockSize - parent.upto;
                    if (blockLeft < left)
                    {
                        Array.Copy(b, offset, parent.currentBlock, parent.upto, blockLeft);
                        parent.blocks.Add(parent.currentBlock);
                        parent.blockEnd.Add(parent.blockSize);
                        parent.currentBlock = new sbyte[parent.blockSize];
                        parent.upto = 0;
                        offset += blockLeft;
                    }
                    else
                    {
                        // Last block
                        Array.Copy(b, offset, parent.currentBlock, parent.upto, left);
                        parent.upto += left;
                        break;
                    }
                }
            }

            public long Position
            {
                get { return parent.Pointer; }
            }
        }

        public PagedBytesDataInput GetDataInput()
        {
            if (!frozen)
            {
                throw new InvalidOperationException("must call freeze() before getDataInput");
            }
            return new PagedBytesDataInput(this);
        }

        public PagedBytesDataOutput GetDataOutput()
        {
            if (frozen)
            {
                throw new InvalidOperationException("cannot get DataOutput after freeze()");
            }
            return new PagedBytesDataOutput(this);
        }
    }
}
