using Lucene.Net.Index;
using Lucene.Net.Store;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Codecs
{
    public abstract class SegmentInfoReader
    {
        protected SegmentInfoReader()
        {
        }

        public abstract SegmentInfo Read(Directory directory, String segmentName, IOContext context);
    }
}
