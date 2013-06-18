using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lucene.Net.Util;

namespace Lucene.Net.Index
{
    public abstract class BinaryDocValues
    {
        protected BinaryDocValues()
        {
        }

        public abstract void Get(int docID, BytesRef result);

        public static readonly sbyte[] MISSING = new sbyte[0];

        private sealed class AnonymousEmptyBinaryDocValues : BinaryDocValues
        {
            public override void Get(int docID, BytesRef result)
            {
                result.bytes = BinaryDocValues.MISSING;
                result.offset = 0;
                result.length = 0;
            }
        }

        public static readonly BinaryDocValues EMPTY = new AnonymousEmptyBinaryDocValues();
    }
}
