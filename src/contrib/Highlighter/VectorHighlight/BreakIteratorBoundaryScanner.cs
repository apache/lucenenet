using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Search.VectorHighlight
{
    public class BreakIteratorBoundaryScanner : IBoundaryScanner
    {
        readonly BreakIterator bi;

        public BreakIteratorBoundaryScanner(BreakIterator bi)
        {
            this.bi = bi;
        }

        public int FindStartOffset(StringBuilder buffer, int start)
        {
            if (start > buffer.Length || start < 1)
                return start;
            bi.Text = buffer.ToString().Substring(0, start);
            bi.Last();
            return bi.Previous();
        }

        public int FindEndOffset(StringBuilder buffer, int start)
        {
            if (start > buffer.Length || start < 0)
                return start;
            bi.Text = buffer.ToString().Substring(start);
            return bi.Next() + start;
        }
    }
}
