using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Facet.Collections
{
    public interface IIntIterator
    {
        bool HasNext();
        int Next();
        void Remove();
    }
}
