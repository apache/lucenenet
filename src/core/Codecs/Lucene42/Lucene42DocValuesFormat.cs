using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lucene.Net.Index;
using Lucene.Net.Util.Packed;

namespace Lucene.Net.Codecs.Lucene42
{
    public sealed class Lucene42DocValuesFormat : DocValuesFormat
    {
        public Lucene42DocValuesFormat()
            : base("Lucene42")
        {
        }

        public override DocValuesConsumer FieldsConsumer(SegmentWriteState state)
        {
            // note: we choose DEFAULT here (its reasonably fast, and for small bpv has tiny waste)
            return new Lucene42DocValuesConsumer(state, DATA_CODEC, DATA_EXTENSION, METADATA_CODEC, METADATA_EXTENSION, PackedInts.DEFAULT);
        }

        public override DocValuesProducer FieldsProducer(SegmentReadState state)
        {
            return new Lucene42DocValuesProducer(state, DATA_CODEC, DATA_EXTENSION, METADATA_CODEC, METADATA_EXTENSION);
        }

        private const string DATA_CODEC = "Lucene42DocValuesData";
        private const string DATA_EXTENSION = "dvd";
        private const string METADATA_CODEC = "Lucene42DocValuesMetadata";
        private const string METADATA_EXTENSION = "dvm";
    }
}
