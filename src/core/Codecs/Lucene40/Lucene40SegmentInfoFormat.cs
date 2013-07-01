using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Codecs.Lucene40
{
    public class Lucene40SegmentInfoFormat : SegmentInfoFormat
    {
        private readonly SegmentInfoReader reader = new Lucene40SegmentInfoReader();
        private readonly SegmentInfoWriter writer = new Lucene40SegmentInfoWriter();

        public Lucene40SegmentInfoFormat()
        {
        }

        public override SegmentInfoReader SegmentInfoReader
        {
            get { return reader; }
        }

        public override SegmentInfoWriter SegmentInfoWriter
        {
            get { return writer; }
        }

        public const string SI_EXTENSION = "si";
        internal const string CODEC_NAME = "Lucene40SegmentInfo";
        internal const int VERSION_START = 0;
        internal const int VERSION_CURRENT = VERSION_START;
    }
}
