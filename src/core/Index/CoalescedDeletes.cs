using Lucene.Net.Search;
using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Index
{
    internal class CoalescedDeletes
    {
        internal readonly IDictionary<Query, int?> queries = new HashMap<Query, int?>();
        internal readonly IList<IEnumerable<Term>> iterables = new List<IEnumerable<Term>>();

        public override string ToString()
        {
            // note: we could add/collect more debugging information
            return "CoalescedDeletes(termSets=" + iterables.Count + ",queries=" + queries.Count + ")";
        }

        internal virtual void Update(FrozenBufferedDeletes input)
        {
            iterables.Add(input.TermsEnumerable);

            for (int queryIdx = 0; queryIdx < input.queries.Length; queryIdx++)
            {
                Query query = input.queries[queryIdx];
                queries[query] = BufferedDeletes.MAX_INT;
            }
        }

        private sealed class AnonymousTermsEnumerable : IEnumerable<Term>
        {
            private readonly IList<IEnumerable<Term>> iterables;

            public AnonymousTermsEnumerable(IList<IEnumerable<Term>> iterables)
            {
                this.iterables = iterables;
            }

            public IEnumerator<Term> GetEnumerator()
            {
                IEnumerator<Term>[] subs = new IEnumerator<Term>[iterables.Count];
                for (int i = 0; i < iterables.Count; i++)
                {
                    subs[i] = iterables[i].GetEnumerator();
                }
                return new MergedIterator<Term>(subs);
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        public virtual IEnumerable<Term> TermsEnumerable
        {
            get
            {
                return new AnonymousTermsEnumerable(iterables);
            }
        }

        public virtual IEnumerable<BufferedDeletesStream.QueryAndLimit> QueriesEnumerable
        {
            get
            {
                foreach (KeyValuePair<Query, int?> ent in queries)
                {
                    yield return new BufferedDeletesStream.QueryAndLimit(ent.Key, ent.Value.GetValueOrDefault());
                }
            }
        }
    }
}
