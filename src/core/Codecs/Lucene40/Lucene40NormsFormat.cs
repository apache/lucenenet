using Lucene.Net.Index;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Codecs.Lucene40
{
    [Obsolete]
    public class Lucene40NormsFormat : NormsFormat
    {
        public Lucene40NormsFormat() { }

        public override DocValuesConsumer NormsConsumer(SegmentWriteState state)
        {
            throw new NotSupportedException("this codec can only be used for reading");
        }

        public override DocValuesProducer NormsProducer(SegmentReadState state)
        {
            String filename = IndexFileNames.SegmentFileName(state.segmentInfo.name,
                                                     "nrm",
                                                     IndexFileNames.COMPOUND_FILE_EXTENSION);
            return new Lucene40DocValuesReader(state, filename, Lucene40FieldInfosReader.LEGACY_NORM_TYPE_KEY);
        }
    }
}
