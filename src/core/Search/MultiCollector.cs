using Lucene.Net.Index;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Search
{
    public class MultiCollector : Collector
    {
        public static Collector Wrap(params Collector[] collectors)
        {
            int n = 0;
            foreach (Collector c in collectors)
            {
                if (c != null)
                {
                    n++;
                }
            }

            if (n == 0)
            {
                throw new ArgumentException(@"At least 1 collector must not be null");
            }
            else if (n == 1)
            {
                Collector col = null;
                foreach (Collector c in collectors)
                {
                    if (c != null)
                    {
                        col = c;
                        break;
                    }
                }

                return col;
            }
            else if (n == collectors.Length)
            {
                return new MultiCollector(collectors);
            }
            else
            {
                Collector[] colls = new Collector[n];
                n = 0;
                foreach (Collector c in collectors)
                {
                    if (c != null)
                    {
                        colls[n++] = c;
                    }
                }

                return new MultiCollector(colls);
            }
        }

        private readonly Collector[] collectors;

        private MultiCollector(params Collector[] collectors)
        {
            this.collectors = collectors;
        }

        public override bool AcceptsDocsOutOfOrder
        {
            get
            {
                foreach (Collector c in collectors)
                {
                    if (!c.AcceptsDocsOutOfOrder)
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        public override void Collect(int doc)
        {
            foreach (Collector c in collectors)
            {
                c.Collect(doc);
            }
        }

        public override void SetNextReader(AtomicReaderContext context)
        {
            foreach (Collector c in collectors)
            {
                c.SetNextReader(context);
            }
        }

        public override void SetScorer(Scorer s)
        {
            foreach (Collector c in collectors)
            {
                c.SetScorer(s);
            }
        }
    }
}
