// Lucene version compatibility level 4.8.1
using System;
using System.Collections;
using System.Collections.Generic;

namespace Lucene.Net.Facet.Taxonomy.WriterCache
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
    /// HashMap to store colliding labels. See <see cref="CompactLabelToOrdinal"/> for
    /// details.
    /// 
    /// @lucene.experimental
    /// </summary>
    public class CollisionMap
    {
        private int capacity;
        private readonly float loadFactor;
        private int size;
        private int threshold;

        internal class Entry
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

        private readonly CharBlockArray labelRepository;

        private Entry[] entries;

        internal CollisionMap(CharBlockArray labelRepository)
            : this(16 * 1024, 0.75f, labelRepository)
        {
        }

        internal CollisionMap(int initialCapacity, CharBlockArray labelRepository)
            : this(initialCapacity, 0.75f, labelRepository)
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

        /// <summary>
        /// How many mappings.
        /// </summary>
        public virtual int Count => this.size;

        /// <summary>
        /// How many slots are allocated. 
        /// </summary>
        public virtual int Capacity => this.capacity;

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
                    } while (e != null);
                }
            }

            this.capacity = newCapacity;
            this.entries = newEntries;
            this.threshold = (int)(this.capacity * this.loadFactor);
        }

        /// <summary>
        /// Return the mapping, or <see cref="LabelToOrdinal.INVALID_ORDINAL"/> 
        /// if the label isn't recognized. 
        /// </summary>
        public virtual int Get(FacetLabel label, int hash)
        {
            int bucketIndex = IndexFor(hash, this.capacity);
            Entry e = this.entries[bucketIndex];

            while (e != null && !(hash == e.hash && CategoryPathUtils.EqualsToSerialized(label, labelRepository, e.offset)))
            {
                e = e.next;
            }
            if (e is null)
            {
                return LabelToOrdinal.INVALID_ORDINAL;
            }

            return e.cid;
        }

        /// <summary>
        /// Add another mapping. 
        /// </summary>
        public virtual int AddLabel(FacetLabel label, int hash, int cid)
        {
            int bucketIndex = IndexFor(hash, this.capacity);
            for (Entry e = this.entries[bucketIndex]; e != null; e = e.next)
            {
                if (e.hash == hash && CategoryPathUtils.EqualsToSerialized(label, labelRepository, e.offset))
                {
                    return e.cid;
                }
            }

            // new string; add to label repository
            int offset = labelRepository.Length;
            CategoryPathUtils.Serialize(label, labelRepository);
            AddEntry(offset, cid, hash, bucketIndex);
            return cid;
        }

        /// <summary>
        /// This method does not check if the same value is already in the map because
        /// we pass in an char-array offset, so so we now that we're in resize-mode
        /// here.
        /// </summary>
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

        internal virtual IEnumerator<CollisionMap.Entry> GetEnumerator()
        {
            return new EntryEnumerator(entries, size);
        }

        /// <summary>
        /// Returns index for hash code h. 
        /// </summary>
        internal static int IndexFor(int h, int length)
        {
            return h & (length - 1);
        }

        /// <summary>
        /// Returns an estimate of the memory usage of this CollisionMap. </summary>
        /// <returns> The approximate number of bytes used by this structure. </returns>
        internal virtual int GetMemoryUsage()
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

        private sealed class EntryEnumerator : IEnumerator<Entry> // LUCENENET: Marked sealed
        {
            internal Entry next; // next entry to return
            internal int index; // current slot
            internal Entry[] ents;

            internal EntryEnumerator(Entry[] entries, int size)
            {
                this.ents = entries;
                Entry[] t = entries;
                int i = t.Length;
                Entry n = null;
                if (size != 0) // advance to first entry
                {
                    while (i > 0 && (n = t[--i]) is null)
                    {
                        // advance
                    }
                }
                this.next = n;
                this.index = i;
            }

            private bool HasNext => this.next != null;

            public Entry Next()
            {
                Entry e = this.next;
                if (e is null)
                {
                    throw IllegalStateException.Create(this.GetType() + " cannot get next entry");
                }

                Entry n = e.next;
                Entry[] t = ents;
                int i = this.index;
                while (n is null && i > 0)
                {
                    n = t[--i];
                }
                this.index = i;
                this.next = n;
                return e;
            }

            // LUCENENET specific - .NET doesn't support Remove() anyway, so we can nix this
            //public void Remove()
            //{
            //    throw UnsupportedOperationException.Create();
            //}

            public void Dispose()
            {
                // LUCENENET: Intentionally blank
            }

            public bool MoveNext()
            {
                if (!HasNext)
                    return false;
                Current = Next();
                return true;
            }

            public void Reset()
            {
                index = 0;
            }

            public Entry Current { get; private set; }

            object IEnumerator.Current => Current;
        }
    }
}