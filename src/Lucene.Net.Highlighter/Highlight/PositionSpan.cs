using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Lucene.Net.Search.Highlight
{
    /// <summary>
    /// Utility class to record Positions Spans
    /// </summary>
    public class PositionSpan
    {
        public int Start { get; set; }
        public int End { get; set; }

        public PositionSpan(int start, int end)
        {
            this.Start = start;
            this.End = end;
        }
    }
}
