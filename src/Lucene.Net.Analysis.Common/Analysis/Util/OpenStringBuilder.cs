// Lucene version compatibility level 4.8.1
using J2N.Text;
using Lucene.Net.Support;
using System;
using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using WritableArrayAttribute = Lucene.Net.Support.WritableArrayAttribute;

namespace Lucene.Net.Analysis.Util
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
    /// A StringBuilder that allows one to access the array.
    /// </summary>
    /// <remarks>
    /// LUCENENET specific: This type implements <see cref="IBufferWriter{T}"/> to allow for efficient access to the underlying buffer.
    /// Note that if you hold a view into the buffer via either <see cref="GetMemory(int)"/> or <see cref="GetSpan(int)"/>,
    /// this view can be invalidated by any operation that changes the position or length of the buffer.
    /// It is recommended to avoid any non-<see cref="IBufferWriter{T}"/> operations while holding a view into the buffer.
    /// <para />
    /// This type also implements <see cref="ISpanAppendable"/> to allow for efficient appending of <see cref="ReadOnlySpan{T}"/>.
    /// </remarks>
    public class OpenStringBuilder : IAppendable, ISpanAppendable, ICharSequence, IBufferWriter<char>
    {
        protected char[] m_buf;
        protected int m_len;

        public OpenStringBuilder()
            : this(32)
        {
        }

        bool ICharSequence.HasValue => m_buf != null;

        public OpenStringBuilder(int size)
        {
            // LUCENENET specific - validate argument
            if (size <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(size), "size must be greater than zero");
            }

            m_buf = new char[size];
        }

        public OpenStringBuilder(char[] arr, int len)
        {
            // LUCENENET specific - calling private method instead of public virtual
            SetInternal(arr, len);
        }

        public virtual int Length
        {
            get => m_len;
            set => m_len = value;
        }

        public virtual void Set(char[] arr, int end) => SetInternal(arr, end);

        // LUCENENET specific - S1699 - introduced this to allow the constructor to
        // still call "Set" functionality without having to call the virtual method
        // that could be overridden by a subclass and don't have the state it expects
        private void SetInternal(char[] arr, int end)
        {
            this.m_buf = arr;
            this.m_len = end;
        }

        [WritableArray]
        [SuppressMessage("Microsoft.Performance", "CA1819", Justification = "Lucene's design requires some writable array properties")]
        public virtual char[] Array => m_buf;

        // LUCENENE NOTE: This is essentially a duplicate of Length (except that property can be set).
        // .NET uses Length for StringBuilder anyway, so that property is preferable to this one.
        //public virtual int Count // LUCENENET NOTE: This was size() in Lucene.
        //{
        //    get{ return m_len; }
        //}

        public virtual int Capacity => m_buf.Length;

        public virtual OpenStringBuilder Append(ICharSequence csq)
        {
            return Append(csq, 0, csq.Length);
        }

        public virtual OpenStringBuilder Append(ICharSequence csq, int startIndex, int count) // LUCENENET specific: changed to startIndex/length to match .NET
        {
            EnsureCapacity(count);
            UnsafeWrite(csq, startIndex, count);
            return this;
        }

        public virtual OpenStringBuilder Append(ReadOnlySpan<char> value) // LUCENENET specific - added to support ReadOnlySpan<char>
        {
            EnsureCapacity(value.Length);
            UnsafeWrite(value);
            return this;
        }

        // LUCENENET specific - overload for string (more common in .NET than ICharSequence)
        public virtual OpenStringBuilder Append(string csq)
        {
            return Append(csq, 0, csq.Length);
        }

        // LUCENENET specific - overload for string (more common in .NET than ICharSequence)
        public virtual OpenStringBuilder Append(string csq, int startIndex, int count) // LUCENENET specific: changed to startIndex/length to match .NET
        {
            EnsureCapacity(count);
            UnsafeWrite(csq, startIndex, count);
            return this;
        }

        // LUCENENET specific - char sequence overload for StringBuilder
        public virtual OpenStringBuilder Append(StringBuilder csq)
        {
            return Append(csq, 0, csq.Length);
        }

        // LUCENENET specific - char sequence overload for StringBuilder
        public virtual OpenStringBuilder Append(StringBuilder csq, int startIndex, int count) // LUCENENET specific: changed to startIndex/length to match .NET
        {
            EnsureCapacity(count);
            UnsafeWrite(csq, startIndex, count);
            return this;
        }

        // LUCENENET specific - char sequence overload for char[]
        public virtual OpenStringBuilder Append(char[] value)
        {
            Write(value);
            return this;
        }

        // LUCENENET specific - char sequence overload for char[]
        public virtual OpenStringBuilder Append(char[] value, int startIndex, int count)
        {
            EnsureCapacity(count);
            UnsafeWrite(value, startIndex, count);
            return this;
        }

        public virtual OpenStringBuilder Append(char c)
        {
            Write(c);
            return this;
        }

        // LUCENENET specific - removed (replaced with this[])
        //public virtual char CharAt(int index)
        //{
        //    return m_buf[index];
        //}

        // LUCENENET specific - removed (replaced with this[])
        //public virtual void SetCharAt(int index, char ch)
        //{
        //    m_buf[index] = ch;
        //}

        // LUCENENET specific - added to .NETify
        public virtual char this[int index]
        {
            get => m_buf[index];
            set => m_buf[index] = value;
        }

        public virtual ICharSequence Subsequence(int startIndex, int length)
        {
            // From Apache Harmony String class
            if (m_buf is null || (startIndex == 0 && length == m_buf.Length))
            {
                return new CharArrayCharSequence(m_buf);
            }
            if (startIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(startIndex));
            if (length < 0)
                throw new ArgumentOutOfRangeException(nameof(length), $"{nameof(length)} may not be negative.");
            if (startIndex > m_buf.Length - length) // LUCENENET: Checks for int overflow
                throw new ArgumentOutOfRangeException(nameof(length), $"Index and length must refer to a location within the string. For example {nameof(startIndex)} + {nameof(length)} <= {nameof(Length)}.");

            char[] result = new char[length];
            for (int i = 0, j = startIndex; i < length; i++, j++)
                result[i] = m_buf[j];

            return new CharArrayCharSequence(result);
        }

        public virtual void UnsafeWrite(char b)
        {
            m_buf[m_len++] = b;
        }

        public virtual void UnsafeWrite(int b)
        {
            UnsafeWrite((char)b);
        }

        public virtual void UnsafeWrite(ReadOnlySpan<char> b) // LUCENENET specific - added to support ReadOnlySpan<char>
        {
            int len = b.Length;
            b.CopyTo(m_buf.AsSpan(this.m_len, len));
            this.m_len += len;
        }

        public virtual void UnsafeWrite(char[] b, int off, int len)
        {
            Arrays.Copy(b, off, m_buf, this.m_len, len);
            this.m_len += len;
        }

        // LUCENENET specific overload for StringBuilder
        public virtual void UnsafeWrite(StringBuilder b, int off, int len)
        {
            b.CopyTo(off, m_buf, this.m_len, len);
            this.m_len += len;
        }

        // LUCENENET specific overload for string
        public virtual void UnsafeWrite(string b, int off, int len)
        {
            b.CopyTo(off, m_buf, this.m_len, len);
            this.m_len += len;
        }

        // LUCENENET specific overload for ICharSequence
        public virtual void UnsafeWrite(ICharSequence b, int off, int len)
        {
            if (!b.HasValue) return;

            if (b is StringBuilderCharSequence sb)
            {
                UnsafeWrite(sb.Value, off, len);
                return;
            }
            if (b is StringCharSequence str)
            {
                UnsafeWrite(str.Value, off, len);
                return;
            }
            if (b is CharArrayCharSequence chars)
            {
                UnsafeWrite(chars.Value, off, len);
                return;
            }

            for (int i = off; i < off + len; i++)
            {
                UnsafeWrite(b[i]);
            }
        }

        protected virtual void Resize(int len)
        {
            char[] newbuf = new char[Math.Max(m_buf.Length << 1, len)];
            Arrays.Copy(m_buf, 0, newbuf, 0, Length);
            m_buf = newbuf;
        }

        public virtual void EnsureCapacity(int capacity) // LUCENENET NOTE: renamed from reserve() in Lucene to match .NET StringBuilder
        {
            if (m_len + capacity > m_buf.Length)
            {
                Resize(m_len + capacity);
            }
        }

        public virtual void Write(char b)
        {
            if (m_len >= m_buf.Length)
            {
                Resize(m_len + 1);
            }
            UnsafeWrite(b);
        }

        public virtual void Write(int b)
        {
            Write((char)b);
        }

        public void Write(char[] b)
        {
            Write(b, 0, b.Length);
        }

        public virtual void Write(char[] b, int off, int len)
        {
            EnsureCapacity(len);
            UnsafeWrite(b, off, len);
        }

        public void Write(OpenStringBuilder arr)
        {
            Write(arr.m_buf, 0, arr.Length); // LUCENENET specific - changed to arr.m_len (original was just len - appears to be a bug)
        }

        // LUCENENET specific overload for StringBuilder
        public void Write(StringBuilder arr)
        {
            EnsureCapacity(arr.Length);
            UnsafeWrite(arr, 0, arr.Length);
        }

        public virtual void Write(string s)
        {
            EnsureCapacity(s.Length);
            s.CopyTo(0, m_buf, m_len, s.Length - 0);
            m_len += s.Length;
        }

        //public virtual void Flush() // LUCENENET specific - removed because this doesn't make much sense on a StringBuilder in .NET, and it is not used
        //{
        //}

        public void Reset()
        {
            m_len = 0;
        }

        public virtual char[] ToCharArray()
        {
            char[] newbuf = new char[Length];
            Arrays.Copy(m_buf, 0, newbuf, 0, Length);
            return newbuf;
        }

        public override string ToString()
        {
            return new string(m_buf, 0, Length);
        }

        public virtual OpenStringBuilder Remove(int startIndex, int length) // LUCENENET specific - added missing remove method
        {
            if (m_len == length && startIndex == 0)
            {
                m_len = 0;
                return this;
            }

            if (length > 0)
            {
                int endIndex = startIndex + length;
                m_buf.AsSpan(endIndex).CopyTo(m_buf.AsSpan(startIndex));
                m_len -= length;
            }
            return this;
        }

        #region IAppendable Members

        IAppendable IAppendable.Append(char value)
        {
            return Append(value);
        }

        IAppendable IAppendable.Append(string value)
        {
            return Append(value);
        }

        IAppendable IAppendable.Append(string value, int startIndex, int count)
        {
            return Append(value, startIndex, count);
        }

        IAppendable IAppendable.Append(StringBuilder value)
        {
            return Append(value);
        }

        IAppendable IAppendable.Append(StringBuilder value, int startIndex, int count)
        {
            return Append(value, startIndex, count);
        }

        IAppendable IAppendable.Append(char[] value)
        {
            return Append(value);
        }

        IAppendable IAppendable.Append(char[] value, int startIndex, int count)
        {
            return Append(value, startIndex, count);
        }

        IAppendable IAppendable.Append(ICharSequence value)
        {
            return Append(value);
        }

        IAppendable IAppendable.Append(ICharSequence value, int startIndex, int count)
        {
            return Append(value, startIndex, count);
        }

        ISpanAppendable ISpanAppendable.Append(ReadOnlySpan<char> value) => Append(value);

        #endregion IAppendable Members

        #region IBufferWriter<char> members and support

        // LUCENENET-specific: IBufferWriter<char> support.
        // See ArrayBufferWriter for an inspiration and reference implementation:
        // https://github.com/dotnet/runtime/blob/v10.0.6/src/libraries/Common/src/System/Buffers/ArrayBufferWriter.cs

        /// <summary>
        /// Notifies <see cref="IBufferWriter{T}"/> that <paramref name="count"/> amount of data was written to the output <see cref="Span{T}"/>/<see cref="Memory{T}"/>
        /// </summary>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="count"/> is negative.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when attempting to advance past the end of the underlying buffer.
        /// </exception>
        /// <remarks>
        /// You must request a new buffer after calling Advance to continue writing more data and cannot write to a previously acquired buffer.
        /// </remarks>
        public void Advance(int count)
        {
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count));

            if (count > m_buf.Length - m_len)
                throw new InvalidOperationException($"Cannot advance {count} characters beyond the end of the buffer.");

            m_len += count;
        }

        /// <summary>
        /// Returns a <see cref="Memory{T}"/> to write to that is at least the requested length (specified by <paramref name="sizeHint"/>).
        /// If no <paramref name="sizeHint"/> is provided (or it's equal to <c>0</c>), some buffer of minimum length 16 is returned.
        /// </summary>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="sizeHint"/> is negative.
        /// </exception>
        /// <remarks>
        /// This will never return an empty <see cref="Memory{T}"/>.
        /// <para />
        /// There is no guarantee that successive calls will return the same buffer or the same-sized buffer.
        /// <para />
        /// You must request a new buffer after calling <see cref="Advance"/> to continue writing more data and cannot write to a previously acquired buffer.
        /// </remarks>
        public Memory<char> GetMemory(int sizeHint = 0)
        {
            CheckAndResizeBufferForBufferWriter(sizeHint);
            Debug.Assert(m_buf.Length > m_len);
            return m_buf.AsMemory(m_len);
        }

        /// <summary>
        /// Returns a <see cref="Span{T}"/> to write to that is at least the requested length (specified by <paramref name="sizeHint"/>).
        /// If no <paramref name="sizeHint"/> is provided (or it's equal to <c>0</c>), some buffer of minimum length 16 is returned.
        /// </summary>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="sizeHint"/> is negative.
        /// </exception>
        /// <remarks>
        /// This will never return an empty <see cref="Span{T}"/>.
        /// <para />
        /// There is no guarantee that successive calls will return the same buffer or the same-sized buffer.
        /// <para />
        /// You must request a new buffer after calling <see cref="Advance"/> to continue writing more data and cannot write to a previously acquired buffer.
        /// </remarks>
        public Span<char> GetSpan(int sizeHint = 0)
        {
            CheckAndResizeBufferForBufferWriter(sizeHint);
            Debug.Assert(m_buf.Length > m_len);
            return m_buf.AsSpan(m_len);
        }

        private void CheckAndResizeBufferForBufferWriter(int sizeHint)
        {
            if (sizeHint < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(sizeHint), "sizeHint must be non-negative");
            }

            if (sizeHint == 0)
            {
                // LUCENENET NOTE: 16 is a reasonable, arbitrary minimum for text.
                // ArrayBufferWriter uses 1, just to ensure that it does not return
                // an empty buffer. If the caller does not care what size they get
                // back (by not providing a sizeHint), they have to work with what
                // is returned. This value should be enough for most short strings.
                sizeHint = 16;
            }

            EnsureCapacity(sizeHint);
        }

        /// <summary>
        /// Returns the amount of space available that can still be written into without forcing the underlying buffer to grow.
        /// </summary>
        public int FreeCapacity => m_buf.Length - m_len;

        #endregion
    }

    /// <summary>
    /// Extension methods for <see cref="OpenStringBuilder"/> that expose the data written
    /// so far as a <see cref="ReadOnlySpan{T}"/> or <see cref="ReadOnlyMemory{T}"/>.
    /// </summary>
    /// <remarks>
    /// LUCENENET specific. These are implemented as extension methods (rather than instance
    /// methods) so that a <c>null</c> <see cref="OpenStringBuilder"/> naturally converts to
    /// an empty span/memory instead of throwing, matching the behavior of the BCL
    /// <c>AsSpan</c>/<c>AsMemory</c> extensions for reference types.
    /// </remarks>
    public static class OpenStringBuilderExtensions
    {
        #region AsSpan

        /// <summary>
        /// Creates a new read-only span over the data written to <paramref name="text"/> so far.
        /// </summary>
        /// <param name="text">The target <see cref="OpenStringBuilder"/>.</param>
        /// <returns>The read-only span representation of the data, or <c>default</c> if
        /// <paramref name="text"/> is <c>null</c>.</returns>
        public static ReadOnlySpan<char> AsSpan(this OpenStringBuilder text)
        {
            if (text is null)
                return default;

            char[] chars = text.Array;

#if FEATURE_MEMORYMARSHAL_CREATEREADONLYSPAN && FEATURE_MEMORYMARSHAL_GETARRAYDATAREFERENCE
            return MemoryMarshal.CreateReadOnlySpan<char>(ref MemoryMarshal.GetArrayDataReference(chars), text.Length);
#else
            return new ReadOnlySpan<char>(chars, 0, text.Length);
#endif
        }

        /// <summary>
        /// Creates a new read-only span over a portion of the data written to
        /// <paramref name="text"/> so far, from a specified position to the end.
        /// </summary>
        /// <param name="text">The target <see cref="OpenStringBuilder"/>.</param>
        /// <param name="start">The index at which to begin this slice.</param>
        /// <returns>The read-only span representation of the data, or <c>default</c> if
        /// <paramref name="text"/> is <c>null</c>.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="start"/> is less than 0 or greater than <c>text.Length</c>.
        /// </exception>
        public static ReadOnlySpan<char> AsSpan(this OpenStringBuilder text, int start)
        {
            if (text is null)
            {
                if (start != 0)
                    throw new ArgumentOutOfRangeException(nameof(start));

                return default;
            }

            if ((uint)start > (uint)text.Length)
                throw new ArgumentOutOfRangeException(nameof(start));

            char[] chars = text.Array;

#if FEATURE_MEMORYMARSHAL_CREATEREADONLYSPAN && FEATURE_MEMORYMARSHAL_GETARRAYDATAREFERENCE
            return MemoryMarshal.CreateReadOnlySpan<char>(ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(chars),
                (nint)(uint)start /* force zero-extension */), text.Length - start);
#else
            return new ReadOnlySpan<char>(chars, start, text.Length - start);
#endif
        }

        /// <summary>
        /// Creates a new read-only span over a portion of the data written to
        /// <paramref name="text"/> so far, from a specified position for a specified length.
        /// </summary>
        /// <param name="text">The target <see cref="OpenStringBuilder"/>.</param>
        /// <param name="start">The index at which to begin this slice.</param>
        /// <param name="length">The desired length for the slice.</param>
        /// <returns>The read-only span representation of the data, or <c>default</c> if
        /// <paramref name="text"/> is <c>null</c>.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="start"/>, <paramref name="length"/>, or
        /// <paramref name="start"/> + <paramref name="length"/> is not in the range of <paramref name="text"/>.
        /// </exception>
        public static ReadOnlySpan<char> AsSpan(this OpenStringBuilder text, int start, int length)
        {
            if (text is null)
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

            char[] chars = text.Array;

#if FEATURE_MEMORYMARSHAL_CREATEREADONLYSPAN && FEATURE_MEMORYMARSHAL_GETARRAYDATAREFERENCE
            return MemoryMarshal.CreateReadOnlySpan<char>(ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(chars),
                (nint)(uint)start /* force zero-extension */), length);
#else
            return new ReadOnlySpan<char>(chars, start, length);
#endif
        }

        /// <summary>
        /// Creates a new read-only span over a portion of the data written to
        /// <paramref name="text"/> so far, from a specified position to the end.
        /// </summary>
        /// <param name="text">The target <see cref="OpenStringBuilder"/>.</param>
        /// <param name="startIndex">The index at which to begin this slice.</param>
        /// <returns>The read-only span representation of the data, or <c>default</c> if
        /// <paramref name="text"/> is <c>null</c>.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="startIndex"/> is less
        /// than 0 or greater than <c>text.Length</c>.</exception>
        public static ReadOnlySpan<char> AsSpan(this OpenStringBuilder text, System.Index startIndex)
        {
            if (text is null)
            {
                if (!startIndex.Equals(System.Index.Start))
                    throw new ArgumentOutOfRangeException(nameof(startIndex));

                return default;
            }

            int actualIndex = startIndex.GetOffset(text.Length);
            if ((uint)actualIndex > (uint)text.Length)
                throw new ArgumentOutOfRangeException(nameof(startIndex));

            char[] chars = text.Array;

#if FEATURE_MEMORYMARSHAL_CREATEREADONLYSPAN && FEATURE_MEMORYMARSHAL_GETARRAYDATAREFERENCE
            return MemoryMarshal.CreateReadOnlySpan<char>(ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(chars),
                (nint)(uint)actualIndex /* force zero-extension */), text.Length - actualIndex);
#else
            return new ReadOnlySpan<char>(chars, actualIndex, text.Length - actualIndex);
#endif
        }

        /// <summary>
        /// Creates a new read-only span over a portion of the data written to
        /// <paramref name="text"/> so far, using the range start and end indexes.
        /// </summary>
        /// <param name="text">The target <see cref="OpenStringBuilder"/>.</param>
        /// <param name="range">The range that has start and end indexes to use for slicing the data.</param>
        /// <returns>The read-only span representation of the data, or <c>default</c> if
        /// <paramref name="text"/> is <c>null</c>.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="range"/>'s start or end index is not within the bounds of the data.
        /// -or-
        /// <paramref name="range"/>'s start index is greater than its end index.
        /// </exception>
        public static ReadOnlySpan<char> AsSpan(this OpenStringBuilder text, Range range)
        {
            if (text is null)
            {
                System.Index startIndex = range.Start;
                System.Index endIndex = range.End;

                if (!startIndex.Equals(System.Index.Start) || !endIndex.Equals(System.Index.Start))
                    throw new ArgumentOutOfRangeException(nameof(range));

                return default;
            }

            (int start, int length) = range.GetOffsetAndLength(text.Length);
            char[] chars = text.Array;

#if FEATURE_MEMORYMARSHAL_CREATEREADONLYSPAN && FEATURE_MEMORYMARSHAL_GETARRAYDATAREFERENCE
            return MemoryMarshal.CreateReadOnlySpan<char>(ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(chars),
                (nint)(uint)start /* force zero-extension */), length);
#else
            return new ReadOnlySpan<char>(chars, start, length);
#endif
        }

        #endregion AsSpan

        #region AsMemory

        /// <summary>
        /// Creates a new <see cref="ReadOnlyMemory{T}"/> over the data written to <paramref name="text"/> so far.
        /// </summary>
        /// <param name="text">The target <see cref="OpenStringBuilder"/>.</param>
        /// <returns>The read-only memory representation of the data, or <c>default</c> if
        /// <paramref name="text"/> is <c>null</c>.</returns>
        public static ReadOnlyMemory<char> AsMemory(this OpenStringBuilder text)
        {
            if (text is null)
                return default;

            return new ReadOnlyMemory<char>(text.Array, 0, text.Length);
        }

        /// <summary>
        /// Creates a new <see cref="ReadOnlyMemory{T}"/> over a portion of the data written to
        /// <paramref name="text"/> so far, from a specified position to the end.
        /// </summary>
        /// <param name="text">The target <see cref="OpenStringBuilder"/>.</param>
        /// <param name="start">The index at which to begin this slice.</param>
        /// <returns>The read-only memory representation of the data, or <c>default</c> if
        /// <paramref name="text"/> is <c>null</c>.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="start"/> is less than 0 or greater than <c>text.Length</c>.
        /// </exception>
        public static ReadOnlyMemory<char> AsMemory(this OpenStringBuilder text, int start)
        {
            if (text is null)
            {
                if (start != 0)
                    throw new ArgumentOutOfRangeException(nameof(start));

                return default;
            }

            if ((uint)start > (uint)text.Length)
                throw new ArgumentOutOfRangeException(nameof(start));

            return new ReadOnlyMemory<char>(text.Array, start, text.Length - start);
        }

        /// <summary>
        /// Creates a new <see cref="ReadOnlyMemory{T}"/> over a portion of the data written to
        /// <paramref name="text"/> so far, from a specified position for a specified length.
        /// </summary>
        /// <param name="text">The target <see cref="OpenStringBuilder"/>.</param>
        /// <param name="start">The index at which to begin this slice.</param>
        /// <param name="length">The desired length for the slice.</param>
        /// <returns>The read-only memory representation of the data, or <c>default</c> if
        /// <paramref name="text"/> is <c>null</c>.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="start"/>, <paramref name="length"/>, or
        /// <paramref name="start"/> + <paramref name="length"/> is not in the range of <paramref name="text"/>.
        /// </exception>
        public static ReadOnlyMemory<char> AsMemory(this OpenStringBuilder text, int start, int length)
        {
            if (text is null)
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

            return new ReadOnlyMemory<char>(text.Array, start, length);
        }

        /// <summary>
        /// Creates a new <see cref="ReadOnlyMemory{T}"/> over a portion of the data written to
        /// <paramref name="text"/> so far, from a specified position to the end.
        /// </summary>
        /// <param name="text">The target <see cref="OpenStringBuilder"/>.</param>
        /// <param name="startIndex">The index at which to begin this slice.</param>
        /// <returns>The read-only memory representation of the data, or <c>default</c> if
        /// <paramref name="text"/> is <c>null</c>.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="startIndex"/> is less
        /// than 0 or greater than <c>text.Length</c>.</exception>
        public static ReadOnlyMemory<char> AsMemory(this OpenStringBuilder text, System.Index startIndex)
        {
            if (text is null)
            {
                if (!startIndex.Equals(System.Index.Start))
                    throw new ArgumentOutOfRangeException(nameof(startIndex));

                return default;
            }

            int actualIndex = startIndex.GetOffset(text.Length);
            if ((uint)actualIndex > (uint)text.Length)
                throw new ArgumentOutOfRangeException(nameof(startIndex));

            return new ReadOnlyMemory<char>(text.Array, actualIndex, text.Length - actualIndex);
        }

        /// <summary>
        /// Creates a new <see cref="ReadOnlyMemory{T}"/> over a portion of the data written to
        /// <paramref name="text"/> so far, using the range start and end indexes.
        /// </summary>
        /// <param name="text">The target <see cref="OpenStringBuilder"/>.</param>
        /// <param name="range">The range used to indicate the start and length of the sliced data.</param>
        /// <returns>The read-only memory representation of the data, or <c>default</c> if
        /// <paramref name="text"/> is <c>null</c>.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="range"/>'s start or end index is not within the bounds of the data.
        /// -or-
        /// <paramref name="range"/>'s start index is greater than its end index.
        /// </exception>
        public static ReadOnlyMemory<char> AsMemory(this OpenStringBuilder text, Range range)
        {
            if (text is null)
            {
                System.Index startIndex = range.Start;
                System.Index endIndex = range.End;

                if (!startIndex.Equals(System.Index.Start) || !endIndex.Equals(System.Index.Start))
                    throw new ArgumentOutOfRangeException(nameof(range));

                return default;
            }

            (int start, int length) = range.GetOffsetAndLength(text.Length);
            return new ReadOnlyMemory<char>(text.Array, start, length);
        }

        #endregion AsMemory
    }
}
