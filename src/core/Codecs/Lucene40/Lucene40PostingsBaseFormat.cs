using Lucene.Net.Index;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Codecs.Lucene40
{
    [Obsolete]
    public sealed class Lucene40PostingsBaseFormat : PostingsBaseFormat
    {
        public Lucene40PostingsBaseFormat()
            : base("Lucene40")
        {
        }

        public override PostingsReaderBase PostingsReaderBase(SegmentReadState state)
        {
            return new Lucene40PostingsReader(state.directory, state.fieldInfos, state.segmentInfo, state.context, state.segmentSuffix);
        }

        public override PostingsWriterBase PostingsWriterBase(SegmentWriteState state)
        {
            throw new NotSupportedException("this codec can only be used for reading");
        }
    }
}
