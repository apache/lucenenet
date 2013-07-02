using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lucene.Net.Index;
using Lucene.Net.Util.Packed;

namespace Lucene.Net.Codecs.Lucene42
{
    public sealed class Lucene42NormsFormat : NormsFormat
    {
        public Lucene42NormsFormat() { }

        public override DocValuesConsumer NormsConsumer(SegmentWriteState state)
        {
            // note: we choose FASTEST here (otherwise our norms are half as big but 15% slower than previous lucene)
            return new Lucene42DocValuesConsumer(state, DATA_CODEC, DATA_EXTENSION, METADATA_CODEC, METADATA_EXTENSION, PackedInts.FASTEST);
        }

        public override DocValuesProducer NormsProducer(SegmentReadState state)
        {
            return new Lucene42DocValuesProducer(state, DATA_CODEC, DATA_EXTENSION, METADATA_CODEC, METADATA_EXTENSION);
        }

        private const string DATA_CODEC = "Lucene41NormsData";
        private const string DATA_EXTENSION = "nvd";
        private const string METADATA_CODEC = "Lucene41NormsMetadata";
        private const string METADATA_EXTENSION = "nvm";
    }
}
