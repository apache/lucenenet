// Lucene version compatibility level 4.8.1
using Lucene.Net.Support;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
#nullable enable

namespace Lucene.Net.Analysis.Compound.Hyphenation
{
    /*
     * Licensed to the Apache Software Foundation (ASF) under one or more
     * contributor license agreements.  See the NOTICE file distributed with
     * this work for additional information regarding copyright ownership.
     * The ASF licenses this file to You under the Apache License, Version 2.0
     * (the "License"); you may not use this file except in compliance with
     * the License.  You may obtain a copy of the License at
     *
     *      http://www.apache.org/licenses/LICENSE-2.0
     *
     * Unless required by applicable law or agreed to in writing, software
     * distributed under the License is distributed on an "AS IS" BASIS,
     * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
     * See the License for the specific language governing permissions and
     * limitations under the License.
     */

    /// <summary>
    /// This class implements a simple byte vector with access to the underlying
    /// array.
    /// This class has been taken from the Apache FOP project (http://xmlgraphics.apache.org/fop/). They have been slightly modified.
    /// </summary>
    public class ByteVector
    {
        /// <summary>
        /// Capacity increment size
        /// </summary>
        private const int DEFAULT_BLOCK_SIZE = 2048;

        private readonly int blockSize; // LUCENENET: marked readonly

        /// <summary>
        /// The encapsulated array
        /// </summary>
        internal byte[] array; // LUCENENET: internal for extension methods

        /// <summary>
        /// Points to next free item
        /// </summary>
        private int n;

        public ByteVector()
            : this(DEFAULT_BLOCK_SIZE)
        {
        }

        public ByteVector(int capacity)
        {
            if (capacity > 0)
            {
                blockSize = capacity;
            }
            else
            {
                blockSize = DEFAULT_BLOCK_SIZE;
            }
            array = new byte[blockSize];
            n = 0;
        }

        public ByteVector(ReadOnlySpan<byte> a)
        {
            blockSize = DEFAULT_BLOCK_SIZE;
            array = new byte[Math.Max(a.Length, blockSize)];
            a.CopyTo(array);
            n = a.Length;
        }

        public ByteVector(ReadOnlySpan<byte> a, int capacity)
        {
            if (capacity > 0)
            {
                blockSize = capacity;
            }
            else
            {
                blockSize = DEFAULT_BLOCK_SIZE;
            }
            array = new byte[Math.Max(a.Length, blockSize)];
            a.CopyTo(array);
            n = a.Length;
        }

        [WritableArray]
        [SuppressMessage("Microsoft.Performance", "CA1819", Justification = "Lucene's design requires some writable array properties")]
        public virtual byte[] Array => array;

        /// <summary>
        /// LUCENENET indexer for .NET
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public virtual byte this[int index]
        {
            get => array[index];
            set => array[index] = value;
        }

        /// <summary>
        /// return number of items in array
        /// </summary>
        public virtual int Length => n;

        /// <summary>
        /// returns current capacity of array
        /// </summary>
        public virtual int Capacity => array.Length;

        // LUCENENET NOTE: Not needed (replaced with this[])
        //public virtual void Put(int index, sbyte val)
        //{
        //    array[index] = val;
        //}

        //public virtual sbyte Get(int index)
        //{
        //    return array[index];
        //}

        /// <summary>
        /// This is to implement memory allocation in the array. Like malloc().
        /// </summary>
        public virtual int Alloc(int size)
        {
            int index = n;
            int len = array.Length;
            if (n + size >= len)
            {
                byte[] aux = new byte[len + blockSize];
                Arrays.Copy(array, 0, aux, 0, len);
                array = aux;
            }
            n += size;
            return index;
        }

        // LUCENENET specific - renamed from trimToSize() (.NET Platform Convention)
        public virtual void TrimExcess()
        {
            if (n < array.Length)
            {
                byte[] aux = new byte[n];
                Arrays.Copy(array, 0, aux, 0, n);
                array = aux;
            }
        }
    }

    /// <summary>
    /// Extension methods for <see cref="ByteVector"/> that expose the data written
    /// so far as a <see cref="ReadOnlySpan{T}"/> or <see cref="ReadOnlyMemory{T}"/>.
    /// </summary>
    /// <remarks>
    /// LUCENENET specific. These are implemented as extension methods (rather than instance
    /// methods) so that a <c>null</c> <see cref="ByteVector"/> naturally converts to
    /// an empty span/memory instead of throwing, matching the behavior of the BCL
    /// <c>AsSpan</c>/<c>AsMemory</c> extensions for reference types.
    /// </remarks>
    public static class ByteVectorExtensions
    {
        #region AsSpan

        /// <summary>
        /// Creates a new read-only span over the data written to <paramref name="vector"/> so far.
        /// </summary>
        /// <param name="vector">The target <see cref="ByteVector"/>.</param>
        /// <returns>The read-only span representation of the data, or <c>default</c> if
        /// <paramref name="vector"/> is <c>null</c>.</returns>
        public static ReadOnlySpan<byte> AsSpan(this ByteVector vector)
        {
            if (vector is null)
                return default;

            byte[] bytes = vector.array;

#if FEATURE_MEMORYMARSHAL_CREATEREADONLYSPAN && FEATURE_MEMORYMARSHAL_GETARRAYDATAREFERENCE
            return MemoryMarshal.CreateReadOnlySpan<byte>(ref MemoryMarshal.GetArrayDataReference(bytes), vector.Length);
#else
            return new ReadOnlySpan<byte>(bytes, 0, vector.Length);
#endif
        }

        /// <summary>
        /// Creates a new read-only span over a portion of the data written to
        /// <paramref name="vector"/> so far, from a specified position to the end.
        /// </summary>
        /// <param name="vector">The target <see cref="ByteVector"/>.</param>
        /// <param name="start">The index at which to begin this slice.</param>
        /// <returns>The read-only span representation of the data, or <c>default</c> if
        /// <paramref name="vector"/> is <c>null</c>.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="start"/> is less than 0 or greater than <c>text.Length</c>.
        /// </exception>
        public static ReadOnlySpan<byte> AsSpan(this ByteVector vector, int start)
        {
            if (vector is null)
            {
                if (start != 0)
                    throw new ArgumentOutOfRangeException(nameof(start));

                return default;
            }

            if ((uint)start > (uint)vector.Length)
                throw new ArgumentOutOfRangeException(nameof(start));

            byte[] bytes = vector.array;

#if FEATURE_MEMORYMARSHAL_CREATEREADONLYSPAN && FEATURE_MEMORYMARSHAL_GETARRAYDATAREFERENCE
            return MemoryMarshal.CreateReadOnlySpan<byte>(ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(bytes),
                (nint)(uint)start /* force zero-extension */), vector.Length - start);
#else
            return new ReadOnlySpan<byte>(bytes, start, vector.Length - start);
#endif
        }

        /// <summary>
        /// Creates a new read-only span over a portion of the data written to
        /// <paramref name="vector"/> so far, from a specified position for a specified length.
        /// </summary>
        /// <param name="vector">The target <see cref="ByteVector"/>.</param>
        /// <param name="start">The index at which to begin this slice.</param>
        /// <param name="length">The desired length for the slice.</param>
        /// <returns>The read-only span representation of the data, or <c>default</c> if
        /// <paramref name="vector"/> is <c>null</c>.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="start"/>, <paramref name="length"/>, or
        /// <paramref name="start"/> + <paramref name="length"/> is not in the range of <paramref name="vector"/>.
        /// </exception>
        public static ReadOnlySpan<byte> AsSpan(this ByteVector vector, int start, int length)
        {
            if (vector is null)
            {
                if (start != 0 || length != 0)
                    throw new ArgumentOutOfRangeException(nameof(start));

                return default;
            }

            if (IntPtr.Size == 8) // 64-bit process
            {
                // See comment in Span<T>.Slice for how this works.
                if ((ulong)(uint)start + (ulong)(uint)length > (ulong)(uint)vector.Length)
                    throw new ArgumentOutOfRangeException(nameof(start));
            }
            else
            {
                if ((uint)start > (uint)vector.Length || (uint)length > (uint)(vector.Length - start))
                    throw new ArgumentOutOfRangeException(nameof(start));
            }

            byte[] bytes = vector.array;

#if FEATURE_MEMORYMARSHAL_CREATEREADONLYSPAN && FEATURE_MEMORYMARSHAL_GETARRAYDATAREFERENCE
            return MemoryMarshal.CreateReadOnlySpan<byte>(ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(bytes),
                (nint)(uint)start /* force zero-extension */), length);
#else
            return new ReadOnlySpan<byte>(bytes, start, length);
#endif
        }

        /// <summary>
        /// Creates a new read-only span over a portion of the data written to
        /// <paramref name="vector"/> so far, from a specified position to the end.
        /// </summary>
        /// <param name="vector">The target <see cref="ByteVector"/>.</param>
        /// <param name="startIndex">The index at which to begin this slice.</param>
        /// <returns>The read-only span representation of the data, or <c>default</c> if
        /// <paramref name="vector"/> is <c>null</c>.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="startIndex"/> is less
        /// than 0 or greater than <c>text.Length</c>.</exception>
        public static ReadOnlySpan<byte> AsSpan(this ByteVector vector, System.Index startIndex)
        {
            if (vector is null)
            {
                if (!startIndex.Equals(System.Index.Start))
                    throw new ArgumentOutOfRangeException(nameof(startIndex));

                return default;
            }

            int actualIndex = startIndex.GetOffset(vector.Length);
            if ((uint)actualIndex > (uint)vector.Length)
                throw new ArgumentOutOfRangeException(nameof(startIndex));

            byte[] bytes = vector.array;

#if FEATURE_MEMORYMARSHAL_CREATEREADONLYSPAN && FEATURE_MEMORYMARSHAL_GETARRAYDATAREFERENCE
            return MemoryMarshal.CreateReadOnlySpan<byte>(ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(bytes),
                (nint)(uint)actualIndex /* force zero-extension */), vector.Length - actualIndex);
#else
            return new ReadOnlySpan<byte>(bytes, actualIndex, vector.Length - actualIndex);
#endif
        }

        /// <summary>
        /// Creates a new read-only span over a portion of the data written to
        /// <paramref name="vector"/> so far, using the range start and end indexes.
        /// </summary>
        /// <param name="vector">The target <see cref="ByteVector"/>.</param>
        /// <param name="range">The range that has start and end indexes to use for slicing the data.</param>
        /// <returns>The read-only span representation of the data, or <c>default</c> if
        /// <paramref name="vector"/> is <c>null</c>.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="range"/>'s start or end index is not within the bounds of the data.
        /// -or-
        /// <paramref name="range"/>'s start index is greater than its end index.
        /// </exception>
        public static ReadOnlySpan<byte> AsSpan(this ByteVector vector, Range range)
        {
            if (vector is null)
            {
                System.Index startIndex = range.Start;
                System.Index endIndex = range.End;

                if (!startIndex.Equals(System.Index.Start) || !endIndex.Equals(System.Index.Start))
                    throw new ArgumentOutOfRangeException(nameof(range));

                return default;
            }

            (int start, int length) = range.GetOffsetAndLength(vector.Length);
            byte[] bytes = vector.array;

#if FEATURE_MEMORYMARSHAL_CREATEREADONLYSPAN && FEATURE_MEMORYMARSHAL_GETARRAYDATAREFERENCE
            return MemoryMarshal.CreateReadOnlySpan<byte>(ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(bytes),
                (nint)(uint)start /* force zero-extension */), length);
#else
            return new ReadOnlySpan<byte>(bytes, start, length);
#endif
        }

        #endregion AsSpan

        #region AsMemory

        /// <summary>
        /// Creates a new <see cref="ReadOnlyMemory{T}"/> over the data written to <paramref name="vector"/> so far.
        /// </summary>
        /// <param name="vector">The target <see cref="ByteVector"/>.</param>
        /// <returns>The read-only memory representation of the data, or <c>default</c> if
        /// <paramref name="vector"/> is <c>null</c>.</returns>
        public static ReadOnlyMemory<byte> AsMemory(this ByteVector vector)
        {
            if (vector is null)
                return default;

            return new ReadOnlyMemory<byte>(vector.array, 0, vector.Length);
        }

        /// <summary>
        /// Creates a new <see cref="ReadOnlyMemory{T}"/> over a portion of the data written to
        /// <paramref name="vector"/> so far, from a specified position to the end.
        /// </summary>
        /// <param name="vector">The target <see cref="ByteVector"/>.</param>
        /// <param name="start">The index at which to begin this slice.</param>
        /// <returns>The read-only memory representation of the data, or <c>default</c> if
        /// <paramref name="vector"/> is <c>null</c>.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="start"/> is less than 0 or greater than <c>text.Length</c>.
        /// </exception>
        public static ReadOnlyMemory<byte> AsMemory(this ByteVector vector, int start)
        {
            if (vector is null)
            {
                if (start != 0)
                    throw new ArgumentOutOfRangeException(nameof(start));

                return default;
            }

            if ((uint)start > (uint)vector.Length)
                throw new ArgumentOutOfRangeException(nameof(start));

            return new ReadOnlyMemory<byte>(vector.array, start, vector.Length - start);
        }

        /// <summary>
        /// Creates a new <see cref="ReadOnlyMemory{T}"/> over a portion of the data written to
        /// <paramref name="vector"/> so far, from a specified position for a specified length.
        /// </summary>
        /// <param name="vector">The target <see cref="ByteVector"/>.</param>
        /// <param name="start">The index at which to begin this slice.</param>
        /// <param name="length">The desired length for the slice.</param>
        /// <returns>The read-only memory representation of the data, or <c>default</c> if
        /// <paramref name="vector"/> is <c>null</c>.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="start"/>, <paramref name="length"/>, or
        /// <paramref name="start"/> + <paramref name="length"/> is not in the range of <paramref name="vector"/>.
        /// </exception>
        public static ReadOnlyMemory<byte> AsMemory(this ByteVector vector, int start, int length)
        {
            if (vector is null)
            {
                if (start != 0 || length != 0)
                    throw new ArgumentOutOfRangeException(nameof(start));

                return default;
            }

            if (IntPtr.Size == 8) // 64-bit process
            {
                // See comment in Span<T>.Slice for how this works.
                if ((ulong)(uint)start + (ulong)(uint)length > (ulong)(uint)vector.Length)
                    throw new ArgumentOutOfRangeException(nameof(start));
            }
            else
            {
                if ((uint)start > (uint)vector.Length || (uint)length > (uint)(vector.Length - start))
                    throw new ArgumentOutOfRangeException(nameof(start));
            }

            return new ReadOnlyMemory<byte>(vector.array, start, length);
        }

        /// <summary>
        /// Creates a new <see cref="ReadOnlyMemory{T}"/> over a portion of the data written to
        /// <paramref name="vector"/> so far, from a specified position to the end.
        /// </summary>
        /// <param name="vector">The target <see cref="ByteVector"/>.</param>
        /// <param name="startIndex">The index at which to begin this slice.</param>
        /// <returns>The read-only memory representation of the data, or <c>default</c> if
        /// <paramref name="vector"/> is <c>null</c>.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="startIndex"/> is less
        /// than 0 or greater than <c>text.Length</c>.</exception>
        public static ReadOnlyMemory<byte> AsMemory(this ByteVector vector, System.Index startIndex)
        {
            if (vector is null)
            {
                if (!startIndex.Equals(System.Index.Start))
                    throw new ArgumentOutOfRangeException(nameof(startIndex));

                return default;
            }

            int actualIndex = startIndex.GetOffset(vector.Length);
            if ((uint)actualIndex > (uint)vector.Length)
                throw new ArgumentOutOfRangeException(nameof(startIndex));

            return new ReadOnlyMemory<byte>(vector.array, actualIndex, vector.Length - actualIndex);
        }

        /// <summary>
        /// Creates a new <see cref="ReadOnlyMemory{T}"/> over a portion of the data written to
        /// <paramref name="vector"/> so far, using the range start and end indexes.
        /// </summary>
        /// <param name="vector">The target <see cref="ByteVector"/>.</param>
        /// <param name="range">The range used to indicate the start and length of the sliced data.</param>
        /// <returns>The read-only memory representation of the data, or <c>default</c> if
        /// <paramref name="vector"/> is <c>null</c>.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="range"/>'s start or end index is not within the bounds of the data.
        /// -or-
        /// <paramref name="range"/>'s start index is greater than its end index.
        /// </exception>
        public static ReadOnlyMemory<byte> AsMemory(this ByteVector vector, Range range)
        {
            if (vector is null)
            {
                System.Index startIndex = range.Start;
                System.Index endIndex = range.End;

                if (!startIndex.Equals(System.Index.Start) || !endIndex.Equals(System.Index.Start))
                    throw new ArgumentOutOfRangeException(nameof(range));

                return default;
            }

            (int start, int length) = range.GetOffsetAndLength(vector.Length);
            return new ReadOnlyMemory<byte>(vector.array, start, length);
        }

        #endregion AsMemory
    }

}
