using Lucene.Net.Index;
using Lucene.Net.Store;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Codecs.Lucene40
{
    public class Lucene40TermVectorsFormat : TermVectorsFormat
    {
        public Lucene40TermVectorsFormat()
        {
        }

        public override TermVectorsReader VectorsReader(Directory directory, SegmentInfo segmentInfo, FieldInfos fieldInfos, IOContext context)
        {
            return new Lucene40TermVectorsReader(directory, segmentInfo, fieldInfos, context);
        }

        public override TermVectorsWriter VectorsWriter(Directory directory, SegmentInfo segmentInfo, IOContext context)
        {
            return new Lucene40TermVectorsWriter(directory, segmentInfo.name, context);
        }
    }
}
