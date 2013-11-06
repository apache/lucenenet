using Lucene.Net.Index;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Facet.Search
{
    public class CountingAggregator : IAggregator
    {
        protected int[] counterArray;

        public CountingAggregator(int[] counterArray)
        {
            this.counterArray = counterArray;
        }

        public virtual void Aggregate(int docID, float score, IntsRef ordinals)
        {
            for (int i = 0; i < ordinals.length; i++)
            {
                counterArray[ordinals.ints[i]]++;
            }
        }

        public override bool Equals(Object obj)
        {
            if (obj == null || obj.GetType() != this.GetType())
            {
                return false;
            }

            CountingAggregator that = (CountingAggregator)obj;
            return that.counterArray == this.counterArray;
        }

        public override int GetHashCode()
        {
            return counterArray == null ? 0 : counterArray.GetHashCode();
        }

        public virtual bool SetNextReader(AtomicReaderContext context)
        {
            return true;
        }
    }
}
