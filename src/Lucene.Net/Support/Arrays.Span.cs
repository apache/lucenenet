using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.Support
{
    internal static partial class Arrays
    {
        // Span overloads of array methods to make usage syntax similar to Lucene.

        /// <summary>
        /// Copies a range of elements from an Array starting at the first element and pastes them
        /// into a Span starting at the first element. The length is specified as a 32-bit integer.
        /// </summary>
        /// <typeparam name="T">The element type.</typeparam>
        /// <param name="sourceArray">The Array that contains the data to copy.</param>
        /// <param name="destinationArray">The Span that receives the data.</param>
        /// <param name="length">A 32-bit integer that represents the number of elements to copy.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Copy<T>(T[] sourceArray, Span<T> destinationArray, int length)
        {
            sourceArray.AsSpan(0, length).CopyTo(destinationArray);
        }

        /// <summary>
        /// Copies a range of elements from a Span starting at the first element and pastes them
        /// into an Array starting at the first element. The length is specified as a 32-bit integer.
        /// </summary>
        /// <typeparam name="T">The element type.</typeparam>
        /// <param name="sourceArray">The Span that contains the data to copy.</param>
        /// <param name="destinationArray">The Array that receives the data.</param>
        /// <param name="length">A 32-bit integer that represents the number of elements to copy.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Copy<T>(ReadOnlySpan<T> sourceArray, T[] destinationArray, int length)
        {
            sourceArray.Slice(0, length).CopyTo(destinationArray);
        }

        /// <summary>
        /// Copies a range of elements from a Span starting at the first element and pastes them
        /// into another Span starting at the first element. The length is specified as a 32-bit integer.
        /// </summary>
        /// <typeparam name="T">The element type.</typeparam>
        /// <param name="sourceArray">The Span that contains the data to copy.</param>
        /// <param name="destinationArray">The Span that receives the data.</param>
        /// <param name="length">A 32-bit integer that represents the number of elements to copy.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Copy<T>(ReadOnlySpan<T> sourceArray, Span<T> destinationArray, int length)
        {
            sourceArray.Slice(0, length).CopyTo(destinationArray);
        }

        /// <summary>
        /// Copies a range of elements from an Array starting at the specified source index and pastes them
        /// to a Span starting at the specified destination index. The length and the indexes are
        /// specified as 32-bit integers.
        /// </summary>
        /// <typeparam name="T">The element type.</typeparam>
        /// <param name="sourceArray">The Array that contains the data to copy.</param>
        /// <param name="sourceIndex">A 32-bit integer that represents the index in the
        /// <paramref name="sourceArray"/> at which copying begins.</param>
        /// <param name="destinationArray">The Span that receives the data.</param>
        /// <param name="destinationIndex">A 32-bit integer that represents the index in the
        /// <paramref name="destinationArray"/> at which storing begins.</param>
        /// <param name="length">A 32-bit integer that represents the number of elements to copy.</param>

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Copy<T>(T[] sourceArray, int sourceIndex, Span<T> destinationArray, int destinationIndex, int length)
        {
            sourceArray.AsSpan(sourceIndex, length).CopyTo(destinationArray.Slice(destinationIndex));
        }

        /// <summary>
        /// Copies a range of elements from a Span starting at the specified source index and pastes them
        /// to an Array starting at the specified destination index. The length and the indexes are
        /// specified as 32-bit integers.
        /// </summary>
        /// <typeparam name="T">The element type.</typeparam>
        /// <param name="sourceArray">The Span that contains the data to copy.</param>
        /// <param name="sourceIndex">A 32-bit integer that represents the index in the
        /// <paramref name="sourceArray"/> at which copying begins.</param>
        /// <param name="destinationArray">The Array that receives the data.</param>
        /// <param name="destinationIndex">A 32-bit integer that represents the index in the
        /// <paramref name="destinationArray"/> at which storing begins.</param>
        /// <param name="length">A 32-bit integer that represents the number of elements to copy.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Copy<T>(ReadOnlySpan<T> sourceArray, int sourceIndex, T[] destinationArray, int destinationIndex, int length)
        {
            sourceArray.Slice(sourceIndex, length).CopyTo(destinationArray.AsSpan(destinationIndex));
        }

        /// <summary>
        /// Copies a range of elements from a Span starting at the specified source index and pastes them
        /// to another Span starting at the specified destination index. The length and the indexes are
        /// specified as 32-bit integers.
        /// </summary>
        /// <typeparam name="T">The element type.</typeparam>
        /// <param name="sourceArray">The Span that contains the data to copy.</param>
        /// <param name="sourceIndex">A 32-bit integer that represents the index in the
        /// <paramref name="sourceArray"/> at which copying begins.</param>
        /// <param name="destinationArray">The Span that receives the data.</param>
        /// <param name="destinationIndex">A 32-bit integer that represents the index in the
        /// <paramref name="destinationArray"/> at which storing begins.</param>
        /// <param name="length">A 32-bit integer that represents the number of elements to copy.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Copy<T>(ReadOnlySpan<T> sourceArray, int sourceIndex, Span<T> destinationArray, int destinationIndex, int length)
        {
            sourceArray.Slice(sourceIndex, length).CopyTo(destinationArray.Slice(destinationIndex));
        }
    }
}
