using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Codecs.Lucene40
{
    [Obsolete]
    public class Lucene40FieldInfosFormat : FieldInfosFormat
    {
        private readonly FieldInfosReader reader = new Lucene40FieldInfosReader();

        public Lucene40FieldInfosFormat()
        {
        }

        public override FieldInfosReader FieldInfosReader
        {
            get { return reader; }
        }

        public override FieldInfosWriter FieldInfosWriter
        {
            get { throw new NotSupportedException("this codec can only be used for reading"); }
        }

        /** Extension of field infos */
        internal const String FIELD_INFOS_EXTENSION = "fnm";

        internal const String CODEC_NAME = "Lucene40FieldInfos";
        internal const int FORMAT_START = 0;
        internal const int FORMAT_CURRENT = FORMAT_START;

        internal const byte IS_INDEXED = 0x1;
        internal const byte STORE_TERMVECTOR = 0x2;
        internal const byte STORE_OFFSETS_IN_POSTINGS = 0x4;
        internal const byte OMIT_NORMS = 0x10;
        internal const byte STORE_PAYLOADS = 0x20;
        internal const byte OMIT_TERM_FREQ_AND_POSITIONS = 0x40;
        internal const byte OMIT_POSITIONS = (byte)(sbyte)-128;
    }
}
