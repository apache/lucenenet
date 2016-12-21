using System;
using System.Collections;
using System.Collections.Generic;

namespace Lucene.Net.Index
{
    using Lucene.Net.Util;
    using BinaryDocValuesUpdate = Lucene.Net.Index.DocValuesUpdate.BinaryDocValuesUpdate;
    using BytesRef = Lucene.Net.Util.BytesRef;
    using NumericDocValuesUpdate = Lucene.Net.Index.DocValuesUpdate.NumericDocValuesUpdate;

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

    using Query = Lucene.Net.Search.Query;
    using QueryAndLimit = Lucene.Net.Index.BufferedUpdatesStream.QueryAndLimit;

    internal class CoalescedUpdates
    {
        internal readonly IDictionary<Query, int> Queries = new Dictionary<Query, int>();
        internal readonly IList<IEnumerable<Term>> Iterables = new List<IEnumerable<Term>>();
        internal readonly IList<NumericDocValuesUpdate> NumericDVUpdates = new List<NumericDocValuesUpdate>();
        internal readonly IList<BinaryDocValuesUpdate> BinaryDVUpdates = new List<BinaryDocValuesUpdate>();

        public override string ToString()
        {
            // note: we could add/collect more debugging information
            return "CoalescedUpdates(termSets=" + Iterables.Count + ",queries=" + Queries.Count + ",numericDVUpdates=" + NumericDVUpdates.Count + ",binaryDVUpdates=" + BinaryDVUpdates.Count + ")";
        }

        internal virtual void Update(FrozenBufferedUpdates @in)
        {
            Iterables.Add(@in.TermsIterable());

            for (int queryIdx = 0; queryIdx < @in.Queries.Length; queryIdx++)
            {
                Query query = @in.Queries[queryIdx];
                Queries[query] = BufferedUpdates.MAX_INT;
            }

            foreach (NumericDocValuesUpdate nu in @in.NumericDVUpdates)
            {
                NumericDocValuesUpdate clone = new NumericDocValuesUpdate(nu.term, nu.field, (long?)nu.value);
                clone.docIDUpto = int.MaxValue;
                NumericDVUpdates.Add(clone);
            }

            foreach (BinaryDocValuesUpdate bu in @in.BinaryDVUpdates)
            {
                BinaryDocValuesUpdate clone = new BinaryDocValuesUpdate(bu.term, bu.field, (BytesRef)bu.value);
                clone.docIDUpto = int.MaxValue;
                BinaryDVUpdates.Add(clone);
            }
        }

        //LUCENE TO-DO Is this right???
        public virtual IEnumerable<Term> TermsIterable()
        {
            return new IterableAnonymousInnerClassHelper(this);
            /*foreach (IEnumerable<Term> iterable in Iterables)
            {
                foreach (Term t in iterable)
                {
                    yield return t;
                }
            }*/
        }

        private class IterableAnonymousInnerClassHelper : IEnumerable<Term>
        {
            private readonly CoalescedUpdates OuterInstance;

            public IterableAnonymousInnerClassHelper(CoalescedUpdates outerInstance)
            {
                this.OuterInstance = outerInstance;
            }

            public virtual IEnumerator<Term> GetEnumerator()
            {
                IEnumerator<Term>[] subs = new IEnumerator<Term>[OuterInstance.Iterables.Count];
                for (int i = 0; i < OuterInstance.Iterables.Count; i++)
                {
                    subs[i] = OuterInstance.Iterables[i].GetEnumerator();
                }
                return new MergedIterator<Term>(subs);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        //LUCENE TO-DO Is this right???
        public virtual IEnumerable<QueryAndLimit> QueriesIterable()
        {
            /*foreach (KeyValuePair<Query, int> entry in Queries)
            {
                yield return new QueryAndLimit(entry.Key, entry.Value);
            }*/
            return new IterableAnonymousInnerClassHelper2(this);
        }

        private class IterableAnonymousInnerClassHelper2 : IEnumerable<QueryAndLimit>
        {
            private readonly CoalescedUpdates OuterInstance;

            public IterableAnonymousInnerClassHelper2(CoalescedUpdates outerInstance)
            {
                this.OuterInstance = outerInstance;
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
                private readonly IterableAnonymousInnerClassHelper2 OuterInstance;
                private readonly IEnumerator<KeyValuePair<Query, int>> iter;
                private QueryAndLimit current;

                public IteratorAnonymousInnerClassHelper(IterableAnonymousInnerClassHelper2 outerInstance)
                {
                    this.OuterInstance = outerInstance;
                    iter = OuterInstance.OuterInstance.Queries.GetEnumerator();
                }

                public void Dispose()
                {
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
                    throw new NotImplementedException();
                }

                public QueryAndLimit Current
                {
                    get { return current; }
                }

                object IEnumerator.Current
                {
                    get { return Current; }
                }
            }
        }
    }
}