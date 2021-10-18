using J2N;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Console = Lucene.Net.Util.SystemConsole;
using JCG = J2N.Collections.Generic;

namespace Lucene.Net
{
    /*
     * Licensed to the Apache Software Foundation (ASF) under one or more
     * contributor license agreements.  See the NOTICE file distributed with
     * this work for additional information regarding copyright ownership.
     * The ASF licenses this file to You under the Apache License, Version 2.0
     * (the "License"); you may not use this file except in compliance with
     * the License.  You may obtain a copy of the License at
     *
     *     http://www.apache.org/licenses/LICENSE-2.0
     *
     * Unless required by applicable law or agreed to in writing, software
     * distributed under the License is distributed on an "AS IS" BASIS,
     * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
     * See the License for the specific language governing permissions and
     * limitations under the License.
     */

    /// <summary>
    /// LUCENENET specific extensions to various .NET types to make it easier to port tests
    /// from Java with fewer changes.
    /// </summary>
    [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "These are intended to make porting tests from Java simpler")]
    internal static class SystemTypesHelpers
    {
        public static char[] toCharArray(this string str)
        {
            return str.ToCharArray();
        }

        public static string toString(this object obj) // LUCENENET TODO: wrap Collections.ToString()
        {
            // LUCENENET: We compensate for the fact that
            // .NET doesn't have reliable results from ToString
            // by defaulting the behavior to return a concatenated
            // list of the contents of enumerables rather than the 
            // .NET type name (similar to the way Java behaves).
            // Unless of course we already have a string (which
            // implements IEnumerable so we need skip it).
            if (obj is IEnumerable && !(obj is string))
            {
                string result = obj.ToString();
                // Assume that this is a default call to object.ToString()
                // when it starts with the same namespace as the type.
                if (!result.StartsWith(obj.GetType().Namespace, StringComparison.Ordinal))
                {
                    return result;
                }

                // If this is the default text, replace it with
                // the contents of the enumerable as Java would.
                IEnumerable list = obj as IEnumerable;
                StringBuilder sb = new StringBuilder();
                bool isArray = obj.GetType().IsArray;
                sb.Append(isArray ? "{" : "[");
                foreach (object item in list)
                {
                    if (sb.Length > 1)
                    {
                        sb.Append(", ");
                    }
                    sb.Append(item != null ? item.ToString() : "null");
                }
                sb.Append(isArray ? "}" : "]");
                return sb.ToString();
            }
            return obj.ToString();
        }

        public static bool equals(this object obj1, object obj2)
        {
            return obj1.Equals(obj2);
        }

        public static StringBuilder append(this StringBuilder sb, bool value)
        {
            sb.Append(value);
            return sb;
        }

        public static StringBuilder append(this StringBuilder sb, byte value)
        {
            // LUCENENET NOTE: .NET uses the current culture by default, and any
            // Java code that calls this is expecting invariant culture
            sb.Append(value.ToString(CultureInfo.InvariantCulture));
            return sb;
        }

        public static StringBuilder append(this StringBuilder sb, char value)
        {
            sb.Append(value);
            return sb;
        }

        public static StringBuilder append(this StringBuilder sb, char[] value)
        {
            sb.Append(value);
            return sb;
        }

        // LUCENENET: These would only work if we copied the format from Java,
        // which is probably not something we want to do anyway.
        // Instead of calling a centralized method, we should be converting the
        // code on a case by case basis.
        //public static StringBuilder append(this StringBuilder sb, decimal value)
        //{
        //    sb.Append(value);
        //    return sb;
        //}

        //public static StringBuilder append(this StringBuilder sb, double value)
        //{
        //    sb.Append(value);
        //    return sb;
        //}

        //public static StringBuilder append(this StringBuilder sb, float value)
        //{
        //    sb.Append(value);
        //    return sb;
        //}

        public static StringBuilder append(this StringBuilder sb, int value)
        {
            // LUCENENET NOTE: .NET uses the current culture by default, and any
            // Java code that calls this is expecting invariant culture
            sb.Append(value.ToString(CultureInfo.InvariantCulture));
            return sb;
        }

        public static StringBuilder append(this StringBuilder sb, long value)
        {
            // LUCENENET NOTE: .NET uses the current culture by default, and any
            // Java code that calls this is expecting invariant culture
            sb.Append(value.ToString(CultureInfo.InvariantCulture));
            return sb;
        }

        public static StringBuilder append(this StringBuilder sb, object value)
        {
            sb.Append(value);
            return sb;
        }

        public static StringBuilder append(this StringBuilder sb, sbyte value)
        {
            // LUCENENET NOTE: .NET uses the current culture by default, and any
            // Java code that calls this is expecting invariant culture
            sb.Append(value.ToString(CultureInfo.InvariantCulture));
            return sb;
        }

        public static StringBuilder append(this StringBuilder sb, short value)
        {
            // LUCENENET NOTE: .NET uses the current culture by default, and any
            // Java code that calls this is expecting invariant culture
            sb.Append(value.ToString(CultureInfo.InvariantCulture));
            return sb;
        }

        public static StringBuilder append(this StringBuilder sb, string value)
        {
            sb.Append(value);
            return sb;
        }

        public static StringBuilder append(this StringBuilder sb, uint value)
        {
            // LUCENENET NOTE: .NET uses the current culture by default, and any
            // Java code that calls this is expecting invariant culture
            sb.Append(value.ToString(CultureInfo.InvariantCulture));
            return sb;
        }

        public static StringBuilder append(this StringBuilder sb, ulong value)
        {
            // LUCENENET NOTE: .NET uses the current culture by default, and any
            // Java code that calls this is expecting invariant culture
            sb.Append(value.ToString(CultureInfo.InvariantCulture));
            return sb;
        }

        public static StringBuilder append(this StringBuilder sb, ushort value)
        {
            // LUCENENET NOTE: .NET uses the current culture by default, and any
            // Java code that calls this is expecting invariant culture
            sb.Append(value.ToString(CultureInfo.InvariantCulture));
            return sb;
        }

        public static sbyte[] getBytes(this string str, string encoding)
        {
            return (sbyte[])(Array)Encoding.GetEncoding(encoding).GetBytes(str);
        }

        public static byte[] getBytes(this string str, Encoding encoding)
        {
            return encoding.GetBytes(str);
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
            s.UnionWith(other);
        }

        public static bool contains<T>(this ISet<T> s, T item)
        {
            return s.Contains(item);
        }

        public static bool containsAll<T>(this ISet<T> s, IEnumerable<T> list)
        {
            return s.IsSupersetOf(list);
        }

        public static bool remove<T>(this ISet<T> s, T item)
        {
            return s.Remove(item);
        }

        public static bool removeAll<T>(this ISet<T> s, IEnumerable<T> list)
        {
            bool modified = false;

            if (s.Count > list.Count())
            {
                foreach (var item in list)
                    modified |= s.Remove(item);
            }
            else
            {
                JCG.List<T> toRemove = new JCG.List<T>();

                foreach (var item in s)
                {
                    if (list.Contains(item))
                    {
                        toRemove.Add(item);
                    }
                }

                foreach (var i in toRemove)
                {
                    s.Remove(i);
                    modified = true;
                }
            }
            return modified;
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
            Console.Error.WriteLine(e.StackTrace);
        }

        public static void printStackTrace(this Exception e, TextWriter destination)
        {
            destination.WriteLine(e.StackTrace);
        }

        /// <summary>
        /// Locates resources in the same directory as this type
        /// </summary>
        public static Stream getResourceAsStream(this Type t, string name)
        {
            return t.FindAndGetManifestResourceStream(name);
        }

        public static int read(this TextReader reader, char[] buffer)
        {
            int bytesRead = reader.Read(buffer, 0, buffer.Length);
            // Convert the .NET 0 based bytes to the Java -1 behavior when reading is done.
            return bytesRead == 0 ? -1 : bytesRead;
        }

        public static string replaceFirst(this string text, string search, string replace)
        {
            var regex = new Regex(search);
            return regex.Replace(text, replace, 1);
        }

        public static byte[] ToByteArray(this sbyte[] arr)
        {
            var unsigned = new byte[arr.Length];
            System.Buffer.BlockCopy(arr, 0, unsigned, 0, arr.Length);
            return unsigned;
        }
    }
}
