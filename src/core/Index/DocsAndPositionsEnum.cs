using Lucene.Net.Util;

namespace Lucene.Net.Index
{
    public abstract class DocsAndPositionsEnum : DocsEnum
    {
        public const int FLAG_OFFSETS = 0x1;

        public const int FLAG_PAYLOADS = 0x2;

        protected DocsAndPositionsEnum()
        {
        }

        public abstract int NextPosition();

        public abstract int StartOffset { get; }

        public abstract int EndOffset { get; }

        public abstract BytesRef Payload { get; }
    }
}
