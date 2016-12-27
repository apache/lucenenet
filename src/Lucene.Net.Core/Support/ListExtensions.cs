using System;
using System.Collections.Generic;

namespace Lucene.Net.Support
{
    public static class ListExtensions
    {
        public static void AddRange<T>(this IList<T> list, IEnumerable<T> values)
        {
            var lt = list as List<T>;

            if (lt != null)
                lt.AddRange(values);
            else
            {
                foreach (var item in values)
                {
                    lt.Add(item);
                }
            }
        }

        public static IList<T> SubList<T>(this IList<T> list, int fromIndex, int toIndex)
        {
            // .NET Port: This is to mimic Java's List.subList method, which has a different usage
            // than .NETs' List.GetRange. subList's parameters are indices, GetRange's parameters are a
            // starting index and a count. So we would need to do some light index math to translate this into
            // GetRange. This will be a safer extension method to use when translating java code
            // as there will be no question as to how to change it into GetRange. Also, subList returns
            // a list instance that, when modified, modifies the original list. So we're duplicating
            // that behavior as well.

            return new SubList<T>(list, fromIndex, toIndex);
        }

        public static IList<T> Swap<T>(this IList<T> list, int indexA, int indexB) // LUCENENET TODO: The swap is in-place. Returning the list makes this confusing.
        {
            T tmp = list[indexA];
            list[indexA] = list[indexB];
            list[indexB] = tmp;
            return list;
        }
    }

    public sealed class SubList<T> : IList<T>
    {
        private readonly IList<T> list;
        private readonly int fromIndex;
        private int toIndex;

        /// <summary>
        /// Creates a ranged view of the given <paramref name="list"/>.
        /// </summary>
        /// <param name="list">The original list to view.</param>
        /// <param name="fromIndex">The inclusive starting index.</param>
        /// <param name="toIndex">The exclusive ending index.</param>
        public SubList(IList<T> list, int fromIndex, int toIndex)
        {
            if (fromIndex < 0)
                throw new ArgumentOutOfRangeException("fromIndex");

            if (toIndex > list.Count)
                throw new ArgumentOutOfRangeException("toIndex");

            if (toIndex < fromIndex)
                throw new ArgumentOutOfRangeException("toIndex");

            if (list == null)
                throw new ArgumentNullException("list");

            this.list = list;
            this.fromIndex = fromIndex;
            this.toIndex = toIndex;
        }

        public int IndexOf(T item)
        {
            for (int i = fromIndex, fakeIndex = 0; i < toIndex; i++, fakeIndex++)
            {
                var current = list[i];

                if (current == null && item == null)
                    return fakeIndex;

                if (current.Equals(item))
                {
                    return fakeIndex;
                }
            }

            return -1;
        }

        public void Insert(int index, T item)
        {
            throw new NotSupportedException();
        }

        public void RemoveAt(int index)
        {
            // TODO: is this the right behavior?
            list.RemoveAt(fromIndex + index);
            toIndex--;
        }

        public T this[int index]
        {
            get
            {
                return list[fromIndex + index];
            }
            set
            {
                list[fromIndex + index] = value;
            }
        }

        public void Add(T item)
        {
            throw new NotSupportedException();
        }

        public void Clear()
        {
            // TODO: is this the correct behavior?

            for (int i = toIndex - 1; i >= fromIndex; i--)
            {
                list.RemoveAt(i);
            }

            toIndex = fromIndex; // can't move further
        }

        public bool Contains(T item)
        {
            return IndexOf(item) >= 0;
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            int count = array.Length - arrayIndex;

            for (int i = fromIndex, arrayi = arrayIndex; i <= Math.Min(toIndex - 1, fromIndex + count - 1); i++, arrayi++)
            {
                array[arrayi] = list[i];
            }
        }

        public int Count
        {
            get { return Math.Max(toIndex - fromIndex, 0); }
        }

        public bool IsReadOnly
        {
            get { return list.IsReadOnly; }
        }

        public bool Remove(T item)
        {
            var index = this.IndexOf(item); // get fake index

            if (index < 0)
                return false;

            list.RemoveAt(fromIndex + index);
            toIndex--;

            return true;
        }

        public IEnumerator<T> GetEnumerator()
        {
            return YieldItems().GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private IEnumerable<T> YieldItems()
        {
            for (int i = fromIndex; i <= Math.Min(toIndex - 1, list.Count - 1); i++)
            {
                yield return list[i];
            }
        }
    }
}