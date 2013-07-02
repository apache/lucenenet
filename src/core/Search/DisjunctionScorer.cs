using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Search
{
    internal abstract class DisjunctionScorer : Scorer
    {
        protected readonly Scorer[] subScorers;
        protected int numScorers;

        protected DisjunctionScorer(Weight weight, Scorer[] subScorers, int numScorers)
            : base(weight)
        {
            this.subScorers = subScorers;
            this.numScorers = numScorers;
            Heapify();
        }

        protected void Heapify()
        {
            for (int i = (numScorers >> 1) - 1; i >= 0; i--)
            {
                HeapAdjust(i);
            }
        }

        protected void HeapAdjust(int root)
        {
            Scorer scorer = subScorers[root];
            int doc = scorer.DocID;
            int i = root;
            while (i <= (numScorers >> 1) - 1)
            {
                int lchild = (i << 1) + 1;
                Scorer lscorer = subScorers[lchild];
                int ldoc = lscorer.DocID;
                int rdoc = int.MaxValue, rchild = (i << 1) + 2;
                Scorer rscorer = null;
                if (rchild < numScorers)
                {
                    rscorer = subScorers[rchild];
                    rdoc = rscorer.DocID;
                }
                if (ldoc < doc)
                {
                    if (rdoc < ldoc)
                    {
                        subScorers[i] = rscorer;
                        subScorers[rchild] = scorer;
                        i = rchild;
                    }
                    else
                    {
                        subScorers[i] = lscorer;
                        subScorers[lchild] = scorer;
                        i = lchild;
                    }
                }
                else if (rdoc < doc)
                {
                    subScorers[i] = rscorer;
                    subScorers[rchild] = scorer;
                    i = rchild;
                }
                else
                {
                    return;
                }
            }
        }

        protected void HeapRemoveRoot()
        {
            if (numScorers == 1)
            {
                subScorers[0] = null;
                numScorers = 0;
            }
            else
            {
                subScorers[0] = subScorers[numScorers - 1];
                subScorers[numScorers - 1] = null;
                --numScorers;
                HeapAdjust(0);
            }
        }

        public override ICollection<ChildScorer> Children
        {
            get
            {
                List<ChildScorer> children = new List<ChildScorer>(numScorers);
                for (int i = 0; i < numScorers; i++)
                {
                    children.Add(new ChildScorer(subScorers[i], "SHOULD"));
                }
                return children;
            }
        }

        public override long Cost
        {
            get
            {
                long sum = 0;
                for (int i = 0; i < numScorers; i++)
                {
                    sum += subScorers[i].Cost;
                }
                return sum;
            }
        }
    }
}
