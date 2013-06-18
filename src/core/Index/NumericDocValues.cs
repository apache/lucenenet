using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Index
{
    public abstract class NumericDocValues
    {
        protected NumericDocValues()
        {
        }

        public abstract long Get(int docID);

        private sealed class AnonymousEmptyNumericDocValues : NumericDocValues
        {
            public override long Get(int docID)
            {
                return 0;
            }
        }

        public static readonly NumericDocValues EMPTY = new AnonymousEmptyNumericDocValues();
    }
}
