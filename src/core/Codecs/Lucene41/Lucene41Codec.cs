using Lucene.Net.Codecs.Compressing;
using Lucene.Net.Codecs.Lucene40;
using Lucene.Net.Codecs.PerField;
using Lucene.Net.Index;
using Lucene.Net.Store;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Codecs.Lucene41
{
    [Obsolete]
    public class Lucene41Codec : Codec
    {
        // TODO: slightly evil
        private readonly StoredFieldsFormat fieldsFormat = new AnonymousCompressingStoredFieldsFormat("Lucene41StoredFields", CompressionMode.FAST, 1 << 14);

        private sealed class AnonymousCompressingStoredFieldsFormat : CompressingStoredFieldsFormat
        {
            public AnonymousCompressingStoredFieldsFormat(string formatName, CompressionMode compressionMode, int chunkSize)
                : base(formatName, compressionMode, chunkSize)
            {
            }

            public override StoredFieldsWriter FieldsWriter(Directory directory, SegmentInfo si, IOContext context)
            {
                throw new NotSupportedException("this codec can only be used for reading");
            }
        }

        private readonly TermVectorsFormat vectorsFormat = new Lucene40TermVectorsFormat();
        private readonly FieldInfosFormat fieldInfosFormat = new Lucene40FieldInfosFormat();
        private readonly SegmentInfoFormat infosFormat = new Lucene40SegmentInfoFormat();
        private readonly LiveDocsFormat liveDocsFormat = new Lucene40LiveDocsFormat();

        private readonly PostingsFormat postingsFormat; // = new AnonymousPerFieldPostingsFormat(this)

        private sealed class AnonymousPerFieldPostingsFormat : PerFieldPostingsFormat
        {
            private readonly Lucene41Codec parent;

            public AnonymousPerFieldPostingsFormat(Lucene41Codec parent)
            {
                this.parent = parent;
            }

            public override PostingsFormat GetPostingsFormatForField(string field)
            {
                return parent.GetPostingsFormatForField(field);
            }
        }

        public Lucene41Codec()
            : base("Lucene41")
        {
            // .NET Port: can't inline this above due to use of "this"
            postingsFormat = new AnonymousPerFieldPostingsFormat(this);
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

        public override DocValuesFormat DocValuesFormat
        {
            get { return dvFormat; }
        }

        private readonly PostingsFormat defaultFormat = PostingsFormat.ForName("Lucene41");
        private readonly DocValuesFormat dvFormat = new Lucene40DocValuesFormat();
        private readonly NormsFormat normsFormat = new Lucene40NormsFormat();

        public override NormsFormat NormsFormat
        {
            get { return normsFormat; }
        }
    }
}
