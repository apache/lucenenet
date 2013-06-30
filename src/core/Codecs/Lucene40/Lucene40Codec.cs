using Lucene.Net.Codecs.PerField;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Codecs.Lucene40
{
    [Obsolete]
    public class Lucene40Codec : Codec
    {
        private readonly StoredFieldsFormat fieldsFormat = new Lucene40StoredFieldsFormat();
        private readonly TermVectorsFormat vectorsFormat = new Lucene40TermVectorsFormat();
        private readonly FieldInfosFormat fieldInfosFormat = new Lucene40FieldInfosFormat();
        private readonly SegmentInfoFormat infosFormat = new Lucene40SegmentInfoFormat();
        private readonly LiveDocsFormat liveDocsFormat = new Lucene40LiveDocsFormat();

        private readonly PostingsFormat postingsFormat; // = new AnonymousLucene40PerFieldPostingsFormat();

        private sealed class AnonymousLucene40PerFieldPostingsFormat : PerFieldPostingsFormat
        {
            private readonly Lucene40Codec parent;

            public AnonymousLucene40PerFieldPostingsFormat(Lucene40Codec parent)
            {
                this.parent = parent;
            }

            public override PostingsFormat GetPostingsFormatForField(string field)
            {
                return parent.GetPostingsFormatForField(field);
            }
        }

        public Lucene40Codec()
            : base("Lucene40")
        {
            // .NET Port: can't use "this" inline, so we must do it here
            postingsFormat = new AnonymousLucene40PerFieldPostingsFormat(this);
        }

        public override StoredFieldsFormat StoredFieldsFormat
        {
            get { return fieldsFormat; }
        }

        public override TermVectorsFormat TermVectorsFormat
        {
            get { return vectorsFormat; }
        }

        public override PostingsFormat PostingsFormat
        {
            get { return postingsFormat; }
        }

        public override FieldInfosFormat FieldInfosFormat
        {
            get { return fieldInfosFormat; }
        }

        public override SegmentInfoFormat SegmentInfoFormat
        {
            get { return infosFormat; }
        }

        private readonly DocValuesFormat defaultDVFormat = new Lucene40DocValuesFormat();

        public override DocValuesFormat DocValuesFormat
        {
            get { return defaultDVFormat; }
        }

        private readonly NormsFormat normsFormat = new Lucene40NormsFormat();

        public override NormsFormat NormsFormat
        {
            get { return normsFormat; }
        }

        public override LiveDocsFormat LiveDocsFormat
        {
            get { return liveDocsFormat; }
        }

        public PostingsFormat GetPostingsFormatForField(string field)
        {
            return defaultFormat;
        }

        private readonly PostingsFormat defaultFormat = PostingsFormat.ForName("Lucene40");
    }
}
