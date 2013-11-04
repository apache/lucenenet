using Lucene.Net.Codecs;
using Lucene.Net.Index;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Facet.Codecs.Facet42
{
    public sealed class Facet42DocValuesFormat : DocValuesFormat
    {
        public const string CODEC = @"FacetsDocValues";
        public const string EXTENSION = @"fdv";
        public const int VERSION_START = 0;
        public const int VERSION_CURRENT = VERSION_START;

        public Facet42DocValuesFormat()
            : base(@"Facet42")
        {
        }

        public override DocValuesConsumer FieldsConsumer(SegmentWriteState state)
        {
            return new Facet42DocValuesConsumer(state);
        }

        public override DocValuesProducer FieldsProducer(SegmentReadState state)
        {
            return new Facet42DocValuesProducer(state);
        }
    }
}
