using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Codecs
{
    public abstract class SegmentInfoFormat
    {
        protected SegmentInfoFormat()
        {
        }

        public abstract SegmentInfoReader SegmentInfoReader { get; }

        public abstract SegmentInfoWriter SegmentInfoWriter { get; }
    }
}
