using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Codecs
{
    public abstract class FilterCodec : Codec
    {
        protected readonly Codec delegated;

        protected FilterCodec(string name, Codec delegated)
            : base(name)
        {
            this.delegated = delegated;
        }

        public override DocValuesFormat DocValuesFormat()
        {
            return delegated.DocValuesFormat();
        }

        public override FieldInfosFormat FieldInfosFormat()
        {
            return delegated.FieldInfosFormat();
        }

        public override LiveDocsFormat LiveDocsFormat()
        {
            return delegated.LiveDocsFormat();
        }

        public override NormsFormat NormsFormat()
        {
            return delegated.NormsFormat();
        }

        public override PostingsFormat PostingsFormat()
        {
            return delegated.PostingsFormat();
        }

        public override SegmentInfoFormat SegmentInfoFormat()
        {
            return delegated.SegmentInfoFormat();
        }

        public override StoredFieldsFormat StoredFieldsFormat()
        {
            return delegated.StoredFieldsFormat();
        }

        public override TermVectorsFormat TermVectorsFormat()
        {
            return delegated.TermVectorsFormat();
        }
    }
}
