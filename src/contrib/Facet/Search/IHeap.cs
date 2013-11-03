using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Facet.Search
{
    public interface IHeap<T>
    {
        T Pop();
        T Top();
        T InsertWithOverflow(T value);
        T Add(T frn);
        void Clear();
        int Size { get; }
    }
}
