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
    /// HashMap to store colliding labels. See <seealso cref="CompactLabelToOrdinal"/> for
    /// details.
    /// 
    /// @lucene.experimental
    /// </summary>
    public class CollisionMap
    {

        private int capacity_Renamed;
        private float loadFactor;
        private int size_Renamed;
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

        private CharBlockArray labelRepository;

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
            this.capacity_Renamed = CompactLabelToOrdinal.DetermineCapacity(2, initialCapacity);

            this.entries = new Entry[this.capacity_Renamed];
            this.threshold = (int)(this.capacity_Renamed * this.loadFactor);
        }

        /// <summary>
        /// How many mappings. </summary>
        public virtual int Size()
        {
            return this.size_Renamed;
        }

        /// <summary>
        /// How many slots are allocated. 
        /// </summary>
        public virtual int Capacity()
        {
            return this.capacity_Renamed;
        }

        private void Grow()
        {
            int newCapacity = this.capacity_Renamed * 2;
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

            this.capacity_Renamed = newCapacity;
            this.entries = newEntries;
            this.threshold = (int)(this.capacity_Renamed * this.loadFactor);
        }

        /// <summary>
        /// Return the mapping, or {@link
        ///  LabelToOrdinal#INVALID_ORDINAL} if the label isn't
        ///  recognized. 
        /// </summary>
        public virtual int Get(FacetLabel label, int hash)
        {
            int bucketIndex = IndexFor(hash, this.capacity_Renamed);
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

        /// <summary>
        /// Add another mapping. 
        /// </summary>
        public virtual int AddLabel(FacetLabel label, int hash, int cid)
        {
            int bucketIndex = IndexFor(hash, this.capacity_Renamed);
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
            int bucketIndex = IndexFor(hash, this.capacity_Renamed);
            AddEntry(offset, cid, hash, bucketIndex);
        }

        private void AddEntry(int offset, int cid, int hash, int bucketIndex)
        {
            Entry e = this.entries[bucketIndex];
            this.entries[bucketIndex] = new Entry(offset, cid, hash, e);
            if (this.size_Renamed++ >= this.threshold)
            {
                Grow();
            }
        }

        internal virtual IEnumerator<CollisionMap.Entry> entryIterator()
        {
            return new EntryIterator(this, entries, size_Renamed);
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
            private readonly CollisionMap outerInstance;

            internal Entry next_Renamed; // next entry to return
            internal int index; // current slot
            internal Entry[] ents;

            internal EntryIterator(CollisionMap outerInstance, Entry[] entries, int size)
            {
                this.outerInstance = outerInstance;
                this.ents = entries;
                Entry[] t = entries;
                int i = t.Length;
                Entry n = null;
                if (size != 0) // advance to first entry
                {
                    while (i > 0 && (n = t[--i]) == null)
                    {
                        // advance
                    }
                }
                this.next_Renamed = n;
                this.index = i;
            }

            public bool HasNext()
            {
                return this.next_Renamed != null;
            }

            public Entry Next()
            {
                Entry e = this.next_Renamed;
                if (e == null)
                {
                    throw new EntryNotFoundException();
                }

                Entry n = e.next;
                Entry[] t = ents;
                int i = this.index;
                while (n == null && i > 0)
                {
                    n = t[--i];
                }
                this.index = i;
                this.next_Renamed = n;
                return e;
            }

            public void Remove()
            {
                throw new System.NotSupportedException();
            }

            public void Dispose()
            {
            }

            public bool MoveNext()
            {
                if (!HasNext())
                    return false;
                Current = Next();
                return true;
            }

            public void Reset()
            {
                index = 0;
            }

            public Entry Current { get; private set; }

            object IEnumerator.Current
            {
                get { return Current; }
            }
        }

        public class EntryNotFoundException : Exception {
            public EntryNotFoundException() : base() { }
            public EntryNotFoundException(string message) : base(message) { }
            public EntryNotFoundException(string message, Exception innerException): base(message, innerException) { }
        }
    }

}