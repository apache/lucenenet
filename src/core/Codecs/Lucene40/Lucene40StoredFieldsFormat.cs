using Lucene.Net.Index;
using Lucene.Net.Store;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Codecs.Lucene40
{
    public class Lucene40StoredFieldsFormat : StoredFieldsFormat
    {
        public Lucene40StoredFieldsFormat()
        {
        }

        public override StoredFieldsReader FieldsReader(Directory directory, SegmentInfo si, FieldInfos fn, IOContext context)
        {
            return new Lucene40StoredFieldsReader(directory, si, fn, context);
        }

        public override StoredFieldsWriter FieldsWriter(Directory directory, SegmentInfo si, IOContext context)
        {
            return new Lucene40StoredFieldsWriter(directory, si.name, context);
        }
    }
}
