using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lucene.Net.Codecs.Lucene40;
using Lucene.Net.Codecs.Lucene41;
using Lucene.Net.Codecs.PerField;

namespace Lucene.Net.Codecs.Lucene42
{
    public class Lucene42Codec : Codec
    {
        private readonly StoredFieldsFormat fieldsFormat = new Lucene41StoredFieldsFormat();
        private readonly TermVectorsFormat vectorsFormat = new Lucene42TermVectorsFormat();
        private readonly FieldInfosFormat fieldInfosFormat = new Lucene42FieldInfosFormat();
        private readonly SegmentInfoFormat infosFormat = new Lucene40SegmentInfoFormat();
        private readonly LiveDocsFormat liveDocsFormat = new Lucene40LiveDocsFormat();

        private readonly PostingsFormat postingsFormat;

        private sealed class AnonymousPerFieldPostingsFormat : PerFieldPostingsFormat
        {
            private readonly Lucene42Codec parent;

            public AnonymousPerFieldPostingsFormat(Lucene42Codec parent)
            {
                this.parent = parent;
            }

            public override PostingsFormat GetPostingsFormatForField(string field)
            {
                return parent.GetPostingsFormatForField(field);
            }
        }

        private readonly DocValuesFormat docValuesFormat;

        private sealed class AnonymousPerFieldDocValuesFormat : PerFieldDocValuesFormat
        {
            private readonly Lucene42Codec parent;

            public AnonymousPerFieldDocValuesFormat(Lucene42Codec parent)
            {
                this.parent = parent;
            }

            public override DocValuesFormat GetDocValuesFormatForField(string field)
            {
                return parent.GetDocValuesFormatForField(field);
            }
        }

        public Lucene42Codec()
            : base("Lucene42")
        {
            // .NET Port: we must do the initialization here since we can't use "this" inline:
            postingsFormat = new AnonymousPerFieldPostingsFormat(this);
            docValuesFormat = new AnonymousPerFieldDocValuesFormat(this);
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

        public override LiveDocsFormat LiveDocsFormat
        {
            get { return liveDocsFormat; }
        }

        public PostingsFormat GetPostingsFormatForField(String field)
        {
            return defaultFormat;
        }

        public DocValuesFormat GetDocValuesFormatForField(String field)
        {
            return defaultDVFormat;
        }

        public override DocValuesFormat DocValuesFormat
        {
            get { return docValuesFormat; }
        }

        private readonly PostingsFormat defaultFormat = PostingsFormat.ForName("Lucene41");
        private readonly DocValuesFormat defaultDVFormat = DocValuesFormat.ForName("Lucene42");

        private readonly NormsFormat normsFormat = new Lucene42NormsFormat();

        public override NormsFormat NormsFormat
        {
            get { return normsFormat; }
        }
    }
}
