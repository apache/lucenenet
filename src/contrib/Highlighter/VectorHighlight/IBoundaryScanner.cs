using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Search.VectorHighlight
{
    public interface IBoundaryScanner
    {
        int FindStartOffset(StringBuilder buffer, int start);
        int FindEndOffset(StringBuilder buffer, int start);
    }
}
