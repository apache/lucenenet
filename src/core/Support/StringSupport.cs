using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lucene.Net.Util;

namespace Lucene.Net.Support
{
    public static class StringSupport
    {
        public static sbyte[] ToSbyteArray(this string str, Encoding enc)
        {
            return (sbyte[]) (Array) enc.GetBytes(str);
        }

        public static BytesRef ToBytesRefArray(this string str, Encoding enc)
        {
            return new BytesRef(str.ToSbyteArray(enc));
        }
    }
}
