using Lucene.Net.Index;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Facet.Search
{
    public interface IAggregator
    {
        bool SetNextReader(AtomicReaderContext context);
        void Aggregate(int docID, float score, IntsRef ordinals);
    }
}
