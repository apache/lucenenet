using Lucene.Net.Index;
using Lucene.Net.Store;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Codecs.Lucene3x
{
    [Obsolete]
    internal class Lucene3xStoredFieldsFormat : StoredFieldsFormat
    {
        public override StoredFieldsReader FieldsReader(Directory directory, SegmentInfo si, FieldInfos fn, IOContext context)
        {
            return new Lucene3xStoredFieldsReader(directory, si, fn, context);
        }

        public override StoredFieldsWriter FieldsWriter(Directory directory, SegmentInfo si, IOContext context)
        {
            throw new NotSupportedException("this codec can only be used for reading");
        }
    }
}
