using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lucene.Net.Index;

namespace Lucene.Net.Codecs
{
    public abstract class DocValuesProducer : IDisposable
    {
        protected DocValuesProducer()
        {
        }

        public abstract NumericDocValues GetNumeric(FieldInfo field);

        public abstract BinaryDocValues GetBinary(FieldInfo field);

        public abstract SortedDocValues GetSorted(FieldInfo field);

        public abstract SortedSetDocValues GetSortedSet(FieldInfo field);

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected abstract void Dispose(bool disposing);
    }
}
