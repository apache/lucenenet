using Lucene.Net.Util;
using System.Text;

namespace Lucene.Net.Support
{
    public static class StringSupport
    {
        public static byte[] GetBytes(this string str, Encoding enc)
        {
            return enc.GetBytes(str);
        }

        public static BytesRef ToBytesRefArray(this string str, Encoding enc)
        {
            return new BytesRef(str.GetBytes(enc));
        }
    }
}