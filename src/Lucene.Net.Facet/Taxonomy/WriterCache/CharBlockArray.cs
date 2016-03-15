using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Lucene.Net.Store;
using Lucene.Net.Support;

namespace Lucene.Net.Facet.Taxonomy.WriterCache
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

    /// <summary>
    /// Similar to <seealso cref="StringBuilder"/>, but with a more efficient growing strategy.
    /// This class uses char array blocks to grow.
    /// 
    /// @lucene.experimental
    /// </summary>
    public class CharBlockArray : ICharSequence
    {

        private const long serialVersionUID = 1L;

        private const int DefaultBlockSize = 32 * 1024; // 32 KB default size

        internal sealed class Block
        {
            internal const long serialVersionUID = 1L;

            internal readonly char[] chars;
            internal int length;

            internal Block(int size)
            {
                this.chars = new char[size];
                this.length = 0;
            }

            public object Clone()
            {
                throw new NotImplementedException();
            }
        }

        internal IList<Block> blocks;
        internal Block current;
        internal int blockSize;
        internal int length_Renamed;

        public CharBlockArray()
            : this(DefaultBlockSize)
        {
        }

        internal CharBlockArray(int blockSize)
        {
            this.blocks = new List<Block>();
            this.blockSize = blockSize;
            AddBlock();
        }

        private void AddBlock()
        {
            this.current = new Block(this.blockSize);
            this.blocks.Add(this.current);
        }

        internal virtual int BlockIndex(int index)
        {
            return index / blockSize;
        }

        internal virtual int IndexInBlock(int index)
        {
            return index % blockSize;
        }

        public CharBlockArray Append(ICharSequence chars)
        {
            return Append(chars, 0, chars.Length);
        }

        public CharBlockArray Append(char c)
        {
            if (this.current.length == this.blockSize)
            {
                AddBlock();
            }
            this.current.chars[this.current.length++] = c;
            this.length_Renamed++;

            return this;
        }

        public CharBlockArray Append(ICharSequence chars, int start, int length)
        {
            int end = start + length;
            for (int i = start; i < end; i++)
            {
                Append(chars.CharAt(i));
            }
            return this;
        }

        public virtual CharBlockArray Append(char[] chars, int start, int length)
        {
            int offset = start;
            int remain = length;
            while (remain > 0)
            {
                if (this.current.length == this.blockSize)
                {
                    AddBlock();
                }
                int toCopy = remain;
                int remainingInBlock = this.blockSize - this.current.length;
                if (remainingInBlock < toCopy)
                {
                    toCopy = remainingInBlock;
                }
                Array.Copy(chars, offset, this.current.chars, this.current.length, toCopy);
                offset += toCopy;
                remain -= toCopy;
                this.current.length += toCopy;
            }

            this.length_Renamed += length;
            return this;
        }

        public virtual CharBlockArray Append(string s)
        {
            int remain = s.Length;
            int offset = 0;
            while (remain > 0)
            {
                if (this.current.length == this.blockSize)
                {
                    AddBlock();
                }
                int toCopy = remain;
                int remainingInBlock = this.blockSize - this.current.length;
                if (remainingInBlock < toCopy)
                {
                    toCopy = remainingInBlock;
                }
                s.CopyTo(offset, this.current.chars, this.current.length, offset + toCopy - offset);
                offset += toCopy;
                remain -= toCopy;
                this.current.length += toCopy;
            }

            this.length_Renamed += s.Length;
            return this;
        }

        public int Length
        {
            get
            {
                return this.length_Renamed;
            }
        }

        public char CharAt(int index)
        {
            Block b = blocks[BlockIndex(index)];
            return b.chars[IndexInBlock(index)];
        }

        public ICharSequence SubSequence(int start, int end)
        {
            int remaining = end - start;
            StringBuilder sb = new StringBuilder(remaining);
            int blockIdx = BlockIndex(start);
            int indexInBlock = IndexInBlock(start);
            while (remaining > 0)
            {
                Block b = blocks[blockIdx++];
                int numToAppend = Math.Min(remaining, b.length - indexInBlock);
                sb.Append(b.chars, indexInBlock, numToAppend);
                remaining -= numToAppend;
                indexInBlock = 0; // 2nd+ iterations read from start of the block
            }
            return new StringCharSequenceWrapper(sb.ToString());
        }




        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            foreach (Block b in blocks)
            {
                sb.Append(b.chars, 0, b.length);
            }
            return sb.ToString();
        }

        internal virtual void Flush(OutputStreamDataOutput @out)
        {
            
            using (var ms = StreamUtils.SerializeToStream(this))
            {
                var bytes = ms.ToArray();
                @out.WriteBytes(bytes, 0, bytes.Length);
            }
        }

        public static CharBlockArray Open(BinaryReader @in)
        {
            return StreamUtils.DeserializeFromStream(@in) as CharBlockArray;
        }

    }

}