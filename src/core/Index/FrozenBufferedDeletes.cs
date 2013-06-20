using Lucene.Net.Search;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Index
{
    internal class FrozenBufferedDeletes
    {
        internal static readonly int BYTES_PER_DEL_QUERY = RamUsageEstimator.NUM_BYTES_OBJECT_REF + RamUsageEstimator.NUM_BYTES_INT + 24;

        internal readonly PrefixCodedTerms terms;
        internal int termCount;

        // Parallel array of deleted query, and the docIDUpto for
        // each
        internal readonly Query[] queries;
        internal readonly int[] queryLimits;
        internal readonly int bytesUsed;
        internal readonly int numTermDeletes;
        private long gen = -1; // assigned by BufferedDeletesStream once pushed
        internal readonly bool isSegmentPrivate;  // set to true iff this frozen packet represents 
        // a segment private deletes. in that case is should
        // only have Queries 

        public FrozenBufferedDeletes(BufferedDeletes deletes, bool isSegmentPrivate)
        {
            this.isSegmentPrivate = isSegmentPrivate;
            //assert !isSegmentPrivate || deletes.terms.size() == 0 : "segment private package should only have del queries"; 
            Term[] termsArray = deletes.terms.Keys.ToArray();
            termCount = termsArray.Length;
            ArrayUtil.MergeSort(termsArray);
            PrefixCodedTerms.Builder builder = new PrefixCodedTerms.Builder();
            foreach (Term term in termsArray)
            {
                builder.Add(term);
            }
            terms = builder.Finish();

            queries = new Query[deletes.queries.Count];
            queryLimits = new int[deletes.queries.Count];
            int upto = 0;
            foreach (KeyValuePair<Query, int?> ent in deletes.queries)
            {
                queries[upto] = ent.Key;
                queryLimits[upto] = ent.Value.GetValueOrDefault();
                upto++;
            }

            bytesUsed = (int)terms.SizeInBytes + queries.Length * BYTES_PER_DEL_QUERY;
            numTermDeletes = deletes.numTermDeletes;
        }

        public long DelGen
        {
            get
            {
                //assert gen != -1;
                return gen;
            }
            set
            {
                //assert this.gen == -1;
                this.gen = value;
            }
        }

        public IEnumerable<Term> TermsEnumerable
        {
            get
            {
                return terms;
            }
        }

        public IEnumerable<BufferedDeletesStream.QueryAndLimit> Queries
        {
            get
            {
                for (int upto = 0; upto < queries.Length; upto++)
                {
                    yield return new BufferedDeletesStream.QueryAndLimit(queries[upto], queryLimits[upto]);
                }
            }
        }

        public override string ToString()
        {
            String s = "";
            if (numTermDeletes != 0)
            {
                s += " " + numTermDeletes + " deleted terms (unique count=" + termCount + ")";
            }
            if (queries.Length != 0)
            {
                s += " " + queries.Length + " deleted queries";
            }
            if (bytesUsed != 0)
            {
                s += " bytesUsed=" + bytesUsed;
            }

            return s;
        }

        internal bool Any()
        {
            return termCount > 0 || queries.Length > 0;
        }
    }
}
