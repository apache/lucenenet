using J2N.Collections;
using Lucene.Net.Diagnostics;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Lucene.Net.Support
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

    internal static class Arrays
    {
        /// <summary>
        /// Compares the entire members of one array whith the other one.
        /// </summary>
        /// <param name="a">The array to be compared.</param>
        /// <param name="b">The array to be compared with.</param>
        /// <returns>Returns true if the two specified arrays of Objects are equal
        /// to one another. The two arrays are considered equal if both arrays
        /// contain the same number of elements, and all corresponding pairs of
        /// elements in the two arrays are equal. Two objects e1 and e2 are
        /// considered equal if (e1==null ? e2==null : e1.Equals(e2)). In other
        /// words, the two arrays are equal if they contain the same elements in
        /// the same order. Also, two array references are considered equal if
        /// both are null.
        /// <para/>
        /// Note that if the type of <typeparam name="T"/> is a <see cref="IDictionary{TKey, TValue}"/>,
        /// <see cref="IList{T}"/>, or <see cref="ISet{T}"/>, its values and any nested collection values
        /// will be compared for equality as well.
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Equals<T>(T[] a, T[] b)
        {
            return ArrayEqualityComparer<T>.OneDimensional.Equals(a, b);
        }

        /// <summary>
        /// Returns a hash code based on the contents of the given array. For any two
        /// <typeparamref name="T"/> arrays <c>a</c> and <c>b</c>, if
        /// <c>Arrays.Equals(b)</c> returns <c>true</c>, it means
        /// that the return value of <c>Arrays.GetHashCode(a)</c> equals <c>Arrays.GetHashCode(b)</c>.
        /// </summary>
        /// <typeparam name="T">The array element type.</typeparam>
        /// <param name="array">The array whose hash code to compute.</param>
        /// <returns>The hash code for <paramref name="array"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetHashCode<T>(T[] array)
        {
            return ArrayEqualityComparer<T>.OneDimensional.GetHashCode(array);
        }

        /// <summary>
        /// Assigns the specified value to each element of the specified array.
        /// </summary>
        /// <typeparam name="T">the type of the array</typeparam>
        /// <param name="a">the array to be filled</param>
        /// <param name="val">the value to be stored in all elements of the array</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Fill<T>(T[] a, T val)
        {
            ArrayFiller<T>.Default.Fill(a, val, 0, a.Length);
        }

        /// <summary>
        /// Assigns the specified long value to each element of the specified
        /// range of the specified array of longs.  The range to be filled
        /// extends from index <paramref name="fromIndex"/>, inclusive, to index
        /// <paramref name="toIndex"/>, exclusive.  (If <c>fromIndex==toIndex</c>, the
        /// range to be filled is empty.)
        /// </summary>
        /// <typeparam name="T">the type of the array</typeparam>
        /// <param name="a">the array to be filled</param>
        /// <param name="fromIndex">
        /// the index of the first element (inclusive) to be
        /// filled with the specified value
        /// </param>
        /// <param name="toIndex">
        /// the index of the last element (exclusive) to be
        /// filled with the specified value
        /// </param>
        /// <param name="val">the value to be stored in all elements of the array</param>
        /// <exception cref="ArgumentException">if <c>fromIndex &gt; toIndex</c></exception>
        /// <exception cref="ArgumentOutOfRangeException">if <c>fromIndex &lt; 0</c> or <c>toIndex &gt; a.Length</c></exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Fill<T>(T[] a, int fromIndex, int toIndex, T val)
        {
            //Java Arrays.fill exception logic
            if (fromIndex > toIndex)
                throw new ArgumentException("fromIndex(" + fromIndex + ") > toIndex(" + toIndex + ")");
            if (fromIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(fromIndex));
            if (toIndex > a.Length)
                throw new ArgumentOutOfRangeException(nameof(toIndex));

            int length = toIndex - fromIndex;
            ArrayFiller<T>.Default.Fill(a, val, fromIndex, length);
        }

        #region ArrayFiller<T>
        private static class ArrayFiller<T>
        {
            public static readonly IArrayFiller<T> Default = LoadArrayFiller();

            private static IArrayFiller<T> LoadArrayFiller()
            {
#if FEATURE_ARRAY_FILL
                if (PlatformDetection.IsNetCore)
                    return new SpanFillArrayFiller<T>();

                return new ArrayFillArrayFiller<T>();
#else
                return new SpanFillArrayFiller<T>();
#endif
            }

        }

        private interface IArrayFiller<in T>
        {
            void Fill(T[] array, T value, int startIndex, int count);
        }

#if FEATURE_ARRAY_FILL
        private sealed class ArrayFillArrayFiller<T> : IArrayFiller<T>
        {
            public void Fill(T[] array, T value, int startIndex, int count)
            {
                Array.Fill(array, value, startIndex, count);
            }
        }
#endif
        private sealed class SpanFillArrayFiller<T> : IArrayFiller<T>
        {
            public void Fill(T[] array, T value, int startIndex, int count)
            {
                array.AsSpan(startIndex, count).Fill(value);
            }
        }

        #endregion ArrayFiller<T>

        /// <summary>
        /// Copies a range of elements from an Array starting at the first element and pastes them
        /// into another Array starting at the first element. The length is specified as a 32-bit integer.
        /// <para/>
        /// <b>Usage Note:</b> This implementation uses the most efficient (known) method for copying the
        /// array based on the data type and platform.
        /// </summary>
        /// <typeparam name="T">The array type.</typeparam>
        /// <param name="sourceArray">The Array that contains the data to copy.</param>
        /// <param name="destinationArray">The Array that receives the data.</param>
        /// <param name="length">A 32-bit integer that represents the number of elements to copy.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Copy<T>(T[] sourceArray, T[] destinationArray, int length)
        {
            if (length == 0)
                return;

            if (Debugging.AssertsEnabled) // LUCENENET: Since this is internal, we are relying on Debugging.Assert to ensure the values passed in are correct.
            {
                Debugging.Assert(sourceArray is not null);
                Debugging.Assert(destinationArray is not null);
                Debugging.Assert(length >= 0 || length <= sourceArray.Length || length <= destinationArray.Length);
            }

            ArrayCopier<T>.Default.Copy(sourceArray, sourceIndex: 0, destinationArray, destinationIndex: 0, length);
        }

        /// <summary>
        /// Copies a range of elements from an Array starting at the specified source index and pastes them
        /// to another Array starting at the specified destination index. The length and the indexes are
        /// specified as 32-bit integers.
        /// <para/>
        /// <b>Usage Note:</b> This implementation uses the most efficient (known) method for copying the
        /// array based on the data type and platform.
        /// </summary>
        /// <typeparam name="T">The array type.</typeparam>
        /// <param name="sourceArray">The Array that contains the data to copy.</param>
        /// <param name="sourceIndex">A 32-bit integer that represents the index in the
        /// <paramref name="sourceArray"/> at which copying begins.</param>
        /// <param name="destinationArray">The Array that receives the data.</param>
        /// <param name="destinationIndex">A 32-bit integer that represents the index in the
        /// <paramref name="destinationArray"/> at which storing begins.</param>
        /// <param name="length">A 32-bit integer that represents the number of elements to copy.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Copy<T>(T[] sourceArray, int sourceIndex, T[] destinationArray, int destinationIndex, int length)
        {
            if (length == 0)
                return;

            if (Debugging.AssertsEnabled) // LUCENENET: Since this is internal, we are relying on Debugging.Assert to ensure the values passed in are correct.
            {
                Debugging.Assert(sourceArray is not null);
                Debugging.Assert(destinationArray is not null);
                Debugging.Assert(sourceIndex >= 0 || sourceIndex <= sourceArray.Length - length);
                Debugging.Assert(destinationIndex >= 0 || destinationIndex <= destinationArray.Length - length);
                Debugging.Assert(length >= 0 || length < sourceArray.Length || length < destinationArray.Length);
            }

            ArrayCopier<T>.Default.Copy(sourceArray, sourceIndex, destinationArray, destinationIndex, length);
        }

        [SuppressMessage("CodeQuality", "IDE0079:Remove unnecessary suppression", Justification = "This is a SonarCloud issue")]
        [SuppressMessage("CodeQuality", "S3400:Methods should not return constants", Justification = "Clearly, this is not always a constant value (SonarCloud bug)")]
        private static class PlatformDetection
        {
            // We put this in its own class so every type doesn't have to reload it. But, at the same time,
            // we don't want to have to load this just to use the Arrays class.
            public static readonly bool IsFullFramework = LoadIsFullFramework();
            public static readonly bool IsNetCore = LoadIsNetCore();

            private static bool LoadIsFullFramework()
            {
#if NETSTANDARD2_0
                return RuntimeInformation.FrameworkDescription.StartsWith(".NET Framework", StringComparison.OrdinalIgnoreCase);
#elif NET40_OR_GREATER
                return true;
#else
                return false;
#endif
            }

            private static bool LoadIsNetCore()
            {
#if NETSTANDARD2_0_OR_GREATER
                return RuntimeInformation.FrameworkDescription.StartsWith(".NET Core", StringComparison.OrdinalIgnoreCase);
#elif NET5_0_OR_GREATER || NETCOREAPP1_0_OR_GREATER
                return true;
#else
                return false;
#endif
            }
        }

        #region ArrayCopier<T>

        private static class ArrayCopier<T>
        {
            public static readonly IArrayCopier<T> Default = LoadArrayCopier();

            private static IArrayCopier<T> LoadArrayCopier()
            {
                // Default to Array.Copy() for unknown platforms (Span<T>.Copy() is slow on Mono)
                if (!PlatformDetection.IsFullFramework && !PlatformDetection.IsNetCore)
                    return new SystemArrayCopyArrayCopier<T>();

                var type = typeof(T);

                if (!type.IsValueType) // Reference types
                {
                    // Span<T> and Memory<T> are horribly slow on.NET Framework for
                    // copying arrays of reference types.
                    if (PlatformDetection.IsFullFramework)
                        return new SystemArrayCopyArrayCopier<T>();

                    return new SpanArrayCopier<T>();
                }

                // Value types are generally tied with or faster than Buffer.MemoryCopy() using
                // Span<T>.Copy() on Linux and macOS.
                if (!Constants.WINDOWS && !PlatformDetection.IsFullFramework)
                    return new SpanArrayCopier<T>();

                if (type == typeof(byte))
                {
                    // On Windows, copying bytes with Buffer.MemoryCopy() is fastest.
                    return (IArrayCopier<T>)(object)new ByteBufferMemoryCopyArrayCopier();
                }
                else if (type == typeof(sbyte))
                {
                    return (IArrayCopier<T>)(object)new SByteBufferMemoryCopyArrayCopier();
                }
                else if (PlatformDetection.IsFullFramework)
                {
                    // .NET Framework is 2-3x faster to use Buffer.MemoryCopy() than any other method for primitive types.
                    if (type == typeof(short))
                        return (IArrayCopier<T>)(object)new Int16BufferMemoryCopyArrayCopier();
                    if (type == typeof(ushort))
                        return (IArrayCopier<T>)(object)new UInt16BufferMemoryCopyArrayCopier();
                    if (type == typeof(int))
                        return (IArrayCopier<T>)(object)new Int32BufferMemoryCopyArrayCopier();
                    if (type == typeof(uint))
                        return (IArrayCopier<T>)(object)new UInt32BufferMemoryCopyArrayCopier();
                    if (type == typeof(long))
                        return (IArrayCopier<T>)(object)new Int64BufferMemoryCopyArrayCopier();
                    if (type == typeof(ulong))
                        return (IArrayCopier<T>)(object)new UInt64BufferMemoryCopyArrayCopier();
                    if (type == typeof(float))
                        return (IArrayCopier<T>)(object)new SingleBufferMemoryCopyArrayCopier();
                    if (type == typeof(double))
                        return (IArrayCopier<T>)(object)new DoubleBufferMemoryCopyArrayCopier();
                    if (type == typeof(char))
                        return (IArrayCopier<T>)(object)new CharBufferMemoryCopyArrayCopier();
                    if (type == typeof(bool))
                        return (IArrayCopier<T>)(object)new BooleanBufferMemoryCopyArrayCopier();

                    return new SystemArrayCopyArrayCopier<T>();
                }
                else
                {
                    return new SpanArrayCopier<T>();
                }
            }
        }

        private interface IArrayCopier<in T>
        {
            void Copy(T[] sourceArray, int sourceIndex, T[] destinationArray, int destinationIndex, int length);
        }

        private sealed class SpanArrayCopier<T> : IArrayCopier<T>
        {
            public void Copy(T[] sourceArray, int sourceIndex, T[] destinationArray, int destinationIndex, int length)
            {
                sourceArray.AsSpan(sourceIndex, length).CopyTo(destinationArray.AsSpan(destinationIndex, length));
            }
        }

        private sealed class SystemArrayCopyArrayCopier<T> : IArrayCopier<T>
        {
            public void Copy(T[] sourceArray, int sourceIndex, T[] destinationArray, int destinationIndex, int length)
            {
                Array.Copy(sourceArray, sourceIndex, destinationArray, destinationIndex, length);
            }
        }

        #region Primitive Type Buffer.MemoryCopy() Array Copiers
        // We save some arithmetic by having a specialized type for byte, since we know it is measured in bytes.
        private sealed class ByteBufferMemoryCopyArrayCopier : IArrayCopier<byte>
        {
            public void Copy(byte[] sourceArray, int sourceIndex, byte[] destinationArray, int destinationIndex, int length)
            {
                unsafe
                {
                    fixed (byte* sourcePointer = &sourceArray[sourceIndex], destinationPointer = &destinationArray[destinationIndex])
                    {
                        // NOTE: We are relying on the fact that passing the pointers into this method is creating copies of them
                        // that are not fixed.
                        Buffer.MemoryCopy(sourcePointer, destinationPointer, destinationArray.Length - destinationIndex, length);
                    }
                }
            }
        }

        // We save some arithmetic by having a specialized type for byte, since we know it is measured in bytes.
        private sealed class SByteBufferMemoryCopyArrayCopier : IArrayCopier<sbyte>
        {
            public void Copy(sbyte[] sourceArray, int sourceIndex, sbyte[] destinationArray, int destinationIndex, int length)
            {
                unsafe
                {
                    fixed (sbyte* sourcePointer = &sourceArray[sourceIndex], destinationPointer = &destinationArray[destinationIndex])
                    {
                        // NOTE: We are relying on the fact that passing the pointers into this method is creating copies of them
                        // that are not fixed.
                        Buffer.MemoryCopy(sourcePointer, destinationPointer, destinationArray.Length - destinationIndex, length);
                    }
                }
            }
        }

        // LUCENENET NOTE: Tried to make the following types one generic type, but SDKs prior to .NET 7 won't compile it.
        // See: https://github.com/dotnet/runtime/issues/76255

        private sealed class Int16BufferMemoryCopyArrayCopier : IArrayCopier<short>
        {
            public void Copy(short[] sourceArray, int sourceIndex, short[] destinationArray, int destinationIndex, int length)
            {
                unsafe
                {
                    fixed (short* sourcePointer = &sourceArray[sourceIndex], destinationPointer = &destinationArray[destinationIndex])
                    {
                        int size = sizeof(short);
                        long destinationSizeInBytes = (destinationArray.Length - destinationIndex) * size;
                        long sourceBytesToCopy = length * size;
                        // NOTE: We are relying on the fact that passing the pointers into this method is creating copies of them
                        // that are not fixed.
                        Buffer.MemoryCopy(sourcePointer, destinationPointer, destinationSizeInBytes, sourceBytesToCopy);
                    }
                }
            }
        }

        private sealed class UInt16BufferMemoryCopyArrayCopier : IArrayCopier<ushort>
        {
            public void Copy(ushort[] sourceArray, int sourceIndex, ushort[] destinationArray, int destinationIndex, int length)
            {
                unsafe
                {
                    fixed (ushort* sourcePointer = &sourceArray[sourceIndex], destinationPointer = &destinationArray[destinationIndex])
                    {
                        int size = sizeof(ushort);
                        long destinationSizeInBytes = (destinationArray.Length - destinationIndex) * size;
                        long sourceBytesToCopy = length * size;
                        // NOTE: We are relying on the fact that passing the pointers into this method is creating copies of them
                        // that are not fixed.
                        Buffer.MemoryCopy(sourcePointer, destinationPointer, destinationSizeInBytes, sourceBytesToCopy);
                    }
                }
            }
        }

        private sealed class Int32BufferMemoryCopyArrayCopier : IArrayCopier<int>
        {
            public void Copy(int[] sourceArray, int sourceIndex, int[] destinationArray, int destinationIndex, int length)
            {
                unsafe
                {
                    fixed (int* sourcePointer = &sourceArray[sourceIndex], destinationPointer = &destinationArray[destinationIndex])
                    {
                        int size = sizeof(int);
                        long destinationSizeInBytes = (destinationArray.Length - destinationIndex) * size;
                        long sourceBytesToCopy = length * size;
                        // NOTE: We are relying on the fact that passing the pointers into this method is creating copies of them
                        // that are not fixed.
                        Buffer.MemoryCopy(sourcePointer, destinationPointer, destinationSizeInBytes, sourceBytesToCopy);
                    }
                }
            }
        }

        private sealed class UInt32BufferMemoryCopyArrayCopier : IArrayCopier<uint>
        {
            public void Copy(uint[] sourceArray, int sourceIndex, uint[] destinationArray, int destinationIndex, int length)
            {
                unsafe
                {
                    fixed (uint* sourcePointer = &sourceArray[sourceIndex], destinationPointer = &destinationArray[destinationIndex])
                    {
                        int size = sizeof(uint);
                        long destinationSizeInBytes = (destinationArray.Length - destinationIndex) * size;
                        long sourceBytesToCopy = length * size;
                        // NOTE: We are relying on the fact that passing the pointers into this method is creating copies of them
                        // that are not fixed.
                        Buffer.MemoryCopy(sourcePointer, destinationPointer, destinationSizeInBytes, sourceBytesToCopy);
                    }
                }
            }
        }

        private sealed class Int64BufferMemoryCopyArrayCopier : IArrayCopier<long>
        {
            public void Copy(long[] sourceArray, int sourceIndex, long[] destinationArray, int destinationIndex, int length)
            {
                unsafe
                {
                    fixed (long* sourcePointer = &sourceArray[sourceIndex], destinationPointer = &destinationArray[destinationIndex])
                    {
                        int size = sizeof(long);
                        long destinationSizeInBytes = (destinationArray.Length - destinationIndex) * size;
                        long sourceBytesToCopy = length * size;
                        // NOTE: We are relying on the fact that passing the pointers into this method is creating copies of them
                        // that are not fixed.
                        Buffer.MemoryCopy(sourcePointer, destinationPointer, destinationSizeInBytes, sourceBytesToCopy);
                    }
                }
            }
        }

        private sealed class UInt64BufferMemoryCopyArrayCopier : IArrayCopier<ulong>
        {
            public void Copy(ulong[] sourceArray, int sourceIndex, ulong[] destinationArray, int destinationIndex, int length)
            {
                unsafe
                {
                    fixed (ulong* sourcePointer = &sourceArray[sourceIndex], destinationPointer = &destinationArray[destinationIndex])
                    {
                        int size = sizeof(ulong);
                        long destinationSizeInBytes = (destinationArray.Length - destinationIndex) * size;
                        long sourceBytesToCopy = length * size;
                        // NOTE: We are relying on the fact that passing the pointers into this method is creating copies of them
                        // that are not fixed.
                        Buffer.MemoryCopy(sourcePointer, destinationPointer, destinationSizeInBytes, sourceBytesToCopy);
                    }
                }
            }
        }

        private sealed class SingleBufferMemoryCopyArrayCopier : IArrayCopier<float>
        {
            public void Copy(float[] sourceArray, int sourceIndex, float[] destinationArray, int destinationIndex, int length)
            {
                unsafe
                {
                    fixed (float* sourcePointer = &sourceArray[sourceIndex], destinationPointer = &destinationArray[destinationIndex])
                    {
                        int size = sizeof(float);
                        long destinationSizeInBytes = (destinationArray.Length - destinationIndex) * size;
                        long sourceBytesToCopy = length * size;
                        // NOTE: We are relying on the fact that passing the pointers into this method is creating copies of them
                        // that are not fixed.
                        Buffer.MemoryCopy(sourcePointer, destinationPointer, destinationSizeInBytes, sourceBytesToCopy);
                    }
                }
            }
        }

        private sealed class DoubleBufferMemoryCopyArrayCopier : IArrayCopier<double>
        {
            public void Copy(double[] sourceArray, int sourceIndex, double[] destinationArray, int destinationIndex, int length)
            {
                unsafe
                {
                    fixed (double* sourcePointer = &sourceArray[sourceIndex], destinationPointer = &destinationArray[destinationIndex])
                    {
                        int size = sizeof(double);
                        long destinationSizeInBytes = (destinationArray.Length - destinationIndex) * size;
                        long sourceBytesToCopy = length * size;
                        // NOTE: We are relying on the fact that passing the pointers into this method is creating copies of them
                        // that are not fixed.
                        Buffer.MemoryCopy(sourcePointer, destinationPointer, destinationSizeInBytes, sourceBytesToCopy);
                    }
                }
            }
        }

        private sealed class CharBufferMemoryCopyArrayCopier : IArrayCopier<char>
        {
            public void Copy(char[] sourceArray, int sourceIndex, char[] destinationArray, int destinationIndex, int length)
            {
                unsafe
                {
                    fixed (char* sourcePointer = &sourceArray[sourceIndex], destinationPointer = &destinationArray[destinationIndex])
                    {
                        int size = sizeof(char);
                        long destinationSizeInBytes = (destinationArray.Length - destinationIndex) * size;
                        long sourceBytesToCopy = length * size;
                        // NOTE: We are relying on the fact that passing the pointers into this method is creating copies of them
                        // that are not fixed.
                        Buffer.MemoryCopy(sourcePointer, destinationPointer, destinationSizeInBytes, sourceBytesToCopy);
                    }
                }
            }
        }

        private sealed class BooleanBufferMemoryCopyArrayCopier : IArrayCopier<bool>
        {
            public void Copy(bool[] sourceArray, int sourceIndex, bool[] destinationArray, int destinationIndex, int length)
            {
                unsafe
                {
                    fixed (bool* sourcePointer = &sourceArray[sourceIndex], destinationPointer = &destinationArray[destinationIndex])
                    {
                        int size = sizeof(bool);
                        long destinationSizeInBytes = (destinationArray.Length - destinationIndex) * size;
                        long sourceBytesToCopy = length * size;
                        // NOTE: We are relying on the fact that passing the pointers into this method is creating copies of them
                        // that are not fixed.
                        Buffer.MemoryCopy(sourcePointer, destinationPointer, destinationSizeInBytes, sourceBytesToCopy);
                    }
                }
            }
        }

        // Intentionally not adding support for IntPtr and UIntPtr

        #endregion Primitive Type Buffer.MemoryCopy() Array Copiers

        #endregion ArrayCopier<T>


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T[] CopyOf<T>(T[] original, int newLength)
        {
            T[] newArray = new T[newLength];
            Copy(original, newArray, Math.Min(original.Length, newLength));
            return newArray;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T[] CopyOfRange<T>(T[] original, int startIndexInc, int endIndexExc)
        {
            int newLength = endIndexExc - startIndexInc;
            T[] newArray = new T[newLength];
            Copy(original, startIndexInc, newArray, 0, newLength);
            return newArray;
        }

        /// <summary>
        /// Creates a <see cref="string"/> representation of the array passed.
        /// The result is surrounded by brackets <c>"[]"</c>, each
        /// element is converted to a <see cref="string"/> via the
        /// <see cref="J2N.Text.StringFormatter.InvariantCulture"/> and separated by <c>", "</c>. If
        /// the array is <c>null</c>, then <c>"null"</c> is returned.
        /// </summary>
        /// <typeparam name="T">The type of array element.</typeparam>
        /// <param name="array">The array to convert.</param>
        /// <returns>The converted array string.</returns>
        public static string ToString<T>(T[] array)
        {
            if (array is null)
                return "null"; //$NON-NLS-1$
            if (array.Length == 0)
                return "[]"; //$NON-NLS-1$
            StringBuilder sb = new StringBuilder(2 + array.Length * 4);
            sb.Append('[');
            sb.AppendFormat(J2N.Text.StringFormatter.InvariantCulture, "{0}", array[0]);
            for (int i = 1; i < array.Length; i++)
            {
                sb.Append(", "); //$NON-NLS-1$
                sb.AppendFormat(J2N.Text.StringFormatter.InvariantCulture, "{0}", array[i]);
            }
            sb.Append(']');
            return sb.ToString();
        }

        /// <summary>
        /// Creates a <see cref="string"/> representation of the array passed.
        /// The result is surrounded by brackets <c>"[]"</c>, each
        /// element is converted to a <see cref="string"/> via the
        /// <paramref name="provider"/> and separated by <c>", "</c>. If
        /// the array is <c>null</c>, then <c>"null"</c> is returned.
        /// </summary>
        /// <typeparam name="T">The type of array element.</typeparam>
        /// <param name="array">The array to convert.</param>
        /// <param name="provider">A <see cref="IFormatProvider"/> instance that supplies the culture formatting information.</param>
        /// <returns>The converted array string.</returns>
        public static string ToString<T>(T[] array, IFormatProvider provider)
        {
            if (array is null)
                return "null"; //$NON-NLS-1$
            if (array.Length == 0)
                return "[]"; //$NON-NLS-1$
            StringBuilder sb = new StringBuilder(2 + array.Length * 4);
            sb.Append('[');
            sb.AppendFormat(provider, "{0}", array[0]);
            for (int i = 1; i < array.Length; i++)
            {
                sb.Append(", "); //$NON-NLS-1$
                sb.AppendFormat(provider, "{0}", array[i]);
            }
            sb.Append(']');
            return sb.ToString();
        }

        /// <summary>
        /// Returns an empty array.
        /// </summary>
        /// <typeparam name="T">The type of the elements of the array.</typeparam>
        /// <returns>An empty array.</returns>
        // LUCENENET: Since Array.Empty<T>() doesn't exist in all supported platforms, we
        // have this wrapper method to add support.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T[] Empty<T>()
        {
#if FEATURE_ARRAYEMPTY
            return Array.Empty<T>();
#else
            return EmptyArrayHolder<T>.EMPTY;
#endif
        }

#if !FEATURE_ARRAYEMPTY
        private static class EmptyArrayHolder<T>
        {
#pragma warning disable CA1825 // Avoid zero-length array allocations.
            public static readonly T[] EMPTY = new T[0];
#pragma warning restore CA1825 // Avoid zero-length array allocations.
        }
#endif
    }
}
