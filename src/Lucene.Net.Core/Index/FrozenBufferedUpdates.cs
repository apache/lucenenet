using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

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

    using ArrayUtil = Lucene.Net.Util.ArrayUtil;
    using BinaryDocValuesUpdate = Lucene.Net.Index.DocValuesUpdate.BinaryDocValuesUpdate;
    using NumericDocValuesUpdate = Lucene.Net.Index.DocValuesUpdate.NumericDocValuesUpdate;
    using Query = Lucene.Net.Search.Query;
    using QueryAndLimit = Lucene.Net.Index.BufferedUpdatesStream.QueryAndLimit;
    using RamUsageEstimator = Lucene.Net.Util.RamUsageEstimator;

    /// <summary>
    /// Holds buffered deletes and updates by term or query, once pushed. Pushed
    /// deletes/updates are write-once, so we shift to more memory efficient data
    /// structure to hold them. We don't hold docIDs because these are applied on
    /// flush.
    /// </summary>
    internal class FrozenBufferedUpdates
    {
        /* Query we often undercount (say 24 bytes), plus int. */
        internal static readonly int BYTES_PER_DEL_QUERY = RamUsageEstimator.NUM_BYTES_OBJECT_REF + RamUsageEstimator.NUM_BYTES_INT + 24;

        // Terms, in sorted order:
        internal readonly PrefixCodedTerms terms;

        internal int termCount; // just for debugging

        // Parallel array of deleted query, and the docIDUpto for each
        internal readonly Query[] queries;

        internal readonly int?[] queryLimits;

        // numeric DV update term and their updates
        internal readonly NumericDocValuesUpdate[] numericDVUpdates;

        // binary DV update term and their updates
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
            Debug.Assert(!isSegmentPrivate || deletes.terms.Count == 0, "segment private package should only have del queries");
            Term[] termsArray = deletes.terms.Keys.ToArray(/*new Term[deletes.Terms.Count]*/);
            termCount = termsArray.Length;
            ArrayUtil.TimSort(termsArray);
            PrefixCodedTerms.Builder builder = new PrefixCodedTerms.Builder();
            foreach (Term term in termsArray)
            {
                builder.Add(term);
            }
            terms = builder.Finish();

            queries = new Query[deletes.queries.Count];
            queryLimits = new int?[deletes.queries.Count];
            int upto = 0;
            foreach (KeyValuePair<Query, int?> ent in deletes.queries)
            {
                queries[upto] = ent.Key;
                queryLimits[upto] = ent.Value;
                upto++;
            }

            // TODO if a Term affects multiple fields, we could keep the updates key'd by Term
            // so that it maps to all fields it affects, sorted by their docUpto, and traverse
            // that Term only once, applying the update to all fields that still need to be
            // updated.
            IList<NumericDocValuesUpdate> allNumericUpdates = new List<NumericDocValuesUpdate>();
            int numericUpdatesSize = 0;
            foreach (var numericUpdates in deletes.numericUpdates.Values)
            {
                foreach (NumericDocValuesUpdate update in numericUpdates.Values)
                {
                    allNumericUpdates.Add(update);
                    numericUpdatesSize += update.SizeInBytes();
                }
            }
            numericDVUpdates = allNumericUpdates.ToArray();

            // TODO if a Term affects multiple fields, we could keep the updates key'd by Term
            // so that it maps to all fields it affects, sorted by their docUpto, and traverse
            // that Term only once, applying the update to all fields that still need to be
            // updated.
            IList<BinaryDocValuesUpdate> allBinaryUpdates = new List<BinaryDocValuesUpdate>();
            int binaryUpdatesSize = 0;
            foreach (var binaryUpdates in deletes.binaryUpdates.Values)
            {
                foreach (BinaryDocValuesUpdate update in binaryUpdates.Values)
                {
                    allBinaryUpdates.Add(update);
                    binaryUpdatesSize += update.SizeInBytes();
                }
            }
            binaryDVUpdates = allBinaryUpdates.ToArray();

            bytesUsed = (int)terms.SizeInBytes + queries.Length * BYTES_PER_DEL_QUERY + numericUpdatesSize + numericDVUpdates.Length * RamUsageEstimator.NUM_BYTES_OBJECT_REF + binaryUpdatesSize + binaryDVUpdates.Length * RamUsageEstimator.NUM_BYTES_OBJECT_REF;

            numTermDeletes = deletes.numTermDeletes.Get();
        }

        public virtual long DelGen
        {
            set
            {
                Debug.Assert(this.gen == -1);
                this.gen = value;
            }
            get
            {
                Debug.Assert(gen != -1);
                return gen;
            }
        }

        public virtual IEnumerable<Term> TermsIterable() // LUCENENET TODO: Rename to TermsEnumerable() ?
        {
            return new IterableAnonymousInnerClassHelper(this);
        }

        private class IterableAnonymousInnerClassHelper : IEnumerable<Term>
        {
            private readonly FrozenBufferedUpdates outerInstance;

            public IterableAnonymousInnerClassHelper(FrozenBufferedUpdates outerInstance)
            {
                this.outerInstance = outerInstance;
            }

            public virtual IEnumerator<Term> GetEnumerator()
            {
                return outerInstance.terms.GetEnumerator();
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        public virtual IEnumerable<QueryAndLimit> QueriesIterable() // LUCENENET TODO: Rename to QueriesEnumerable() ?
        {
            return new IterableAnonymousInnerClassHelper2(this);
        }

        private class IterableAnonymousInnerClassHelper2 : IEnumerable<QueryAndLimit>
        {
            private readonly FrozenBufferedUpdates outerInstance;

            public IterableAnonymousInnerClassHelper2(FrozenBufferedUpdates outerInstance)
            {
                this.outerInstance = outerInstance;
            }

            public virtual IEnumerator<QueryAndLimit> GetEnumerator()
            {
                return new IteratorAnonymousInnerClassHelper(this);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            private class IteratorAnonymousInnerClassHelper : IEnumerator<QueryAndLimit>
            {
                private readonly IterableAnonymousInnerClassHelper2 outerInstance;
                private int upto, i;
                private QueryAndLimit current;

                public IteratorAnonymousInnerClassHelper(IterableAnonymousInnerClassHelper2 outerInstance)
                {
                    this.outerInstance = outerInstance;
                    upto = this.outerInstance.outerInstance.queries.Length;
                    i = 0;
                }

                public virtual bool MoveNext()
                {
                    if (i < upto)
                    {
                        current = new QueryAndLimit(outerInstance.outerInstance.queries[i], outerInstance.outerInstance.queryLimits[i]);
                        i++;
                        return true;
                    }
                    return false;
                }

                public virtual QueryAndLimit Current
                {
                    get
                    {
                        return current;
                    }
                }

                object System.Collections.IEnumerator.Current
                {
                    get { return Current; }
                }

                public virtual void Reset()
                {
                    throw new NotImplementedException();
                }

                public void Dispose()
                {
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

        public virtual bool Any() // LUCENENET TODO: Make property?
        {
            return termCount > 0 || queries.Length > 0 || numericDVUpdates.Length > 0 || binaryDVUpdates.Length > 0;
        }
    }
}