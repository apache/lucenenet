using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;

namespace Lucene.Net
{
    public static class Collections
    {
        public static bool AddAll<T>(ISet<T> set, IEnumerable<T> elements)
        {
            bool result = false;
            foreach (T element in elements)
            {
                result |= set.Add(element);
            }
            return result;
        }

        public static IList<T> EmptyList<T>()
        {
            return (IList<T>)Enumerable.Empty<T>();
        }

        public static IDictionary<TKey, TValue> EmptyMap<TKey, TValue>()
        {
            return new Dictionary<TKey, TValue>();
        }

        public static ISet<T> NewSetFromMap<T, S>(IDictionary<T, bool?> map)
        {
            return new SetFromMap<T>(map);
        }

        public static IComparer<T> ReverseOrder<T>()
        {
            return (IComparer<T>)ReverseComparer<T>.REVERSE_ORDER;
        }

        public static IComparer<T> ReverseOrder<T>(IComparer<T> cmp)
        {
            if (cmp == null)
                return ReverseOrder<T>();

            if (cmp is ReverseComparer2<T>)
                return ((ReverseComparer2<T>)cmp).cmp;

            return new ReverseComparer2<T>(cmp);
        }

        public static void Shuffle<T>(IList<T> list)
        {
            Shuffle(list, new Random());
        }

        // Method found here http://stackoverflow.com/a/2301091/181087
        // This shuffles the list in place without using LINQ, which is fast and efficient.
        public static void Shuffle<T>(IList<T> list, Random random)
        {
            for (int i = list.Count; i > 1; i--)
            {
                int pos = random.Next(i);
                var x = list[i - 1];
                list[i - 1] = list[pos];
                list[pos] = x;
            }
        }

        public static ISet<T> Singleton<T>(T o)
        {
            return new HashSet<T>(new T[] { o });
        }

        public static IDictionary<TKey, TValue> SingletonMap<TKey, TValue>(TKey key, TValue value)
        {
            return new Dictionary<TKey, TValue> { { key, value } };
        }

        public static void Swap<T>(IList<T> list, int index1, int index2)
        {
            T tmp = list[index1];
            list[index1] = list[index2];
            list[index2] = tmp;
        }

        public static IList<T> UnmodifiableList<T>(IList<T> list)
        {
            return new UnmodifiableListImpl<T>(list);
        }

        public static IDictionary<TKey, TValue> UnmodifiableMap<TKey, TValue>(IDictionary<TKey, TValue> d)
        {
            return new UnmodifiableDictionary<TKey, TValue>(d);
        }

        public static ISet<T> UnmodifiableSet<T>(ISet<T> list)
        {
            return new UnmodifiableSetImpl<T>(list);
        }


        /// <summary>
        /// The same implementation of GetHashCode from Java's AbstractList
        /// (the default implementation for all lists).
        /// <para/>
        /// This algorithm depends on the order of the items in the list.
        /// It is recursive and will build the hash code based on the values of
        /// all nested collections.
        /// <para/>
        /// Note this operation currently only supports <see cref="IList{T}"/>, <see cref="ISet{T}"/>, 
        /// and <see cref="IDictionary{TKey, TValue}"/>.
        /// </summary>
        public static int GetHashCode<T>(IList<T> list)
        {
            int hashCode = 1;
            bool isValueType = typeof(T).GetTypeInfo().IsValueType;
            foreach (T e in list)
            {
                hashCode = 31 * hashCode +
                    (isValueType ? e.GetHashCode() : (e == null ? 0 : GetHashCode(e)));
            }

            return hashCode;
        }

        /// <summary>
        /// The same implementation of GetHashCode from Java's AbstractSet
        /// (the default implementation for all sets)
        /// <para/>
        /// This algorithm does not depend on the order of the items in the set.
        /// It is recursive and will build the hash code based on the values of
        /// all nested collections.
        /// <para/>
        /// Note this operation currently only supports <see cref="IList{T}"/>, <see cref="ISet{T}"/>, 
        /// and <see cref="IDictionary{TKey, TValue}"/>.
        /// </summary>
        public static int GetHashCode<T>(ISet<T> set)
        {
            int h = 0;
            bool isValueType = typeof(T).GetTypeInfo().IsValueType;
            using (var i = set.GetEnumerator())
            {
                while (i.MoveNext())
                {
                    T obj = i.Current;
                    if (isValueType)
                    {
                        h += obj.GetHashCode();
                    }
                    else if (obj != null)
                    {
                        h += GetHashCode(obj);
                    }
                }
            }
            return h;
        }

        /// <summary>
        /// The same implementation of GetHashCode from Java's AbstractMap
        /// (the default implementation for all dictionaries)
        /// <para/>
        /// This algoritm does not depend on the order of the items in the dictionary.
        /// It is recursive and will build the hash code based on the values of
        /// all nested collections.
        /// <para/>
        /// Note this operation currently only supports <see cref="IList{T}"/>, <see cref="ISet{T}"/>, 
        /// and <see cref="IDictionary{TKey, TValue}"/>.
        /// </summary>
        public static int GetHashCode<TKey, TValue>(IDictionary<TKey, TValue> dictionary)
        {
            int h = 0;
            bool keyIsValueType = typeof(TKey).GetTypeInfo().IsValueType;
            bool valueIsValueType = typeof(TValue).GetTypeInfo().IsValueType;
            using (var i = dictionary.GetEnumerator())
            {
                while (i.MoveNext())
                {
                    TKey key = i.Current.Key;
                    TValue value = i.Current.Value;
                    int keyHash = (keyIsValueType ? key.GetHashCode() : (key == null ? 0 : GetHashCode(key)));
                    int valueHash = (valueIsValueType ? value.GetHashCode() : (value == null ? 0 : GetHashCode(value)));
                    h += keyHash ^ valueHash;
                }
            }
            return h;
        }

        /// <summary>
        /// This method generally assists with the recursive GetHashCode() that
        /// builds a hash code based on all of the values in a collection 
        /// including any nested collections (lists, sets, arrays, and dictionaries).
        /// <para/>
        /// Note this currently only supports <see cref="IList{T}"/>, <see cref="ISet{T}"/>, 
        /// and <see cref="IDictionary{TKey, TValue}"/>.
        /// </summary>
        /// <param name="obj">the object to build the hash code for</param>
        /// <returns>a value that represents the unique state of all of the values and 
        /// nested collection values in the object, provided the main object itself is 
        /// a collection, otherwise calls <see cref="object.GetHashCode()"/> on the 
        /// object that is passed.</returns>
        public static int GetHashCode(object obj)
        {
            if (obj == null)
            {
                return 0; // 0 for null
            }

            Type t = obj.GetType();
            if (t.GetTypeInfo().IsGenericType
                && (t.ImplementsGenericInterface(typeof(IList<>))
                || t.ImplementsGenericInterface(typeof(ISet<>))
                || t.ImplementsGenericInterface(typeof(IDictionary<,>))))
            {
                dynamic genericType = Convert.ChangeType(obj, t);
                return GetHashCode(genericType);
            }

            return obj.GetHashCode();
        }

        /// <summary>
        /// The same implementation of Equals from Java's AbstractList
        /// (the default implementation for all lists)
        /// <para/>
        /// This algorithm depends on the order of the items in the list. 
        /// It is recursive and will determine equality based on the values of
        /// all nested collections.
        /// <para/>
        /// Note this operation currently only supports <see cref="IList{T}"/>, <see cref="ISet{T}"/>, 
        /// and <see cref="IDictionary{TKey, TValue}"/>.
        /// </summary>
        public static bool Equals<T>(IList<T> listA, IList<T> listB)
        {
            if (object.ReferenceEquals(listA, listB))
            {
                return true;
            }

            bool isValueType = typeof(T).GetTypeInfo().IsValueType;

            if (!isValueType && listA == null)
            {
                if (listB == null)
                {
                    return true;
                }
                return false;
            }


            using (IEnumerator<T> eA = listA.GetEnumerator())
            {
                using (IEnumerator<T> eB = listB.GetEnumerator())
                {
                    while (eA.MoveNext() && eB.MoveNext())
                    {
                        T o1 = eA.Current;
                        T o2 = eB.Current;

                        if (isValueType ?
                            !o1.Equals(o2) :
                            (!(o1 == null ? o2 == null : Equals(o1, o2))))
                        {
                            return false;
                        }
                    }

                    return (!(eA.MoveNext() || eB.MoveNext()));
                }
            }
        }

        /// <summary>
        /// The same implementation of Equals from Java's AbstractSet
        /// (the default implementation for all sets)
        /// <para/>
        /// This algoritm does not depend on the order of the items in the set.
        /// It is recursive and will determine equality based on the values of
        /// all nested collections.
        /// <para/>
        /// Note this operation currently only supports <see cref="IList{T}"/>, <see cref="ISet{T}"/>, 
        /// and <see cref="IDictionary{TKey, TValue}"/>.
        /// </summary>
        public static bool Equals<T>(ISet<T> setA, ISet<T> setB)
        {
            if (object.ReferenceEquals(setA, setB))
            {
                return true;
            }

            if (setA == null)
            {
                if (setB == null)
                {
                    return true;
                }
                return false;
            }

            if (setA.Count != setB.Count)
            {
                return false;
            }

            bool isValueType = typeof(T).GetTypeInfo().IsValueType;

            // same operation as containsAll()
            foreach (T eB in setB)
            {
                bool contains = false;
                foreach (T eA in setA)
                {
                    if (isValueType ? eA.Equals(eB) : Equals(eA, eB))
                    {
                        contains = true;
                        break;
                    }
                }
                if (!contains)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// This is the same implemenation of Equals from Java's AbstractMap
        /// (the default implementation of all dictionaries)
        /// <para/>
        /// This algoritm does not depend on the order of the items in the dictionary.
        /// It is recursive and will determine equality based on the values of
        /// all nested collections.
        /// <para/>
        /// Note this operation currently only supports <see cref="IList{T}"/>, <see cref="ISet{T}"/>, 
        /// and <see cref="IDictionary{TKey, TValue}"/>.
        /// </summary>
        public static bool Equals<TKey, TValue>(IDictionary<TKey, TValue> dictionaryA, IDictionary<TKey, TValue> dictionaryB)
        {
            if (object.ReferenceEquals(dictionaryA, dictionaryB))
            {
                return true;
            }

            if (dictionaryA == null)
            {
                if (dictionaryB == null)
                {
                    return true;
                }
                return false;
            }

            if (dictionaryA.Count != dictionaryB.Count)
            {
                return false;
            }

            bool valueIsValueType = typeof(TValue).GetTypeInfo().IsValueType;

            using (var i = dictionaryB.GetEnumerator())
            {
                while (i.MoveNext())
                {
                    KeyValuePair<TKey, TValue> e = i.Current;
                    TKey keyB = e.Key;
                    TValue valueB = e.Value;
                    if (valueB == null)
                    {
                        if (!(dictionaryA.ContainsKey(keyB)))
                        {
                            return false;
                        }
                    }
                    else
                    {
                        TValue valueA;
                        if (!dictionaryA.TryGetValue(keyB, out valueA) || (valueIsValueType ? !valueA.Equals(valueB) : !Equals(valueA, valueB)))
                        {
                            return false;
                        }
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// A helper method to recursively determine equality based on
        /// the values of the collection and all nested collections.
        /// <para/>
        /// Note this operation currently only supports <see cref="IList{T}"/>, <see cref="ISet{T}"/>, 
        /// and <see cref="IDictionary{TKey, TValue}"/>.
        /// </summary>
        new public static bool Equals(object objA, object objB)
        {
            if (objA == null)
            {
                if (objB == null)
                {
                    return true;
                }
                return false;
            }
            else if (objB == null)
            {
                return false;
            }

            Type tA = objA.GetType();
            Type tB = objB.GetType();
            if (tA.GetTypeInfo().IsGenericType)
            {
                bool shouldReturn = false;

                if (tA.ImplementsGenericInterface(typeof(IList<>)))
                {
                    if (!(tB.GetTypeInfo().IsGenericType && tB.ImplementsGenericInterface(typeof(IList<>))))
                    {
                        return false; // type mismatch - must be a list
                    }
                    shouldReturn = true;
                }
                else if (tA.ImplementsGenericInterface(typeof(ISet<>)))
                {
                    if (!(tB.GetTypeInfo().IsGenericType && tB.ImplementsGenericInterface(typeof(ISet<>))))
                    {
                        return false; // type mismatch - must be a set
                    }
                    shouldReturn = true;
                }
                else if (tA.ImplementsGenericInterface(typeof(IDictionary<,>)))
                {
                    if (!(tB.GetTypeInfo().IsGenericType && tB.ImplementsGenericInterface(typeof(IDictionary<,>))))
                    {
                        return false; // type mismatch - must be a dictionary
                    }
                    shouldReturn = true;
                }

                if (shouldReturn)
                {
                    dynamic genericTypeA = Convert.ChangeType(objA, tA);
                    dynamic genericTypeB = Convert.ChangeType(objB, tB);
                    return Equals(genericTypeA, genericTypeB);
                }
            }

            return objA.Equals(objB);
        }

        // LUCENENET TODO: Move to a new TypeExtensions class
        private static bool ImplementsGenericInterface(this Type target, Type interfaceType)
        {
            return target.GetTypeInfo().IsGenericType && target.GetGenericTypeDefinition().GetInterfaces().Any(
                x => x.GetTypeInfo().IsGenericType && interfaceType.IsAssignableFrom(x.GetGenericTypeDefinition())
            );
        }


        /// <summary>
        /// This is the same implementation of ToString from Java's AbstractCollection
        /// (the default implementation for all sets and lists)
        /// </summary>
        public static string ToString<T>(ICollection<T> collection)
        {
            if (collection.Count == 0)
            {
                return "[]";
            }

            bool isValueType = typeof(T).GetTypeInfo().IsValueType;
            using (var it = collection.GetEnumerator())
            {
                StringBuilder sb = new StringBuilder();
                sb.Append('[');
                it.MoveNext();
                while (true)
                {
                    T e = it.Current;
                    sb.Append(object.ReferenceEquals(e, collection) ? "(this Collection)" : (isValueType ? e.ToString() : ToString(e)));
                    if (!it.MoveNext())
                    {
                        return sb.Append(']').ToString();
                    }
                    sb.Append(',').Append(' ');
                }
            }
        }

        /// <summary>
        /// This is the same implementation of ToString from Java's AbstractCollection
        /// (the default implementation for all sets and lists), plus the ability
        /// to specify culture for formatting of nested numbers and dates. Note that
        /// this overload will change the culture of the current thread.
        /// </summary>
        public static string ToString<T>(ICollection<T> collection, CultureInfo culture)
        {
            using (var context = new Support.CultureContext(culture))
            {
                return ToString(collection);
            }
        }

        /// <summary>
        /// This is the same implementation of ToString from Java's AbstractMap
        /// (the default implementation for all dictionaries)
        /// </summary>
        public static string ToString<TKey, TValue>(IDictionary<TKey, TValue> dictionary)
        {
            if (dictionary.Count == 0)
            {
                return "{}";
            }

            bool keyIsValueType = typeof(TKey).GetTypeInfo().IsValueType;
            bool valueIsValueType = typeof(TValue).GetTypeInfo().IsValueType;
            using (var i = dictionary.GetEnumerator())
            {
                StringBuilder sb = new StringBuilder();
                sb.Append('{');
                i.MoveNext();
                while (true)
                {
                    KeyValuePair<TKey, TValue> e = i.Current;
                    TKey key = e.Key;
                    TValue value = e.Value;
                    sb.Append(object.ReferenceEquals(key, dictionary) ? "(this Dictionary)" : (keyIsValueType ? key.ToString() : ToString(key)));
                    sb.Append('=');
                    sb.Append(object.ReferenceEquals(value, dictionary) ? "(this Dictionary)" : (valueIsValueType ? value.ToString() : ToString(value)));
                    if (!i.MoveNext())
                    {
                        return sb.Append('}').ToString();
                    }
                    sb.Append(',').Append(' ');
                }
            }
        }

        /// <summary>
        /// This is the same implementation of ToString from Java's AbstractMap
        /// (the default implementation for all dictionaries), plus the ability
        /// to specify culture for formatting of nested numbers and dates. Note that
        /// this overload will change the culture of the current thread.
        /// </summary>
        public static string ToString<TKey, TValue>(IDictionary<TKey, TValue> dictionary, CultureInfo culture)
        {
            using (var context = new Support.CultureContext(culture))
            {
                return ToString(dictionary);
            }
        }

        /// <summary>
        /// This is a helper method that assists with recursively building
        /// a string of the current collection and all nested collections.
        /// </summary>
        public static string ToString(object obj)
        {
            Type t = obj.GetType();
            if (t.GetTypeInfo().IsGenericType
                && (t.ImplementsGenericInterface(typeof(ICollection<>)))
                || t.ImplementsGenericInterface(typeof(IDictionary<,>)))
            {
                dynamic genericType = Convert.ChangeType(obj, t);
                return ToString(genericType);
            }

            return obj.ToString();
        }

        /// <summary>
        /// This is a helper method that assists with recursively building
        /// a string of the current collection and all nested collections, plus the ability
        /// to specify culture for formatting of nested numbers and dates. Note that
        /// this overload will change the culture of the current thread.
        /// </summary>
        public static string ToString(object obj, CultureInfo culture)
        {
            using (var context = new Support.CultureContext(culture))
            {
                return ToString(obj);
            }
        }

        #region Nested Types

        #region SetFromMap
        internal class SetFromMap<T> : ICollection<T>, IEnumerable<T>, IEnumerable, ISet<T>, IReadOnlyCollection<T>
#if FEATURE_SERIALIZABLE
            , ISerializable, IDeserializationCallback
#endif
        {
            private readonly IDictionary<T, bool?> m; // The backing map
#if FEATURE_SERIALIZABLE
            [NonSerialized]
#endif
            private ICollection<T> s;

            internal SetFromMap(IDictionary<T, bool?> map)
            {
                if (map.Any())
                    throw new ArgumentException("Map is not empty");
                m = map;
                s = map.Keys;
            }

            public void Clear()
            {
                m.Clear();
            }

            public int Count
            {
                get
                {
                    return m.Count;
                }
            }

            // LUCENENET: IsEmpty doesn't exist here

            public bool Contains(T item)
            {
                return m.ContainsKey(item);
            }

            public bool Remove(T item)
            {
                return m.Remove(item);
            }

            public bool Add(T item)
            {
                m.Add(item, true);
                return m.ContainsKey(item);
            }

            void ICollection<T>.Add(T item)
            {
                m.Add(item, true);
            }

            public IEnumerator<T> GetEnumerator()
            {
                return s.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return s.GetEnumerator();
            }

            // LUCENENET: ToArray() is part of LINQ

            public override string ToString()
            {
                return s.ToString();
            }

            public override int GetHashCode()
            {
                return s.GetHashCode();
            }

            public override bool Equals(object obj)
            {
                return obj == this || s.Equals(obj);
            }

            public virtual bool ContainsAll(IEnumerable<T> other)
            {
                // we don't care about order, so sort both sequences before comparing
                return this.OrderBy(x => x).SequenceEqual(other.OrderBy(x => x));
            }

            public void CopyTo(T[] array, int arrayIndex)
            {
                m.Keys.CopyTo(array, arrayIndex);
            }


            public bool IsReadOnly
            {
                get
                {
                    return false;
                }
            }

            public bool SetEquals(IEnumerable<T> other)
            {
                if (other == null)
                {
                    throw new ArgumentNullException("other");
                }
                SetFromMap<T> set = other as SetFromMap<T>;
                if (set != null)
                {
                    if (this.m.Count != set.Count)
                    {
                        return false;
                    }
                    return this.ContainsAll(set);
                }
                ICollection<T> is2 = other as ICollection<T>;
                if (((is2 != null) && (this.m.Count == 0)) && (is2.Count > 0))
                {
                    return false;
                }
                foreach (var item in this)
                {
                    if (!is2.Contains(item))
                    {
                        return false;
                    }
                }
                return true;
            }

            #region Not Implemented Members
            public void ExceptWith(IEnumerable<T> other)
            {
                throw new NotImplementedException();
            }

            public void IntersectWith(IEnumerable<T> other)
            {
                throw new NotImplementedException();
            }

            public bool IsProperSubsetOf(IEnumerable<T> other)
            {
                throw new NotImplementedException();
            }

            public bool IsProperSupersetOf(IEnumerable<T> other)
            {
                throw new NotImplementedException();
            }

            public bool IsSubsetOf(IEnumerable<T> other)
            {
                throw new NotImplementedException();
            }

            public bool IsSupersetOf(IEnumerable<T> other)
            {
                throw new NotImplementedException();
            }

            public bool Overlaps(IEnumerable<T> other)
            {
                throw new NotImplementedException();
            }

            public void SymmetricExceptWith(IEnumerable<T> other)
            {
                throw new NotImplementedException();
            }

            public void UnionWith(IEnumerable<T> other)
            {
                throw new NotImplementedException();
            }

#if FEATURE_SERIALIZABLE
            public void GetObjectData(SerializationInfo info, StreamingContext context)
            {
                throw new NotImplementedException();
            }
#endif

            public void OnDeserialization(object sender)
            {
                throw new NotImplementedException();
            }
            #endregion
        }
        #endregion SetFromMap

        #region ReverseComparer

        //private class ReverseComparer : IComparer<IComparable>
        //{
        //    internal static readonly ReverseComparer REVERSE_ORDER = new ReverseComparer();


        //    public int Compare(IComparable c1, IComparable c2)
        //    {
        //        return c2.CompareTo(c1);
        //    }
        //}

        // LUCENENET NOTE: When consolidating this, it turns out that only the 
        // CaseInsensitiveComparer works correctly in .NET (not sure why).
        // So, this hybrid was made from the original Java implementation and the
        // original implemenation (above) that used CaseInsensitiveComparer.
        private class ReverseComparer<T> : IComparer<T>
        {
            internal static readonly ReverseComparer<T> REVERSE_ORDER = new ReverseComparer<T>();

            public int Compare(T x, T y)
            {
                return (new CaseInsensitiveComparer()).Compare(y, x);
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
                Debug.Assert(cmp != null);
                this.cmp = cmp;
            }

            public int Compare(T t1, T t2)
            {
                return cmp.Compare(t2, t1);
            }

            public override bool Equals(object o)
            {
                return (o == this) ||
                    (o is ReverseComparer2<T> &&
                     cmp.Equals(((ReverseComparer2<T>)o).cmp));
            }

            public override int GetHashCode()
            {
                return cmp.GetHashCode() ^ int.MinValue;
            }

            public IComparer<T> Reversed()
            {
                return cmp;
            }
        }

        #endregion ReverseComparer2

        #region UnmodifiableListImpl

        private class UnmodifiableListImpl<T> : IList<T>
        {
            private readonly IList<T> list;

            public UnmodifiableListImpl(IList<T> list)
            {
                if (list == null)
                    throw new ArgumentNullException("list");
                this.list = list;
            }

            public T this[int index]
            {
                get
                {
                    return list[index];
                }
                set
                {
                    throw new InvalidOperationException("Unable to modify this list.");
                }
            }

            public int Count
            {
                get
                {
                    return list.Count;
                }
            }

            public bool IsReadOnly
            {
                get
                {
                    return true;
                }
            }

            public void Add(T item)
            {
                throw new InvalidOperationException("Unable to modify this list.");
            }

            public void Clear()
            {
                throw new InvalidOperationException("Unable to modify this list.");
            }

            public bool Contains(T item)
            {
                return list.Contains(item);
            }

            public void CopyTo(T[] array, int arrayIndex)
            {
                list.CopyTo(array, arrayIndex);
            }

            public IEnumerator<T> GetEnumerator()
            {
                return list.GetEnumerator();
            }

            public int IndexOf(T item)
            {
                return list.IndexOf(item);
            }

            public void Insert(int index, T item)
            {
                throw new InvalidOperationException("Unable to modify this list.");
            }

            public bool Remove(T item)
            {
                throw new InvalidOperationException("Unable to modify this list.");
            }

            public void RemoveAt(int index)
            {
                throw new InvalidOperationException("Unable to modify this list.");
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        #endregion UnmodifiableListImpl

        #region UnmodifiableDictionary

        private class UnmodifiableDictionary<TKey, TValue> : IDictionary<TKey, TValue>
        {
            private IDictionary<TKey, TValue> _dict;

            public UnmodifiableDictionary(IDictionary<TKey, TValue> dict)
            {
                _dict = dict;
            }

            public UnmodifiableDictionary()
            {
                _dict = new Dictionary<TKey, TValue>();
            }

            public void Add(TKey key, TValue value)
            {
                throw new InvalidOperationException("Unable to modify this dictionary.");
            }

            public bool ContainsKey(TKey key)
            {
                return _dict.ContainsKey(key);
            }

            public ICollection<TKey> Keys
            {
                get { return _dict.Keys; }
            }

            public bool Remove(TKey key)
            {
                throw new InvalidOperationException("Unable to modify this dictionary.");
            }

            public bool TryGetValue(TKey key, out TValue value)
            {
                return _dict.TryGetValue(key, out value);
            }

            public ICollection<TValue> Values
            {
                get { return _dict.Values; }
            }

            public TValue this[TKey key]
            {
                get
                {
                    TValue ret;
                    _dict.TryGetValue(key, out ret);
                    return ret;
                }
                set
                {
                    throw new InvalidOperationException("Unable to modify this dictionary.");
                }
            }

            public void Add(KeyValuePair<TKey, TValue> item)
            {
                throw new InvalidOperationException("Unable to modify this dictionary.");
            }

            public void Clear()
            {
                throw new InvalidOperationException("Unable to modify this dictionary.");
            }

            public bool Contains(KeyValuePair<TKey, TValue> item)
            {
                return _dict.Contains(item);
            }

            public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
            {
                _dict.CopyTo(array, arrayIndex);
            }

            public int Count
            {
                get { return _dict.Count; }
            }

            public bool IsReadOnly
            {
                get { return true; }
            }

            public bool Remove(KeyValuePair<TKey, TValue> item)
            {
                throw new InvalidOperationException("Unable to modify this dictionary.");
            }

            public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
            {
                return _dict.GetEnumerator();
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return _dict.GetEnumerator();
            }
        }

        #endregion UnmodifiableDictionary

        #region UnmodifiableSetImpl

        private class UnmodifiableSetImpl<T> : ISet<T>
        {
            private readonly ISet<T> set;
            public UnmodifiableSetImpl(ISet<T> set)
            {
                if (set == null)
                    throw new ArgumentNullException("set");
                this.set = set;
            }
            public int Count
            {
                get
                {
                    return set.Count;
                }
            }

            public bool IsReadOnly
            {
                get
                {
                    return true;
                }
            }

            public void Add(T item)
            {
                throw new InvalidOperationException("Unable to modify this set.");
            }

            public void Clear()
            {
                throw new InvalidOperationException("Unable to modify this set.");
            }

            public bool Contains(T item)
            {
                return set.Contains(item);
            }

            public void CopyTo(T[] array, int arrayIndex)
            {
                set.CopyTo(array, arrayIndex);
            }

            public void ExceptWith(IEnumerable<T> other)
            {
                throw new InvalidOperationException("Unable to modify this set.");
            }

            public IEnumerator<T> GetEnumerator()
            {
                return set.GetEnumerator();
            }

            public void IntersectWith(IEnumerable<T> other)
            {
                throw new InvalidOperationException("Unable to modify this set.");
            }

            public bool IsProperSubsetOf(IEnumerable<T> other)
            {
                return set.IsProperSubsetOf(other);
            }

            public bool IsProperSupersetOf(IEnumerable<T> other)
            {
                return set.IsProperSupersetOf(other);
            }

            public bool IsSubsetOf(IEnumerable<T> other)
            {
                return set.IsSubsetOf(other);
            }

            public bool IsSupersetOf(IEnumerable<T> other)
            {
                return set.IsSupersetOf(other);
            }

            public bool Overlaps(IEnumerable<T> other)
            {
                return set.Overlaps(other);
            }

            public bool Remove(T item)
            {
                throw new InvalidOperationException("Unable to modify this set.");
            }

            public bool SetEquals(IEnumerable<T> other)
            {
                return set.SetEquals(other);
            }

            public void SymmetricExceptWith(IEnumerable<T> other)
            {
                throw new InvalidOperationException("Unable to modify this set.");
            }

            public void UnionWith(IEnumerable<T> other)
            {
                throw new InvalidOperationException("Unable to modify this set.");
            }

            bool ISet<T>.Add(T item)
            {
                throw new InvalidOperationException("Unable to modify this set.");
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        #endregion

        #endregion Nested Types
    }
}
