using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lucene.Net.Index;

namespace Lucene.Net.Codecs
{
    public abstract class NormsFormat
    {
        protected NormsFormat()
        {
        }

        public abstract DocValuesConsumer NormsConsumer(SegmentWriteState state);

        public abstract DocValuesProducer NormsProducer(SegmentReadState state);
    }
}
