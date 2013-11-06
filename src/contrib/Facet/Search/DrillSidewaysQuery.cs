using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Support;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Facet.Search
{
    internal class DrillSidewaysQuery : Query
    {
        readonly Query baseQuery;
        readonly Collector drillDownCollector;
        readonly Collector[] drillSidewaysCollectors;
        readonly Term[][] drillDownTerms;

        internal DrillSidewaysQuery(Query baseQuery, Collector drillDownCollector, Collector[] drillSidewaysCollectors, Term[][] drillDownTerms)
        {
            this.baseQuery = baseQuery;
            this.drillDownCollector = drillDownCollector;
            this.drillSidewaysCollectors = drillSidewaysCollectors;
            this.drillDownTerms = drillDownTerms;
        }

        public override string ToString(string field)
        {
            return @"DrillSidewaysQuery";
        }

        public override Query Rewrite(IndexReader reader)
        {
            Query newQuery = baseQuery;
            while (true)
            {
                Query rewrittenQuery = newQuery.Rewrite(reader);
                if (rewrittenQuery == newQuery)
                {
                    break;
                }

                newQuery = rewrittenQuery;
            }

            if (newQuery == baseQuery)
            {
                return this;
            }
            else
            {
                return new DrillSidewaysQuery(newQuery, drillDownCollector, drillSidewaysCollectors, drillDownTerms);
            }
        }

        public override Weight CreateWeight(IndexSearcher searcher)
        {
            Weight baseWeight = baseQuery.CreateWeight(searcher);
            return new AnonymousWeight(this, baseWeight);
        }

        private sealed class AnonymousWeight : Weight
        {
            public AnonymousWeight(DrillSidewaysQuery parent, Weight baseWeight)
            {
                this.parent = parent;
                this.baseWeight = baseWeight;
            }

            private readonly DrillSidewaysQuery parent;
            private readonly Weight baseWeight;

            public override Explanation Explain(AtomicReaderContext context, int doc)
            {
                return baseWeight.Explain(context, doc);
            }

            public override Query Query
            {
                get
                {
                    return parent.baseQuery;
                }
            }

            public override float ValueForNormalization
            {
                get
                {
                    return baseWeight.ValueForNormalization;
                }
            }

            public override void Normalize(float norm, float topLevelBoost)
            {
                baseWeight.Normalize(norm, topLevelBoost);
            }

            public override bool ScoresDocsOutOfOrder
            {
                get
                {
                    return false;
                }
            }

            public override Scorer Scorer(AtomicReaderContext context, bool scoreDocsInOrder, bool topScorer, IBits acceptDocs)
            {
                DrillSidewaysScorer.DocsEnumsAndFreq[] dims = new DrillSidewaysScorer.DocsEnumsAndFreq[parent.drillDownTerms.Length];
                TermsEnum termsEnum = null;
                string lastField = null;
                int nullCount = 0;
                for (int dim = 0; dim < dims.Length; dim++)
                {
                    dims[dim] = new DrillSidewaysScorer.DocsEnumsAndFreq();
                    dims[dim].sidewaysCollector = parent.drillSidewaysCollectors[dim];
                    string field = parent.drillDownTerms[dim][0].Field;
                    dims[dim].dim = parent.drillDownTerms[dim][0].Text;
                    if (lastField == null || !lastField.Equals(field))
                    {
                        AtomicReader reader = context.AtomicReader;
                        Terms terms = reader.Terms(field);
                        if (terms != null)
                        {
                            termsEnum = terms.Iterator(null);
                        }

                        lastField = field;
                    }

                    if (termsEnum == null)
                    {
                        nullCount++;
                        continue;
                    }

                    dims[dim].docsEnums = new DocsEnum[parent.drillDownTerms[dim].Length];
                    for (int i = 0; i < parent.drillDownTerms[dim].Length; i++)
                    {
                        if (termsEnum.SeekExact(parent.drillDownTerms[dim][i].Bytes, false))
                        {
                            dims[dim].freq = Math.Max(dims[dim].freq, termsEnum.DocFreq);
                            dims[dim].docsEnums[i] = termsEnum.Docs(null, null);
                        }
                    }
                }

                if (nullCount > 1)
                {
                    return null;
                }

                Array.Sort(dims);
                Scorer baseScorer = baseWeight.Scorer(context, scoreDocsInOrder, false, acceptDocs);
                if (baseScorer == null)
                {
                    return null;
                }

                return new DrillSidewaysScorer(this, context, baseScorer, parent.drillDownCollector, dims);
            }
        }

        public override int GetHashCode()
        {
            int prime = 31;
            int result = base.GetHashCode();
            result = prime * result + ((baseQuery == null) ? 0 : baseQuery.GetHashCode());
            result = prime * result + ((drillDownCollector == null) ? 0 : drillDownCollector.GetHashCode());
            result = prime * result + Arrays.GetHashCode(drillDownTerms);
            result = prime * result + Arrays.GetHashCode(drillSidewaysCollectors);
            return result;
        }

        public override bool Equals(Object obj)
        {
            if (this == obj)
                return true;
            if (!base.Equals(obj))
                return false;
            if (GetType() != obj.GetType())
                return false;
            DrillSidewaysQuery other = (DrillSidewaysQuery)obj;
            if (baseQuery == null)
            {
                if (other.baseQuery != null)
                    return false;
            }
            else if (!baseQuery.Equals(other.baseQuery))
                return false;
            if (drillDownCollector == null)
            {
                if (other.drillDownCollector != null)
                    return false;
            }
            else if (!drillDownCollector.Equals(other.drillDownCollector))
                return false;
            if (!Arrays.Equals(drillDownTerms, other.drillDownTerms))
                return false;
            if (!Arrays.Equals(drillSidewaysCollectors, other.drillSidewaysCollectors))
                return false;
            return true;
        }
    }
}
