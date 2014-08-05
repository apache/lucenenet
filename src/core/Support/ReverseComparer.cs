using System.Collections;
using System.Collections.Generic;

namespace Lucene.Net.Support
{
    sealed class ReverseComparer<T> : IComparer<T>
    {
        private readonly IComparer<T> inner;
        public ReverseComparer() : this(null) { }
        public ReverseComparer(IComparer<T> inner)
        {
            this.inner = inner ?? Comparer<T>.Default;
        }
        int IComparer<T>.Compare(T x, T y) { return inner.Compare(y, x); }
    }

    sealed class ReverseComparer : IComparer
    {
        private readonly IComparer inner;
        public ReverseComparer() : this(null) { }
        public ReverseComparer(IComparer inner)
        {
            this.inner = inner ?? Comparer.Default;
        }
        int IComparer.Compare(object x, object y) { return inner.Compare(y, x); }
    }
}
