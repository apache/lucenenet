using Lucene.Net.Util;
using System.Collections;
using System.Collections.Generic;
using JCG = J2N.Collections.Generic;

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

    using BytesRef = Lucene.Net.Util.BytesRef;
    using Query = Lucene.Net.Search.Query;
    using QueryAndLimit = Lucene.Net.Index.BufferedUpdatesStream.QueryAndLimit;

    internal class CoalescedUpdates
    {
        internal readonly IDictionary<Query, int> queries = new Dictionary<Query, int>();
        internal readonly IList<IEnumerable<Term>> iterables = new JCG.List<IEnumerable<Term>>();
        internal readonly IList<NumericDocValuesUpdate> numericDVUpdates = new JCG.List<NumericDocValuesUpdate>();
        internal readonly IList<BinaryDocValuesUpdate> binaryDVUpdates = new JCG.List<BinaryDocValuesUpdate>();

        public override string ToString()
        {
            // note: we could add/collect more debugging information
            return "CoalescedUpdates(termSets=" + iterables.Count + ",queries=" + queries.Count + ",numericDVUpdates=" + numericDVUpdates.Count + ",binaryDVUpdates=" + binaryDVUpdates.Count + ")";
        }

        internal virtual void Update(FrozenBufferedUpdates @in)
        {
            iterables.Add(@in.GetTermsEnumerable());

            for (int queryIdx = 0; queryIdx < @in.queries.Length; queryIdx++)
            {
                Query query = @in.queries[queryIdx];
                queries[query] = BufferedUpdates.MAX_INT32;
            }

            foreach (NumericDocValuesUpdate nu in @in.numericDVUpdates)
            {
                NumericDocValuesUpdate clone = new NumericDocValuesUpdate(nu.term, nu.field, nu.value);
                clone.docIDUpto = int.MaxValue;
                numericDVUpdates.Add(clone);
            }

            foreach (BinaryDocValuesUpdate bu in @in.binaryDVUpdates)
            {
                BinaryDocValuesUpdate clone = new BinaryDocValuesUpdate(bu.term, bu.field, (BytesRef)bu.value);
                clone.docIDUpto = int.MaxValue;
                binaryDVUpdates.Add(clone);
            }
        }

        /// <summary>
        /// This was termsIterable() in Lucene.
        /// </summary>
        public virtual IEnumerable<Term> GetTermsEnumerable()
        {
            return new EnumerableAnonymousClass(this);
        }

        private sealed class EnumerableAnonymousClass : IEnumerable<Term>
        {
            private readonly CoalescedUpdates outerInstance;

            public EnumerableAnonymousClass(CoalescedUpdates outerInstance)
            {
                this.outerInstance = outerInstance;
            }

            public IEnumerator<Term> GetEnumerator()
            {
                IEnumerator<Term>[] subs = new IEnumerator<Term>[outerInstance.iterables.Count];
                for (int i = 0; i < outerInstance.iterables.Count; i++)
                {
                    subs[i] = outerInstance.iterables[i].GetEnumerator();
                }
                return new MergedEnumerator<Term>(subs);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        /// <summary>
        /// This was queriesIterable() in Lucene.
        /// </summary>
        public virtual IEnumerable<QueryAndLimit> GetQueriesEnumerable()
        {
            return new EnumerableAnonymousClass2(this);
        }

        private sealed class EnumerableAnonymousClass2 : IEnumerable<QueryAndLimit>
        {
            private readonly CoalescedUpdates outerInstance;

            public EnumerableAnonymousClass2(CoalescedUpdates outerInstance)
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
                private readonly IEnumerator<KeyValuePair<Query, int>> iter;
                private QueryAndLimit current;

                public EnumeratorAnonymousClass(EnumerableAnonymousClass2 outerInstance)
                {
                    this.outerInstance = outerInstance;
                    iter = this.outerInstance.outerInstance.queries.GetEnumerator();
                }

                public void Dispose()
                {
                    iter.Dispose();
                }

                public bool MoveNext()
                {
                    if (!iter.MoveNext())
                    {
                        return false;
                    }
                    KeyValuePair<Query, int> ent = iter.Current;
                    current = new QueryAndLimit(ent.Key, ent.Value);
                    return true;
                }

                public void Reset()
                {
                    throw UnsupportedOperationException.Create();
                }

                public QueryAndLimit Current => current;

                object IEnumerator.Current => Current;
            }
        }
    }
}