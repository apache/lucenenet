using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Facet.Encoding
{
    public abstract class IntDecoder
    {
        public abstract void Decode(BytesRef buf, IntsRef values);
    }
}
