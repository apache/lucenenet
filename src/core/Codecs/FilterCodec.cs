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

        public override DocValuesFormat DocValuesFormat
        {
            get
            {
                return delegated.DocValuesFormat;
            }
        }

        public override FieldInfosFormat FieldInfosFormat
        {
            get
            {
                return delegated.FieldInfosFormat;
            }
        }

        public override LiveDocsFormat LiveDocsFormat
        {
            get
            {
                return delegated.LiveDocsFormat;
            }
        }

        public override NormsFormat NormsFormat
        {
            get
            {
                return delegated.NormsFormat;
            }
        }

        public override PostingsFormat PostingsFormat
        {
            get
            {
                return delegated.PostingsFormat;
            }
        }

        public override SegmentInfoFormat SegmentInfoFormat
        {
            get
            {
                return delegated.SegmentInfoFormat;
            }
        }

        public override StoredFieldsFormat StoredFieldsFormat
        {
            get
            {
                return delegated.StoredFieldsFormat;
            }
        }

        public override TermVectorsFormat TermVectorsFormat
        {
            get
            {
                return delegated.TermVectorsFormat;
            }
        }
    }
}
