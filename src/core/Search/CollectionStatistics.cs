using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Search
{
    public class CollectionStatistics
    {
        private readonly string field;
        private readonly long maxDoc;
        private readonly long docCount;
        private readonly long sumTotalTermFreq;
        private readonly long sumDocFreq;

        public CollectionStatistics(string field, long maxDoc, long docCount, long sumTotalTermFreq, long sumDocFreq)
        {
            //assert maxDoc >= 0;
            //assert docCount >= -1 && docCount <= maxDoc; // #docs with field must be <= #docs
            //assert sumDocFreq == -1 || sumDocFreq >= docCount; // #postings must be >= #docs with field
            //assert sumTotalTermFreq == -1 || sumTotalTermFreq >= sumDocFreq; // #positions must be >= #postings
            this.field = field;
            this.maxDoc = maxDoc;
            this.docCount = docCount;
            this.sumTotalTermFreq = sumTotalTermFreq;
            this.sumDocFreq = sumDocFreq;
        }

        public string Field
        {
            get { return field; }
        }

        public long MaxDoc
        {
            get { return maxDoc; }
        }

        public long DocCount
        {
            get { return docCount; }
        }

        public long SumTotalTermFreq
        {
            get { return sumTotalTermFreq; }
        }

        public long SumDocFreq
        {
            get { return sumDocFreq; }
        }
    }
}
