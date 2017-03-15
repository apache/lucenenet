using System;
using System.IO.MemoryMappedFiles;

namespace Lucene.Net.Support
{
    internal sealed class MemoryMappedFileByteBuffer : ByteBuffer, IDisposable
    {
        private MemoryMappedViewAccessor _accessor;
        private readonly int offset;
        new private bool bigEndian = true;

        public MemoryMappedFileByteBuffer(MemoryMappedViewAccessor accessor, int mark, int pos, int lim, int cap)
            : base(mark, pos, lim, cap)
        {
            _accessor = accessor;
        }

        public MemoryMappedFileByteBuffer(MemoryMappedViewAccessor accessor, int mark, int pos, int lim, int cap, int offset)
            : this(accessor, mark, pos, lim, cap)
        {
            this.offset = offset;
        }

        public override ByteBuffer Slice()
        {
            return new MemoryMappedFileByteBuffer(_accessor, -1, 0, Remaining, Remaining);
        }

        public override ByteBuffer Duplicate()
        {
            return new MemoryMappedFileByteBuffer(_accessor, MarkValue, Position, Limit, Capacity);
        }

        public override ByteBuffer AsReadOnlyBuffer()
        {
            throw new NotImplementedException();
        }


        private int Ix(int i)
        {
            return i + offset;
        }

        public override byte Get()
        {
            return _accessor.ReadByte(Ix(NextGetIndex()));
        }

        public override byte Get(int index)
        {
            return _accessor.ReadByte(Ix(CheckIndex(index)));
        }

#if !NETSTANDARD
        // Implementation provided by Vincent Van Den Berghe: http://git.net/ml/general/2017-02/msg31639.html
        public override ByteBuffer Get(byte[] dst, int offset, int length)
        {
            CheckBounds(offset, length, dst.Length);
            if (length > Remaining)
            {
                throw new BufferUnderflowException();
            }
            // we need to check for 0-length reads, since 
            // ReadArray will throw an ArgumentOutOfRange exception if position is at
            // the end even when nothing is read
            if (length > 0)
            {
                _accessor.ReadArray(Ix(NextGetIndex(length)), dst, offset, length);
            }

            return this;
        }
#endif

        public override bool IsDirect
        {
            get { return false; }
        }

        public override bool IsReadOnly
        {
            get { return false; }
        }

        public override ByteBuffer Put(byte b)
        {
            _accessor.Write(Ix(NextPutIndex()), b);
            return this;
        }

        public override ByteBuffer Put(int index, byte b)
        {
            _accessor.Write(Ix(CheckIndex(index)), b);
            return this;
        }

#if !NETSTANDARD
        // Implementation provided by Vincent Van Den Berghe: http://git.net/ml/general/2017-02/msg31639.html
        public override ByteBuffer Put(byte[] src, int offset, int length)
        {
            CheckBounds(offset, length, src.Length);
            if (length > Remaining)
            {
                throw new BufferOverflowException();
            }
            // we need to check for 0-length writes, since 
            // ReadArray will throw an ArgumentOutOfRange exception if position is at 
            // the end even when nothing is read
            if (length > 0)
            {
                _accessor.WriteArray(Ix(NextPutIndex(length)), src, offset, length);
            }
            return this;
        }
#endif

        public override ByteBuffer Compact()
        {
            throw new NotSupportedException();
        }

        internal override byte _get(int i)
        {
            throw new NotSupportedException();
        }

        internal override void _put(int i, byte b)
        {
            throw new NotSupportedException();
        }


        public override char GetChar()
        {
            var littleEndian = _accessor.ReadChar(Ix(NextGetIndex(2)));
            if (bigEndian)
            {
                return Number.FlipEndian(littleEndian);
            }
            return littleEndian;
        }

        public override char GetChar(int index)
        {
            var littleEndian = _accessor.ReadChar(Ix(CheckIndex(index, 2)));
            if (bigEndian)
            {
                return Number.FlipEndian(littleEndian);
            }
            return littleEndian;
        }

        public override ByteBuffer PutChar(char value)
        {
            _accessor.Write(Ix(NextPutIndex(2)), bigEndian ? Number.FlipEndian(value) : value);
            return this;
        }

        

        public override ByteBuffer PutChar(int index, char value)
        {
            _accessor.Write(Ix(CheckIndex(index, 2)), bigEndian ? Number.FlipEndian(value) : value);
            return this;
        }

        /// <summary>
        /// NOTE: This was getShort() in the JDK
        /// </summary>
        public override short GetInt16()
        {
            var littleEndian = _accessor.ReadInt16(Ix(NextGetIndex(2)));
            if (bigEndian)
            {
                return Number.FlipEndian(littleEndian);
            }
            return littleEndian;
        }

        /// <summary>
        /// NOTE: This was getShort() in the JDK
        /// </summary>
        public override short GetInt16(int index)
        {
            var littleEndian = _accessor.ReadInt16(Ix(CheckIndex(index, 2)));
            if (bigEndian)
            {
                return Number.FlipEndian(littleEndian);
            }
            return littleEndian;
        }

        /// <summary>
        /// NOTE: This was putShort() in the JDK
        /// </summary>
        public override ByteBuffer PutInt16(short value)
        {
            _accessor.Write(Ix(NextPutIndex(2)), bigEndian ? Number.FlipEndian(value) : value);
            return this;
        }

        /// <summary>
        /// NOTE: This was putShort() in the JDK
        /// </summary>
        public override ByteBuffer PutInt16(int index, short value)
        {
            _accessor.Write(Ix(CheckIndex(index, 2)), bigEndian ? Number.FlipEndian(value) : value);
            return this;
        }

        /// <summary>
        /// NOTE: This was getInt() in the JDK
        /// </summary>
        public override int GetInt32()
        {
            var littleEndian = _accessor.ReadInt32(Ix(NextGetIndex(4)));
            if (bigEndian)
            {
                return Number.FlipEndian(littleEndian);
            }
            return littleEndian;
        }

        /// <summary>
        /// NOTE: This was getInt() in the JDK
        /// </summary>
        public override int GetInt32(int index)
        {
            var littleEndian = _accessor.ReadInt32(Ix(CheckIndex(index, 4)));
            if (bigEndian)
            {
                return Number.FlipEndian(littleEndian);
            }
            return littleEndian;
        }

        /// <summary>
        /// NOTE: This was putInt() in the JDK
        /// </summary>
        public override ByteBuffer PutInt32(int value)
        {
            _accessor.Write(Ix(NextPutIndex(4)), bigEndian ? Number.FlipEndian(value) : value);
            return this;
        }


        /// <summary>
        /// NOTE: This was putInt() in the JDK
        /// </summary>
        public override ByteBuffer PutInt32(int index, int value)
        {
            _accessor.Write(Ix(CheckIndex(index, 4)), bigEndian ? Number.FlipEndian(value) : value);
            return this;
        }

        /// <summary>
        /// NOTE: This was getLong() in the JDK
        /// </summary>
        public override long GetInt64()
        {
            var littleEndian = _accessor.ReadInt64(Ix(NextGetIndex(8)));
            if (bigEndian)
            {
                return Number.FlipEndian(littleEndian);
            }
            return littleEndian;
        }

        /// <summary>
        /// NOTE: This was getLong() in the JDK
        /// </summary>
        public override long GetInt64(int index)
        {
            var littleEndian = _accessor.ReadInt64(Ix(CheckIndex(index, 8)));
            if (bigEndian)
            {
                return Number.FlipEndian(littleEndian);
            }
            return littleEndian;
        }

        /// <summary>
        /// NOTE: This was putLong() in the JDK
        /// </summary>
        public override ByteBuffer PutInt64(long value)
        {
            _accessor.Write(Ix(NextPutIndex(8)), bigEndian ? Number.FlipEndian(value) : value);
            return this;
        }

        /// <summary>
        /// NOTE: This was putLong() in the JDK
        /// </summary>
        public override ByteBuffer PutInt64(int index, long value)
        {
            _accessor.Write(Ix(CheckIndex(index, 8)), bigEndian ? Number.FlipEndian(value) : value);
            return this;
        }

        /// <summary>
        /// NOTE: This was getFloat() in the JDK
        /// </summary>
        public override float GetSingle()
        {
            byte[] temp = new byte[4];
            temp[0] = _accessor.ReadByte(Ix(NextGetIndex()));
            temp[1] = _accessor.ReadByte(Ix(NextGetIndex()));
            temp[2] = _accessor.ReadByte(Ix(NextGetIndex()));
            temp[3] = _accessor.ReadByte(Ix(NextGetIndex()));
            if (bigEndian)
            {
                System.Array.Reverse(temp);
            }
            return BitConverter.ToSingle(temp, 0);
        }

        /// <summary>
        /// NOTE: This was getFloat() in the JDK
        /// </summary>
        public override float GetSingle(int index)
        {
            byte[] temp = new byte[4];
            temp[0] = _accessor.ReadByte(Ix(NextGetIndex(index)));
            temp[1] = _accessor.ReadByte(Ix(NextGetIndex()));
            temp[2] = _accessor.ReadByte(Ix(NextGetIndex()));
            temp[3] = _accessor.ReadByte(Ix(NextGetIndex()));
            if (bigEndian)
            {
                System.Array.Reverse(temp);
            }
            return BitConverter.ToSingle(temp, 0);
        }

        /// <summary>
        /// NOTE: This was putFloat() in the JDK
        /// </summary>
        public override ByteBuffer PutSingle(float value)
        {
            var bytes = BitConverter.GetBytes(value);

            if (bigEndian)
            {
                System.Array.Reverse(bytes);
            }

            _accessor.Write(Ix(NextPutIndex()), bytes[0]);
            _accessor.Write(Ix(NextPutIndex()), bytes[1]);
            _accessor.Write(Ix(NextPutIndex()), bytes[2]);
            _accessor.Write(Ix(NextPutIndex()), bytes[3]);
            return this;
        }

        /// <summary>
        /// NOTE: This was putFloat() in the JDK
        /// </summary>
        public override ByteBuffer PutSingle(int index, float value)
        {
            var bytes = BitConverter.GetBytes(value);

            if (bigEndian)
            {
                System.Array.Reverse(bytes);
            }

            _accessor.Write(Ix(NextPutIndex(index)), bytes[0]);
            _accessor.Write(Ix(NextPutIndex()), bytes[1]);
            _accessor.Write(Ix(NextPutIndex()), bytes[2]);
            _accessor.Write(Ix(NextPutIndex()), bytes[3]);
            return this;
        }

        public override double GetDouble()
        {
            var littleEndian = _accessor.ReadDouble(Ix(NextGetIndex(8)));
            if (bigEndian)
            {
                return Number.FlipEndian(littleEndian);
            }
            return littleEndian;
        }

        public override double GetDouble(int index)
        {
            var littleEndian = _accessor.ReadDouble(Ix(CheckIndex(index, 8)));
            if (bigEndian)
            {
                return Number.FlipEndian(littleEndian);
            }
            return littleEndian;
        }

        public override ByteBuffer PutDouble(double value)
        {
            _accessor.Write(Ix(NextPutIndex(8)), bigEndian ? Number.FlipEndian(value) : value);
            return this;
        }

        public override ByteBuffer PutDouble(int index, double value)
        {
            _accessor.Write(Ix(CheckIndex(index, 8)), bigEndian ? Number.FlipEndian(value) : value);
            return this;
        }

        public void Dispose()
        {
            if (_accessor != null)
                _accessor.Dispose();

            _accessor = null;
        }

        /// <summary>
        /// NOTE: This was asLongBuffer() in the JDK
        /// </summary>
        public override Int64Buffer AsInt64Buffer()
        {
            throw new NotSupportedException();
        }
    }
}