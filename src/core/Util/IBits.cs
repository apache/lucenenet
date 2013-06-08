using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Util
{
    public interface IBits
    {
        bool this[int index] { get; }

        int Length { get; }
    }
}
