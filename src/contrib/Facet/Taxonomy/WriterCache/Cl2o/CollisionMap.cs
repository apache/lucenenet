using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Facet.Taxonomy.WriterCache.Cl2o
{
    public class CollisionMap : IEnumerable<CollisionMap.Entry>
    {
        private int capacity;
        private float loadFactor;
        private int size;
        private int threshold;

        public class Entry
        {
            internal int offset;
            internal int cid;
            internal Entry next;
            internal int hash;

            internal Entry(int offset, int cid, int h, Entry e)
            {
                this.offset = offset;
                this.cid = cid;
                this.next = e;
                this.hash = h;
            }
        }

        private CharBlockArray labelRepository;
        private Entry[] entries;

        internal CollisionMap(CharBlockArray labelRepository)
            : this(16 * 1024, 0.75F, labelRepository)
        {
        }

        internal CollisionMap(int initialCapacity, CharBlockArray labelRepository)
            : this(initialCapacity, 0.75F, labelRepository)
        {
        }

        private CollisionMap(int initialCapacity, float loadFactor, CharBlockArray labelRepository)
        {
            this.labelRepository = labelRepository;
            this.loadFactor = loadFactor;
            this.capacity = CompactLabelToOrdinal.DetermineCapacity(2, initialCapacity);
            this.entries = new Entry[this.capacity];
            this.threshold = (int)(this.capacity * this.loadFactor);
        }

        public virtual int Size
        {
            get
            {
                return this.size;
            }
        }

        public virtual int Capacity
        {
            get
            {
                return this.capacity;
            }
        }

        private void Grow()
        {
            int newCapacity = this.capacity * 2;
            Entry[] newEntries = new Entry[newCapacity];
            Entry[] src = this.entries;
            for (int j = 0; j < src.Length; j++)
            {
                Entry e = src[j];
                if (e != null)
                {
                    src[j] = null;
                    do
                    {
                        Entry next = e.next;
                        int hash = e.hash;
                        int i = IndexFor(hash, newCapacity);
                        e.next = newEntries[i];
                        newEntries[i] = e;
                        e = next;
                    }
                    while (e != null);
                }
            }

            this.capacity = newCapacity;
            this.entries = newEntries;
            this.threshold = (int)(this.capacity * this.loadFactor);
        }

        public virtual int Get(CategoryPath label, int hash)
        {
            int bucketIndex = IndexFor(hash, this.capacity);
            Entry e = this.entries[bucketIndex];
            while (e != null && !(hash == e.hash && CategoryPathUtils.EqualsToSerialized(label, labelRepository, e.offset)))
            {
                e = e.next;
            }

            if (e == null)
            {
                return LabelToOrdinal.INVALID_ORDINAL;
            }

            return e.cid;
        }

        public virtual int AddLabel(CategoryPath label, int hash, int cid)
        {
            int bucketIndex = IndexFor(hash, this.capacity);
            for (Entry e = this.entries[bucketIndex]; e != null; e = e.next)
            {
                if (e.hash == hash && CategoryPathUtils.EqualsToSerialized(label, labelRepository, e.offset))
                {
                    return e.cid;
                }
            }

            int offset = labelRepository.Length;
            CategoryPathUtils.Serialize(label, labelRepository);
            AddEntry(offset, cid, hash, bucketIndex);
            return cid;
        }

        public virtual void AddLabelOffset(int hash, int offset, int cid)
        {
            int bucketIndex = IndexFor(hash, this.capacity);
            AddEntry(offset, cid, hash, bucketIndex);
        }

        private void AddEntry(int offset, int cid, int hash, int bucketIndex)
        {
            Entry e = this.entries[bucketIndex];
            this.entries[bucketIndex] = new Entry(offset, cid, hash, e);
            if (this.size++ >= this.threshold)
            {
                Grow();
            }
        }

        public virtual IEnumerator<CollisionMap.Entry> GetEnumerator()
        {
            return new EntryIterator(entries, size);
        }

        internal static int IndexFor(int h, int length)
        {
            return h & (length - 1);
        }

        internal virtual int MemoryUsage
        {
            get
            {
                int memoryUsage = 0;
                if (this.entries != null)
                {
                    foreach (Entry e in this.entries)
                    {
                        if (e != null)
                        {
                            memoryUsage += (4 * 4);
                            for (Entry ee = e.next; ee != null; ee = ee.next)
                            {
                                memoryUsage += (4 * 4);
                            }
                        }
                    }
                }

                return memoryUsage;
            }
        }

        private class EntryIterator : IEnumerator<Entry>
        {
            Entry next;
            int index;
            Entry[] ents;

            internal EntryIterator(Entry[] entries, int size)
            {
                this.ents = entries;
                Entry[] t = entries;
                int i = t.Length;
                Entry n = null;
                if (size != 0)
                {
                    while (i > 0 && (n = t[--i]) == null)
                    {
                    }
                }

                this.next = n;
                this.index = i;
            }

            public bool MoveNext()
            {
                return this.next != null;
            }

            public Entry Current
            {
                get
                {
                    Entry e = this.next;
                    if (e == null)
                        throw new InvalidOperationException();
                    Entry n = e.next;
                    Entry[] t = ents;
                    int i = this.index;
                    while (n == null && i > 0)
                    {
                        n = t[--i];
                    }

                    this.index = i;
                    this.next = n;
                    return e;
                }
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

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
