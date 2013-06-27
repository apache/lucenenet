using Lucene.Net.Index;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Codecs.Lucene3x
{
    [Obsolete]
    internal class Lucene3xNormsFormat : NormsFormat
    {
        public override DocValuesConsumer NormsConsumer(SegmentWriteState state)
        {
            throw new NotSupportedException("this codec can only be used for reading");
        }

        public override DocValuesProducer NormsProducer(SegmentReadState state)
        {
            return new Lucene3xNormsProducer(state.directory, state.segmentInfo, state.fieldInfos, state.context);
        }
    }
}
