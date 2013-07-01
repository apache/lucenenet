using Lucene.Net.Codecs.Compressing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Codecs.Lucene41
{
    public sealed class Lucene41StoredFieldsFormat : CompressingStoredFieldsFormat
    {
        public Lucene41StoredFieldsFormat()
            : base("Lucene41StoredFields", CompressionMode.FAST, 1 << 14)
        {
        }
    }
}
