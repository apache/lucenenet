using Lucene.Net.Diagnostics;
using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
#if FEATURE_SERIALIZABLE_EXCEPTIONS
using System.Runtime.Serialization;
#endif

namespace Lucene.Net.Util
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

    using DirectAllocator = Lucene.Net.Util.ByteBlockPool.DirectAllocator;

    /// <summary>
    /// <see cref="BytesRefHash"/> is a special purpose hash-map like data-structure
    /// optimized for <see cref="BytesRef"/> instances. <see cref="BytesRefHash"/> maintains mappings of
    /// byte arrays to ids (Map&lt;BytesRef,int&gt;) storing the hashed bytes
    /// efficiently in continuous storage. The mapping to the id is
    /// encapsulated inside <see cref="BytesRefHash"/> and is guaranteed to be increased
    /// for each added <see cref="BytesRef"/>.
    ///
    /// <para>
    /// Note: The maximum capacity <see cref="BytesRef"/> instance passed to
    /// <see cref="Add(BytesRef)"/> must not be longer than <see cref="ByteBlockPool.BYTE_BLOCK_SIZE"/>-2.
    /// The internal storage is limited to 2GB total byte storage.
    /// </para>
    ///
    /// @lucene.internal
    /// </summary>
    public sealed class BytesRefHash : IDisposable // LUCENENET specific: Implemented IDisposable to enable usage of the using statement
    {
        public const int DEFAULT_CAPACITY = 16;

        // the following fields are needed by comparer,
        // so package private to prevent access$-methods:
        internal readonly ByteBlockPool pool;

        internal int[] bytesStart;

        private readonly BytesRef scratch1 = new BytesRef();
        private int hashSize;
        private int hashHalfSize;
        private int hashMask;
        private int count;
        private int lastCount = -1;
        private int[] ids;
        private readonly BytesStartArray bytesStartArray;
        private readonly Counter bytesUsed; // LUCENENET: marked readonly

        /// <summary>
        /// Creates a new <see cref="BytesRefHash"/> with a <see cref="ByteBlockPool"/> using a
        /// <see cref="DirectAllocator"/>.
        /// </summary>
        public BytesRefHash()
            : this(new ByteBlockPool(new DirectAllocator()))
        {
        }

        /// <summary>
        /// Creates a new <see cref="BytesRefHash"/>
        /// </summary>
        public BytesRefHash(ByteBlockPool pool)
            : this(pool, DEFAULT_CAPACITY, new DirectBytesStartArray(DEFAULT_CAPACITY))
        {
        }

        /// <summary>
        /// Creates a new <see cref="BytesRefHash"/>
        /// </summary>
        public BytesRefHash(ByteBlockPool pool, int capacity, BytesStartArray bytesStartArray)
        {
            hashSize = capacity;
            hashHalfSize = hashSize >> 1;
            hashMask = hashSize - 1;
            this.pool = pool;
            ids = new int[hashSize];
            Arrays.Fill(ids, -1);
            this.bytesStartArray = bytesStartArray;
            bytesStart = bytesStartArray.Init();
            bytesUsed = bytesStartArray.BytesUsed() ?? Counter.NewCounter();
            bytesUsed.AddAndGet(hashSize * RamUsageEstimator.NUM_BYTES_INT32);
        }

        /// <summary>
        /// Returns the number of <see cref="BytesRef"/> values in this <see cref="BytesRefHash"/>.
        /// <para/>
        /// NOTE: This was size() in Lucene.
        /// </summary>
        /// <returns> The number of <see cref="BytesRef"/> values in this <see cref="BytesRefHash"/>. </returns>
        public int Count => count;

        /// <summary>
        /// Populates and returns a <see cref="BytesRef"/> with the bytes for the given
        /// bytesID.
        /// <para/>
        /// Note: the given bytesID must be a positive integer less than the current
        /// size (<see cref="Count"/>)
        /// </summary>
        /// <param name="bytesID">
        ///          The id </param>
        /// <param name="ref">
        ///          The <see cref="BytesRef"/> to populate
        /// </param>
        /// <returns> The given <see cref="BytesRef"/> instance populated with the bytes for the given
        ///         bytesID </returns>
        public BytesRef Get(int bytesID, BytesRef @ref)
        {
            if (Debugging.AssertsEnabled)
            {
                Debugging.Assert(bytesStart != null, "bytesStart is null - not initialized");
                Debugging.Assert(bytesID < bytesStart.Length,"bytesID exceeds byteStart len: {0}", bytesStart.Length);
            }
            pool.SetBytesRef(@ref, bytesStart[bytesID]);
            return @ref;
        }

        /// <summary>
        /// Returns the ids array in arbitrary order. Valid ids start at offset of 0
        /// and end at a limit of <see cref="Count"/> - 1
        /// <para>
        /// Note: this is a destructive operation. <see cref="Clear()"/> must be called in
        /// order to reuse this <see cref="BytesRefHash"/> instance.
        /// </para>
        /// </summary>
        public int[] Compact()
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(bytesStart != null, "bytesStart is null - not initialized");
            int upto = 0;
            for (int i = 0; i < hashSize; i++)
            {
                if (ids[i] != -1)
                {
                    if (upto < i)
                    {
                        ids[upto] = ids[i];
                        ids[i] = -1;
                    }
                    upto++;
                }
            }

            if (Debugging.AssertsEnabled) Debugging.Assert(upto == count);
            lastCount = count;
            return ids;
        }

        /// <summary>
        /// Returns the values array sorted by the referenced byte values.
        /// <para>
        /// Note: this is a destructive operation. <see cref="Clear()"/> must be called in
        /// order to reuse this <see cref="BytesRefHash"/> instance.
        /// </para>
        /// </summary>
        /// <param name="comp">
        ///          The <see cref="T:IComparer{BytesRef}"/> used for sorting </param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int[] Sort(IComparer<BytesRef> comp)
        {
            int[] compact = Compact();
            new IntroSorterAnonymousClass(this, comp, compact).Sort(0, count);
            return compact;
        }

        private sealed class IntroSorterAnonymousClass : IntroSorter
        {
            private readonly BytesRefHash outerInstance;

            private readonly IComparer<BytesRef> comp;
            private readonly int[] compact;
            private readonly BytesRef pivot = new BytesRef(), /*scratch1 = new BytesRef(), // LUCENENET: Never read */ scratch2 = new BytesRef();

            public IntroSorterAnonymousClass(BytesRefHash outerInstance, IComparer<BytesRef> comp, int[] compact)
            {
                this.outerInstance = outerInstance;
                this.comp = comp;
                this.compact = compact;
            }

            protected override void Swap(int i, int j)
            {
                int o = compact[i];
                compact[i] = compact[j];
                compact[j] = o;
            }

            protected override int Compare(int i, int j)
            {
                int id1 = compact[i], id2 = compact[j];
                if (Debugging.AssertsEnabled) Debugging.Assert(outerInstance.bytesStart.Length > id1 && outerInstance.bytesStart.Length > id2);
                // LUCENENET NOTE: It is critical that this be outerInstance.scratch1 instead of scratch1
                outerInstance.pool.SetBytesRef(outerInstance.scratch1, outerInstance.bytesStart[id1]);
                outerInstance.pool.SetBytesRef(scratch2, outerInstance.bytesStart[id2]);
                return comp.Compare(outerInstance.scratch1, scratch2);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            protected override void SetPivot(int i)
            {
                int id = compact[i];
                if (Debugging.AssertsEnabled) Debugging.Assert(outerInstance.bytesStart.Length > id);
                outerInstance.pool.SetBytesRef(pivot, outerInstance.bytesStart[id]);
            }

            protected override int ComparePivot(int j)
            {
                int id = compact[j];
                if (Debugging.AssertsEnabled) Debugging.Assert(outerInstance.bytesStart.Length > id);
                outerInstance.pool.SetBytesRef(scratch2, outerInstance.bytesStart[id]);
                return comp.Compare(pivot, scratch2);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool Equals(int id, BytesRef b)
        {
            pool.SetBytesRef(scratch1, bytesStart[id]);
            return scratch1.BytesEquals(b);
        }

        private bool Shrink(int targetSize)
        {
            // Cannot use ArrayUtil.shrink because we require power
            // of 2:
            int newSize = hashSize;
            while (newSize >= 8 && newSize / 4 > targetSize)
            {
                newSize /= 2;
            }
            if (newSize != hashSize)
            {
                bytesUsed.AddAndGet(RamUsageEstimator.NUM_BYTES_INT32 * -(hashSize - newSize));
                hashSize = newSize;
                ids = new int[hashSize];
                Arrays.Fill(ids, -1);
                hashHalfSize = newSize / 2;
                hashMask = newSize - 1;
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Clears the <see cref="BytesRef"/> which maps to the given <see cref="BytesRef"/>
        /// </summary>
        public void Clear(bool resetPool)
        {
            lastCount = count;
            count = 0;
            if (resetPool)
            {
                pool.Reset(false, false); // we don't need to 0-fill the buffers
            }
            bytesStart = bytesStartArray.Clear();
            if (lastCount != -1 && Shrink(lastCount))
            {
                // shrink clears the hash entries
                return;
            }
            Arrays.Fill(ids, -1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear()
        {
            Clear(true);
        }

        /// <summary>
        /// Closes the <see cref="BytesRefHash"/> and releases all internally used memory
        /// </summary>
        public void Dispose()
        {
            Clear(true);
            ids = null;
            bytesUsed.AddAndGet(RamUsageEstimator.NUM_BYTES_INT32 * -hashSize);
        }

        /// <summary>
        /// Adds a new <see cref="BytesRef"/>
        /// </summary>
        /// <param name="bytes">
        ///          The bytes to hash </param>
        /// <returns> The id the given bytes are hashed if there was no mapping for the
        ///         given bytes, otherwise <c>(-(id)-1)</c>. this guarantees
        ///         that the return value will always be &gt;= 0 if the given bytes
        ///         haven't been hashed before.
        /// </returns>
        /// <exception cref="MaxBytesLengthExceededException">
        ///           if the given bytes are > 2 +
        ///           <see cref="ByteBlockPool.BYTE_BLOCK_SIZE"/> </exception>
        public int Add(BytesRef bytes)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(bytesStart != null, "bytesStart is null - not initialized");
            int length = bytes.Length;
            // final position
            int hashPos = FindHash(bytes);
            int e = ids[hashPos];

            if (e == -1)
            {
                // new entry
                int len2 = 2 + bytes.Length;
                if (len2 + pool.ByteUpto > ByteBlockPool.BYTE_BLOCK_SIZE)
                {
                    if (len2 > ByteBlockPool.BYTE_BLOCK_SIZE)
                    {
                        throw new MaxBytesLengthExceededException("bytes can be at most " + (ByteBlockPool.BYTE_BLOCK_SIZE - 2) + " in length; got " + bytes.Length);
                    }
                    pool.NextBuffer();
                }
                var buffer = pool.Buffer;
                int bufferUpto = pool.ByteUpto;
                if (count >= bytesStart.Length)
                {
                    bytesStart = bytesStartArray.Grow();
                    if (Debugging.AssertsEnabled) Debugging.Assert(count < bytesStart.Length + 1,"count: {0} len: {1}", count, bytesStart.Length);
                }
                e = count++;

                bytesStart[e] = bufferUpto + pool.ByteOffset;

                // We first encode the length, followed by the
                // bytes. Length is encoded as vInt, but will consume
                // 1 or 2 bytes at most (we reject too-long terms,
                // above).
                if (length < 128)
                {
                    // 1 byte to store length
                    buffer[bufferUpto] = (byte)length;
                    pool.ByteUpto += length + 1;
                    if (Debugging.AssertsEnabled) Debugging.Assert(length >= 0,"Length must be positive: {0}", length);
                    Arrays.Copy(bytes.Bytes, bytes.Offset, buffer, bufferUpto + 1, length);
                }
                else
                {
                    // 2 byte to store length
                    buffer[bufferUpto] = (byte)(0x80 | (length & 0x7f));
                    buffer[bufferUpto + 1] = (byte)((length >> 7) & 0xff);
                    pool.ByteUpto += length + 2;
                    Arrays.Copy(bytes.Bytes, bytes.Offset, buffer, bufferUpto + 2, length);
                }
                if (Debugging.AssertsEnabled) Debugging.Assert(ids[hashPos] == -1);
                ids[hashPos] = e;

                if (count == hashHalfSize)
                {
                    Rehash(2 * hashSize, true);
                }
                return e;
            }
            return -(e + 1);
        }

        /// <summary>
        /// Returns the id of the given <see cref="BytesRef"/>.
        /// </summary>
        /// <param name="bytes">
        ///          The bytes to look for
        /// </param>
        /// <returns> The id of the given bytes, or <c>-1</c> if there is no mapping for the
        ///         given bytes. </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Find(BytesRef bytes)
        {
            return ids[FindHash(bytes)];
        }

        private int FindHash(BytesRef bytes)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(bytesStart != null, "bytesStart is null - not initialized");

            int code = DoHash(bytes.Bytes, bytes.Offset, bytes.Length);

            // final position
            int hashPos = code & hashMask;
            int e = ids[hashPos];
            if (e != -1 && !Equals(e, bytes))
            {
                // Conflict; use linear probe to find an open slot
                // (see LUCENE-5604):
                do
                {
                    code++;
                    hashPos = code & hashMask;
                    e = ids[hashPos];
                } while (e != -1 && !Equals(e, bytes));
            }

            return hashPos;
        }

        /// <summary>
        /// Adds a "arbitrary" int offset instead of a <see cref="BytesRef"/>
        /// term.  This is used in the indexer to hold the hash for term
        /// vectors, because they do not redundantly store the <see cref="T:byte[]"/> term
        /// directly and instead reference the <see cref="T:byte[]"/> term
        /// already stored by the postings <see cref="BytesRefHash"/>.  See
        /// <see cref="Index.TermsHashPerField.Add(int)"/>.
        /// </summary>
        public int AddByPoolOffset(int offset)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(bytesStart != null, "bytesStart is null - not initialized");
            // final position
            int code = offset;
            int hashPos = offset & hashMask;
            int e = ids[hashPos];
            if (e != -1 && bytesStart[e] != offset)
            {
                // Conflict; use linear probe to find an open slot
                // (see LUCENE-5604):
                do
                {
                    code++;
                    hashPos = code & hashMask;
                    e = ids[hashPos];
                } while (e != -1 && bytesStart[e] != offset);
            }
            if (e == -1)
            {
                // new entry
                if (count >= bytesStart.Length)
                {
                    bytesStart = bytesStartArray.Grow();
                    if (Debugging.AssertsEnabled) Debugging.Assert(count < bytesStart.Length + 1,"count: {0} len: {1}", count, bytesStart.Length);
                }
                e = count++;
                bytesStart[e] = offset;
                if (Debugging.AssertsEnabled) Debugging.Assert(ids[hashPos] == -1);
                ids[hashPos] = e;

                if (count == hashHalfSize)
                {
                    Rehash(2 * hashSize, false);
                }
                return e;
            }
            return -(e + 1);
        }

        /// <summary>
        /// Called when hash is too small (&gt; 50% occupied) or too large (&lt; 20%
        /// occupied).
        /// </summary>
        private void Rehash(int newSize, bool hashOnData)
        {
            int newMask = newSize - 1;
            bytesUsed.AddAndGet(RamUsageEstimator.NUM_BYTES_INT32 * (newSize));
            int[] newHash = new int[newSize];
            Arrays.Fill(newHash, -1);
            for (int i = 0; i < hashSize; i++)
            {
                int e0 = ids[i];
                if (e0 != -1)
                {
                    int code;
                    if (hashOnData)
                    {
                        int off = bytesStart[e0];
                        int start = off & ByteBlockPool.BYTE_BLOCK_MASK;
                        var bytes = pool.Buffers[off >> ByteBlockPool.BYTE_BLOCK_SHIFT];
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
                        code = bytesStart[e0];
                    }

                    int hashPos = code & newMask;
                    if (Debugging.AssertsEnabled) Debugging.Assert(hashPos >= 0);
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

            hashMask = newMask;
            bytesUsed.AddAndGet(RamUsageEstimator.NUM_BYTES_INT32 * (-ids.Length));
            ids = newHash;
            hashSize = newSize;
            hashHalfSize = newSize / 2;
        }

        // TODO: maybe use long?  But our keys are typically short...
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int DoHash(byte[] bytes, int offset, int length)
        {
            return StringHelper.Murmurhash3_x86_32(bytes, offset, length, StringHelper.GoodFastHashSeed);
        }

        /// <summary>
        /// Reinitializes the <see cref="BytesRefHash"/> after a previous <see cref="Clear()"/>
        /// call. If <see cref="Clear()"/> has not been called previously this method has no
        /// effect.
        /// </summary>
        public void Reinit()
        {
            if (bytesStart is null)
            {
                bytesStart = bytesStartArray.Init();
            }

            if (ids is null)
            {
                ids = new int[hashSize];
                bytesUsed.AddAndGet(RamUsageEstimator.NUM_BYTES_INT32 * hashSize);
            }
        }

        /// <summary>
        /// Returns the bytesStart offset into the internally used
        /// <see cref="ByteBlockPool"/> for the given <paramref name="bytesID"/>
        /// </summary>
        /// <param name="bytesID">
        ///          The id to look up </param>
        /// <returns> The bytesStart offset into the internally used
        ///         <see cref="ByteBlockPool"/> for the given id </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int ByteStart(int bytesID)
        {
            if (Debugging.AssertsEnabled)
            {
                Debugging.Assert(bytesStart != null, "bytesStart is null - not initialized");
                Debugging.Assert(bytesID >= 0 && bytesID < count, "{0}", bytesID);
            }
            return bytesStart[bytesID];
        }

        /// <summary>
        /// Thrown if a <see cref="BytesRef"/> exceeds the <see cref="BytesRefHash"/> limit of
        /// <see cref="ByteBlockPool.BYTE_BLOCK_SIZE"/>-2.
        /// </summary>
        // LUCENENET: It is no longer good practice to use binary serialization. 
        // See: https://github.com/dotnet/corefx/issues/23584#issuecomment-325724568
#if FEATURE_SERIALIZABLE_EXCEPTIONS
    [Serializable]
#endif
        public class MaxBytesLengthExceededException : Exception, IRuntimeException // LUCENENET specific: Added IRuntimeException for identification of the Java superclass in .NET
        {
            internal MaxBytesLengthExceededException(string message)
                : base(message)
            {
            }

#if FEATURE_SERIALIZABLE_EXCEPTIONS
            /// <summary>
            /// Initializes a new instance of this class with serialized data.
            /// </summary>
            /// <param name="info">The <see cref="SerializationInfo"/> that holds the serialized object data about the exception being thrown.</param>
            /// <param name="context">The <see cref="StreamingContext"/> that contains contextual information about the source or destination.</param>
            protected MaxBytesLengthExceededException(SerializationInfo info, StreamingContext context)
                : base(info, context)
            {
            }
#endif
        }

        /// <summary>
        /// Manages allocation of the per-term addresses. </summary>
        public abstract class BytesStartArray
        {
            /// <summary>
            /// Initializes the <see cref="BytesStartArray"/>. This call will allocate memory.
            /// </summary>
            /// <returns> The initialized bytes start array. </returns>
            public abstract int[] Init();

            /// <summary>
            /// Grows the <see cref="BytesStartArray"/>.
            /// </summary>
            /// <returns> The grown array. </returns>
            public abstract int[] Grow();

            /// <summary>
            /// Clears the <see cref="BytesStartArray"/> and returns the cleared instance.
            /// </summary>
            /// <returns> The cleared instance, this might be <c>null</c>. </returns>
            public abstract int[] Clear();

            /// <summary>
            /// A <see cref="Counter"/> reference holding the number of bytes used by this
            /// <see cref="BytesStartArray"/>. The <see cref="BytesRefHash"/> uses this reference to
            /// track it memory usage.
            /// </summary>
            /// <returns> a <see cref="J2N.Threading.Atomic.AtomicInt64"/> reference holding the number of bytes used
            ///         by this <see cref="BytesStartArray"/>. </returns>
            public abstract Counter BytesUsed();
        }

        /// <summary>
        /// A simple <see cref="BytesStartArray"/> that tracks
        /// memory allocation using a private <see cref="Counter"/>
        /// instance.
        /// </summary>
        public class DirectBytesStartArray : BytesStartArray
        {
            // TODO: can't we just merge this w/
            // TrackingDirectBytesStartArray...?  Just add a ctor
            // that makes a private bytesUsed?

            protected readonly int m_initSize;
            internal int[] bytesStart;
            internal readonly Counter bytesUsed;

            public DirectBytesStartArray(int initSize, Counter counter)
            {
                this.bytesUsed = counter;
                this.m_initSize = initSize;
            }

            public DirectBytesStartArray(int initSize)
                : this(initSize, Counter.NewCounter())
            {
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override int[] Clear()
            {
                return bytesStart = null;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override int[] Grow()
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(bytesStart != null);
                return bytesStart = ArrayUtil.Grow(bytesStart, bytesStart.Length + 1);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override int[] Init()
            {
                return bytesStart = new int[ArrayUtil.Oversize(m_initSize, RamUsageEstimator.NUM_BYTES_INT32)];
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override Counter BytesUsed()
            {
                return bytesUsed;
            }
        }
    }
}