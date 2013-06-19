using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lucene.Net.Util;

namespace Lucene.Net.Index
{
    internal sealed class BitsSlice : IBits
    {
        private readonly IBits parent;
        private readonly int start;
        private readonly int length;

        // start is inclusive; end is exclusive (length = end-start)
        public BitsSlice(IBits parent, ReaderSlice slice)
        {
            this.parent = parent;
            this.start = slice.start;
            this.length = slice.length;
            //assert length >= 0: "length=" + length;
        }

        public bool this[int doc]
        {
            get
            {
                if (doc >= length)
                {
                    throw new InvalidOperationException("doc " + doc + " is out of bounds 0 .. " + (length - 1));
                }
                //assert doc < length: "doc=" + doc + " length=" + length;
                return parent[doc + start];
            }
        }

        public int Length
        {
            get { return length; }
        }
    }
}
