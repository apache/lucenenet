using System;
using System.IO.MemoryMappedFiles;

namespace Lucene.Net.Support
{
    internal sealed class MemoryMappedFileByteBuffer : ByteBuffer, IDisposable
    {
        private MemoryMappedViewAccessor _accessor;
        private readonly int offset; // always 0 (add constructors to fix this)
        new private bool bigEndian = true;

        public MemoryMappedFileByteBuffer(MemoryMappedViewAccessor accessor, int mark, int pos, int lim, int cap)
            : base(mark, pos, lim, cap)
        {
            _accessor = accessor;
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

        public override short GetShort()
        {
            var littleEndian = _accessor.ReadInt16(Ix(NextGetIndex(2)));
            if (bigEndian)
            {
                return Number.FlipEndian(littleEndian);
            }
            return littleEndian;
        }

        public override short GetShort(int index)
        {
            var littleEndian = _accessor.ReadInt16(Ix(CheckIndex(index, 2)));
            if (bigEndian)
            {
                return Number.FlipEndian(littleEndian);
            }
            return littleEndian;
        }

        public override ByteBuffer PutShort(short value)
        {
            _accessor.Write(Ix(NextPutIndex(2)), bigEndian ? Number.FlipEndian(value) : value);
            return this;
        }

        public override ByteBuffer PutShort(int index, short value)
        {
            _accessor.Write(Ix(CheckIndex(index, 2)), bigEndian ? Number.FlipEndian(value) : value);
            return this;
        }

        public override int GetInt()
        {
            var littleEndian = _accessor.ReadInt32(Ix(NextGetIndex(4)));
            if (bigEndian)
            {
                return Number.FlipEndian(littleEndian);
            }
            return littleEndian;
        }

        public override int GetInt(int index)
        {
            var littleEndian = _accessor.ReadInt32(Ix(CheckIndex(index, 4)));
            if (bigEndian)
            {
                return Number.FlipEndian(littleEndian);
            }
            return littleEndian;
        }

        public override ByteBuffer PutInt(int value)
        {
            _accessor.Write(Ix(NextPutIndex(4)), bigEndian ? Number.FlipEndian(value) : value);
            return this;
        }

        

        public override ByteBuffer PutInt(int index, int value)
        {
            _accessor.Write(Ix(CheckIndex(index, 4)), bigEndian ? Number.FlipEndian(value) : value);
            return this;
        }

        public override long GetLong()
        {
            var littleEndian = _accessor.ReadInt64(Ix(NextGetIndex(8)));
            if (bigEndian)
            {
                return Number.FlipEndian(littleEndian);
            }
            return littleEndian;
        }

        public override long GetLong(int index)
        {
            var littleEndian = _accessor.ReadInt64(Ix(CheckIndex(index, 8)));
            if (bigEndian)
            {
                return Number.FlipEndian(littleEndian);
            }
            return littleEndian;
        }

        public override ByteBuffer PutLong(long value)
        {
            _accessor.Write(Ix(NextPutIndex(8)), bigEndian ? Number.FlipEndian(value) : value);
            return this;
        }

        public override ByteBuffer PutLong(int index, long value)
        {
            _accessor.Write(Ix(CheckIndex(index, 8)), bigEndian ? Number.FlipEndian(value) : value);
            return this;
        }

        public override float GetFloat()
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

        public override float GetFloat(int index)
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

        public override ByteBuffer PutFloat(float value)
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

        public override ByteBuffer PutFloat(int index, float value)
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

        public override LongBuffer AsLongBuffer()
        {
            throw new NotSupportedException();
        }
    }
}