using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Lucene.Net.Util.Fst
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

    // TODO: merge with PagedBytes, except PagedBytes doesn't
    // let you read while writing which FST needs
    internal class BytesStore : DataOutput
    {
        private readonly List<byte[]> blocks = new List<byte[]>();

        private readonly int blockSize;
        private readonly int blockBits;
        private readonly int blockMask;

        private byte[] current;
        private int nextWrite;

        public BytesStore(int blockBits)
        {
            this.blockBits = blockBits;
            blockSize = 1 << blockBits;
            blockMask = blockSize - 1;
            nextWrite = blockSize;
        }

        /// <summary>
        /// Pulls bytes from the provided IndexInput. </summary>
        public BytesStore(DataInput @in, long numBytes, int maxBlockSize)
        {
            int blockSize = 2;
            int blockBits = 1;
            while (blockSize < numBytes && blockSize < maxBlockSize)
            {
                blockSize *= 2;
                blockBits++;
            }
            this.blockBits = blockBits;
            this.blockSize = blockSize;
            this.blockMask = blockSize - 1;
            long left = numBytes;
            while (left > 0)
            {
                int chunk = (int)Math.Min(blockSize, left);
                byte[] block = new byte[chunk];
                @in.ReadBytes(block, 0, block.Length);
                blocks.Add(block);
                left -= chunk;
            }

            // So .getPosition still works
            nextWrite = blocks[blocks.Count - 1].Length;
        }

        /// <summary>
        /// Absolute write byte; you must ensure dest is < max
        ///  position written so far.
        /// </summary>
        public virtual void WriteByte(int dest, byte b)
        {
            int blockIndex = dest >> blockBits;
            byte[] block = blocks[blockIndex];
            block[dest & blockMask] = b;
        }

        public override void WriteByte(byte b)
        {
            if (nextWrite == blockSize)
            {
                current = new byte[blockSize];
                blocks.Add(current);
                nextWrite = 0;
            }
            current[nextWrite++] = b;
        }

        public override void WriteBytes(byte[] b, int offset, int len)
        {
            while (len > 0)
            {
                int chunk = blockSize - nextWrite;
                if (len <= chunk)
                {
                    System.Buffer.BlockCopy(b, offset, current, nextWrite, len);
                    nextWrite += len;
                    break;
                }
                else
                {
                    if (chunk > 0)
                    {
                        Array.Copy(b, offset, current, nextWrite, chunk);
                        offset += chunk;
                        len -= chunk;
                    }
                    current = new byte[blockSize];
                    blocks.Add(current);
                    nextWrite = 0;
                }
            }
        }

        internal virtual int BlockBits
        {
            get
            {
                return blockBits;
            }
        }

        /// <summary>
        /// Absolute writeBytes without changing the current
        ///  position.  Note: this cannot "grow" the bytes, so you
        ///  must only call it on already written parts.
        /// </summary>
        internal virtual void WriteBytes(long dest, byte[] b, int offset, int len)
        {
            //System.out.println("  BS.writeBytes dest=" + dest + " offset=" + offset + " len=" + len);
            Debug.Assert(dest + len <= Position, "dest=" + dest + " pos=" + Position + " len=" + len);

            // Note: weird: must go "backwards" because copyBytes
            // calls us with overlapping src/dest.  If we
            // go forwards then we overwrite bytes before we can
            // copy them:

            /*
            int blockIndex = dest >> blockBits;
            int upto = dest & blockMask;
            byte[] block = blocks.get(blockIndex);
            while (len > 0) {
              int chunk = blockSize - upto;
              System.out.println("    cycle chunk=" + chunk + " len=" + len);
              if (len <= chunk) {
                System.arraycopy(b, offset, block, upto, len);
                break;
              } else {
                System.arraycopy(b, offset, block, upto, chunk);
                offset += chunk;
                len -= chunk;
                blockIndex++;
                block = blocks.get(blockIndex);
                upto = 0;
              }
            }
            */

            long end = dest + len;
            int blockIndex = (int)(end >> blockBits);
            int downTo = (int)(end & blockMask);
            if (downTo == 0)
            {
                blockIndex--;
                downTo = blockSize;
            }
            byte[] block = blocks[blockIndex];

            while (len > 0)
            {
                //System.out.println("    cycle downTo=" + downTo + " len=" + len);
                if (len <= downTo)
                {
                    //System.out.println("      final: offset=" + offset + " len=" + len + " dest=" + (downTo-len));
                    Array.Copy(b, offset, block, downTo - len, len);
                    break;
                }
                else
                {
                    len -= downTo;
                    //System.out.println("      partial: offset=" + (offset + len) + " len=" + downTo + " dest=0");
                    Array.Copy(b, offset + len, block, 0, downTo);
                    blockIndex--;
                    block = blocks[blockIndex];
                    downTo = blockSize;
                }
            }
        }

        /// <summary>
        /// Absolute copy bytes self to self, without changing the
        ///  position. Note: this cannot "grow" the bytes, so must
        ///  only call it on already written parts.
        /// </summary>
        public virtual void CopyBytes(long src, long dest, int len)
        {
            //System.out.println("BS.copyBytes src=" + src + " dest=" + dest + " len=" + len);
            Debug.Assert(src < dest);

            // Note: weird: must go "backwards" because copyBytes
            // calls us with overlapping src/dest.  If we
            // go forwards then we overwrite bytes before we can
            // copy them:

            /*
            int blockIndex = src >> blockBits;
            int upto = src & blockMask;
            byte[] block = blocks.get(blockIndex);
            while (len > 0) {
              int chunk = blockSize - upto;
              System.out.println("  cycle: chunk=" + chunk + " len=" + len);
              if (len <= chunk) {
                writeBytes(dest, block, upto, len);
                break;
              } else {
                writeBytes(dest, block, upto, chunk);
                blockIndex++;
                block = blocks.get(blockIndex);
                upto = 0;
                len -= chunk;
                dest += chunk;
              }
            }
            */

            long end = src + len;

            int blockIndex = (int)(end >> blockBits);
            int downTo = (int)(end & blockMask);
            if (downTo == 0)
            {
                blockIndex--;
                downTo = blockSize;
            }
            byte[] block = blocks[blockIndex];

            while (len > 0)
            {
                //System.out.println("  cycle downTo=" + downTo);
                if (len <= downTo)
                {
                    //System.out.println("    finish");
                    WriteBytes(dest, block, downTo - len, len);
                    break;
                }
                else
                {
                    //System.out.println("    partial");
                    len -= downTo;
                    WriteBytes(dest + len, block, 0, downTo);
                    blockIndex--;
                    block = blocks[blockIndex];
                    downTo = blockSize;
                }
            }
        }

        /// <summary>
        /// Writes an int at the absolute position without
        ///  changing the current pointer.
        /// </summary>
        public virtual void WriteInt(long pos, int value) // LUCENENET TODO: Change to WriteInt32 ?
        {
            int blockIndex = (int)(pos >> blockBits);
            int upto = (int)(pos & blockMask);
            byte[] block = blocks[blockIndex];
            int shift = 24;
            for (int i = 0; i < 4; i++)
            {
                block[upto++] = (byte)(value >> shift);
                shift -= 8;
                if (upto == blockSize)
                {
                    upto = 0;
                    blockIndex++;
                    block = blocks[blockIndex];
                }
            }
        }

        /// <summary>
        /// Reverse from srcPos, inclusive, to destPos, inclusive. </summary>
        public virtual void Reverse(long srcPos, long destPos)
        {
            Debug.Assert(srcPos < destPos);
            Debug.Assert(destPos < Position);
            //System.out.println("reverse src=" + srcPos + " dest=" + destPos);

            int srcBlockIndex = (int)(srcPos >> blockBits);
            int src = (int)(srcPos & blockMask);
            byte[] srcBlock = blocks[srcBlockIndex];

            int destBlockIndex = (int)(destPos >> blockBits);
            int dest = (int)(destPos & blockMask);
            byte[] destBlock = blocks[destBlockIndex];
            //System.out.println("  srcBlock=" + srcBlockIndex + " destBlock=" + destBlockIndex);

            int limit = (int)(destPos - srcPos + 1) / 2;
            for (int i = 0; i < limit; i++)
            {
                //System.out.println("  cycle src=" + src + " dest=" + dest);
                byte b = srcBlock[src];
                srcBlock[src] = destBlock[dest];
                destBlock[dest] = b;
                src++;
                if (src == blockSize)
                {
                    srcBlockIndex++;
                    srcBlock = blocks[srcBlockIndex];
                    //System.out.println("  set destBlock=" + destBlock + " srcBlock=" + srcBlock);
                    src = 0;
                }

                dest--;
                if (dest == -1)
                {
                    destBlockIndex--;
                    destBlock = blocks[destBlockIndex];
                    //System.out.println("  set destBlock=" + destBlock + " srcBlock=" + srcBlock);
                    dest = blockSize - 1;
                }
            }
        }

        public virtual void SkipBytes(int len)
        {
            while (len > 0)
            {
                int chunk = blockSize - nextWrite;
                if (len <= chunk)
                {
                    nextWrite += len;
                    break;
                }
                else
                {
                    len -= chunk;
                    current = new byte[blockSize];
                    blocks.Add(current);
                    nextWrite = 0;
                }
            }
        }

        public virtual long Position
        {
            get
            {
                return ((long)blocks.Count - 1) * blockSize + nextWrite;
            }
        }

        /// <summary>
        /// Pos must be less than the max position written so far!
        ///  Ie, you cannot "grow" the file with this!
        /// </summary>
        public virtual void Truncate(long newLen)
        {
            Debug.Assert(newLen <= Position);
            Debug.Assert(newLen >= 0);
            int blockIndex = (int)(newLen >> blockBits);
            nextWrite = (int)(newLen & blockMask);
            if (nextWrite == 0)
            {
                blockIndex--;
                nextWrite = blockSize;
            }
            blocks.RemoveRange(blockIndex + 1, blocks.Count - (blockIndex + 1));
            if (newLen == 0)
            {
                current = null;
            }
            else
            {
                current = blocks[blockIndex];
            }
            Debug.Assert(newLen == Position);
        }

        public virtual void Finish()
        {
            if (current != null)
            {
                byte[] lastBuffer = new byte[nextWrite];
                Array.Copy(current, 0, lastBuffer, 0, nextWrite);
                blocks[blocks.Count - 1] = lastBuffer;
                current = null;
            }
        }

        /// <summary>
        /// Writes all of our bytes to the target <seealso cref="DataOutput"/>. </summary>
        public virtual void WriteTo(DataOutput @out)
        {
            foreach (byte[] block in blocks)
            {
                @out.WriteBytes(block, 0, block.Length);
            }
        }

        public virtual FST.BytesReader ForwardReader // LUCENENET TODO: Change to GetForwardReader() (returns new instance)
        {
            get
            {
                if (blocks.Count == 1)
                {
                    return new ForwardBytesReader(blocks[0]);
                }
                return new ForwardBytesReaderAnonymousInner(this);
            }
        }

        private class ForwardBytesReaderAnonymousInner : FST.BytesReader
        {
            private readonly BytesStore outerInstance;

            public ForwardBytesReaderAnonymousInner(BytesStore outerInstance)
            {
                this.outerInstance = outerInstance;
                nextRead = outerInstance.blockSize;
            }

            private byte[] Current;
            private int nextBuffer;
            private int nextRead;

            public override byte ReadByte()
            {
                if (nextRead == outerInstance.blockSize)
                {
                    Current = outerInstance.blocks[nextBuffer++];
                    nextRead = 0;
                }
                return Current[nextRead++];
            }

            public override void SkipBytes(int count)
            {
                Position = Position + count;
            }

            public override void ReadBytes(byte[] b, int offset, int len)
            {
                while (len > 0)
                {
                    int chunkLeft = outerInstance.blockSize - nextRead;
                    if (len <= chunkLeft)
                    {
                        Array.Copy(Current, nextRead, b, offset, len);
                        nextRead += len;
                        break;
                    }
                    else
                    {
                        if (chunkLeft > 0)
                        {
                            Array.Copy(Current, nextRead, b, offset, chunkLeft);
                            offset += chunkLeft;
                            len -= chunkLeft;
                        }
                        Current = outerInstance.blocks[nextBuffer++];
                        nextRead = 0;
                    }
                }
            }

            public override long Position
            {
                get
                {
                    return ((long)nextBuffer - 1) * outerInstance.blockSize + nextRead;
                }
                set // LUCENENET TODO: change to SetPosition(long position) (side-effect)
                {
                    int bufferIndex = (int)(value >> outerInstance.blockBits);
                    nextBuffer = bufferIndex + 1;
                    Current = outerInstance.blocks[bufferIndex];
                    nextRead = (int)(value & outerInstance.blockMask);
                    Debug.Assert(this.Position == value, "pos=" + value + " getPos()=" + this.Position);
                }
            }

            public override bool IsReversed
            {
                get { return false; }
            }
        }

        public virtual FST.BytesReader ReverseReader // LUCENENET TODO: Change to GetReverseReader() (returns new instance)
        {
            get
            {
                return GetReverseReader(true);
            }
        }

        internal virtual FST.BytesReader GetReverseReader(bool allowSingle)
        {
            if (allowSingle && blocks.Count == 1)
            {
                return new ReverseBytesReader(blocks[0]);
            }
            return new ReverseBytesReaderAnonymousInner(this);
        }

        private class ReverseBytesReaderAnonymousInner : FST.BytesReader
        {
            private readonly BytesStore outerInstance;

            public ReverseBytesReaderAnonymousInner(BytesStore outerInstance)
            {
                this.outerInstance = outerInstance;
                Current = outerInstance.blocks.Count == 0 ? null : outerInstance.blocks[0];
                nextBuffer = -1;
                nextRead = 0;
            }

            private byte[] Current; // LUCENENET TODO: rename
            private int nextBuffer;
            private int nextRead;

            public override byte ReadByte()
            {
                if (nextRead == -1)
                {
                    Current = outerInstance.blocks[nextBuffer--];
                    nextRead = outerInstance.blockSize - 1;
                }
                return Current[nextRead--];
            }

            public override void SkipBytes(int count)
            {
                Position = Position - count;
            }

            public override void ReadBytes(byte[] b, int offset, int len)
            {
                for (int i = 0; i < len; i++)
                {
                    b[offset + i] = ReadByte();
                }
            }

            public override long Position
            {
                get
                {
                    return ((long)nextBuffer + 1) * outerInstance.blockSize + nextRead;
                }
                set // LUCENENET TODO: change to SetPosition(long position) (side-effect)
                {
                    // NOTE: a little weird because if you
                    // setPosition(0), the next byte you read is
                    // bytes[0] ... but I would expect bytes[-1] (ie,
                    // EOF)...?
                    int bufferIndex = (int)(value >> outerInstance.blockBits);
                    nextBuffer = bufferIndex - 1;
                    Current = outerInstance.blocks[bufferIndex];
                    nextRead = (int)(value & outerInstance.blockMask);
                    Debug.Assert(this.Position == value, "value=" + value + " this.Position=" + this.Position);
                }
            }

            public override bool IsReversed
            {
                get { return true; }
            }
        }
    }
}