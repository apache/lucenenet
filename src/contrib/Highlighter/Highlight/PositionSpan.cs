using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Search.Highlight
{
    public class PositionSpan
    {
        internal int start;
        internal int end;

        public PositionSpan(int start, int end)
        {
            this.start = start;
            this.end = end;
        }
    }
}
