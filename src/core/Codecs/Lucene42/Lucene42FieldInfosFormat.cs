using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Codecs.Lucene42
{
    public sealed class Lucene42FieldInfosFormat : FieldInfosFormat
    {
        private readonly FieldInfosReader reader = new Lucene42FieldInfosReader();
        private readonly FieldInfosWriter writer = new Lucene42FieldInfosWriter();

        public Lucene42FieldInfosFormat()
        {
        }

        public override FieldInfosReader FieldInfosReader
        {
            get { return reader; }
        }

        public override FieldInfosWriter FieldInfosWriter
        {
            get { return writer; }
        }

        /** Extension of field infos */
        internal const string EXTENSION = "fnm";

        // Codec header
        internal const string CODEC_NAME = "Lucene42FieldInfos";
        internal const int FORMAT_START = 0;
        internal const int FORMAT_CURRENT = FORMAT_START;

        // Field flags
        internal const sbyte IS_INDEXED = 0x1;
        internal const sbyte STORE_TERMVECTOR = 0x2;
        internal const sbyte STORE_OFFSETS_IN_POSTINGS = 0x4;
        internal const sbyte OMIT_NORMS = 0x10;
        internal const sbyte STORE_PAYLOADS = 0x20;
        internal const sbyte OMIT_TERM_FREQ_AND_POSITIONS = 0x40;
        internal const sbyte OMIT_POSITIONS = -128;
    }
}
