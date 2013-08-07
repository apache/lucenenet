using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Store
{
    public sealed class ByteArrayDataInput : DataInput
    {
        private byte[] bytes;

        private int pos;
        private int limit;

        public ByteArrayDataInput(byte[] bytes)
        {
            Reset(bytes);
        }

        public ByteArrayDataInput(byte[] bytes, int offset, int len)
        {
            Reset(bytes, offset, len);
        }

        public ByteArrayDataInput()
        {
            Reset((byte[])(Array)BytesRef.EMPTY_BYTES);
        }

        public void Reset(byte[] bytes)
        {
            Reset(bytes, 0, bytes.Length);
        }

        // NOTE: sets pos to 0, which is not right if you had
        // called reset w/ non-zero offset!!
        public void Rewind()
        {
            pos = 0;
        }

        public int Position
        {
            get { return pos; }
            set { this.pos = value; }
        }

        public void Reset(byte[] bytes, int offset, int len)
        {
            this.bytes = bytes;
            pos = offset;
            limit = offset + len;
        }

        public int Length
        {
            get { return limit; }
        }

        public bool EOF
        {
            get { return pos == limit; }
        }

        public void SkipBytes(int count)
        {
            pos += count;
        }

        public override short ReadShort()
        {
            return (short)(((bytes[pos++] & 0xFF) << 8) | (bytes[pos++] & 0xFF));
        }

        public override int ReadInt()
        {
            return ((bytes[pos++] & 0xFF) << 24) | ((bytes[pos++] & 0xFF) << 16)
                | ((bytes[pos++] & 0xFF) << 8) | (bytes[pos++] & 0xFF);
        }

        public override long ReadLong()
        {
            int i1 = ((bytes[pos++] & 0xff) << 24) | ((bytes[pos++] & 0xff) << 16) |
                ((bytes[pos++] & 0xff) << 8) | (bytes[pos++] & 0xff);
            int i2 = ((bytes[pos++] & 0xff) << 24) | ((bytes[pos++] & 0xff) << 16) |
                ((bytes[pos++] & 0xff) << 8) | (bytes[pos++] & 0xff);
            return (((long)i1) << 32) | (i2 & 0xFFFFFFFFL);
        }

        public override int ReadVInt()
        {
            // .NET Port: going back to original style code instead of Java code below due to sbyte/byte diff
            byte b = bytes[pos++];
            int i = b & 0x7F;
            for (int shift = 7; (b & 0x80) != 0; shift += 7)
            {
                b = bytes[pos++];
                i |= (b & 0x7F) << shift;
            }
            return i;

            //byte b = bytes[pos++];
            //if (b >= 0) return b;
            //int i = b & 0x7F;
            //b = bytes[pos++];
            //i |= (b & 0x7F) << 7;
            //if (b >= 0) return i;
            //b = bytes[pos++];
            //i |= (b & 0x7F) << 14;
            //if (b >= 0) return i;
            //b = bytes[pos++];
            //i |= (b & 0x7F) << 21;
            //if (b >= 0) return i;
            //b = bytes[pos++];
            //// Warning: the next ands use 0x0F / 0xF0 - beware copy/paste errors:
            //i |= (b & 0x0F) << 28;
            //if ((b & 0xF0) == 0) return i;
            //throw new InvalidOperationException("Invalid vInt detected (too many bits)");
        }

        public override long ReadVLong()
        {
            byte b = bytes[pos++];
            if (b >= 0) return b;
            long i = b & 0x7FL;
            b = bytes[pos++];
            i |= (b & 0x7FL) << 7;
            if (b >= 0) return i;
            b = bytes[pos++];
            i |= (b & 0x7FL) << 14;
            if (b >= 0) return i;
            b = bytes[pos++];
            i |= (b & 0x7FL) << 21;
            if (b >= 0) return i;
            b = bytes[pos++];
            i |= (b & 0x7FL) << 28;
            if (b >= 0) return i;
            b = bytes[pos++];
            i |= (b & 0x7FL) << 35;
            if (b >= 0) return i;
            b = bytes[pos++];
            i |= (b & 0x7FL) << 42;
            if (b >= 0) return i;
            b = bytes[pos++];
            i |= (b & 0x7FL) << 49;
            if (b >= 0) return i;
            b = bytes[pos++];
            i |= (b & 0x7FL) << 56;
            if (b >= 0) return i;
            throw new InvalidOperationException("Invalid vLong detected (negative values disallowed)");
        }

        public override byte ReadByte()
        {
            return bytes[pos++];
        }

        public override void ReadBytes(byte[] b, int offset, int len)
        {
            Buffer.BlockCopy(bytes, pos, b, offset, len);
            pos += len;
        }
    }
}
