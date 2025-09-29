// Based on: https://github.com/dotnet/runtime/blob/v9.0.1/src/libraries/System.Private.CoreLib/src/System/MemoryExtensions.cs
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Lucene.Net.Analysis.TokenAttributes;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Lucene.Net
{
    /// <summary>
    /// Extensions to System.Memory types, such as <see cref="Span{T}"/>
    /// and <see cref="ReadOnlySpan{T}"/>.
    /// </summary>
    public static class MemoryExtensions
    {
        #region AsSpan (ICharTermAttribute)

        /// <summary>
        /// Creates a new readonly span over the portion of the target string.
        /// </summary>
        /// <param name="text">The target string.</param>
        /// <returns>The read-only span representation of the string.</returns>
        /// <remarks>Returns <c>default</c> when <paramref name="text"/> is <c>null</c>.</remarks>
        public static ReadOnlySpan<char> AsSpan(this ICharTermAttribute text)
        {
            if (text is null)
                return default;

            char[] chars = text is CharTermAttribute c ? c.termBuffer : text.Buffer;

#if FEATURE_MEMORYMARSHAL_CREATEREADONLYSPAN && FEATURE_MEMORYMARSHAL_GETARRAYDATAREFERENCE
            return MemoryMarshal.CreateReadOnlySpan<char>(ref MemoryMarshal.GetArrayDataReference(chars), text.Length);
#else
            return new ReadOnlySpan<char>(chars, 0, text.Length);
#endif
        }

        /// <summary>
        /// Creates a new read-only span over a portion of the target string from
        /// a specified position to the end of the string.
        /// </summary>
        /// <param name="text">The target string.</param>
        /// <param name="start">The index at which to begin this slice.</param>
        /// <returns>The read-only span representation of the string.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="start"/> is less than 0 or greater than <c>text.Length</c>.
        /// </exception>
        /// <exception cref="ArgumentNullException"><paramref name="text"/> is <c>null</c>.</exception>
        public static ReadOnlySpan<char> AsSpan(this ICharTermAttribute text, int start)
        {
            if (text == null)
            {
                if (start != 0)
                    throw new ArgumentOutOfRangeException(nameof(start));

                return default;
            }

            if ((uint)start > (uint)text.Length)
                throw new ArgumentOutOfRangeException(nameof(start));

            char[] chars = text is CharTermAttribute c ? c.termBuffer : text.Buffer;

#if FEATURE_MEMORYMARSHAL_CREATEREADONLYSPAN && FEATURE_MEMORYMARSHAL_GETARRAYDATAREFERENCE
            return MemoryMarshal.CreateReadOnlySpan<char>(ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(chars),
                (nint)(uint)start /* force zero-extension */), text.Length - start);
#else
            return new ReadOnlySpan<char>(chars, start, text.Length - start);
#endif
        }

        /// <summary>
        /// Creates a new read-only span over a portion of the target string from a
        /// specified position for a specified number of characters.
        /// </summary>
        /// <param name="text">The target string.</param>
        /// <param name="start">The index at which to begin this slice.</param>
        /// <param name="length">The desired length for the slice.</param>
        /// <returns>The read-only span representation of the string.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="start"/>, <paramref name="length"/>, or
        /// <paramref name="start"/> + <paramref name="length"/> is not
        /// in the range of <paramref name="text"/>.
        /// </exception>
        public static ReadOnlySpan<char> AsSpan(this ICharTermAttribute text, int start, int length)
        {
            if (text == null)
            {
                if (start != 0 || length != 0)
                    throw new ArgumentOutOfRangeException(nameof(start));
                return default;
            }

            if (IntPtr.Size == 8) // 64-bit process
            {
                // See comment in Span<T>.Slice for how this works.
                if ((ulong)(uint)start + (ulong)(uint)length > (ulong)(uint)text.Length)
                    throw new ArgumentOutOfRangeException(nameof(start));
            }
            else
            {
                if ((uint)start > (uint)text.Length || (uint)length > (uint)(text.Length - start))
                    throw new ArgumentOutOfRangeException(nameof(start));
            }

            char[] chars = text is CharTermAttribute c ? c.termBuffer : text.Buffer;

#if FEATURE_MEMORYMARSHAL_CREATEREADONLYSPAN && FEATURE_MEMORYMARSHAL_GETARRAYDATAREFERENCE
            return MemoryMarshal.CreateReadOnlySpan<char>(ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(chars),
                (nint)(uint)start /* force zero-extension */), length);
#else
            return new ReadOnlySpan<char>(chars, start, length);
#endif
        }

        /// <summary>
        /// Creates a new read-only span over a portion of the
        /// target string from a specified position to the end of the string.
        /// </summary>
        /// <param name="text">The target string.</param>
        /// <param name="startIndex">The index at which to begin this slice.</param>
        /// <returns>The read-only span representation of the string.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="text"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="startIndex"/> is less
        /// than 0 or greater than <c>text.Length</c>.</exception>
        public static ReadOnlySpan<char> AsSpan(this ICharTermAttribute text, System.Index startIndex)
        {
            if (text is null)
            {
                if (!startIndex.Equals(System.Index.Start))
                {
                    throw new ArgumentOutOfRangeException(nameof(startIndex));
                }

                return default;
            }

            int actualIndex = startIndex.GetOffset(text.Length);
            if ((uint)actualIndex > (uint)text.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(startIndex));
            }

            char[] chars = text is CharTermAttribute c ? c.termBuffer : text.Buffer;

#if FEATURE_MEMORYMARSHAL_CREATEREADONLYSPAN && FEATURE_MEMORYMARSHAL_GETARRAYDATAREFERENCE
            return MemoryMarshal.CreateReadOnlySpan<char>(ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(chars),
                (nint)(uint)actualIndex /* force zero-extension */), text.Length - actualIndex);
#else
            return new ReadOnlySpan<char>(chars, actualIndex, text.Length - actualIndex);
#endif
        }

        /// <summary>
        /// Creates a new read-only span over a portion of a target string
        /// using the range start and end indexes.
        /// </summary>
        /// <param name="text">The target string.</param>
        /// <param name="range">The range that has start and end indexes to use for slicing the string.</param>
        /// <returns>The read-only span representation of the string.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="text"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="range"/>'s start or end index is not within the bounds of the string.
        /// -or-
        /// <paramref name="range"/>'s start index is greater than its end index.
        /// </exception>
        public static ReadOnlySpan<char> AsSpan(this ICharTermAttribute text, Range range)
        {
            if (text is null)
            {
                System.Index startIndex = range.Start;
                System.Index endIndex = range.End;

                if (!startIndex.Equals(System.Index.Start) || !endIndex.Equals(System.Index.Start))
                {
                    throw new ArgumentNullException(nameof(text));
                }

                return default;
            }

            (int start, int length) = range.GetOffsetAndLength(text.Length);
            char[] chars = text is CharTermAttribute c ? c.termBuffer : text.Buffer;

#if FEATURE_MEMORYMARSHAL_CREATEREADONLYSPAN && FEATURE_MEMORYMARSHAL_GETARRAYDATAREFERENCE
            return MemoryMarshal.CreateReadOnlySpan<char>(ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(chars),
                (nint)(uint)start /* force zero-extension */), length);
#else
            return new ReadOnlySpan<char>(chars, start, length);
#endif
        }

        #endregion AsSpan (ICharTermAttribute)

        #region AsMemory (ICharTermAttribute)

        /// <summary>
        /// Creates a new <see cref="ReadOnlyMemory{T}"/> over the portion of the target string.
        /// </summary>
        /// <param name="text">The target string.</param>
        /// <returns>The read-only character memory representation of the string, or <c>default</c> if
        /// <paramref name="text"/> is <c>null</c>.</returns>
        /// <remarks>Returns default when <paramref name="text"/> is <c>null</c>.</remarks>
        public static ReadOnlyMemory<char> AsMemory(this ICharTermAttribute text)
        {
            if (text is null)
                return default;

            char[] chars = text is CharTermAttribute c ? c.termBuffer : text.Buffer;
            return new ReadOnlyMemory<char>(chars, 0, text.Length);
        }

        /// <summary>
        /// Creates a new <see cref="ReadOnlyMemory{T}"/> over the portion of the target string.
        /// </summary>
        /// <param name="text">The target string.</param>
        /// <param name="start">The index at which to begin this slice.</param>
        /// <returns>Returns default when <paramref name="text"/> is null.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="start"/> is not in range of <paramref name="text"/>
        /// (<paramref name="start"/> is &lt;0 or &gt;<c>text.Length</c>).
        /// </exception>
        /// <remarks>Returns default when <paramref name="text"/> is <c>null</c>.</remarks>
        public static ReadOnlyMemory<char> AsMemory(this ICharTermAttribute text, int start)
        {
            if (text == null)
            {
                if (start != 0)
                    throw new ArgumentOutOfRangeException(nameof(start));

                return default;
            }

            if ((uint)start > (uint)text.Length)
                throw new ArgumentOutOfRangeException(nameof(start));

            char[] chars = text is CharTermAttribute c ? c.termBuffer : text.Buffer;
            return new ReadOnlyMemory<char>(chars, start, text.Length - start);
        }

        /// <summary>
        /// Creates a new <see cref="ReadOnlyMemory{T}"/> over the portion of the target string.
        /// </summary>
        /// <param name="text">The target string.</param>
        /// <param name="start">The index at which to begin this slice.</param>
        /// <param name="length">The desired length for the slice (exclusive).</param>
        /// <returns>The read-only character memory representation of the string, or <c>default</c>
        /// if <paramref name="text"/> is <c>null</c>.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="start"/>, <paramref name="length"/>,
        /// or <paramref name="start"/> + <paramref name="length"/> is not in the range of <paramref name="text"/>.</exception>
        /// <remarks>Returns <c>default</c> when <paramref name="text"/> is <c>null</c>.</remarks>
        public static ReadOnlyMemory<char> AsMemory(this ICharTermAttribute text, int start, int length)
        {
            if (text == null)
            {
                if (start != 0 || length != 0)
                    throw new ArgumentOutOfRangeException(nameof(start));

                return default;
            }

            if (IntPtr.Size == 8) // 64-bit process
            {
                // See comment in Span<T>.Slice for how this works.
                if ((ulong)(uint)start + (ulong)(uint)length > (ulong)(uint)text.Length)
                    throw new ArgumentOutOfRangeException(nameof(start));
            }
            else
            {
                if ((uint)start > (uint)text.Length || (uint)length > (uint)(text.Length - start))
                    throw new ArgumentOutOfRangeException(nameof(start));
            }

            char[] chars = text is CharTermAttribute c ? c.termBuffer : text.Buffer;
            return new ReadOnlyMemory<char>(chars, start, length);
        }

        /// <summary>
        /// Creates a new <see cref="ReadOnlyMemory{T}"/> over the portion of the target string.
        /// </summary>
        /// <param name="text">The target string.</param>
        /// <param name="startIndex">The index at which to begin this slice.</param>
        /// <returns>The read-only character memory representation of the string.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="text"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="startIndex"/> is less
        /// than 0 or greater than <c>text.Length</c>.</exception>
        public static ReadOnlyMemory<char> AsMemory(this ICharTermAttribute text, System.Index startIndex)
        {
            if (text == null)
            {
                if (!startIndex.Equals(System.Index.Start))
                    throw new ArgumentNullException(nameof(text));

                return default;
            }

            int actualIndex = startIndex.GetOffset(text.Length);
            if ((uint)actualIndex > (uint)text.Length)
                throw new ArgumentOutOfRangeException();

            char[] chars = text is CharTermAttribute c ? c.termBuffer : text.Buffer;
            return new ReadOnlyMemory<char>(chars, actualIndex, text.Length - actualIndex);
        }

        /// <summary>
        /// Creates a new <see cref="ReadOnlyMemory{T}"/> over the portion of the target string.
        /// </summary>
        /// <param name="text">The target string.</param>
        /// <param name="range">The range used to indicate the start and length of the sliced string.</param>
        /// <returns>The read-only character memory representation of the string.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="text"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="range"/>'s start or end index is not within the bounds of the string.
        /// -or-
        /// <paramref name="range"/>'s start index is greater than its end index.
        /// </exception>
        public static ReadOnlyMemory<char> AsMemory(this ICharTermAttribute text, Range range)
        {
            if (text == null)
            {
                System.Index startIndex = range.Start;
                System.Index endIndex = range.End;

                if (!startIndex.Equals(System.Index.Start) || !endIndex.Equals(System.Index.Start))
                    throw new ArgumentNullException(nameof(text));

                return default;
            }

            (int start, int length) = range.GetOffsetAndLength(text.Length);
            char[] chars = text is CharTermAttribute c ? c.termBuffer : text.Buffer;
            return new ReadOnlyMemory<char>(chars, start, length);
        }

        #endregion AsMemory (ICharTermAttribute)
    }
}
