using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization.Formatters;
using System.Text;
using Lucene.Net.Support;

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

        public static int size<T>(this ICollection<T> list)
        {
            return list.Count;
        }

        public static T[] clone<T>(this T[] e)
        {
            return (T[]) e.Clone();
        }

        public static void add<T>(this ISet<T> s, T item)
        {
            s.Add(item);
        }

        public static void addAll<T>(this ISet<T> s, IEnumerable<T> other)
        {
            s.AddAll(other);
        }

        public static bool contains<T>(this ISet<T> s, T item)
        {
            return s.Contains(item);
        }

        public static bool containsAll<T>(this ISet<T> s, IEnumerable<T> list)
        {
            return list.Any(s.Contains);
        }

        public static bool remove<T>(this ISet<T> s, T item)
        {
            return s.Remove(item);
        }

        public static bool removeAll<T>(this ISet<T> s, IEnumerable<T> list)
        {
            return s.removeAll(list);
        }

        public static void clear<T>(this ISet<T> s)
        {
            s.Clear();
        }

        public static void retainAll<T>(this ISet<T> s, ISet<T> other)
        {
            foreach (var e in s)
            {
                if (!other.Contains(e))
                    s.Remove(e);
            }
        }

        public static void printStackTrace(this Exception e)
        {
            Console.WriteLine(e.StackTrace);
        }
    }
}
