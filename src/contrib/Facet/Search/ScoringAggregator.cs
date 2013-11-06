using Lucene.Net.Index;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Facet.Search
{
    public class ScoringAggregator : IAggregator
    {
        private readonly float[] scoreArray;
        private readonly int hashCode;

        public ScoringAggregator(float[] counterArray)
        {
            this.scoreArray = counterArray;
            this.hashCode = scoreArray == null ? 0 : scoreArray.GetHashCode();
        }

        public void Aggregate(int docID, float score, IntsRef ordinals)
        {
            for (int i = 0; i < ordinals.length; i++)
            {
                scoreArray[ordinals.ints[i]] += score;
            }
        }

        public override bool Equals(Object obj)
        {
            if (obj == null || obj.GetType() != this.GetType())
            {
                return false;
            }

            ScoringAggregator that = (ScoringAggregator)obj;
            return that.scoreArray == this.scoreArray;
        }

        public override int GetHashCode()
        {
            return hashCode;
        }

        public bool SetNextReader(AtomicReaderContext context)
        {
            return true;
        }
    }
}
