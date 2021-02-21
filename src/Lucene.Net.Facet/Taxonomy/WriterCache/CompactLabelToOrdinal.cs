// Lucene version compatibility level 4.8.1
using J2N.Numerics;
using System;
using System.IO;
using System.Runtime.Serialization;

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
    /// This is a very efficient <see cref="LabelToOrdinal"/> implementation that uses a
    /// <see cref="CharBlockArray"/> to store all labels and a configurable number of <see cref="HashArray"/>s to
    /// reference the labels.
    /// <para>
    /// Since the <see cref="HashArray"/>s don't handle collisions, a <see cref="CollisionMap"/> is used
    /// to store the colliding labels.
    /// </para>
    /// <para>
    /// This data structure grows by adding a new HashArray whenever the number of
    /// collisions in the <see cref="CollisionMap"/> exceeds <see cref="loadFactor"/>
    /// <c>GetMaxOrdinal().</c> Growing also includes reinserting all colliding
    /// labels into the <see cref="HashArray"/>s to possibly reduce the number of collisions.
    /// 
    /// For setting the <see cref="loadFactor"/> see 
    /// <see cref="CompactLabelToOrdinal(int, float, int)"/>. 
    /// </para>
    /// <para>
    /// This data structure has a much lower memory footprint (~30%) compared to a
    /// Java HashMap&lt;String, Integer&gt;. It also only uses a small fraction of objects
    /// a HashMap would use, thus limiting the GC overhead. Ingestion speed was also
    /// ~50% faster compared to a HashMap for 3M unique labels.
    /// 
    /// @lucene.experimental
    /// </para>
    /// </summary>
    public class CompactLabelToOrdinal : LabelToOrdinal
    {
        /// <summary>
        /// Default maximum load factor. </summary>
        public const float DefaultLoadFactor = 0.15f;

        public const char TERMINATOR_CHAR = (char)0xffff;
        private const int COLLISION = -5;

        private HashArray[] hashArrays;
        private CollisionMap collisionMap;
        private CharBlockArray labelRepository;

        private int capacity;
        private int threshold;
        private float loadFactor;

        /// <summary>
        /// How many labels. 
        /// </summary>
        public virtual int SizeOfMap => this.collisionMap.Count;

        private CompactLabelToOrdinal()
        {
        }

        /// <summary>
        /// Sole constructor.
        /// </summary>
        public CompactLabelToOrdinal(int initialCapacity, float loadFactor, int numHashArrays)
        {
            this.hashArrays = new HashArray[numHashArrays];

            this.capacity = DetermineCapacity((int)Math.Pow(2, numHashArrays), initialCapacity);
            Init();
            this.collisionMap = new CollisionMap(this.labelRepository);

            this.m_counter = 0;
            this.loadFactor = loadFactor;

            this.threshold = (int)(this.loadFactor * this.capacity);
        }

        internal static int DetermineCapacity(int minCapacity, int initialCapacity)
        {
            int capacity = minCapacity;
            while (capacity < initialCapacity)
            {
                capacity <<= 1;
            }
            return capacity;
        }

        private void Init()
        {
            labelRepository = new CharBlockArray();
            CategoryPathUtils.Serialize(new FacetLabel(), labelRepository);

            int c = this.capacity;
            for (int i = 0; i < this.hashArrays.Length; i++)
            {
                this.hashArrays[i] = new HashArray(c);
                c /= 2;
            }
        }

        public override void AddLabel(FacetLabel label, int ordinal)
        {
            if (collisionMap.Count > threshold)
            {
                Grow();
            }

            int hash = CompactLabelToOrdinal.StringHashCode(label);
            for (int i = 0; i < this.hashArrays.Length; i++)
            {
                if (AddLabel(this.hashArrays[i], label, hash, ordinal))
                {
                    return;
                }
            }

            int prevVal = collisionMap.AddLabel(label, hash, ordinal);
            if (prevVal != ordinal)
            {
                throw new ArgumentException("Label already exists: " + label + " prev ordinal " + prevVal);
            }
        }

        public override int GetOrdinal(FacetLabel label)
        {
            if (label is null)
            {
                return LabelToOrdinal.INVALID_ORDINAL;
            }

            int hash = CompactLabelToOrdinal.StringHashCode(label);
            for (int i = 0; i < this.hashArrays.Length; i++)
            {
                int ord = GetOrdinal(this.hashArrays[i], label, hash);
                if (ord != COLLISION)
                {
                    return ord;
                }
            }

            return this.collisionMap.Get(label, hash);
        }

        private void Grow()
        {
            HashArray temp = this.hashArrays[this.hashArrays.Length - 1];

            for (int i = this.hashArrays.Length - 1; i > 0; i--)
            {
                this.hashArrays[i] = this.hashArrays[i - 1];
            }

            this.capacity *= 2;
            this.hashArrays[0] = new HashArray(this.capacity);

            for (int i = 1; i < this.hashArrays.Length; i++)
            {
                int[] sourceOffsetArray = this.hashArrays[i].offsets;
                int[] sourceCidsArray = this.hashArrays[i].cids;

                for (int k = 0; k < sourceOffsetArray.Length; k++)
                {

                    for (int j = 0; j < i && sourceOffsetArray[k] != 0; j++)
                    {
                        int[] targetOffsetArray = this.hashArrays[j].offsets;
                        int[] targetCidsArray = this.hashArrays[j].cids;

                        int newIndex = IndexFor(StringHashCode(this.labelRepository, sourceOffsetArray[k]), targetOffsetArray.Length);
                        if (targetOffsetArray[newIndex] == 0)
                        {
                            targetOffsetArray[newIndex] = sourceOffsetArray[k];
                            targetCidsArray[newIndex] = sourceCidsArray[k];
                            sourceOffsetArray[k] = 0;
                        }
                    }
                }
            }

            for (int i = 0; i < temp.offsets.Length; i++)
            {
                int offset = temp.offsets[i];
                if (offset > 0)
                {
                    int hash = StringHashCode(this.labelRepository, offset);
                    AddLabelOffset(hash, temp.cids[i], offset);
                }
            }

            CollisionMap oldCollisionMap = this.collisionMap;
            this.collisionMap = new CollisionMap(oldCollisionMap.Capacity, this.labelRepository);
            this.threshold = (int)(this.capacity * this.loadFactor);

            using var it = oldCollisionMap.GetEnumerator();
            while (it.MoveNext())
            {
                var e = it.Current;
                AddLabelOffset(StringHashCode(this.labelRepository, e.offset), e.cid, e.offset);
            }
        }

        private bool AddLabel(HashArray a, FacetLabel label, int hash, int ordinal)
        {
            int index = CompactLabelToOrdinal.IndexFor(hash, a.offsets.Length);
            int offset = a.offsets[index];

            if (offset == 0)
            {
                a.offsets[index] = this.labelRepository.Length;
                CategoryPathUtils.Serialize(label, labelRepository);
                a.cids[index] = ordinal;
                return true;
            }

            return false;
        }

        private void AddLabelOffset(int hash, int cid, int knownOffset)
        {
            for (int i = 0; i < this.hashArrays.Length; i++)
            {
                if (AddLabelOffsetToHashArray(this.hashArrays[i], hash, cid, knownOffset))
                {
                    return;
                }
            }

            this.collisionMap.AddLabelOffset(hash, knownOffset, cid);

            if (this.collisionMap.Count > this.threshold)
            {
                Grow();
            }
        }

        private static bool AddLabelOffsetToHashArray(HashArray a, int hash, int ordinal, int knownOffset) // LUCENENET: CA1822: Mark members as static
        {
            int index = CompactLabelToOrdinal.IndexFor(hash, a.offsets.Length);
            int offset = a.offsets[index];

            if (offset == 0)
            {
                a.offsets[index] = knownOffset;
                a.cids[index] = ordinal;
                return true;
            }

            return false;
        }

        private int GetOrdinal(HashArray a, FacetLabel label, int hash)
        {
            if (label is null)
            {
                return LabelToOrdinal.INVALID_ORDINAL;
            }

            int index = IndexFor(hash, a.offsets.Length);
            int offset = a.offsets[index];
            if (offset == 0)
            {
                return LabelToOrdinal.INVALID_ORDINAL;
            }

            if (CategoryPathUtils.EqualsToSerialized(label, labelRepository, offset))
            {
                return a.cids[index];
            }

            return COLLISION;
        }

        /// <summary>
        /// Returns index for hash code h.
        /// </summary>
        internal static int IndexFor(int h, int length)
        {
            return h & (length - 1);
        }

        // static int stringHashCode(String label) {
        // int len = label.length();
        // int hash = 0;
        // int i;
        // for (i = 0; i < len; ++i)
        // hash = 33 * hash + label.charAt(i);
        //
        // hash = hash ^ ((hash >>> 20) ^ (hash >>> 12));
        // hash = hash ^ (hash >>> 7) ^ (hash >>> 4);
        //
        // return hash;
        //
        // }

        internal static int StringHashCode(FacetLabel label)
        {
            int hash = label.GetHashCode();

            hash = hash ^ hash.TripleShift(20) ^ hash.TripleShift(12);
            hash = hash ^ hash.TripleShift(7) ^ hash.TripleShift(4);

            return hash;

        }

        internal static int StringHashCode(CharBlockArray labelRepository, int offset)
        {
            int hash = CategoryPathUtils.HashCodeOfSerialized(labelRepository, offset);
            hash = hash ^ hash.TripleShift(20) ^ hash.TripleShift(12);
            hash = hash ^ hash.TripleShift(7) ^ hash.TripleShift(4);
            return hash;
        }

        // public static boolean equals(CharSequence label, CharBlockArray array,
        // int offset) {
        // // CONTINUE HERE
        // int len = label.length();
        // int bi = array.blockIndex(offset);
        // CharBlockArray.Block b = array.blocks.get(bi);
        // int index = array.indexInBlock(offset);
        //
        // for (int i = 0; i < len; i++) {
        // if (label.charAt(i) != b.chars[index]) {
        // return false;
        // }
        // index++;
        // if (index == b.length) {
        // b = array.blocks.get(++bi);
        // index = 0;
        // }
        // }
        //
        // return b.chars[index] == TerminatorChar;
        // }

        /// <summary>
        /// Returns an estimate of the amount of memory used by this table. Called only in
        /// this package. Memory is consumed mainly by three structures: the hash arrays,
        /// label repository and collision map.
        /// </summary>
        internal virtual int GetMemoryUsage()
        {
            int memoryUsage = 0;
            if (this.hashArrays != null)
            {
                // HashArray capacity is instance-specific.
                foreach (HashArray ha in this.hashArrays)
                {
                    // Each has 2 capacity-length arrays of ints.
                    memoryUsage += (ha.capacity * 2 * 4) + 4;
                }
            }
            if (this.labelRepository != null)
            {
                // All blocks are the same size.
                int blockSize = this.labelRepository.blockSize;
                // Each block has room for blockSize UTF-16 chars.
                int actualBlockSize = (blockSize * 2) + 4;
                memoryUsage += this.labelRepository.blocks.Count * actualBlockSize;
                memoryUsage += 8; // Two int values for array as a whole.
            }
            if (this.collisionMap != null)
            {
                memoryUsage += this.collisionMap.GetMemoryUsage();
            }
            return memoryUsage;
        }

        /// <summary>
        /// Opens the file and reloads the CompactLabelToOrdinal. The file it expects
        /// is generated from the <see cref="Flush(Stream)"/> command.
        /// </summary>
        internal static CompactLabelToOrdinal Open(FileInfo file, float loadFactor, int numHashArrays)
        {
            // Part of the file is the labelRepository, which needs to be rehashed
            // and label offsets re-added to the object. I am unsure as to why we
            // can't just store these off in the file as well, but in keeping with
            // the spirit of the original code, I did it this way. (ssuppe)
            CompactLabelToOrdinal l2o = new CompactLabelToOrdinal
            {
                loadFactor = loadFactor,
                hashArrays = new HashArray[numHashArrays]
            };

            BinaryReader dis = null;
            try
            {
                dis = new BinaryReader(new FileStream(file.FullName, FileMode.Open, FileAccess.Read));

                // TaxiReader needs to load the "counter" or occupancy (L2O) to know
                // the next unique facet. we used to load the delimiter too, but
                // never used it.
                l2o.m_counter = dis.ReadInt32();

                l2o.capacity = DetermineCapacity((int)Math.Pow(2, l2o.hashArrays.Length), l2o.m_counter);
                l2o.Init();

                // now read the chars
                l2o.labelRepository = CharBlockArray.Open(dis.BaseStream);

                l2o.collisionMap = new CollisionMap(l2o.labelRepository);

                // Calculate hash on the fly based on how CategoryPath hashes
                // itself. Maybe in the future we can call some static based methods
                // in CategoryPath so that this doesn't break again? I don't like
                // having code in two different places...
                int cid = 0;
                // Skip the initial offset, it's the CategoryPath(0,0), which isn't
                // a hashed value.
                int offset = 1;
                int lastStartOffset = offset;
                // This loop really relies on a well-formed input (assumes pretty blindly
                // that array offsets will work).  Since the initial file is machine 
                // generated, I think this should be OK.
                while (offset < l2o.labelRepository.Length)
                {
                    // identical code to CategoryPath.hashFromSerialized. since we need to
                    // advance offset, we cannot call the method directly. perhaps if we
                    // could pass a mutable Integer or something...
                    int length = (ushort)l2o.labelRepository[offset++];
                    int hash = length;
                    if (length != 0)
                    {
                        for (int i = 0; i < length; i++)
                        {
                            int len = (ushort)l2o.labelRepository[offset++];
                            hash = hash * 31 + l2o.labelRepository.Subsequence(offset, len).GetHashCode(); // LUCENENET: Corrected 2nd Subsequence parameter
                            offset += len;
                        }
                    }
                    // Now that we've hashed the components of the label, do the
                    // final part of the hash algorithm.
                    hash = hash ^ hash.TripleShift(20) ^ hash.TripleShift(12);
                    hash = hash ^ hash.TripleShift(7) ^ hash.TripleShift(4);
                    // Add the label, and let's keep going
                    l2o.AddLabelOffset(hash, cid, lastStartOffset);
                    cid++;
                    lastStartOffset = offset;
                }

            }
            catch (SerializationException se)
            {
                throw new IOException("Invalid file format. Cannot deserialize.", se);
            }
            finally
            {
                if (dis != null)
                {
                    dis.Dispose();
                }
            }

            l2o.threshold = (int)(l2o.loadFactor * l2o.capacity);
            return l2o;

        }

        internal virtual void Flush(Stream stream)
        {
            using BinaryWriter dos = new BinaryWriter(stream);
            dos.Write(this.m_counter);

            // write the labelRepository
            this.labelRepository.Flush(dos.BaseStream);
        }

        private sealed class HashArray
        {
            internal int[] offsets;
            internal int[] cids;

            internal int capacity;

            internal HashArray(int c)
            {
                this.capacity = c;
                this.offsets = new int[this.capacity];
                this.cids = new int[this.capacity];
            }
        }
    }
}