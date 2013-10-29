using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Search.VectorHighlight
{
    public class SimpleBoundaryScanner : IBoundaryScanner
    {
        public static readonly int DEFAULT_MAX_SCAN = 20;
        public static readonly char[] DEFAULT_BOUNDARY_CHARS = { '.', ',', '!', '?', ' ', '\t', '\n' };
        protected int maxScan;
        protected ISet<char> boundaryChars;

        public SimpleBoundaryScanner()
            : this(DEFAULT_MAX_SCAN, DEFAULT_BOUNDARY_CHARS)
        {
        }

        public SimpleBoundaryScanner(int maxScan)
            : this(maxScan, DEFAULT_BOUNDARY_CHARS)
        {
        }

        public SimpleBoundaryScanner(char[] boundaryChars)
            : this(DEFAULT_MAX_SCAN, boundaryChars)
        {
        }

        public SimpleBoundaryScanner(int maxScan, char[] boundaryChars)
        {
            this.maxScan = maxScan;
            this.boundaryChars = new HashSet<char>();
            this.boundaryChars.UnionWith(boundaryChars);
        }

        public SimpleBoundaryScanner(int maxScan, ISet<char> boundaryChars)
        {
            this.maxScan = maxScan;
            this.boundaryChars = boundaryChars;
        }

        public int FindStartOffset(StringBuilder buffer, int start)
        {
            if (start > buffer.Length || start < 1)
                return start;
            int offset, count = maxScan;
            for (offset = start; offset > 0 && count > 0; count--)
            {
                if (boundaryChars.Contains(buffer[offset - 1]))
                    return offset;
                offset--;
            }

            if (offset == 0)
            {
                return 0;
            }

            return start;
        }

        public int FindEndOffset(StringBuilder buffer, int start)
        {
            if (start > buffer.Length || start < 0)
                return start;
            int offset, count = maxScan;
            for (offset = start; offset < buffer.Length && count > 0; count--)
            {
                if (boundaryChars.Contains(buffer[offset]))
                    return offset;
                offset++;
            }

            return start;
        }
    }
}
