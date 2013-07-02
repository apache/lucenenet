using System;
using Lucene.Net.Util;

namespace Lucene.Net.Search
{
    public class TermStatistics
    {
        private readonly BytesRef term;
        private readonly long docFreq;
        private readonly long totalTermFreq;

        public TermStatistics(BytesRef term, long docFreq, long totalTermFreq)
        {
            if (docFreq < 0) throw new ArgumentException("docFreq must be >= 0");
            if (!(totalTermFreq == -1 || totalTermFreq >= docFreq))
                throw new ArgumentException("totalTermFreq must equal -1 or be >= docFreq");

            this.term = term;
            this.docFreq = docFreq;
            this.totalTermFreq = totalTermFreq;
        }

        public BytesRef Term
        {
            get { return term; }
        }

        public long DocFreq
        {
            get { return docFreq; }
        }

        public long TotalTermFreq
        {
            get { return totalTermFreq; }
        }
    }
}
