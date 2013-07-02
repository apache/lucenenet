using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Support
{
    public class DelegatedComparer<T> : IComparer<T>
    {
        private Func<T, T, int> delegated;

        public DelegatedComparer(Func<T, T, int> delegated)
        {
            this.delegated = delegated;
        }

        public int Compare(T x, T y)
        {
            return delegated(x, y);
        }
    }
}
