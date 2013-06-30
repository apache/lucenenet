using Lucene.Net.Index;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Codecs.Lucene41
{
    public sealed class Lucene41PostingsBaseFormat : PostingsBaseFormat
    {
        public Lucene41PostingsBaseFormat()
            : base("Lucene41")
        {
        }

        public override PostingsReaderBase PostingsReaderBase(SegmentReadState state)
        {
            return new Lucene41PostingsReader(state.directory, state.fieldInfos, state.segmentInfo, state.context, state.segmentSuffix);
        }

        public override PostingsWriterBase PostingsWriterBase(SegmentWriteState state)
        {
            return new Lucene41PostingsWriter(state);
        }
    }
}
