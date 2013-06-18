using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Index
{
    public sealed class ReaderSlice
    {
        public static readonly ReaderSlice[] EMPTY_ARRAY = new ReaderSlice[0];

        public readonly int start;

        public readonly int length;

        public readonly int readerIndex;

        public ReaderSlice(int start, int length, int readerIndex)
        {
            this.start = start;
            this.length = length;
            this.readerIndex = readerIndex;
        }

        public override string ToString()
        {
            return "slice start=" + start + " length=" + length + " readerIndex=" + readerIndex;
        }
    }
}
