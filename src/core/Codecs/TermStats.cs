using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Codecs
{
    public class TermStats
    {
        public readonly int docFreq;

        public readonly long totalTermFreq;

        public TermStats(int docFreq, long totalTermFreq)
        {
            this.docFreq = docFreq;
            this.totalTermFreq = totalTermFreq;
        }
    }
}
