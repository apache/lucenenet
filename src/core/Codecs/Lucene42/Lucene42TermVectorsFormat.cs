using Lucene.Net.Codecs.Compressing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Codecs.Lucene42
{
    public sealed class Lucene42TermVectorsFormat : CompressingTermVectorsFormat
    {        
        public Lucene42TermVectorsFormat()
            : base("Lucene41StoredFields", "", CompressionMode.FAST, 1 << 12)
        {            
        }
    }
}
