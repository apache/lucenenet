using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lucene.Net.Index;

namespace Lucene.Net.Codecs
{
    public abstract class PostingsBaseFormat
    {
        public readonly string name;

        protected PostingsBaseFormat(String name)
        {
            this.name = name;
        }

        public abstract PostingsReaderBase PostingsReaderBase(SegmentReadState state);

        public abstract PostingsWriterBase PostingsWriterBase(SegmentWriteState state);
    }
}
