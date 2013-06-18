using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lucene.Net.Index;
using Lucene.Net.Store;

namespace Lucene.Net.Codecs
{
    public abstract class StoredFieldsFormat
    {
        protected StoredFieldsFormat()
        {
        }

        public abstract StoredFieldsReader FieldsReader(Directory directory, SegmentInfo si, FieldInfos fn, IOContext context);

        public abstract StoredFieldsWriter FieldsWriter(Directory directory, SegmentInfo si, IOContext context);
    }
}
