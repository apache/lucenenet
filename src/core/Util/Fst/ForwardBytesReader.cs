using System;

namespace Lucene.Net.Util.Fst
{
    public class ForwardBytesReader : FST.BytesReader
    {
        private readonly sbyte[] bytes;

        public ForwardBytesReader(sbyte[] bytes)
        {
            this.bytes = bytes;
        }

        public override long Position { get; set; }

        public override Boolean Reversed()
        {
            return false;
        }

        public override void SkipBytes(int count)
        {
            Position += count;
        }

        public override byte ReadByte()
        {
            return bytes[Position++];
        }

        public override void ReadBytes(byte[] bytes, int offset, int len)
        {
            Array.Copy(this.bytes, Position, bytes, offset, len);
            Position += len;
        }
    }
}
