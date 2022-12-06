using J2N.Collections.Generic.Extensions;
using Lucene.Net.Diagnostics;
using Lucene.Net.Search;
using Lucene.Net.Util;
using System.Collections;
using System.Collections.Generic;
using JCG = J2N.Collections.Generic;
using QueryAndLimit = Lucene.Net.Index.BufferedUpdatesStream.QueryAndLimit;

namespace Lucene.Net.Index
{
    /*
     * Licensed to the Apache Software Foundation (ASF) under one or more
     * contributor license agreements.  See the NOTICE file distributed with
     * this work for additional information regarding copyright ownership.
     * The ASF licenses this file to You under the Apache License, Version 2.0
     * (the "License"); you may not use this file except in compliance with
     * the License.  You may obtain a copy of the License at
     *
     *     http://www.apache.org/licenses/LICENSE-2.0
     *
     * Unless required by applicable law or agreed to in writing, software
     * distributed under the License is distributed on an "AS IS" BASIS,
     * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
     * See the License for the specific language governing permissions and
     * limitations under the License.
     */

    /// <summary>
    /// Holds buffered deletes and updates by term or query, once pushed. Pushed
    /// deletes/updates are write-once, so we shift to more memory efficient data
    /// structure to hold them. We don't hold docIDs because these are applied on
    /// flush.
    /// </summary>
    internal class FrozenBufferedUpdates
    {
        /// <summary>Query we often undercount (say 24 bytes), plus int.</summary>
        internal static readonly int BYTES_PER_DEL_QUERY = RamUsageEstimator.NUM_BYTES_OBJECT_REF + RamUsageEstimator.NUM_BYTES_INT32 + 24;

        /// <summary>Terms, in sorted order:</summary>
        internal readonly PrefixCodedTerms terms;

        internal int termCount; // just for debugging

        /// <summary>Parallel array of deleted query, and the docIDUpto for each</summary>
        internal readonly Query[] queries;

        internal readonly int[] queryLimits;

        /// <summary>numeric DV update term and their updates</summary>
        internal readonly NumericDocValuesUpdate[] numericDVUpdates;

        /// <summary>binary DV update term and their updates</summary>
        internal readonly BinaryDocValuesUpdate[] binaryDVUpdates;

        internal readonly int bytesUsed;
        internal readonly int numTermDeletes;
        private long gen = -1; // assigned by BufferedDeletesStream once pushed
        internal readonly bool isSegmentPrivate; // set to true iff this frozen packet represents
        // a segment private deletes. in that case is should
        // only have Queries

        public FrozenBufferedUpdates(BufferedUpdates deletes, bool isSegmentPrivate)
        {
            this.isSegmentPrivate = isSegmentPrivate;
            if (Debugging.AssertsEnabled) Debugging.Assert(!isSegmentPrivate || deletes.terms.Count == 0, "segment private package should only have del queries");
            Term[] termsArray = deletes.terms.Keys.ToArray(/*new Term[deletes.terms.Count]*/);

            termCount = termsArray.Length;
            ArrayUtil.TimSort(termsArray);
            PrefixCodedTerms.Builder builder = new PrefixCodedTerms.Builder();
            foreach (Term term in termsArray)
            {
                builder.Add(term);
            }
            terms = builder.Finish();

            queries = new Query[deletes.queries.Count];
            queryLimits = new int[deletes.queries.Count];
            int upto = 0;
            foreach (KeyValuePair<Query, int> ent in deletes.queries)
            {
                queries[upto] = ent.Key;
                queryLimits[upto] = ent.Value;
                upto++;
            }

            // TODO if a Term affects multiple fields, we could keep the updates key'd by Term
            // so that it maps to all fields it affects, sorted by their docUpto, and traverse
            // that Term only once, applying the update to all fields that still need to be
            // updated.
            IList<NumericDocValuesUpdate> allNumericUpdates = new JCG.List<NumericDocValuesUpdate>();
            int numericUpdatesSize = 0;
            foreach (var numericUpdates in deletes.numericUpdates.Values)
            {
                foreach (NumericDocValuesUpdate update in numericUpdates.Values)
                {
                    allNumericUpdates.Add(update);
                    numericUpdatesSize += update.GetSizeInBytes();
                }
            }
            numericDVUpdates = allNumericUpdates.ToArray();

            // TODO if a Term affects multiple fields, we could keep the updates key'd by Term
            // so that it maps to all fields it affects, sorted by their docUpto, and traverse
            // that Term only once, applying the update to all fields that still need to be
            // updated.
            IList<BinaryDocValuesUpdate> allBinaryUpdates = new JCG.List<BinaryDocValuesUpdate>();
            int binaryUpdatesSize = 0;
            foreach (var binaryUpdates in deletes.binaryUpdates.Values)
            {
                foreach (BinaryDocValuesUpdate update in binaryUpdates.Values)
                {
                    allBinaryUpdates.Add(update);
                    binaryUpdatesSize += update.GetSizeInBytes();
                }
            }
            binaryDVUpdates = allBinaryUpdates.ToArray();

            bytesUsed = (int)terms.GetSizeInBytes() + queries.Length * BYTES_PER_DEL_QUERY + numericUpdatesSize + numericDVUpdates.Length * RamUsageEstimator.NUM_BYTES_OBJECT_REF + binaryUpdatesSize + binaryDVUpdates.Length * RamUsageEstimator.NUM_BYTES_OBJECT_REF;

            numTermDeletes = deletes.numTermDeletes;
        }

        public virtual long DelGen
        {
            set
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(this.gen == -1);
                this.gen = value;
            }
            get
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(gen != -1);
                return gen;
            }
        }

        // LUCENENET NOTE: This was termsIterable() in Lucene
        public virtual IEnumerable<Term> GetTermsEnumerable()
        {
            return new EnumerableAnonymousClass(this);
        }

        private sealed class EnumerableAnonymousClass : IEnumerable<Term>
        {
            private readonly FrozenBufferedUpdates outerInstance;

            public EnumerableAnonymousClass(FrozenBufferedUpdates outerInstance)
            {
                this.outerInstance = outerInstance;
            }

            public IEnumerator<Term> GetEnumerator()
            {
                return outerInstance.terms.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        // LUCENENET NOTE: This was queriesIterable() in Lucene
        public virtual IEnumerable<QueryAndLimit> GetQueriesEnumerable()
        {
            return new EnumerableAnonymousClass2(this);
        }

        private sealed class EnumerableAnonymousClass2 : IEnumerable<QueryAndLimit>
        {
            private readonly FrozenBufferedUpdates outerInstance;

            public EnumerableAnonymousClass2(FrozenBufferedUpdates outerInstance)
            {
                this.outerInstance = outerInstance;
            }

            public IEnumerator<QueryAndLimit> GetEnumerator()
            {
                return new EnumeratorAnonymousClass(this);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            private sealed class EnumeratorAnonymousClass : IEnumerator<QueryAndLimit>
            {
                private readonly EnumerableAnonymousClass2 outerInstance;
                private readonly int upto;
                private int i;
                private QueryAndLimit current;

                public EnumeratorAnonymousClass(EnumerableAnonymousClass2 outerInstance)
                {
                    this.outerInstance = outerInstance;
                    upto = this.outerInstance.outerInstance.queries.Length;
                    i = 0;
                }

                public bool MoveNext()
                {
                    if (i < upto)
                    {
                        current = new QueryAndLimit(outerInstance.outerInstance.queries[i], outerInstance.outerInstance.queryLimits[i]);
                        i++;
                        return true;
                    }
                    return false;
                }

                public QueryAndLimit Current => current;

                object IEnumerator.Current => Current;

                public void Reset()
                {
                    throw UnsupportedOperationException.Create();
                }

                public void Dispose()
                {
                    // LUCENENET: Intentionally blank
                }
            }
        }

        public override string ToString()
        {
            string s = "";
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

        public virtual bool Any()
        {
            return termCount > 0 || queries.Length > 0 || numericDVUpdates.Length > 0 || binaryDVUpdates.Length > 0;
        }
    }
}