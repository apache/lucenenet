using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Facet.Encoding
{
    public abstract class IntEncoder
    {
        public IntEncoder()
        {
        }

        public abstract void Encode(IntsRef values, BytesRef buf);

        public abstract IntDecoder CreateMatchingDecoder();
    }
}
