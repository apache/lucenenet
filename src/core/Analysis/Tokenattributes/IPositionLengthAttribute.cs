using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lucene.Net.Util;
using Attribute = Lucene.Net.Util.Attribute;

namespace Lucene.Net.Analysis.Tokenattributes
{
    public interface IPositionLengthAttribute : IAttribute
    {
        int PositionLength { get; set; }
    }
}
