// Lucene version compatibility level 4.8.1
using J2N.Text;
using Lucene.Net.Support;
using Lucene.Net.Support.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
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
    internal class CharBlockArray : IAppendable, ICharSequence,
        ISpanAppendable /* LUCENENET specific */
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

        internal readonly IList<Block> blocks; // LUCENENET: marked readonly
        internal Block current;
        internal readonly int blockSize; // LUCENENET: marked readonly
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual int BlockIndex(int index)
        {
            return index / blockSize;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual int IndexInBlock(int index)
        {
            return index % blockSize;
        }

#nullable enable

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

        public virtual CharBlockArray Append(ICharSequence? value)
        {
            if (value is null) // needed for Appendable compliance
            {
                return this; // No-op
            }

            return Append(value, 0, value.Length);
        }

        public virtual CharBlockArray Append(ICharSequence? value, int startIndex, int length)
        {
            // LUCENENET: Changed semantics to be the same as the StringBuilder in .NET
            if (startIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(startIndex), $"{nameof(startIndex)} must not be negative.");
            if (length < 0)
                throw new ArgumentOutOfRangeException(nameof(length), $"{nameof(length)} must not be negative.");

            if (value is null)
            {
                if (startIndex == 0 && length == 0)
                    return this;
                throw new ArgumentNullException(nameof(value));
            }
            if (length == 0)
                return this;
            if (startIndex > value.Length - length)
                throw new ArgumentOutOfRangeException(nameof(startIndex), $"Index and length must refer to a location within the string. For example {nameof(startIndex)} + {nameof(length)} <= {nameof(Length)}.");


            int end = startIndex + length;
            for (int i = startIndex; i < end; i++)
            {
                Append(value[i]);
            }
            return this;
        }

        public virtual CharBlockArray Append(char[]? value)
        {
            if (value is null) // needed for Appendable compliance
            {
                return this; // No-op
            }

            int remain = value.Length;
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
                Arrays.Copy(value, offset, this.current.chars, this.current.length, toCopy);
                offset += toCopy;
                remain -= toCopy;
                this.current.length += toCopy;
            }

            this.length += value.Length;
            return this;
        }

        public virtual CharBlockArray Append(char[]? value, int startIndex, int length)
        {
            // LUCENENET: Changed semantics to be the same as the StringBuilder in .NET
            if (startIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(startIndex), $"{nameof(startIndex)} must not be negative.");
            if (length < 0)
                throw new ArgumentOutOfRangeException(nameof(length), $"{nameof(length)} must not be negative.");

            if (value is null)
            {
                if (startIndex == 0 && length == 0)
                    return this;
                throw new ArgumentNullException(nameof(value));
            }
            if (length == 0)
                return this;
            if (startIndex > value.Length - length)
                throw new ArgumentOutOfRangeException(nameof(startIndex), $"Index and length must refer to a location within the string. For example {nameof(startIndex)} + {nameof(length)} <= {nameof(Length)}.");

            int offset = startIndex;
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
                Arrays.Copy(value, offset, this.current.chars, this.current.length, toCopy);
                offset += toCopy;
                remain -= toCopy;
                this.current.length += toCopy;
            }

            this.length += length;
            return this;
        }

        public virtual CharBlockArray Append(string? value)
        {
            if (value is null) // needed for Appendable compliance
            {
                return this; // No-op
            }

            int remain = value.Length;
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
                value.CopyTo(offset, this.current.chars, this.current.length, toCopy);
                offset += toCopy;
                remain -= toCopy;
                this.current.length += toCopy;
            }

            this.length += value.Length;
            return this;
        }

        public virtual CharBlockArray Append(string? value, int startIndex, int length)
        {
            // LUCENENET: Changed semantics to be the same as the StringBuilder in .NET
            if (startIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(startIndex), $"{nameof(startIndex)} must not be negative.");
            if (length < 0)
                throw new ArgumentOutOfRangeException(nameof(length), $"{nameof(length)} must not be negative.");

            if (value is null)
            {
                if (startIndex == 0 && length == 0)
                    return this;
                throw new ArgumentNullException(nameof(value));
            }
            if (length == 0)
                return this;
            if (startIndex > value.Length - length)
                throw new ArgumentOutOfRangeException(nameof(startIndex), $"Index and length must refer to a location within the string. For example {nameof(startIndex)} + {nameof(length)} <= {nameof(Length)}.");

            int offset = startIndex;
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
                value.CopyTo(offset, this.current.chars, this.current.length, toCopy);
                offset += toCopy;
                remain -= toCopy;
                this.current.length += toCopy;
            }

            this.length += length;
            return this;
        }

        public virtual CharBlockArray Append(StringBuilder? value)
        {
            if (value is null) // needed for Appendable compliance
            {
                return this; // No-op
            }

            int remain = value.Length;
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
                value.CopyTo(offset, this.current.chars, this.current.length, toCopy);
                offset += toCopy;
                remain -= toCopy;
                this.current.length += toCopy;
            }

            this.length += value.Length;
            return this;
        }

        public virtual CharBlockArray Append(StringBuilder? value, int startIndex, int length)
        {
            // LUCENENET: Changed semantics to be the same as the StringBuilder in .NET
            if (startIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(startIndex), $"{nameof(startIndex)} must not be negative.");
            if (length < 0)
                throw new ArgumentOutOfRangeException(nameof(length), $"{nameof(length)} must not be negative.");

            if (value is null)
            {
                if (startIndex == 0 && length == 0)
                    return this;
                throw new ArgumentNullException(nameof(value));
            }
            if (length == 0)
                return this;
            if (startIndex > value.Length - length)
                throw new ArgumentOutOfRangeException(nameof(startIndex), $"Index and length must refer to a location within the string. For example {nameof(startIndex)} + {nameof(length)} <= {nameof(Length)}.");

            int offset = startIndex;
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
                value.CopyTo(offset, this.current.chars, this.current.length, toCopy);
                offset += toCopy;
                remain -= toCopy;
                this.current.length += toCopy;
            }

            this.length += length;
            return this;
        }

        public virtual CharBlockArray Append(ReadOnlySpan<char> value)
        {
            int offset = 0;
            int remain = value.Length;
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
                value.Slice(offset, toCopy).CopyTo(this.current.chars.AsSpan(this.current.length));
                offset += toCopy;
                remain -= toCopy;
                this.current.length += toCopy;
            }

            this.length += value.Length;
            return this;
        }

#nullable restore

        #region IAppendable Members

        IAppendable IAppendable.Append(char value) => Append(value);

        IAppendable IAppendable.Append(string value) => Append(value);

        IAppendable IAppendable.Append(string value, int startIndex, int count) => Append(value, startIndex, count);

        IAppendable IAppendable.Append(StringBuilder value) => Append(value);

        IAppendable IAppendable.Append(StringBuilder value, int startIndex, int count) => Append(value, startIndex, count);

        IAppendable IAppendable.Append(char[] value) => Append(value);

        IAppendable IAppendable.Append(char[] value, int startIndex, int count) => Append(value, startIndex, count);

        IAppendable IAppendable.Append(ICharSequence value) => Append(value);

        IAppendable IAppendable.Append(ICharSequence value, int startIndex, int count) => Append(value, startIndex, count);

        #endregion

        #region ISpanAppendable Members

        ISpanAppendable ISpanAppendable.Append(ReadOnlySpan<char> value) => Append(value);

        #endregion

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


        // LUCENENET specific - Lucene allocated memory using Subsequence and
        // then called hashCode(), which calculated based on the value of the subsequence.
        // However, in .NET this uses the indexer of the StringBuilder that Subsequence returned,
        // which is super slow
        // (see: https://learn.microsoft.com/en-us/dotnet/api/system.text.stringbuilder.chars).
        // But this operation doesn't require an allocation at all if we simply calculate the
        // value based off of the chars that are in the CharArrayBlock.
        //
        // This is a combination of Subsequence(int, int) and the J2N.Text.CharSequenceComparer.Ordinal.GetHashCode()
        // implementation. The hash code calculated must be kept in sync with the J2N implementation
        // (which originated in Apache Harmony) in order to return the correct result.
        internal int GetHashCode(int startIndex, int length)
        {
            if (length == 0)
                return 0;
            int hash = 0;
            int remaining = length;
            int blockIdx = BlockIndex(startIndex);
            int indexInBlock = IndexInBlock(startIndex);
            while (remaining > 0)
            {
                Block b = blocks[blockIdx++];
                int numToCheck = Math.Min(remaining, b.length - indexInBlock);
                int end = indexInBlock + numToCheck;
                var chars = b.chars;
                for (int i = indexInBlock; i < end; i++)
                {
                    unchecked
                    {
                        // Hash code calculation from J2N/Apache Harmony
                        hash = chars[i] + ((hash << 5) - hash);
                    }
                }
                remaining -= numToCheck;
                indexInBlock = 0; // 2nd+ iterations read from start of the block
            }
            return hash;
        }

        /// <summary>
        /// Compares a slice of this <see cref="CharBlockArray"/> to <paramref name="other"/>
        /// for binary (ordinal) equality. Does not allocate any memory.
        /// <para/>
        /// LUCENENET specific.
        /// </summary>
        /// <param name="startIndex">The start index of this <see cref="CharBlockArray"/>.</param>
        /// <param name="length">The length of characters to compare.</param>
        /// <param name="other">The other character sequence to check for equality.</param>
        /// <returns><c>true</c> if the two character sequences are equal; otherwise <c>false</c></returns>
        internal bool Equals(int startIndex, int length, ReadOnlySpan<char> other)
        {
            if (other.Length != length) return false;

            int remaining = length;
            int blockIdx = BlockIndex(startIndex);
            int indexInBlock = IndexInBlock(startIndex);
            int otherIndex = 0;
            while (remaining > 0)
            {
                Block b = blocks[blockIdx++];
                int numToCheck = Math.Min(remaining, b.length - indexInBlock);
                var charsToCheck = b.chars.AsSpan(indexInBlock, numToCheck);
                if (!other.Slice(otherIndex, numToCheck).Equals(charsToCheck, StringComparison.Ordinal))
                    return false;
                remaining -= numToCheck;
                otherIndex += numToCheck;
                indexInBlock = 0; // 2nd+ iterations read from start of the block
            }
            return true;
        }
    }
}
