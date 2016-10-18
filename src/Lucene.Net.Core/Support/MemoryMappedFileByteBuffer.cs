using System;
using System.IO.MemoryMappedFiles;

namespace Lucene.Net.Support
{
    internal sealed class MemoryMappedFileByteBuffer : ByteBuffer, IDisposable
    {
        private MemoryMappedViewAccessor _accessor;

        public MemoryMappedFileByteBuffer(MemoryMappedViewAccessor accessor, int mark, int pos, int lim, int cap)
            : base(mark, pos, lim, cap)
        {
            _accessor = accessor;
        }

        internal override byte _get(int i)
        {
            throw new NotImplementedException();
        }

        internal override void _put(int i, byte b)
        {
            throw new NotImplementedException();
        }

        public override bool IsDirect
        {
            get { return true; }
        }

        public override bool IsReadOnly
        {
            get { return false; }
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

        public override byte Get()
        {
            return _accessor.ReadByte(Position++);
        }

        public override ByteBuffer Put(byte b)
        {
            _accessor.Write(Position++, b);
            return this;
        }

        public override byte Get(int index)
        {
            return _accessor.ReadByte(index);
        }

        public override ByteBuffer Put(int index, byte b)
        {
            _accessor.Write(index, b);
            return this;
        }

        public override ByteBuffer Compact()
        {
            throw new NotSupportedException();
        }

        public override char GetChar()
        {
            char c = _accessor.ReadChar(Position);
            Position += 2;

            //conform to how the index was written
            return Number.FlipEndian(c);
        }

        public override ByteBuffer PutChar(char value)
        {
            //conform to how the index was written
            _accessor.Write(Position, Number.FlipEndian(value));
            Position += 2;

            return this;
        }

        public override char GetChar(int index)
        {
            var c = _accessor.ReadChar(index);

            //conform to how the index was written
            return Number.FlipEndian(c);
        }

        public override ByteBuffer PutChar(int index, char value)
        {
            _accessor.Write(index, Number.FlipEndian(value));

            return this;
        }

        public override short GetShort()
        {
            var c = _accessor.ReadInt16(Position);
            Position += 2;

            //conform to how the index was written
            return Number.FlipEndian(c);
        }

        public override ByteBuffer PutShort(short value)
        {
            //conform to how the index was written
            _accessor.Write(Position, Number.FlipEndian(value));
            Position += 2;

            return this;
        }

        public override short GetShort(int index)
        {
            var c = _accessor.ReadInt16(index);

            //conform to how the index was written
            return Number.FlipEndian(c);
        }

        public override ByteBuffer PutShort(int index, short value)
        {
            //conform to how the index was written
            _accessor.Write(index, Number.FlipEndian(value));

            return this;
        }

        public override int GetInt()
        {
            var c = _accessor.ReadInt32(Position);
            Position += 4;

            //conform to how the index was written
            return Number.FlipEndian(c);
        }

        public override ByteBuffer PutInt(int value)
        {
            //conform to how the index was written
            _accessor.Write(Position, Number.FlipEndian(value));
            Position += 4;

            return this;
        }

        public override int GetInt(int index)
        {
            var c = _accessor.ReadInt32(index);

            //conform to how the index was written
            return Number.FlipEndian(c);
        }

        public override ByteBuffer PutInt(int index, int value)
        {
            //conform to how the index was written
            _accessor.Write(index, Number.FlipEndian(value));

            return this;
        }

        public override long GetLong()
        {
            var c = _accessor.ReadInt64(Position);
            Position += 8;

            //conform to how the index was written
            return Number.FlipEndian(c);
        }

        public override ByteBuffer PutLong(long value)
        {
            //conform to how the index was written
            _accessor.Write(Position, Number.FlipEndian(value));
            Position += 8;

            return this;
        }

        public override long GetLong(int index)
        {
            var c = _accessor.ReadInt64(index);

            //conform to how the index was written
            return Number.FlipEndian(c);
        }

        public override ByteBuffer PutLong(int index, long value)
        {
            //conform to how the index was written
            _accessor.Write(index, Number.FlipEndian(value));

            return this;
        }

        public override float GetFloat()
        {
            var c = _accessor.ReadSingle(Position);
            Position += 4;

            //conform to how the index was written
            return Number.FlipEndian(c);
        }

        public override ByteBuffer PutFloat(float value)
        {
            //conform to how the index was written
            _accessor.Write(Position, Number.FlipEndian(value));
            Position += 4;

            return this;
        }

        public override float GetFloat(int index)
        {
            var c = _accessor.ReadSingle(index);

            //conform to how the index was written
            return Number.FlipEndian(c);
        }

        public override ByteBuffer PutFloat(int index, float value)
        {
            //conform to how the index was written
            _accessor.Write(index, Number.FlipEndian(value));

            return this;
        }

        public override double GetDouble()
        {
            var c = _accessor.ReadDouble(Position);
            Position += 4;

            //conform to how the index was written
            return Number.FlipEndian(c);
        }

        public override ByteBuffer PutDouble(double value)
        {
            //conform to how the index was written
            _accessor.Write(Position, Number.FlipEndian(value));
            Position += 8;

            return this;
        }

        public override double GetDouble(int index)
        {
            var c = _accessor.ReadDouble(index);

            //conform to how the index was written
            return Number.FlipEndian(c);
        }

        public override ByteBuffer PutDouble(int index, double value)
        {
            //conform to how the index was written
            _accessor.Write(index, Number.FlipEndian(value));

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
            throw new NotImplementedException();
        }
    }
}