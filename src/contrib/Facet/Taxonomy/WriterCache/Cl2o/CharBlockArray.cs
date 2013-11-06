using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

namespace Lucene.Net.Facet.Taxonomy.WriterCache.Cl2o
{
    [Serializable]
    internal class CharBlockArray : ICharSequence
    {
        private static readonly long serialVersionUID = 1L;
        private static readonly int DefaultBlockSize = 32 * 1024;

        [Serializable]
        internal sealed class Block : ICloneable
        {
            private static readonly long serialVersionUID = 1L;
            internal readonly char[] chars;
            internal int length;

            internal Block(int size)
            {
                this.chars = new char[size];
                this.length = 0;
            }

            public object Clone()
            {
                return this.MemberwiseClone();
            }
        }

        internal IList<Block> blocks;
        internal Block current;
        internal int blockSize;
        internal int length;

        internal CharBlockArray()
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
            this.length++;
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

                s.CopyTo(offset, this.current.chars, this.current.length, offset + toCopy);
                offset += toCopy;
                remain -= toCopy;
                this.current.length += toCopy;
            }

            this.length += s.Length;
            return this;
        }

        public char CharAt(int index)
        {
            Block b = blocks[BlockIndex(index)];
            return b.chars[IndexInBlock(index)];
        }

        public int Length
        {
            get
            {
                return this.length;
            }
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
                indexInBlock = 0;
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

        internal virtual void Flush(Stream out_renamed)
        {
            BinaryFormatter oos = null;
            try
            {
                oos = new BinaryFormatter();
                oos.Serialize(out_renamed, this);
                //oos.Flush();
            }
            finally
            {
                //if (oos != null)
                //{
                //    oos.Close();
                //}
            }
        }

        public static CharBlockArray Open(Stream in_renamed)
        {
            BinaryFormatter ois = null;
            try
            {
                ois = new BinaryFormatter();
                CharBlockArray a = (CharBlockArray)ois.Deserialize(in_renamed);
                return a;
            }
            finally
            {
                //if (ois != null)
                //{
                //    ois.Close();
                //}
            }
        }
    }
}
