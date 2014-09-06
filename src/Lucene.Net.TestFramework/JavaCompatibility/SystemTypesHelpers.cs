using System;
using System.Collections.Generic;
using System.Text;

namespace Lucene.Net
{
    public static class SystemTypesHelpers
    {
        public static char[] toCharArray(this string str)
        {
            return str.ToCharArray();
        }

        public static string toString(this object obj)
        {
            return obj.ToString();
        }

        public static bool equals(this object obj1, object obj2)
        {
            return obj1.Equals(obj2);
        }

        public static StringBuilder append(this StringBuilder sb, long value)
        {
            sb.Append(value);
            return sb;
        }

        public static StringBuilder append(this StringBuilder sb, string value)
        {
            sb.Append(value);
            return sb;
        }

        public static sbyte[] getBytes(this string str, string encoding)
        {
            return (sbyte[])(Array)Encoding.GetEncoding(encoding).GetBytes(str);
        }

        public static long size<T>(this IList<T> list)
        {
            return list.Count;
        }

        public static T[] clone<T>(this T[] e)
        {
            return (T[]) e.Clone();
        }

        public static void printStackTrace(this Exception e)
        {
            Console.WriteLine(e.StackTrace);
        }
    }
}
