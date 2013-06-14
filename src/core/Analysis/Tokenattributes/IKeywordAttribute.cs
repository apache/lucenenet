using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Analysis.Tokenattributes
{
    public interface IKeywordAttribute : IAttribute
    {
        bool IsKeyword { get; set; }
    }
}
