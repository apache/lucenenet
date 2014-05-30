using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Support
{
    public abstract class ByteBuffer
    {
        // .NET Port Notes: This has been implemented from the best interpretation
        // of the description of the methods on the Java documentation here:
        // http://docs.oracle.com/javase/6/docs/api/java/nio/ByteBuffer.html
        // and from various Googling of the implementation's usage

        private bool _readonly;
        private byte[] _data;
        private int _offset;

        private int mark = -1;
        private int position = 0;
        private int limit;
        private int capacity;

        protected ByteBuffer(int mark, int pos, int lim, int cap)
        {
            if (cap < 0)
                throw new Exception ("Negative capacity: " + cap);
            this.capacity = cap;
            Limit(lim);
            position = pos;
            if (mark >= 0)
            {
                if (mark > pos)
                    throw new Exception("mark > position: ("
                                                       + mark + " > " + pos + ")");
                this.mark = mark;
            }
        }

        public override object Array
        {
            get { return _data; }
        }

        public override int ArrayOffset
        {
            get { return _offset; }
        }

        public sealed override bool HasArray
        {
            get { return _data != null; }
        }

        public abstract override bool IsDirect { get; }

        public abstract override bool IsReadOnly { get; }

        public static ByteBuffer Allocate(int capacity)
        {
            return new WrappedByteBuffer(-1, 0, capacity, capacity)
            {
                _data = new byte[capacity],
                _offset = 0
            };
        }

        public int Position
        {
            get
            {
                return position;
            }

            set
            {
                if ((value > limit) || value < 0)
                    throw new Exception("Position is too large.");
                position = value;
                if (mark > position) mark = -1;
            }
        }

        public ByteBuffer Limit (int limit) {
            if ((limit > capacity || limit < 0))
                throw new Exception("Limit is too large.");
            this.limit = limit;
            if (position > this.limit)
            {
                position = this.limit;
            }
            if (mark > this.limit)
            {
                mark = -1;
            }
            return this;
        }

        public Boolean HasRemaining()
        {
            return position < limit;
        }

        public int Remaining()
        {
            return limit - position;
        }

        // public static ByteBuffer AllocateDirect(int capacity)

        public static ByteBuffer Wrap(byte[] array, int offset, int length)
        {
            return new WrappedByteBuffer(-1, offset, offset + length, array.Length)
            {
                _data = array,
                _offset = 0
            };
        }

        public static ByteBuffer Wrap(byte[] array)
        {
            return new WrappedByteBuffer(-1, 0, array.Length, array.Length)
            {
                _data = array,
                _offset = 0
            };
        }

        public abstract ByteBuffer Slice();

        public abstract ByteBuffer Duplicate();

        public abstract ByteBuffer AsReadOnlyBuffer();

        public abstract byte Get();

        public abstract ByteBuffer Put(byte b);

        public abstract byte Get(int index);

        public abstract ByteBuffer Put(int index, byte b);

        public virtual ByteBuffer Get(byte[] dst, int offset, int length)
        {
            for (int i = offset; i < offset + length; i++)
            {
                dst[i] = Get();
            }

            return this;
        }

        public virtual ByteBuffer Get(byte[] dst)
        {
            return Get(dst, 0, dst.Length);
        }

        public virtual ByteBuffer Put(ByteBuffer src)
        {
            while (src.HasRemaining())
                Put(src.Get());

            return this;
        }

        public virtual ByteBuffer Put(byte[] src, int offset, int length)
        {
            for (int i = offset; i < offset + length; i++)
                Put(src[i]);

            return this;
        }

        public ByteBuffer Put(byte[] src)
        {
            return Put(src, 0, src.Length);
        }

        public abstract ByteBuffer Compact();

        public int CompareTo(ByteBuffer other)
        {
            // TODO: implement this
            return 0;
        }

        public abstract char GetChar();

        public abstract ByteBuffer PutChar(char value);

        public abstract char GetChar(int index);

        public abstract ByteBuffer PutChar(int index, char value);

        //public abstract CharBuffer AsCharBuffer();

        public abstract short GetShort();

        public abstract ByteBuffer PutShort(short value);

        public abstract short GetShort(int index);

        public abstract ByteBuffer PutShort(int index, short value);

        // public abstract ShortBuffer AsShortBuffer();

        public abstract int GetInt();

        public abstract ByteBuffer PutInt(int value);

        public abstract int GetInt(int index);

        public abstract ByteBuffer PutInt(int index, int value);

        //public abstract IntBuffer AsIntBuffer();

        public abstract long GetLong();

        public abstract ByteBuffer PutLong(long value);

        public abstract long GetLong(int index);

        public abstract ByteBuffer PutLong(int index, long value);

        //public abstract LongBuffer AsLongBuffer();

        public abstract float GetFloat();

        public abstract ByteBuffer PutFloat(float value);

        public abstract float GetFloat(int index);

        public abstract ByteBuffer PutFloat(int index, float value);

        //public abstract FloatBuffer AsFloatBuffer();

        public abstract double GetDouble();

        public abstract ByteBuffer PutDouble(double value);

        public abstract double GetDouble(int index);

        public abstract ByteBuffer PutDouble(int index, double value);

        //public abstract DoubleBuffer AsDoubleBuffer();

        public class WrappedByteBuffer : ByteBuffer
        {
            public WrappedByteBuffer(int mark, int pos, int lim, int cap)
                : base(mark, pos, lim, cap)
            {
            }

            public override bool IsDirect
            {
                get { return false; }
            }

            public override bool IsReadOnly
            {
                get { return _readonly; }
            }

            public override ByteBuffer Slice()
            {
                return new WrappedByteBuffer(-1, 0, Remaining(), Remaining())
                {
                    _data = this._data,
                    _offset = this._offset
                };
            }

            public override ByteBuffer Duplicate()
            {
                return new WrappedByteBuffer(mark, position, limit, capacity)
                {
                    _data = this._data,
                    _offset = this._offset
                };
            }

            public override ByteBuffer AsReadOnlyBuffer()
            {
                throw new NotImplementedException();
            }

            public override byte Get()
            {
                return _data[position++];
            }

            public override ByteBuffer Put(byte b)
            {
                _data[position++] = b;
                return this;
            }

            public override byte Get(int index)
            {
                return _data[index];
            }

            public override ByteBuffer Put(int index, byte b)
            {
                _data[index] = b;
                return this;
            }

            public override ByteBuffer Compact()
            {
                if (position == 0)
                    return this;

                int p = position;
                for (int i = 0; i < limit - 1; i++)
                {
                    _data[i] = _data[p];
                    p++;
                }

                position = limit - position;
                limit = capacity;
                mark = -1;

                return this;
            }

            public override char GetChar()
            {
                var c = BitConverter.ToChar(_data, position);

                position += 2;

                return c;
            }

            public override ByteBuffer PutChar(char value)
            {
                var bytes = BitConverter.GetBytes(value);

                _data[position++] = bytes[0];
                _data[position++] = bytes[1];

                return this;
            }

            public override char GetChar(int index)
            {
                var c = BitConverter.ToChar(_data, index);

                return c;
            }

            public override ByteBuffer PutChar(int index, char value)
            {
                var bytes = BitConverter.GetBytes(value);

                _data[index++] = bytes[0];
                _data[index++] = bytes[1];

                return this;
            }

            public override short GetShort()
            {
                var c = BitConverter.ToInt16(_data, position);

                position += 2;

                return c;
            }

            public override ByteBuffer PutShort(short value)
            {
                var bytes = BitConverter.GetBytes(value);

                _data[position++] = bytes[0];
                _data[position++] = bytes[1];

                return this;
            }

            public override short GetShort(int index)
            {
                var c = BitConverter.ToInt16(_data, index);

                return c;
            }

            public override ByteBuffer PutShort(int index, short value)
            {
                var bytes = BitConverter.GetBytes(value);

                _data[index++] = bytes[0];
                _data[index++] = bytes[1];

                return this;
            }

            public override int GetInt()
            {
                var c = BitConverter.ToInt32(_data, position);

                position += 4;

                return c;
            }

            public override ByteBuffer PutInt(int value)
            {
                var bytes = BitConverter.GetBytes(value);

                _data[position++] = bytes[0];
                _data[position++] = bytes[1];
                _data[position++] = bytes[2];
                _data[position++] = bytes[3];

                return this;
            }

            public override int GetInt(int index)
            {
                var c = BitConverter.ToInt32(_data, index);

                return c;
            }

            public override ByteBuffer PutInt(int index, int value)
            {
                var bytes = BitConverter.GetBytes(value);

                _data[index++] = bytes[0];
                _data[index++] = bytes[1];
                _data[index++] = bytes[2];
                _data[index++] = bytes[3];

                return this;
            }

            public override long GetLong()
            {
                var c = BitConverter.ToInt64(_data, position);

                position += 8;

                return c;
            }

            public override ByteBuffer PutLong(long value)
            {
                var bytes = BitConverter.GetBytes(value);

                _data[position++] = bytes[0];
                _data[position++] = bytes[1];
                _data[position++] = bytes[2];
                _data[position++] = bytes[3];
                _data[position++] = bytes[4];
                _data[position++] = bytes[5];
                _data[position++] = bytes[6];
                _data[position++] = bytes[7];

                return this;
            }

            public override long GetLong(int index)
            {
                var c = BitConverter.ToInt64(_data, index);

                return c;
            }

            public override ByteBuffer PutLong(int index, long value)
            {
                var bytes = BitConverter.GetBytes(value);

                _data[index++] = bytes[0];
                _data[index++] = bytes[1];
                _data[index++] = bytes[2];
                _data[index++] = bytes[3];
                _data[index++] = bytes[4];
                _data[index++] = bytes[5];
                _data[index++] = bytes[6];
                _data[index++] = bytes[7];

                return this;
            }

            public override float GetFloat()
            {
                var c = BitConverter.ToSingle(_data, position);

                position += 4;

                return c;
            }

            public override ByteBuffer PutFloat(float value)
            {
                var bytes = BitConverter.GetBytes(value);

                _data[position++] = bytes[0];
                _data[position++] = bytes[1];
                _data[position++] = bytes[2];
                _data[position++] = bytes[3];

                return this;
            }

            public override float GetFloat(int index)
            {
                var c = BitConverter.ToSingle(_data, index);

                return c;
            }

            public override ByteBuffer PutFloat(int index, float value)
            {
                var bytes = BitConverter.GetBytes(value);

                _data[index++] = bytes[0];
                _data[index++] = bytes[1];
                _data[index++] = bytes[2];
                _data[index++] = bytes[3];

                return this;
            }

            public override double GetDouble()
            {
                var c = BitConverter.ToDouble(_data, position);

                position += 8;

                return c;
            }

            public override ByteBuffer PutDouble(double value)
            {
                var bytes = BitConverter.GetBytes(value);

                _data[position++] = bytes[0];
                _data[position++] = bytes[1];
                _data[position++] = bytes[2];
                _data[position++] = bytes[3];
                _data[position++] = bytes[4];
                _data[position++] = bytes[5];
                _data[position++] = bytes[6];
                _data[position++] = bytes[7];

                return this;
            }

            public override double GetDouble(int index)
            {
                var c = BitConverter.ToDouble(_data, index);

                return c;
            }

            public override ByteBuffer PutDouble(int index, double value)
            {
                var bytes = BitConverter.GetBytes(value);

                _data[index++] = bytes[0];
                _data[index++] = bytes[1];
                _data[index++] = bytes[2];
                _data[index++] = bytes[3];
                _data[index++] = bytes[4];
                _data[index++] = bytes[5];
                _data[index++] = bytes[6];
                _data[index++] = bytes[7];

                return this;
            }
        }

    }
}