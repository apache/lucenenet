using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.Serialization;

namespace Lucene.Net
{
    public static class Collections
    {
        public static ISet<T> Singleton<T>(T o)
        {
            return ImmutableHashSet.Create(o);
        }

        public static IList<T> EmptyList<T>()
        {
            return ImmutableList<T>.Empty;
        }

        public static IList<T> UnmodifiableList<T>(IEnumerable<T> items)
        {
            return ImmutableList.Create<T>(items.ToArray());
        }

        public static IList<T> UnmodifiableList<T>(List<T> items)
        {
            return items.AsReadOnly();
        }

        public static ISet<T> UnmodifiableSet<T>(IEnumerable<T> items)
        {
            return ImmutableHashSet.Create<T>(items.ToArray());
        }

        public static IDictionary<T, TS> UnmodifiableMap<T, TS>(IDictionary<T, TS> d)
        {
            var builder = ImmutableDictionary.CreateBuilder<T, TS>();
            builder.AddRange(d);
            return builder.ToImmutable();
        }

        public static IDictionary<T, S> SingletonMap<T, S>(T key, S value)
        {
            return new Dictionary<T, S> {{key, value}};
        }

        public static ISet<T> NewSetFromMap<T, S>(IDictionary<T, bool?> map)
        {
            return new SetFromMap<T>(map);
        }

        internal class SetFromMap<T> : ICollection<T>, IEnumerable<T>, IEnumerable, ISerializable, IDeserializationCallback, ISet<T>, IReadOnlyCollection<T>
        {
            private readonly IDictionary<T, bool?> m; // The backing map
            [NonSerialized]
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

            public void GetObjectData(SerializationInfo info, StreamingContext context)
            {
                throw new NotImplementedException();
            }

            public void OnDeserialization(object sender)
            {
                throw new NotImplementedException();
            }
            #endregion
        }
    }
}
