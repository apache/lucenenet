using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lucene.Net.Index;
using Lucene.Net.Store;

namespace Lucene.Net.Codecs
{
    public abstract class TermVectorsFormat
    {
        protected TermVectorsFormat()
        {
        }

        public abstract TermVectorsReader VectorsReader(Directory directory, SegmentInfo segmentInfo, FieldInfos fieldInfos, IOContext context);

        public abstract TermVectorsWriter VectorsWriter(Directory directory, SegmentInfo segmentInfo, IOContext context);
    }
}
