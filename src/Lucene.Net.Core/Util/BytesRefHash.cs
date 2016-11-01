using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Lucene.Net.Util
{
    using Lucene.Net.Support;

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

    using DirectAllocator = Lucene.Net.Util.ByteBlockPool.DirectAllocator;

    /// <summary>
    /// <seealso cref="BytesRefHash"/> is a special purpose hash-map like data-structure
    /// optimized for <seealso cref="BytesRef"/> instances. BytesRefHash maintains mappings of
    /// byte arrays to ids (Map&lt;BytesRef,int&gt;) storing the hashed bytes
    /// efficiently in continuous storage. The mapping to the id is
    /// encapsulated inside <seealso cref="BytesRefHash"/> and is guaranteed to be increased
    /// for each added <seealso cref="BytesRef"/>.
    ///
    /// <p>
    /// Note: The maximum capacity <seealso cref="BytesRef"/> instance passed to
    /// <seealso cref="#add(BytesRef)"/> must not be longer than <seealso cref="ByteBlockPool#BYTE_BLOCK_SIZE"/>-2.
    /// The internal storage is limited to 2GB total byte storage.
    /// </p>
    ///
    /// @lucene.internal
    /// </summary>
    public sealed class BytesRefHash
    {
        public const int DEFAULT_CAPACITY = 16;

        // the following fields are needed by comparator,
        // so package private to prevent access$-methods:
        internal readonly ByteBlockPool Pool;

        internal int[] BytesStart;

        private readonly BytesRef Scratch1 = new BytesRef();
        private int HashSize;
        private int HashHalfSize;
        private int HashMask;
        private int Count;
        private int LastCount = -1;
        private int[] Ids;
        private readonly BytesStartArray bytesStartArray;
        private Counter BytesUsed;

        /// <summary>
        /// Creates a new <seealso cref="BytesRefHash"/> with a <seealso cref="ByteBlockPool"/> using a
        /// <seealso cref="DirectAllocator"/>.
        /// </summary>
        public BytesRefHash()
            : this(new ByteBlockPool(new DirectAllocator()))
        {
        }

        /// <summary>
        /// Creates a new <seealso cref="BytesRefHash"/>
        /// </summary>
        public BytesRefHash(ByteBlockPool pool)
            : this(pool, DEFAULT_CAPACITY, new DirectBytesStartArray(DEFAULT_CAPACITY))
        {
        }

        /// <summary>
        /// Creates a new <seealso cref="BytesRefHash"/>
        /// </summary>
        public BytesRefHash(ByteBlockPool pool, int capacity, BytesStartArray bytesStartArray)
        {
            HashSize = capacity;
            HashHalfSize = HashSize >> 1;
            HashMask = HashSize - 1;
            this.Pool = pool;
            Ids = new int[HashSize];
            Arrays.Fill(Ids, -1);
            this.bytesStartArray = bytesStartArray;
            BytesStart = bytesStartArray.Init();
            BytesUsed = bytesStartArray.BytesUsed() == null ? Counter.NewCounter() : bytesStartArray.BytesUsed();
            BytesUsed.AddAndGet(HashSize * RamUsageEstimator.NUM_BYTES_INT);
        }

        /// <summary>
        /// Returns the number of <seealso cref="BytesRef"/> values in this <seealso cref="BytesRefHash"/>.
        /// </summary>
        /// <returns> the number of <seealso cref="BytesRef"/> values in this <seealso cref="BytesRefHash"/>. </returns>
        public int Size()
        {
            return Count;
        }

        /// <summary>
        /// Populates and returns a <seealso cref="BytesRef"/> with the bytes for the given
        /// bytesID.
        /// <p>
        /// Note: the given bytesID must be a positive integer less than the current
        /// size (<seealso cref="#size()"/>)
        /// </summary>
        /// <param name="bytesID">
        ///          the id </param>
        /// <param name="ref">
        ///          the <seealso cref="BytesRef"/> to populate
        /// </param>
        /// <returns> the given BytesRef instance populated with the bytes for the given
        ///         bytesID </returns>
        public BytesRef Get(int bytesID, BytesRef @ref)
        {
            Debug.Assert(BytesStart != null, "bytesStart is null - not initialized");
            Debug.Assert(bytesID < BytesStart.Length, "bytesID exceeds byteStart len: " + BytesStart.Length);
            Pool.SetBytesRef(@ref, BytesStart[bytesID]);
            return @ref;
        }

        /// <summary>
        /// Returns the ids array in arbitrary order. Valid ids start at offset of 0
        /// and end at a limit of <seealso cref="#size()"/> - 1
        /// <p>
        /// Note: this is a destructive operation. <seealso cref="#clear()"/> must be called in
        /// order to reuse this <seealso cref="BytesRefHash"/> instance.
        /// </p>
        /// </summary>
        public int[] Compact()
        {
            Debug.Assert(BytesStart != null, "bytesStart is null - not initialized");
            int upto = 0;
            for (int i = 0; i < HashSize; i++)
            {
                if (Ids[i] != -1)
                {
                    if (upto < i)
                    {
                        Ids[upto] = Ids[i];
                        Ids[i] = -1;
                    }
                    upto++;
                }
            }

            Debug.Assert(upto == Count);
            LastCount = Count;
            return Ids;
        }

        /// <summary>
        /// Returns the values array sorted by the referenced byte values.
        /// <p>
        /// Note: this is a destructive operation. <seealso cref="#clear()"/> must be called in
        /// order to reuse this <seealso cref="BytesRefHash"/> instance.
        /// </p>
        /// </summary>
        /// <param name="comp">
        ///          the <seealso cref="Comparator"/> used for sorting </param>
        public int[] Sort(IComparer<BytesRef> comp)
        {
            int[] compact = Compact();
            new IntroSorterAnonymousInnerClassHelper(this, comp, compact).Sort(0, Count);
            return compact;
        }

        private class IntroSorterAnonymousInnerClassHelper : IntroSorter
        {
            private BytesRefHash OuterInstance;

            private IComparer<BytesRef> Comp;
            private int[] Compact;
            private readonly BytesRef pivot = new BytesRef(), scratch1 = new BytesRef(), scratch2 = new BytesRef();

            public IntroSorterAnonymousInnerClassHelper(BytesRefHash outerInstance, IComparer<BytesRef> comp, int[] compact)
            {
                this.OuterInstance = outerInstance;
                this.Comp = comp;
                this.Compact = compact;
            }

            protected override void Swap(int i, int j)
            {
                int o = Compact[i];
                Compact[i] = Compact[j];
                Compact[j] = o;
            }

            protected override int Compare(int i, int j)
            {
                int id1 = Compact[i], id2 = Compact[j];
                Debug.Assert(OuterInstance.BytesStart.Length > id1 && OuterInstance.BytesStart.Length > id2);
                OuterInstance.Pool.SetBytesRef(OuterInstance.Scratch1, OuterInstance.BytesStart[id1]);
                OuterInstance.Pool.SetBytesRef(scratch2, OuterInstance.BytesStart[id2]);
                return Comp.Compare(OuterInstance.Scratch1, scratch2);
            }

            protected internal override int Pivot
            {
                set
                {
                    int id = Compact[value];
                    Debug.Assert(OuterInstance.BytesStart.Length > id);
                    OuterInstance.Pool.SetBytesRef(pivot, OuterInstance.BytesStart[id]);
                }
            }

            protected internal override int ComparePivot(int j)
            {
                int id = Compact[j];
                Debug.Assert(OuterInstance.BytesStart.Length > id);
                OuterInstance.Pool.SetBytesRef(scratch2, OuterInstance.BytesStart[id]);
                return Comp.Compare(pivot, scratch2);
            }
        }

        private bool Equals(int id, BytesRef b)
        {
            Pool.SetBytesRef(Scratch1, BytesStart[id]);
            return Scratch1.BytesEquals(b);
        }

        private bool Shrink(int targetSize)
        {
            // Cannot use ArrayUtil.shrink because we require power
            // of 2:
            int newSize = HashSize;
            while (newSize >= 8 && newSize / 4 > targetSize)
            {
                newSize /= 2;
            }
            if (newSize != HashSize)
            {
                BytesUsed.AddAndGet(RamUsageEstimator.NUM_BYTES_INT * -(HashSize - newSize));
                HashSize = newSize;
                Ids = new int[HashSize];
                Arrays.Fill(Ids, -1);
                HashHalfSize = newSize / 2;
                HashMask = newSize - 1;
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Clears the <seealso cref="BytesRef"/> which maps to the given <seealso cref="BytesRef"/>
        /// </summary>
        public void Clear(bool resetPool)
        {
            LastCount = Count;
            Count = 0;
            if (resetPool)
            {
                Pool.Reset(false, false); // we don't need to 0-fill the buffers
            }
            BytesStart = bytesStartArray.Clear();
            if (LastCount != -1 && Shrink(LastCount))
            {
                // shrink clears the hash entries
                return;
            }
            Arrays.Fill(Ids, -1);
        }

        public void Clear()
        {
            Clear(true);
        }

        /// <summary>
        /// Closes the BytesRefHash and releases all internally used memory
        /// </summary>
        public void Close()
        {
            Clear(true);
            Ids = null;
            BytesUsed.AddAndGet(RamUsageEstimator.NUM_BYTES_INT * -HashSize);
        }

        /// <summary>
        /// Adds a new <seealso cref="BytesRef"/>
        /// </summary>
        /// <param name="bytes">
        ///          the bytes to hash </param>
        /// <returns> the id the given bytes are hashed if there was no mapping for the
        ///         given bytes, otherwise <code>(-(id)-1)</code>. this guarantees
        ///         that the return value will always be &gt;= 0 if the given bytes
        ///         haven't been hashed before.
        /// </returns>
        /// <exception cref="MaxBytesLengthExceededException">
        ///           if the given bytes are > 2 +
        ///           <seealso cref="ByteBlockPool#BYTE_BLOCK_SIZE"/> </exception>
        public int Add(BytesRef bytes)
        {
            Debug.Assert(BytesStart != null, "Bytesstart is null - not initialized");
            int length = bytes.Length;
            // final position
            int hashPos = FindHash(bytes);
            int e = Ids[hashPos];

            if (e == -1)
            {
                // new entry
                int len2 = 2 + bytes.Length;
                if (len2 + Pool.ByteUpto > ByteBlockPool.BYTE_BLOCK_SIZE)
                {
                    if (len2 > ByteBlockPool.BYTE_BLOCK_SIZE)
                    {
                        throw new MaxBytesLengthExceededException("bytes can be at most " + (ByteBlockPool.BYTE_BLOCK_SIZE - 2) + " in length; got " + bytes.Length);
                    }
                    Pool.NextBuffer();
                }
                var buffer = Pool.Buffer;
                int bufferUpto = Pool.ByteUpto;
                if (Count >= BytesStart.Length)
                {
                    BytesStart = bytesStartArray.Grow();
                    Debug.Assert(Count < BytesStart.Length + 1, "count: " + Count + " len: " + BytesStart.Length);
                }
                e = Count++;

                BytesStart[e] = bufferUpto + Pool.ByteOffset;

                // We first encode the length, followed by the
                // bytes. Length is encoded as vInt, but will consume
                // 1 or 2 bytes at most (we reject too-long terms,
                // above).
                if (length < 128)
                {
                    // 1 byte to store length
                    buffer[bufferUpto] = (byte)length;
                    Pool.ByteUpto += length + 1;
                    Debug.Assert(length >= 0, "Length must be positive: " + length);
                    System.Buffer.BlockCopy(bytes.Bytes, bytes.Offset, buffer, bufferUpto + 1, length);
                }
                else
                {
                    // 2 byte to store length
                    buffer[bufferUpto] = unchecked((byte)(0x80 | (length & 0x7f)));
                    buffer[bufferUpto + 1] = unchecked((byte)((length >> 7) & 0xff));
                    Pool.ByteUpto += length + 2;
                    System.Buffer.BlockCopy(bytes.Bytes, bytes.Offset, buffer, bufferUpto + 2, length);
                }
                Debug.Assert(Ids[hashPos] == -1);
                Ids[hashPos] = e;

                if (Count == HashHalfSize)
                {
                    Rehash(2 * HashSize, true);
                }
                return e;
            }
            return -(e + 1);
        }

        /// <summary>
        /// Returns the id of the given <seealso cref="BytesRef"/>.
        /// </summary>
        /// <param name="bytes">
        ///          the bytes to look for
        /// </param>
        /// <returns> the id of the given bytes, or {@code -1} if there is no mapping for the
        ///         given bytes. </returns>
        public int Find(BytesRef bytes)
        {
            return Ids[FindHash(bytes)];
        }

        private int FindHash(BytesRef bytes)
        {
            Debug.Assert(BytesStart != null, "bytesStart is null - not initialized");

            int code = DoHash(bytes.Bytes, bytes.Offset, bytes.Length);

            // final position
            int hashPos = code & HashMask;
            int e = Ids[hashPos];
            if (e != -1 && !Equals(e, bytes))
            {
                // Conflict; use linear probe to find an open slot
                // (see LUCENE-5604):
                do
                {
                    code++;
                    hashPos = code & HashMask;
                    e = Ids[hashPos];
                } while (e != -1 && !Equals(e, bytes));
            }

            return hashPos;
        }

        /// <summary>
        /// Adds a "arbitrary" int offset instead of a BytesRef
        ///  term.  this is used in the indexer to hold the hash for term
        ///  vectors, because they do not redundantly store the byte[] term
        ///  directly and instead reference the byte[] term
        ///  already stored by the postings BytesRefHash.  See
        ///  add(int textStart) in TermsHashPerField.
        /// </summary>
        public int AddByPoolOffset(int offset)
        {
            Debug.Assert(BytesStart != null, "Bytesstart is null - not initialized");
            // final position
            int code = offset;
            int hashPos = offset & HashMask;
            int e = Ids[hashPos];
            if (e != -1 && BytesStart[e] != offset)
            {
                // Conflict; use linear probe to find an open slot
                // (see LUCENE-5604):
                do
                {
                    code++;
                    hashPos = code & HashMask;
                    e = Ids[hashPos];
                } while (e != -1 && BytesStart[e] != offset);
            }
            if (e == -1)
            {
                // new entry
                if (Count >= BytesStart.Length)
                {
                    BytesStart = bytesStartArray.Grow();
                    Debug.Assert(Count < BytesStart.Length + 1, "count: " + Count + " len: " + BytesStart.Length);
                }
                e = Count++;
                BytesStart[e] = offset;
                Debug.Assert(Ids[hashPos] == -1);
                Ids[hashPos] = e;

                if (Count == HashHalfSize)
                {
                    Rehash(2 * HashSize, false);
                }
                return e;
            }
            return -(e + 1);
        }

        /// <summary>
        /// Called when hash is too small (> 50% occupied) or too large (< 20%
        /// occupied).
        /// </summary>
        private void Rehash(int newSize, bool hashOnData)
        {
            int newMask = newSize - 1;
            BytesUsed.AddAndGet(RamUsageEstimator.NUM_BYTES_INT * (newSize));
            int[] newHash = new int[newSize];
            Arrays.Fill(newHash, -1);
            for (int i = 0; i < HashSize; i++)
            {
                int e0 = Ids[i];
                if (e0 != -1)
                {
                    int code;
                    if (hashOnData)
                    {
                        int off = BytesStart[e0];
                        int start = off & ByteBlockPool.BYTE_BLOCK_MASK;
                        var bytes = Pool.Buffers[off >> ByteBlockPool.BYTE_BLOCK_SHIFT];
                        int len;
                        int pos;
                        if ((bytes[start] & 0x80) == 0)
                        {
                            // length is 1 byte
                            len = bytes[start];
                            pos = start + 1;
                        }
                        else
                        {
                            len = (bytes[start] & 0x7f) + ((bytes[start + 1] & 0xff) << 7);
                            pos = start + 2;
                        }
                        code = DoHash(bytes, pos, len);
                    }
                    else
                    {
                        code = BytesStart[e0];
                    }

                    int hashPos = code & newMask;
                    Debug.Assert(hashPos >= 0);
                    if (newHash[hashPos] != -1)
                    {
                        // Conflict; use linear probe to find an open slot
                        // (see LUCENE-5604):
                        do
                        {
                            code++;
                            hashPos = code & newMask;
                        } while (newHash[hashPos] != -1);
                    }
                    newHash[hashPos] = e0;
                }
            }

            HashMask = newMask;
            BytesUsed.AddAndGet(RamUsageEstimator.NUM_BYTES_INT * (-Ids.Length));
            Ids = newHash;
            HashSize = newSize;
            HashHalfSize = newSize / 2;
        }

        // TODO: maybe use long?  But our keys are typically short...
        private static int DoHash(byte[] bytes, int offset, int length)
        {
            return StringHelper.Murmurhash3_x86_32(bytes, offset, length, StringHelper.GOOD_FAST_HASH_SEED);
        }

        /// <summary>
        /// reinitializes the <seealso cref="BytesRefHash"/> after a previous <seealso cref="#clear()"/>
        /// call. If <seealso cref="#clear()"/> has not been called previously this method has no
        /// effect.
        /// </summary>
        public void Reinit()
        {
            if (BytesStart == null)
            {
                BytesStart = bytesStartArray.Init();
            }

            if (Ids == null)
            {
                Ids = new int[HashSize];
                BytesUsed.AddAndGet(RamUsageEstimator.NUM_BYTES_INT * HashSize);
            }
        }

        /// <summary>
        /// Returns the bytesStart offset into the internally used
        /// <seealso cref="ByteBlockPool"/> for the given bytesID
        /// </summary>
        /// <param name="bytesID">
        ///          the id to look up </param>
        /// <returns> the bytesStart offset into the internally used
        ///         <seealso cref="ByteBlockPool"/> for the given id </returns>
        public int ByteStart(int bytesID)
        {
            Debug.Assert(BytesStart != null, "bytesStart is null - not initialized");
            Debug.Assert(bytesID >= 0 && bytesID < Count, bytesID.ToString());
            return BytesStart[bytesID];
        }

        /// <summary>
        /// Thrown if a <seealso cref="BytesRef"/> exceeds the <seealso cref="BytesRefHash"/> limit of
        /// <seealso cref="ByteBlockPool#BYTE_BLOCK_SIZE"/>-2.
        /// </summary>
        public class MaxBytesLengthExceededException : Exception
        {
            internal MaxBytesLengthExceededException(string message)
                : base(message)
            {
            }
        }

        /// <summary>
        /// Manages allocation of the per-term addresses. </summary>
        public abstract class BytesStartArray
        {
            /// <summary>
            /// Initializes the BytesStartArray. this call will allocate memory
            /// </summary>
            /// <returns> the initialized bytes start array </returns>
            public abstract int[] Init();

            /// <summary>
            /// Grows the <seealso cref="BytesStartArray"/>
            /// </summary>
            /// <returns> the grown array </returns>
            public abstract int[] Grow();

            /// <summary>
            /// clears the <seealso cref="BytesStartArray"/> and returns the cleared instance.
            /// </summary>
            /// <returns> the cleared instance, this might be <code>null</code> </returns>
            public abstract int[] Clear();

            /// <summary>
            /// A <seealso cref="Counter"/> reference holding the number of bytes used by this
            /// <seealso cref="BytesStartArray"/>. The <seealso cref="BytesRefHash"/> uses this reference to
            /// track it memory usage
            /// </summary>
            /// <returns> a <seealso cref="AtomicLong"/> reference holding the number of bytes used
            ///         by this <seealso cref="BytesStartArray"/>. </returns>
            public abstract Counter BytesUsed();
        }

        /// <summary>
        /// A simple <seealso cref="BytesStartArray"/> that tracks
        ///  memory allocation using a private <seealso cref="Counter"/>
        ///  instance.
        /// </summary>
        public class DirectBytesStartArray : BytesStartArray
        {
            // TODO: can't we just merge this w/
            // TrackingDirectBytesStartArray...?  Just add a ctor
            // that makes a private bytesUsed?

            protected internal readonly int InitSize;
            internal int[] BytesStart;
            internal readonly Counter BytesUsed_Renamed;

            public DirectBytesStartArray(int initSize, Counter counter)
            {
                this.BytesUsed_Renamed = counter;
                this.InitSize = initSize;
            }

            public DirectBytesStartArray(int initSize)
                : this(initSize, Counter.NewCounter())
            {
            }

            public override int[] Clear()
            {
                return BytesStart = null;
            }

            public override int[] Grow()
            {
                Debug.Assert(BytesStart != null);
                return BytesStart = ArrayUtil.Grow(BytesStart, BytesStart.Length + 1);
            }

            public override int[] Init()
            {
                return BytesStart = new int[ArrayUtil.Oversize(InitSize, RamUsageEstimator.NUM_BYTES_INT)];
            }

            public override Counter BytesUsed()
            {
                return BytesUsed_Renamed;
            }
        }
    }
}