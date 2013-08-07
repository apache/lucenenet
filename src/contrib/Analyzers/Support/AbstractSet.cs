using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Analysis.Support
{
    public abstract class AbstractSet<T> : ISet<T>
    {
        public virtual bool Add(T item)
        {
            return false;
        }

        public void ExceptWith(IEnumerable<T> other)
        {
            foreach (var item in other)
            {
                this.Remove(item);
            }
        }

        public void IntersectWith(IEnumerable<T> other)
        {
            var set = new HashSet<T>(other);

            foreach (var item in this.ToList())
            {
                if (!set.Contains(item))
                    this.Remove(item);
            }
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

        public bool SetEquals(IEnumerable<T> other)
        {
            throw new NotImplementedException();
        }

        public void SymmetricExceptWith(IEnumerable<T> other)
        {
            throw new NotImplementedException();
        }

        public void UnionWith(IEnumerable<T> other)
        {
            foreach (var item in other)
            {
                this.Add(item);
            }
        }

        void ICollection<T>.Add(T item)
        {
            Add(item);
        }

        public abstract void Clear();

        public abstract bool Contains(T item);

        public void CopyTo(T[] array, int arrayIndex)
        {
            var enumerator = GetEnumerator();

            for (int i = arrayIndex; i < array.Length; i++)
            {
                if (!enumerator.MoveNext())
                    break;

                array[i] = enumerator.Current;
            }
        }

        public abstract int Count { get; }

        public bool IsReadOnly
        {
            get { return false ; }
        }

        public abstract bool Remove(T item);

        public abstract IEnumerator<T> GetEnumerator();

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void AddAll(IEnumerable<T> values)
        {
            this.UnionWith(values);
        }
    }
}
