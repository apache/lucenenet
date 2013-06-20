using Lucene.Net.Codecs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Index
{
    internal abstract class DocValuesWriter
    {
        internal abstract void Abort();
        internal abstract void Finish(int numDoc);
        internal abstract void Flush(SegmentWriteState state, DocValuesConsumer consumer);
    }
}
