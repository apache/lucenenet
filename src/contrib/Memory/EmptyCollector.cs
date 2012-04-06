using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lucene.Net.Search;

namespace Lucene.Net.Index.Memory
{
    public partial class MemoryIndex
    {
        /// <summary>
        /// Fills the given float array with the values
        /// as the collector scores the search
        /// </summary>
        private sealed class FillingCollector : Collector
        {
            private readonly float[] _scores;
            private Scorer _scorer;

            public FillingCollector(float[] scores)
            {
                _scores = scores;
            }

            public override void SetScorer(Scorer scorer)
            {
                _scorer = scorer;
            }

            public override void Collect(int doc)
            {
                _scores[0] = _scorer.Score();
            }

            public override void SetNextReader(IndexReader reader, int docBase)
            { }

            public override bool AcceptsDocsOutOfOrder
            {
                get { return true; }
            }
        }
    }
}
