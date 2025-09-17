using J2N;
using J2N.Collections.Generic.Extensions;
using J2N.Collections.ObjectModel;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using JCG = J2N.Collections.Generic;
#nullable enable

namespace Lucene.Net.Support
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

    internal static class Collections
    {
        private static class EmptyListHolder<T>
        {
            public static readonly ReadOnlyList<T> EMPTY_LIST = new JCG.List<T>().AsReadOnly();
        }

        private static class EmptyDictionaryHolder<TKey, TValue>
        {
            public static readonly ReadOnlyDictionary<TKey, TValue> EMPTY_DICTIONARY = new JCG.Dictionary<TKey, TValue>().AsReadOnly(); // LUCENENET-615: Must support nullable keys
        }

        private static class EmptySetHolder<T>
        {
            public static readonly ReadOnlySet<T> EMPTY_SET = new JCG.HashSet<T>().AsReadOnly();
        }

        public static ReadOnlyList<T> EmptyList<T>()
        {
            return EmptyListHolder<T>.EMPTY_LIST; // LUCENENET NOTE: Enumerable.Empty<T>() fails to cast to IList<T> on .NET Core 3.x, so we just create a new list
        }

        public static ReadOnlyDictionary<TKey, TValue> EmptyMap<TKey, TValue>()
        {
            return EmptyDictionaryHolder<TKey, TValue>.EMPTY_DICTIONARY;
        }

        public static ReadOnlySet<T> EmptySet<T>()
        {
            return EmptySetHolder<T>.EMPTY_SET;
        }

        public static void Reverse<T>(IList<T> list)
        {
            int size = list.Count;
            for (int i = 0, mid = size >> 1, j = size - 1; i < mid; i++, j--)
            {
                list.Swap(i, j);
            }
        }

        public static IComparer<T> ReverseOrder<T>()
        {
            return ReverseComparer<T>.REVERSE_ORDER;
        }

        public static IComparer<T> ReverseOrder<T>(IComparer<T>? cmp)
        {
            if (cmp is null)
                return ReverseOrder<T>();

            if (cmp is ReverseComparer2<T> reverseComparer2)
                return reverseComparer2.cmp;

            return new ReverseComparer2<T>(cmp);
        }

        public static IDictionary<TKey, TValue> SingletonMap<TKey, TValue>(TKey key, TValue value)
            where TKey : notnull
        {
            return AsReadOnly(new Dictionary<TKey, TValue> { { key, value } });
        }

        /// <summary>
        /// This is the same implementation of ToString from Java's AbstractCollection
        /// (the default implementation for all sets and lists)
        /// </summary>
        public static string ToString<T>(ICollection<T>? collection)
        {
            if (collection is null)
                return "null";

            if (collection.Count == 0)
            {
                return "[]";
            }

            bool isValueType = typeof(T).IsValueType;
            using var it = collection.GetEnumerator();
            StringBuilder sb = new StringBuilder();
            sb.Append('[');
            it.MoveNext();
            while (true)
            {
                T e = it.Current;
                sb.Append(ReferenceEquals(e, collection) ? "(this Collection)" : (isValueType ? Convert.ToString(e, CultureInfo.InvariantCulture) : ToString(e)));
                if (!it.MoveNext())
                {
                    return sb.Append(']').ToString();
                }
                sb.Append(',').Append(' ');
            }
        }

        /// <summary>
        /// This is the same implementation of ToString from Java's AbstractMap
        /// (the default implementation for all dictionaries)
        /// </summary>
        public static string ToString<TKey, TValue>(IDictionary<TKey, TValue>? dictionary)
        {
            if (dictionary is null)
                return "null";

            if (dictionary.Count == 0)
            {
                return "{}";
            }

            bool keyIsValueType = typeof(TKey).IsValueType;
            bool valueIsValueType = typeof(TValue).IsValueType;
            using var i = dictionary.GetEnumerator();
            StringBuilder sb = new StringBuilder();
            sb.Append('{');
            i.MoveNext();
            while (true)
            {
                KeyValuePair<TKey, TValue> e = i.Current;
                TKey key = e.Key;
                TValue value = e.Value;
                sb.Append(ReferenceEquals(key, dictionary) ? "(this Dictionary)" : (keyIsValueType ? Convert.ToString(key, CultureInfo.InvariantCulture) : ToString(key)));
                sb.Append('=');
                sb.Append(ReferenceEquals(value, dictionary) ? "(this Dictionary)" : (valueIsValueType ? Convert.ToString(value, CultureInfo.InvariantCulture) : ToString(value)));
                if (!i.MoveNext())
                {
                    return sb.Append('}').ToString();
                }
                sb.Append(',').Append(' ');
            }
        }

        /// <summary>
        /// This is a helper method that assists with recursively building
        /// a string of the current collection and all nested collections.
        /// </summary>
        public static string? ToString(object? obj)
        {
            if (obj is null)
            {
                return "null";
            }

            Type t = obj.GetType();
            if (t.IsGenericType
                && (t.ImplementsGenericInterface(typeof(ICollection<>)))
                || t.ImplementsGenericInterface(typeof(IDictionary<,>)))
            {
                dynamic genericType = Convert.ChangeType(obj, t);
                return ToString(genericType);
            }

            return Convert.ToString(obj, CultureInfo.InvariantCulture);
        }

        public static ReadOnlyList<T> AsReadOnly<T>(IList<T> list)
        {
            return new ReadOnlyList<T>(list);
        }

        public static ReadOnlyDictionary<TKey, TValue> AsReadOnly<TKey, TValue>(IDictionary<TKey, TValue> dictionary)
        {
            return new ReadOnlyDictionary<TKey, TValue>(dictionary);
        }

        #region Nested Types

        #region ReverseComparer

        private class ReverseComparer<T> : IComparer<T?>
        {
            internal static readonly ReverseComparer<T> REVERSE_ORDER = new ReverseComparer<T>();

            public int Compare(T? x, T? y)
            {
                // LUCENENET specific: Use J2N's Comparer<T> to mimic Java comparison behavior
                return JCG.Comparer<T?>.Default.Compare(y, x);
            }
        }

        #endregion ReverseComparer

        #region ReverseComparer2

        private class ReverseComparer2<T> : IComparer<T>
        {
            /**
             * The comparer specified in the static factory.  This will never
             * be null, as the static factory returns a ReverseComparer
             * instance if its argument is null.
             *
             * @serial
             */
            internal readonly IComparer<T> cmp;

            public ReverseComparer2(IComparer<T> cmp)
            {
                this.cmp = cmp;
            }

            public int Compare(T? t1, T? t2)
            {
                // [!]: In .NET Framework and Standard, it thinks that IComparer<T>.Compare cannot take nulls.
                // In .NET 8, it is typed to allow nulls.  We will assume that the passed comparer can handle nulls
                // if used with a nullable type.
                return cmp.Compare(t2!, t1!);
            }

            public override bool Equals(object? o)
            {
                return (o == this) ||
                    (o is ReverseComparer2<T> reverseComparer2 &&
                     cmp.Equals(reverseComparer2.cmp));
            }

            public override int GetHashCode()
            {
                return cmp.GetHashCode() ^ int.MinValue;
            }
        }

        #endregion ReverseComparer2

        #endregion Nested Types
    }
}
