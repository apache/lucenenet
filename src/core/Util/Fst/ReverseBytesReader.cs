namespace Lucene.Net.Util.Fst
{
    public class ReverseBytesReader : FST.BytesReader
    {
        private readonly sbyte[] bytes;
        public override long Position { get; set; }

        public ReverseBytesReader(sbyte[] bytes)
        {
            this.bytes = bytes;
        }

        public override byte ReadByte()
        {
            return (byte)bytes[Position--];
        }

        public override void ReadBytes(byte[] b, int offset, int len)
        {
            for (var i = 0; i < len; i++)
            {
                b[offset + i] = (byte)bytes[Position--];
            }
        }

        public override void SkipBytes(int count)
        {
            Position -= count;
        }

        public override bool Reversed()
        {
            return true;
        }
    }
}
