using Lucene.Net.Index;
using Lucene.Net.Store;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Codecs
{
    public abstract class SegmentInfoWriter
    {
        protected SegmentInfoWriter()
        {
        }

        public abstract void Write(Directory dir, SegmentInfo info, FieldInfos fis, IOContext ioContext);
    }
}
