using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Facet.Encoding
{
    public abstract class IntEncoderFilter : IntEncoder
    {
        protected readonly IntEncoder encoder;

        protected IntEncoderFilter(IntEncoder encoder)
        {
            this.encoder = encoder;
        }
    }
}
