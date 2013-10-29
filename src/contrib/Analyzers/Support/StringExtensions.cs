using System;
using Lucene.Net.Support;

namespace Lucene.Net.Analysis
{
    public static class StringExtensions
    {
        public static bool EqualsIgnoreCase(this string s, string other)
        {
            return string.Equals(s, other, StringComparison.OrdinalIgnoreCase);
        }

        public static void GetChars(this string s, int srcBegin, int srcEnd, char[] dst, int dstBegin)
        {
            for (int i = srcBegin, j = 0; i < srcEnd; i++, j++)
            {
                dst[dstBegin + j] = s[srcBegin + i];
            }
        }

        public static ICharSequence AsCharSequence(this string s)
        {
            return new StringCharSequenceWrapper(s);
        }
    }
}
