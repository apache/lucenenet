// This class was sourced from the Apache Harmony project
// https://svn.apache.org/repos/asf/harmony/enhanced/java/trunk/

using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text;

namespace Lucene.Net.Support.IO
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
    /// A buffer for bytes.
    /// <para/>
    /// A byte buffer can be created in either one of the following ways:
    /// <list type="bullet">
    ///     <item><see cref="Allocate(int)"/> a new byte array and create a
    ///     buffer based on it</item>
    ///     <item><see cref="AllocateDirect(int)"/> a memory block and create a direct
    ///     buffer based on it</item>
    ///     <item><see cref="Wrap(byte[])"/> an existing byte array to create a new buffer</item>
    /// </list>
    /// </summary>
#if FEATURE_SERIALIZABLE
    [Serializable]
#endif
    public abstract class ByteBuffer : Buffer, IComparable<ByteBuffer>
    {
        /// <summary>
        /// Creates a byte buffer based on a newly allocated byte array.
        /// </summary>
        /// <param name="capacity">the capacity of the new buffer</param>
        /// <returns>The created byte buffer.</returns>
        /// <exception cref="ArgumentException">If the <c>capacity &lt; 0</c>.</exception>
        public static ByteBuffer Allocate(int capacity)
        {
            if (capacity < 0)
                throw new ArgumentException();
            return new ReadWriteHeapByteBuffer(capacity);
        }

        /// <summary>
        /// Creates a direct byte buffer based on a newly allocated memory block. (NOT IMPLEMENTED IN LUCENE.NET)
        /// </summary>
        /// <param name="capacity">the capacity of the new buffer</param>
        /// <returns>The new byte buffer</returns>
        /// <exception cref="ArgumentException">If the <c>capacity &lt; 0</c>.</exception>
        public static ByteBuffer AllocateDirect(int capacity)
        {
            throw new NotImplementedException();
            //return new DirectByteBuffer(capacity);
        }

        /// <summary>
        /// Creates a new byte buffer by wrapping the given byte array.
        /// <para/>
        /// Calling this method has the same effect as
        /// <c>Wrap(array, 0, array.Length)</c>.
        /// </summary>
        /// <param name="array">The byte array which the new buffer will be based on</param>
        /// <returns>The new byte buffer</returns>
        public static ByteBuffer Wrap(byte[] array)
        {
            return new ReadWriteHeapByteBuffer(array);
        }

        /// <summary>
        /// Creates a new byte buffer by wrapping the given byte array.
        /// <para/>
        /// The new buffer's position will be <paramref name="start"/>, limit will be
        /// <c>start + len</c>, capacity will be the length of the array.
        /// </summary>
        /// <param name="array">The byte array which the new buffer will be based on.</param>
        /// <param name="start">
        /// The start index, must not be negative and not greater than <c>array.Length</c>.
        /// </param>
        /// <param name="length">
        /// The length, must not be negative and not greater than
        /// <c>array.Length - start</c>.
        /// </param>
        /// <returns>The new byte buffer</returns>
        /// <exception cref="IndexOutOfRangeException">if either <paramref name="start"/> or <paramref name="length"/> are invalid.</exception>
        public static ByteBuffer Wrap(byte[] array, int start, int length)
        {
            int actualLength = array.Length;
            if ((start < 0) || (length < 0) || ((long)start + (long)length > actualLength))
            {
                throw new IndexOutOfRangeException();
            }

            ByteBuffer buf = new ReadWriteHeapByteBuffer(array);
            buf.position = start;
            buf.limit = start + length;

            return buf;
        }

        /// <summary>
        /// The byte order of this buffer, default is <see cref="ByteOrder.BIG_ENDIAN"/>.
        /// </summary>
        internal Endianness order = Endianness.BIG_ENDIAN;

        /// <summary>
        /// Constructs a <see cref="ByteBuffer"/> with given capacity.
        /// </summary>
        /// <param name="capacity">the capacity of the buffer.</param>
        internal ByteBuffer(int capacity)
            : base(capacity)
        {
        }

        /// <summary>
        /// Returns the byte array which this buffer is based on, if there is one.
        /// </summary>
        /// <returns>the byte array which this buffer is based on.</returns>
        /// <exception cref="ReadOnlyBufferException">if this buffer is based on a read-only array.</exception>
        /// <exception cref="InvalidOperationException">if this buffer is not based on an array.</exception>
        [WritableArray]
        [SuppressMessage("Microsoft.Performance", "CA1819", Justification = "Lucene's design requires some writable array properties")]
        public byte[] Array
        {
            get { return ProtectedArray; }
        }

        /// <summary>
        /// Returns the offset of the byte array which this buffer is based on, if
        /// there is one.
        /// <para/>
        /// The offset is the index of the array which corresponds to the zero
        /// position of the buffer.
        /// </summary>
        /// <exception cref="ReadOnlyBufferException">if this buffer is based on a read-only array.</exception>
        /// <exception cref="InvalidOperationException">if this buffer is not based on an array.</exception>
        public int ArrayOffset
        {
            get { return ProtectedArrayOffset; }
        }

        //public abstract CharBuffer AsCharBuffer();
        //public abstract DoubleBuffer AsDoubleBuffer();
        //public abstract FloatBuffer AsSingleBuffer();
        //public abstract IntBuffer AsInt32Buffer();

        /// <summary>
        /// Returns a long buffer which is based on the remaining content of this
        /// byte buffer.
        /// <para/>
        /// The new buffer's position is zero, its limit and capacity is the number
        /// of remaining bytes divided by eight, and its mark is not set. The new
        /// buffer's read-only property and byte order are the same as this buffer's.
        /// The new buffer is direct if this byte buffer is direct.
        /// <para/>
        /// The new buffer shares its content with this buffer, which means either
        /// buffer's change of content will be visible to the other. The two buffer's
        /// position, limit and mark are independent.
        /// </summary>
        /// <returns>a long buffer which is based on the content of this byte buffer.</returns>
        public abstract Int64Buffer AsInt64Buffer();

        /// <summary>
        /// Returns a read-only buffer that shares its content with this buffer.
        /// <para/>
        /// The returned buffer is guaranteed to be a new instance, even if this
        /// buffer is read-only itself. The new buffer's position, limit, capacity
        /// and mark are the same as this buffer.
        /// <para/>
        /// The new buffer shares its content with this buffer, which means this
        /// buffer's change of content will be visible to the new buffer. The two
        /// buffer's position, limit and mark are independent.
        /// </summary>
        /// <returns>a read-only version of this buffer.</returns>
        public abstract ByteBuffer AsReadOnlyBuffer();

        //public abstract ShortBuffer AsInt16Buffer();

        /// <summary>
        /// Compacts this byte buffer.
        /// <para/>
        /// The remaining bytes will be moved to the head of the
        /// buffer, starting from position zero. Then the position is set to
        /// <see cref="Remaining"/>; the limit is set to capacity; the mark is
        /// cleared.
        /// </summary>
        /// <returns>this buffer.</returns>
        /// <exception cref="ReadOnlyBufferException">if no changes may be made to the contents of this buffer.</exception>
        public abstract ByteBuffer Compact();

        /// <summary>
        /// Compares the remaining bytes of this buffer to another byte buffer's
        /// remaining bytes.
        /// </summary>
        /// <param name="otherBuffer">another byte buffer.</param>
        /// <returns>
        /// a negative value if this is less than <c>other</c>; 0 if this
        /// equals to <c>other</c>; a positive value if this is greater
        /// than <c>other</c>.
        /// </returns>
        public virtual int CompareTo(ByteBuffer otherBuffer)
        {
            int compareRemaining = (Remaining < otherBuffer.Remaining) ? Remaining
                    : otherBuffer.Remaining;
            int thisPos = position;
            int otherPos = otherBuffer.position;
            byte thisByte, otherByte;
            while (compareRemaining > 0)
            {
                thisByte = Get(thisPos);
                otherByte = otherBuffer.Get(otherPos);
                if (thisByte != otherByte)
                {
                    // LUCENENET specific - comparison should return
                    // the diff, not be hard coded to 1/-1
                    return thisByte - otherByte;
                    //return thisByte < otherByte ? -1 : 1;

                }
                thisPos++;
                otherPos++;
                compareRemaining--;
            }
            return Remaining - otherBuffer.Remaining;
        }

        /// <summary>
        /// Returns a duplicated buffer that shares its content with this buffer.
        /// <para/>
        /// The duplicated buffer's position, limit, capacity and mark are the same
        /// as this buffer's. The duplicated buffer's read-only property and byte
        /// order are the same as this buffer's too.
        /// <para/>
        /// The new buffer shares its content with this buffer, which means either
        /// buffer's change of content will be visible to the other. The two buffer's
        /// position, limit and mark are independent.
        /// </summary>
        /// <returns>a duplicated buffer that shares its content with this buffer.</returns>
        public abstract ByteBuffer Duplicate();

        /// <summary>
        /// Checks whether this byte buffer is equal to another object.
        /// <para/>
        /// If <paramref name="other"/> is not a byte buffer then <c>false</c> is returned. Two
        /// byte buffers are equal if and only if their remaining bytes are exactly
        /// the same. Position, limit, capacity and mark are not considered.
        /// </summary>
        /// <param name="other">the object to compare with this byte buffer.</param>
        /// <returns>
        /// <c>true</c> if this byte buffer is equal to <paramref name="other"/>,
        /// <c>false</c> otherwise.
        /// </returns>
        public override bool Equals(object other)
        {
            if (!(other is ByteBuffer)) {
                return false;
            }
            ByteBuffer otherBuffer = (ByteBuffer)other;

            if (Remaining != otherBuffer.Remaining)
            {
                return false;
            }

            int myPosition = position;
            int otherPosition = otherBuffer.position;
            bool equalSoFar = true;
            while (equalSoFar && (myPosition < limit))
            {
                equalSoFar = Get(myPosition++) == otherBuffer.Get(otherPosition++);
            }

            return equalSoFar;
        }

        /// <summary>
        /// Returns the byte at the current position and increases the position by 1.
        /// </summary>
        /// <returns>the byte at the current position.</returns>
        /// <exception cref="BufferUnderflowException">if the position is equal or greater than limit.</exception>
        public abstract byte Get();

        /// <summary>
        /// Reads bytes from the current position into the specified byte array and
        /// increases the position by the number of bytes read.
        /// <para/>
        /// Calling this method has the same effect as
        /// <c>Get(dest, 0, dest.Length)</c>.
        /// </summary>
        /// <param name="dest">the destination byte array.</param>
        /// <returns>this buffer.</returns>
        /// <exception cref="BufferUnderflowException">if <c>dest.Length</c> is greater than <see cref="Remaining"/>.</exception>
        public virtual ByteBuffer Get(byte[] dest)
        {
            return Get(dest, 0, dest.Length);
        }

        /// <summary>
        /// Reads bytes from the current position into the specified byte array,
        /// starting at the specified offset, and increases the position by the
        /// number of bytes read.
        /// </summary>
        /// <param name="dest">the target byte array.</param>
        /// <param name="off">
        /// the offset of the byte array, must not be negative and
        /// not greater than <c>dest.Length</c>.</param>
        /// <param name="len">
        /// the number of bytes to read, must not be negative and not
        /// greater than <c>dest.Length - off</c>
        /// </param>
        /// <returns>this buffer.</returns>
        /// <exception cref="IndexOutOfRangeException">if either <paramref name="off"/> or <paramref name="len"/> is invalid.</exception>
        /// <exception cref="BufferUnderflowException">if <paramref name="len"/> is greater than <see cref="Remaining"/>.</exception>
        public virtual ByteBuffer Get(byte[] dest, int off, int len)
        {
            int length = dest.Length;
            if ((off < 0) || (len < 0) || ((long)off + (long)len > length))
            {
                throw new IndexOutOfRangeException();
            }

            if (len > Remaining)
            {
                throw new BufferUnderflowException();
            }
            for (int i = off; i < off + len; i++)
            {
                dest[i] = Get();
            }
            return this;
        }

        /// <summary>
        /// Returns the byte at the specified index and does not change the position.
        /// 
        /// </summary>
        /// <param name="index">the index, must not be negative and less than limit.</param>
        /// <returns>the byte at the specified index.</returns>
        /// <exception cref="IndexOutOfRangeException">if index is invalid.</exception>
        public abstract byte Get(int index);

        /// <summary>
        /// Returns the char at the current position and increases the position by 2.
        /// <para/>
        /// The 2 bytes starting at the current position are composed into a char
        /// according to the current byte order and returned.
        /// </summary>
        /// <returns>the char at the current position.</returns>
        /// <exception cref="BufferUnderflowException">if the position is greater than <c>limit - 2</c>.</exception>
        public abstract char GetChar();

        /// <summary>
        /// Returns the char at the specified index.
        /// <para/>
        /// The 2 bytes starting from the specified index are composed into a char
        /// according to the current byte order and returned. The position is not
        /// changed.
        /// </summary>
        /// <param name="index">the index, must not be negative and equal or less than <c>limit - 2</c>.</param>
        /// <returns>the char at the specified index.</returns>
        /// <exception cref="IndexOutOfRangeException">if <paramref name="index"/> is invalid.</exception>
        public abstract char GetChar(int index);

        /// <summary>
        /// Returns the double at the current position and increases the position by 8.
        /// <para/>
        /// The 8 bytes starting from the current position are composed into a double
        /// according to the current byte order and returned.
        /// </summary>
        /// <returns>the double at the current position.</returns>
        /// <exception cref="BufferUnderflowException">if the position is greater than <c>limit - 8</c>.</exception>
        public abstract double GetDouble();

        /// <summary>
        /// Returns the <see cref="double"/> at the specified index.
        /// <para/>
        /// The 8 bytes starting at the specified index are composed into a <see cref="double"/>
        /// according to the current byte order and returned. The position is not
        /// changed.
        /// </summary>
        /// <param name="index">the index, must not be negative and equal or less than <c>limit - 8</c>.</param>
        /// <returns>the <see cref="double"/> at the specified index.</returns>
        /// <returns>the <see cref="double"/> at the current position.</returns>
        /// <exception cref="IndexOutOfRangeException">if <paramref name="index"/> is invalid.</exception>
        public abstract double GetDouble(int index);

        /// <summary>
        /// Returns the <see cref="float"/> at the current position and increases the position by 4.
        /// <para/>
        /// The 4 bytes starting at the current position are composed into a <see cref="float"/>
        /// according to the current byte order and returned.
        /// <para/>
        /// NOTE: This was getFloat() in the JDK
        /// </summary>
        /// <returns>the <see cref="float"/> at the current position.</returns>
        /// <exception cref="BufferUnderflowException">if the position is greater than <c>limit - 4</c>.</exception>
        public abstract float GetSingle();

        /// <summary>
        /// Returns the <see cref="float"/> at the specified index.
        /// <para/>
        /// The 4 bytes starting at the specified index are composed into a <see cref="float"/>
        /// according to the current byte order and returned. The position is not
        /// changed.
        /// <para/>
        /// NOTE: This was getFloat() in the JDK
        /// </summary>
        /// <param name="index">the index, must not be negative and equal or less than <c>limit - 4</c>.</param>
        /// <returns>the <see cref="float"/> at the specified index.</returns>
        /// <exception cref="IndexOutOfRangeException">if <paramref name="index"/> is invalid.</exception>
        public abstract float GetSingle(int index);

        /// <summary>
        /// Returns the <see cref="int"/> at the current position and increases the position by 4.
        /// <para/>
        /// The 4 bytes starting at the current position are composed into a <see cref="int"/>
        /// according to the current byte order and returned.
        /// <para/>
        /// NOTE: This was getInt() in the JDK
        /// </summary>
        /// <returns>the <see cref="int"/> at the current position.</returns>
        /// <exception cref="BufferUnderflowException">if the position is greater than <c>limit - 4</c>.</exception>
        public abstract int GetInt32();

        /// <summary>
        /// Returns the <see cref="int"/> at the specified index.
        /// <para/>
        /// The 4 bytes starting at the specified index are composed into a <see cref="int"/>
        /// according to the current byte order and returned. The position is not
        /// changed.
        /// <para/>
        /// NOTE: This was getInt() in the JDK
        /// </summary>
        /// <param name="index">the index, must not be negative and equal or less than <c>limit - 4</c>.</param>
        /// <returns>the <see cref="int"/> at the specified index.</returns>
        /// <exception cref="IndexOutOfRangeException">if <paramref name="index"/> is invalid.</exception>
        public abstract int GetInt32(int index);

        /// <summary>
        /// Returns the <see cref="long"/> at the current position and increases the position by 8.
        /// <para/>
        /// The 8 bytes starting at the current position are composed into a <see cref="long"/>
        /// according to the current byte order and returned.
        /// <para/>
        /// NOTE: This was getLong() in the JDK
        /// </summary>
        /// <returns>the <see cref="long"/> at the current position.</returns>
        /// <exception cref="BufferUnderflowException">if the position is greater than <c>limit - 8</c>.</exception>
        public abstract long GetInt64();


        /// <summary>
        /// Returns the <see cref="long"/> at the specified index.
        /// <para/>
        /// The 8 bytes starting at the specified index are composed into a <see cref="long"/>
        /// according to the current byte order and returned. The position is not
        /// changed.
        /// <para/>
        /// NOTE: This was getLong() in the JDK
        /// </summary>
        /// <param name="index">the index, must not be negative and equal or less than <c>limit - 8</c>.</param>
        /// <returns>the <see cref="long"/> at the specified index.</returns>
        /// <exception cref="IndexOutOfRangeException">if <paramref name="index"/> is invalid.</exception>
        public abstract long GetInt64(int index);

        /// <summary>
        /// Returns the <see cref="short"/> at the current position and increases the position by 2.
        /// <para/>
        /// The 2 bytes starting at the current position are composed into a <see cref="short"/>
        /// according to the current byte order and returned.
        /// <para/>
        /// NOTE: This was getShort() in the JDK
        /// </summary>
        /// <returns>the <see cref="short"/> at the current position.</returns>
        /// <exception cref="BufferUnderflowException">if the position is greater than <c>limit - 2</c>.</exception>
        public abstract short GetInt16();


        /// <summary>
        /// Returns the <see cref="short"/> at the specified index.
        /// <para/>
        /// The 2 bytes starting at the specified index are composed into a <see cref="short"/>
        /// according to the current byte order and returned. The position is not
        /// changed.
        /// <para/>
        /// NOTE: This was getShort() in the JDK
        /// </summary>
        /// <param name="index">the index, must not be negative and equal or less than <c>limit - 2</c>.</param>
        /// <returns>the <see cref="short"/> at the specified index.</returns>
        /// <exception cref="IndexOutOfRangeException">if <paramref name="index"/> is invalid.</exception>
        public abstract short GetInt16(int index);

        /// <summary>
        /// Indicates whether this buffer is based on a byte array and provides
        /// read/write access.
        /// </summary>
        public bool HasArray
        {
            get { return ProtectedHasArray; }
        }

        /// <summary>
        /// Calculates this buffer's hash code from the remaining chars. The
        /// position, limit, capacity and mark don't affect the hash code.
        /// </summary>
        /// <returns>the hash code calculated from the remaining bytes.</returns>
        public override int GetHashCode()
        {
            int myPosition = position;
            int hash = 0;
            while (myPosition < limit)
            {
                hash = hash + Get(myPosition++);
            }
            return hash;
        }

        /// <summary>
        /// Indicates whether this buffer is direct.
        /// </summary>
        public abstract bool IsDirect { get; }

        /// <summary>
        /// Returns the byte order used by this buffer when converting bytes from/to
        /// other primitive types.
        /// <para/>
        /// The default byte order of byte buffer is always
        /// <see cref="ByteOrder.BIG_ENDIAN"/>.
        /// </summary>
        public ByteOrder Order
        {
            get
            {
                return order == Endianness.BIG_ENDIAN ? ByteOrder.BIG_ENDIAN
                        : ByteOrder.LITTLE_ENDIAN;
            }
            set
            {
                SetOrder(value);
            }
        }

        /// <summary>
        /// Sets the byte order of this buffer.
        /// </summary>
        /// <param name="byteOrder">the byte order to set.</param>
        /// <returns>this buffer.</returns>
        public ByteBuffer SetOrder(ByteOrder byteOrder)
        {
            order = byteOrder == ByteOrder.BIG_ENDIAN ? Endianness.BIG_ENDIAN
                : Endianness.LITTLE_ENDIAN;
            return this;
        }

        /// <summary>
        /// Child class implements this method to realize <see cref="Array"/>.
        /// </summary>
        /// <seealso cref="Array"/>
        protected abstract byte[] ProtectedArray { get; }

        /// <summary>
        /// Child class implements this method to realize <see cref="ArrayOffset"/>.
        /// </summary>
        protected abstract int ProtectedArrayOffset { get; }

        /// <summary>
        /// Child class implements this method to realize <seealso cref="HasArray"/>.
        /// </summary>
        protected abstract bool ProtectedHasArray { get; }


        /// <summary>
        /// Writes the given byte to the current position and increases the position
        /// by 1.
        /// </summary>
        /// <param name="b">the byte to write.</param>
        /// <returns>this buffer.</returns>
        /// <exception cref="BufferOverflowException">if position is equal or greater than limit.</exception>
        /// <exception cref="ReadOnlyBufferException">if no changes may be made to the contents of this buffer.</exception>
        public abstract ByteBuffer Put(byte b);

        /// <summary>
        /// Writes bytes in the given byte array to the current position and
        /// increases the position by the number of bytes written.
        /// <para/>
        /// Calling this method has the same effect as
        /// <c>Put(src, 0, src.Length)</c>.
        /// </summary>
        /// <param name="src">the source byte array.</param>
        /// <returns>this buffer.</returns>
        /// <exception cref="BufferOverflowException">if <see cref="Remaining"/> is less than <c>src.Length</c>.</exception>
        /// <exception cref="ReadOnlyBufferException">if no changes may be made to the contents of this buffer.</exception>
        public ByteBuffer Put(byte[] src)
        {
            return Put(src, 0, src.Length);
        }

        /// <summary>
        /// Writes bytes in the given byte array, starting from the specified offset,
        /// to the current position and increases the position by the number of bytes
        /// written.
        /// </summary>
        /// <param name="src">the source byte array.</param>
        /// <param name="off">
        /// the offset of byte array, must not be negative and not greater
        /// than <c>src.Length</c>.
        /// </param>
        /// <param name="len">
        /// the number of bytes to write, must not be negative and not
        /// greater than <c>src.Length - off</c>.
        /// </param>
        /// <returns>this buffer.</returns>
        /// <exception cref="BufferOverflowException">if <see cref="Remaining"/> is less than <paramref name="len"/>.</exception>
        /// <exception cref="IndexOutOfRangeException">if either <paramref name="off"/> or <paramref name="len"/> is invalid.</exception>
        /// <exception cref="ReadOnlyBufferException">if no changes may be made to the contents of this buffer.</exception>
        public virtual ByteBuffer Put(byte[] src, int off, int len)
        {
            int length = src.Length;
            if ((off < 0) || (len < 0) || ((long)off + (long)len > length))
            {
                throw new IndexOutOfRangeException();
            }

            if (len > Remaining)
            {
                throw new BufferOverflowException();
            }
            for (int i = off; i < off + len; i++)
            {
                Put(src[i]);
            }
            return this;
        }

        /// <summary>
        /// Writes all the remaining bytes of the <paramref name="src"/> byte buffer to this
        /// buffer's current position, and increases both buffers' position by the
        /// number of bytes copied.
        /// </summary>
        /// <param name="src">the source byte buffer.</param>
        /// <returns>this buffer.</returns>
        /// <exception cref="BufferOverflowException">if <c>src.Remaining</c> is greater than this buffer's <see cref="Remaining"/>.</exception>
        /// <exception cref="ArgumentException">if <paramref name="src"/> is this buffer.</exception>
        /// <exception cref="ReadOnlyBufferException">if no changes may be made to the contents of this buffer.</exception>
        public virtual ByteBuffer Put(ByteBuffer src)
        {
            if (src == this)
            {
                throw new ArgumentException();
            }
            if (src.Remaining > Remaining)
            {
                throw new BufferOverflowException();
            }
            byte[] contents = new byte[src.Remaining];
            src.Get(contents);
            Put(contents);
            return this;
        }

        /// <summary>
        /// Write a <see cref="byte"/> to the specified index of this buffer without changing the
        /// position.
        /// </summary>
        /// <param name="index">the index, must not be negative and less than the limit.</param>
        /// <param name="b">the <see cref="byte"/> to write.</param>
        /// <returns>this buffer.</returns>
        /// <exception cref="IndexOutOfRangeException">if <paramref name="index"/> is invalid.</exception>
        /// <exception cref="ReadOnlyBufferException">if no changes may be made to the contents of this buffer.</exception>
        public abstract ByteBuffer Put(int index, byte b);

        /// <summary>
        /// Writes the given <see cref="char"/> to the current position and increases the position
        /// by 2.
        /// <para/>
        /// The <see cref="char"/> is converted to bytes using the current byte order.
        /// </summary>
        /// <param name="value">the <see cref="char"/> to write.</param>
        /// <returns>this buffer.</returns>
        /// <exception cref="BufferOverflowException">if position is greater than <c>limit - 2</c>.</exception>
        /// <exception cref="ReadOnlyBufferException">if no changes may be made to the contents of this buffer.</exception>
        public abstract ByteBuffer PutChar(char value);

        /// <summary>
        /// Writes the given <see cref="char"/> to the specified index of this buffer.
        /// <para/>
        /// The <see cref="char"/> is converted to bytes using the current byte order. The position
        /// is not changed.
        /// </summary>
        /// <param name="index">the index, must not be negative and equal or less than <c>limit - 2</c>.</param>
        /// <param name="value">the <see cref="char"/> to write.</param>
        /// <returns>this buffer.</returns>
        /// <exception cref="IndexOutOfRangeException">if <paramref name=""index/> is invalid.</exception>
        /// <exception cref="ReadOnlyBufferException">if no changes may be made to the contents of this buffer.</exception>
        public abstract ByteBuffer PutChar(int index, char value);

        /// <summary>
        /// Writes the given <see cref="double"/> to the current position and increases the position
        /// by 8.
        /// <para/>
        /// The <see cref="double"/> is converted to bytes using the current byte order.
        /// </summary>
        /// <param name="value">the <see cref="double"/> to write.</param>
        /// <returns>this buffer.</returns>
        /// <exception cref="BufferOverflowException">if position is greater than <c>limit - 8</c>.</exception>
        /// <exception cref="ReadOnlyBufferException">if no changes may be made to the contents of this buffer.</exception>
        public abstract ByteBuffer PutDouble(double value);

        /// <summary>
        /// Writes the given <see cref="double"/> to the specified index of this buffer.
        /// <para/>
        /// The <see cref="double"/> is converted to bytes using the current byte order. The
        /// position is not changed.
        /// </summary>
        /// <param name="index">the index, must not be negative and equal or less than <c>limit - 8</c>.</param>
        /// <param name="value">the <see cref="double"/> to write.</param>
        /// <returns></returns>
        /// <exception cref="IndexOutOfRangeException">if <paramref name=""index/> is invalid.</exception>
        /// <exception cref="ReadOnlyBufferException">if no changes may be made to the contents of this buffer.</exception>
        public abstract ByteBuffer PutDouble(int index, double value);

        /// <summary>
        /// Writes the given <see cref="float"/> to the current position and increases the position
        /// by 4.
        /// <para/>
        /// The <see cref="float"/> is converted to bytes using the current byte order.
        /// <para/>
        /// NOTE: This was putSingle() in the JDK
        /// </summary>
        /// <param name="value">the <see cref="float"/> to write.</param>
        /// <returns>this buffer.</returns>
        /// <exception cref="BufferOverflowException">if position is greater than <c>limit - 4</c>.</exception>
        /// <exception cref="ReadOnlyBufferException">if no changes may be made to the contents of this buffer.</exception>
        public abstract ByteBuffer PutSingle(float value);

        /// <summary>
        /// Writes the given <see cref="float"/> to the specified index of this buffer.
        /// <para/>
        /// The <see cref="float"/> is converted to bytes using the current byte order. The
        /// position is not changed.
        /// <para/>
        /// NOTE: This was putSingle() in the JDK
        /// </summary>
        /// <param name="index">the index, must not be negative and equal or less than <c>limit - 4</c>.</param>
        /// <param name="value">the <see cref="float"/> to write.</param>
        /// <returns></returns>
        /// <exception cref="IndexOutOfRangeException">if <paramref name=""index/> is invalid.</exception>
        /// <exception cref="ReadOnlyBufferException">if no changes may be made to the contents of this buffer.</exception>
        public abstract ByteBuffer PutSingle(int index, float value);

        /// <summary>
        /// Writes the given <see cref="int"/> to the current position and increases the position by
        /// 4.
        /// <para/>
        /// The <see cref="int"/> is converted to bytes using the current byte order.
        /// <para/>
        /// NOTE: This was putInt() in the JDK
        /// </summary>
        /// <param name="value">the <see cref="int"/> to write.</param>
        /// <returns>this buffer.</returns>
        /// <exception cref="BufferOverflowException">if position is greater than <c>limit - 4</c>.</exception>
        /// <exception cref="ReadOnlyBufferException">if no changes may be made to the contents of this buffer.</exception>
        public abstract ByteBuffer PutInt32(int value);

        /// <summary>
        /// Writes the given <see cref="int"/> to the specified index of this buffer.
        /// <para/>
        /// The <see cref="int"/> is converted to bytes using the current byte order. The position
        /// is not changed.
        /// <para/>
        /// NOTE: This was putInt() in the JDK
        /// </summary>
        /// <param name="index">the index, must not be negative and equal or less than <c>limit - 4</c>.</param>
        /// <param name="value">the <see cref="int"/> to write.</param>
        /// <returns>this buffer.</returns>
        /// <exception cref="IndexOutOfRangeException">if <paramref name=""index/> is invalid.</exception>
        /// <exception cref="ReadOnlyBufferException">if no changes may be made to the contents of this buffer.</exception>
        public abstract ByteBuffer PutInt32(int index, int value);

        /// <summary>
        /// Writes the given <see cref="long"/> to the current position and increases the position
        /// by 8.
        /// <para/>
        /// The <see cref="long"/> is converted to bytes using the current byte order.
        /// <para/>
        /// NOTE: This was putLong() in the JDK
        /// </summary>
        /// <param name="value">the <see cref="long"/> to write.</param>
        /// <returns>this buffer.</returns>
        /// <exception cref="BufferOverflowException">if position is greater than <c>limit - 8</c>.</exception>
        /// <exception cref="ReadOnlyBufferException">if no changes may be made to the contents of this buffer.</exception>
        public abstract ByteBuffer PutInt64(long value);

        /// <summary>
        /// Writes the given <see cref="long"/> to the specified index of this buffer.
        /// <para/>
        /// The <see cref="long"/> is converted to bytes using the current byte order. The position
        /// is not changed.
        /// <para/>
        /// NOTE: This was putLong() in the JDK
        /// </summary>
        /// <param name="index">the index, must not be negative and equal or less than <c>limit - 8</c>.</param>
        /// <param name="value">the <see cref="long"/> to write.</param>
        /// <returns>this buffer.</returns>
        /// <exception cref="IndexOutOfRangeException">if <paramref name=""index/> is invalid.</exception>
        /// <exception cref="ReadOnlyBufferException">if no changes may be made to the contents of this buffer.</exception>
        public abstract ByteBuffer PutInt64(int index, long value);

        /// <summary>
        /// Writes the given <see cref="short"/> to the current position and increases the position
        /// by 2.
        /// <para/>
        /// The <see cref="short"/> is converted to bytes using the current byte order.
        /// <para/>
        /// NOTE: This was putShort() in the JDK
        /// </summary>
        /// <param name="value">the <see cref="short"/> to write.</param>
        /// <returns>this buffer.</returns>
        /// <exception cref="BufferOverflowException">if position is greater than <c>limit - 2</c>.</exception>
        /// <exception cref="ReadOnlyBufferException">if no changes may be made to the contents of this buffer.</exception>
        public abstract ByteBuffer PutInt16(short value);

        /// <summary>
        /// Writes the given <see cref="short"/> to the specified index of this buffer.
        /// <para/>
        /// The <see cref="short"/> is converted to bytes using the current byte order. The
        /// position is not changed.
        /// <para/>
        /// NOTE: This was putShort() in the JDK
        /// </summary>
        /// <param name="index">the index, must not be negative and equal or less than <c>limit - 2</c>.</param>
        /// <param name="value">the <see cref="short"/> to write.</param>
        /// <returns>this buffer.</returns>
        /// <exception cref="IndexOutOfRangeException">if <paramref name=""index/> is invalid.</exception>
        /// <exception cref="ReadOnlyBufferException">if no changes may be made to the contents of this buffer.</exception>
        public abstract ByteBuffer PutInt16(int index, short value);

        /// <summary>
        /// Returns a sliced buffer that shares its content with this buffer.
        /// <para/>
        /// The sliced buffer's capacity will be this buffer's
        /// <see cref="Remaining"/>, and it's zero position will correspond to
        /// this buffer's current position. The new buffer's position will be 0,
        /// limit will be its capacity, and its mark is cleared. The new buffer's
        /// read-only property and byte order are the same as this buffer's.
        /// <para/>
        /// The new buffer shares its content with this buffer, which means either
        /// buffer's change of content will be visible to the other. The two buffer's
        /// position, limit and mark are independent.
        /// </summary>
        /// <returns>A sliced buffer that shares its content with this buffer.</returns>
        public abstract ByteBuffer Slice();

        /// <summary>
        /// Returns a string representing the state of this byte buffer.
        /// </summary>
        /// <returns>A string representing the state of this byte buffer.</returns>
        public override string ToString()
        {
            StringBuilder buf = new StringBuilder();
            buf.Append(GetType().GetTypeInfo().Name);
            buf.Append(", status: capacity="); //$NON-NLS-1$
            buf.Append(Capacity);
            buf.Append(" position="); //$NON-NLS-1$
            buf.Append(Position);
            buf.Append(" limit="); //$NON-NLS-1$
            buf.Append(Limit);
            return buf.ToString();
        }
    }
}
