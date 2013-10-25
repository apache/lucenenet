using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Search.Function.ValueSources
{
    public abstract class MultiValueSource : ValueSource
    {
        public abstract int Dimension { get; }
    }
}
