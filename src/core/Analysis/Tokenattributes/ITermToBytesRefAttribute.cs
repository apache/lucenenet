using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Analysis.Tokenattributes
{
    public interface ITermToBytesRefAttribute : IAttribute
    {
        int FillBytesRef();

        BytesRef BytesRef { get; }
    }
}
