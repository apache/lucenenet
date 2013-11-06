using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Facet.Collections
{
    public class IntToObjectMap<T> : IEnumerable<T>
    {
        private sealed class IndexIterator : IIntIterator
        {
            private int baseHashIndex = 0;
            private int index = 0;
            private int lastIndex = 0;
            private readonly IntToObjectMap<T> parent;

            public IndexIterator(IntToObjectMap<T> parent)
            {
                this.parent = parent;

                for (baseHashIndex = 0; baseHashIndex < parent.baseHash.Length; ++baseHashIndex)
                {
                    index = parent.baseHash[baseHashIndex];
                    if (index != 0)
                    {
                        break;
                    }
                }
            }

            public bool HasNext()
            {
                return (index != 0);
            }

            public int Next()
            {
                lastIndex = index;
                index = parent.next[index];
                while (index == 0 && ++baseHashIndex < parent.baseHash.Length)
                {
                    index = parent.baseHash[baseHashIndex];
                }

                return lastIndex;
            }

            public void Remove()
            {
                parent.Remove(parent.keys[lastIndex]);
            }
        }

        private sealed class KeyIterator : IIntIterator
        {
            private IIntIterator iterator; // = new IndexIterator();
            private readonly IntToObjectMap<T> parent;

            internal KeyIterator(IntToObjectMap<T> parent)
            {
                this.parent = parent;
                iterator = new IndexIterator(parent);
            }

            public bool HasNext()
            {
                return iterator.HasNext();
            }

            public int Next()
            {
                return parent.keys[iterator.Next()];
            }

            public void Remove()
            {
                iterator.Remove();
            }
        }

        private sealed class ValueIterator : IEnumerator<T>
        {
            private IIntIterator iterator; // = new IndexIterator();
            private readonly IntToObjectMap<T> parent;

            internal ValueIterator(IntToObjectMap<T> parent)
            {
                this.parent = parent;
                iterator = new IndexIterator(parent);
            }

            public bool MoveNext()
            {
                return iterator.HasNext();
            }

            public T Current
            {
                get
                {
                    return (T)parent.values[iterator.Next()];
                }
            }

            public void Remove()
            {
                iterator.Remove();
            }

            public void Dispose()
            {
            }

            object System.Collections.IEnumerator.Current
            {
                get { return Current; }
            }

            public void Reset()
            {
            }
        }

        private static int defaultCapacity = 16;
        int[] baseHash;
        private int capacity;
        private int firstEmpty;
        private int hashFactor;
        int[] keys;
        int[] next;
        private int prev;
        private int size;
        Object[] values;

        public IntToObjectMap()
            : this(defaultCapacity)
        {
        }

        public IntToObjectMap(int capacity)
        {
            this.capacity = 16;
            while (this.capacity < capacity)
            {
                this.capacity <<= 1;
            }

            int arrayLength = this.capacity + 1;
            this.values = new Object[arrayLength];
            this.keys = new int[arrayLength];
            this.next = new int[arrayLength];
            int baseHashSize = this.capacity << 1;
            this.baseHash = new int[baseHashSize];
            this.hashFactor = baseHashSize - 1;
            this.size = 0;
            Clear();
        }

        private void Prvt_put(int key, T e)
        {
            int hashIndex = CalcBaseHashIndex(key);
            int objectIndex = firstEmpty;
            firstEmpty = next[firstEmpty];
            values[objectIndex] = e;
            keys[objectIndex] = key;
            next[objectIndex] = baseHash[hashIndex];
            baseHash[hashIndex] = objectIndex;
            ++size;
        }

        protected virtual int CalcBaseHashIndex(int key)
        {
            return key & hashFactor;
        }

        public virtual void Clear()
        {
            Arrays.Fill(this.baseHash, 0);
            size = 0;
            firstEmpty = 1;
            for (int i = 1; i < this.capacity; )
            {
                next[i] = ++i;
            }

            next[this.capacity] = 0;
        }

        public virtual bool ContainsKey(int key)
        {
            return Find(key) != 0;
        }

        public virtual bool ContainsValue(Object o)
        {
            for (IEnumerator<T> iterator = GetEnumerator(); iterator.MoveNext(); )
            {
                T obj = iterator.Current;
                if (obj.Equals(o))
                {
                    return true;
                }
            }

            return false;
        }

        protected virtual int Find(int key)
        {
            int baseHashIndex = CalcBaseHashIndex(key);
            int localIndex = baseHash[baseHashIndex];
            while (localIndex != 0)
            {
                if (keys[localIndex] == key)
                {
                    return localIndex;
                }

                localIndex = next[localIndex];
            }

            return 0;
        }

        private int FindForRemove(int key, int baseHashIndex)
        {
            this.prev = 0;
            int index = baseHash[baseHashIndex];
            while (index != 0)
            {
                if (keys[index] == key)
                {
                    return index;
                }

                prev = index;
                index = next[index];
            }

            this.prev = 0;
            return 0;
        }

        public virtual T Get(int key)
        {
            return (T)values[Find(key)];
        }

        protected virtual void Grow()
        {
            IntToObjectMap<T> that = new IntToObjectMap<T>(this.capacity * 2);
            for (IndexIterator iterator = new IndexIterator(this); iterator.HasNext(); )
            {
                int index = iterator.Next();
                that.Prvt_put(this.keys[index], (T)this.values[index]);
            }

            this.capacity = that.capacity;
            this.size = that.size;
            this.firstEmpty = that.firstEmpty;
            this.values = that.values;
            this.keys = that.keys;
            this.next = that.next;
            this.baseHash = that.baseHash;
            this.hashFactor = that.hashFactor;
        }

        public virtual bool IsEmpty
        {
            get
            {
                return size == 0;
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            return new ValueIterator(this);
        }

        public virtual IIntIterator GetKeyIterator()
        {
            return new KeyIterator(this);
        }

        private string GetBaseHashAsString()
        {
            return Arrays.ToString(baseHash);
        }

        public virtual T Put(int key, T e)
        {
            int index = Find(key);
            if (index != 0)
            {
                T old = (T)values[index];
                values[index] = e;
                return old;
            }

            if (size == capacity)
            {
                Grow();
            }

            Prvt_put(key, e);
            return default(T);
        }

        public virtual T Remove(int key)
        {
            int baseHashIndex = CalcBaseHashIndex(key);
            int index = FindForRemove(key, baseHashIndex);
            if (index != 0)
            {
                if (prev == 0)
                {
                    baseHash[baseHashIndex] = next[index];
                }

                next[prev] = next[index];
                next[index] = firstEmpty;
                firstEmpty = index;
                --size;
                return (T)values[index];
            }

            return default(T);
        }

        public virtual int Size
        {
            get
            {
                return this.size;
            }
        }

        public virtual Object[] ToArray()
        {
            int j = -1;
            Object[] array = new Object[size];
            for (IEnumerator<T> iterator = GetEnumerator(); iterator.MoveNext(); )
            {
                array[++j] = iterator.Current;
            }

            return array;
        }

        public virtual T[] ToArray(T[] a)
        {
            int j = 0;
            for (IEnumerator<T> iterator = GetEnumerator(); j < a.Length && iterator.MoveNext(); ++j)
            {
                a[j] = iterator.Current;
            }

            if (j < a.Length)
            {
                a[j] = default(T);
            }

            return a;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append('{');
            IIntIterator keyIterator = GetKeyIterator();
            while (keyIterator.HasNext())
            {
                int key = keyIterator.Next();
                sb.Append(key);
                sb.Append('=');
                sb.Append(Get(key));
                if (keyIterator.HasNext())
                {
                    sb.Append(',');
                    sb.Append(' ');
                }
            }

            sb.Append('}');
            return sb.ToString();
        }

        public override int GetHashCode()
        {
            return GetType().GetHashCode() ^ Size;
        }

        public override bool Equals(Object o)
        {
            IntToObjectMap<T> that = (IntToObjectMap<T>)o;
            if (that.Size != this.Size)
            {
                return false;
            }

            IIntIterator it = GetKeyIterator();
            while (it.HasNext())
            {
                int key = it.Next();
                if (!that.ContainsKey(key))
                {
                    return false;
                }

                T v1 = this.Get(key);
                T v2 = that.Get(key);
                if ((v1 == null && v2 != null) || (v1 != null && v2 == null) || (!v1.Equals(v2)))
                {
                    return false;
                }
            }

            return true;
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
