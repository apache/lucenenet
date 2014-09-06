using Lucene.Net.Util;
using System;
using System.Text;

namespace Lucene.Net.Support
{
    public static class StringSupport
    {
        public static sbyte[] GetBytes(this string str, Encoding enc)
        {
            return (sbyte[])(Array)enc.GetBytes(str);
        }

        public static BytesRef ToBytesRefArray(this string str, Encoding enc)
        {
            return new BytesRef(str.GetBytes(enc));
        }
    }
}