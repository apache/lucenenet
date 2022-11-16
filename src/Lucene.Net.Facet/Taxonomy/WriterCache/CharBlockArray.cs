// Lucene version compatibility level 4.8.1
using J2N.Text;
using Lucene.Net.Support;
using Lucene.Net.Support.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using JCG = J2N.Collections.Generic;

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
    /// Similar to <see cref="StringBuilder"/>, but with a more efficient growing strategy.
    /// This class uses char array blocks to grow.
    /// <para/>
    /// @lucene.experimental
    /// </summary>
    // LUCENENET NOTE: The serialization features here are strictly for testing purposes,
    // therefore it doesn't make any difference what type of serialization is used. 
    // To make things simpler, we are using BinaryReader and BinaryWriter since 
    // BinaryFormatter is not implemented in .NET Standard 1.x.
    internal class CharBlockArray : ICharSequence
    {
        private const long serialVersionUID = 1L;

        private const int DEFAULT_BLOCK_SIZE = 32 * 1024; // 32 KB default size

        internal sealed class Block // LUCENENET specific: Not implementing ICloneable per Microsoft's recommendation
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
                var clone = new Block(chars.Length);
                clone.length = length;
                Arrays.Copy(chars, clone.chars, chars.Length);
                return clone;
            }

            

            // LUCENENET specific
            public void Serialize(Stream writer)
            {
                writer.Write(serialVersionUID); // Version of this object to use when deserializing
                writer.Write(chars.Length);
                writer.Write(chars);
                writer.Write(length);
            }

            // LUCENENET specific
            // Deserialization constructor
            public Block(Stream reader)
            {
                long serialVersion = reader.ReadInt64();

                switch (serialVersion)
                {
                    case serialVersionUID:
                        int charsLength = reader.ReadInt32();
                        this.chars = reader.ReadChars(charsLength);
                        this.length = reader.ReadInt32();
                        break;

                    // case 1L:
                    // LUCENENET TODO: When object fields change, increment serialVersionUID and move the above block here for legacy support...
                    default:
                        throw new InvalidDataException($"Version {serialVersion} of {this.GetType()} deserialization is not supported.");
                }
            }
        }

        internal IList<Block> blocks;
        internal Block current;
        internal int blockSize;
        internal int length;

        public CharBlockArray()
            : this(DEFAULT_BLOCK_SIZE)
        {
        }

        internal CharBlockArray(int blockSize)
        {
            this.blocks = new JCG.List<Block>();
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

        public virtual CharBlockArray Append(ICharSequence chars)
        {
            return Append(chars, 0, chars.Length);
        }

        public virtual CharBlockArray Append(char c)
        {
            if (this.current.length == this.blockSize)
            {
                AddBlock();
            }
            this.current.chars[this.current.length++] = c;
            this.length++;

            return this;
        }

        public virtual CharBlockArray Append(ICharSequence chars, int start, int length)
        {
            int end = start + length;
            for (int i = start; i < end; i++)
            {
                Append(chars[i]);
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
                Arrays.Copy(chars, offset, this.current.chars, this.current.length, toCopy);
                offset += toCopy;
                remain -= toCopy;
                this.current.length += toCopy;
            }

            this.length += length;
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
                s.CopyTo(offset, this.current.chars, this.current.length, toCopy);
                offset += toCopy;
                remain -= toCopy;
                this.current.length += toCopy;
            }

            this.length += s.Length;
            return this;
        }

        // LUCENENET specific - replaced with this[index]
        //public virtual char CharAt(int index)
        //{
        //    Block b = blocks[BlockIndex(index)];
        //    return b.chars[IndexInBlock(index)];
        //}

        // LUCENENET specific - added to .NETify
        public virtual char this[int index]
        {
            get
            {
                Block b = blocks[BlockIndex(index)];
                return b.chars[IndexInBlock(index)];
            }
        }

        public virtual int Length => this.length;


        // LUCENENET specific
        bool ICharSequence.HasValue => true;

        public virtual ICharSequence Subsequence(int startIndex, int length)
        {
            int remaining = length;
            StringBuilder sb = new StringBuilder(remaining);
            int blockIdx = BlockIndex(startIndex);
            int indexInBlock = IndexInBlock(startIndex);
            while (remaining > 0)
            {
                Block b = blocks[blockIdx++];
                int numToAppend = Math.Min(remaining, b.length - indexInBlock);
                sb.Append(b.chars, indexInBlock, numToAppend);
                remaining -= numToAppend;
                indexInBlock = 0; // 2nd+ iterations read from start of the block
            }
            return new StringBuilderCharSequence(sb);
        }

        ICharSequence ICharSequence.Subsequence(int startIndex, int length)
        {
            return Subsequence(startIndex, length);
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

        internal virtual void Flush(Stream @out)
        {
            @out.Write(serialVersionUID); // version of this object to use when deserializing
            @out.Write(blocks.Count);
            int currentIndex = 0;
            for (int i = 0; i < blocks.Count; i++)
            {
                var block = blocks[i];
                block.Serialize(@out);
                if (block == current)
                {
                    currentIndex = i;
                }
            }
            // Write the index of the current block so we can
            // set the reference when deserializing
            @out.Write(currentIndex);
            @out.Write(blockSize);
            @out.Write(length);
            @out.Flush();
        }

        // LUCENENET specific
        // Deserialization constructor
        internal CharBlockArray(Stream reader)
        {
            long serialVersion = reader.ReadInt64();

            switch (serialVersion)
            {
                case serialVersionUID:
                    var blocksCount = reader.ReadInt32();
                    this.blocks = new JCG.List<Block>(blocksCount);
                    for (int i = 0; i < blocksCount; i++)
                    {
                        blocks.Add(new Block(reader));
                    }
                    this.current = blocks[reader.ReadInt32()];
                    this.blockSize = reader.ReadInt32();
                    this.length = reader.ReadInt32();
                    break;

                // case 1L:
                // LUCENENET TODO: When object fields change, increment serialVersionUID and move the above block here for legacy support...
                default:
                    throw new InvalidDataException($"Version {serialVersion} of {this.GetType()} deserialization is not supported.");
            }
        }

        public static CharBlockArray Open(Stream @in)
        {
            return new CharBlockArray(@in);
        }
    }
}