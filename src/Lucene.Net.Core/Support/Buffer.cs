using System;

namespace Lucene.Net.Support
{
    /// <summary>
    /// Base class for <see cref="ByteBuffer"/> and <see cref="LongBuffer"/> (ported from Java)
    /// </summary>
    public abstract class Buffer
    {
        private int mark = -1;
        private int position;
        private int capacity;
        private int limit;

        // Used only by direct buffers
        // NOTE: hoisted here for speed in JNI GetDirectBufferAddress
        internal long address;

        /// <summary>
        /// Creates a new buffer with the given mark, position, limit, and capacity,
        /// after checking invariants.
        /// </summary>
        internal Buffer(int mark, int pos, int lim, int cap)
        {
            if (cap < 0)
                throw new ArgumentException("Negative capacity: " + cap);
            this.capacity = cap;
            SetLimit(lim);
            SetPosition(pos);
            if (mark >= 0)
            {
                if (mark > pos)
                    throw new ArgumentException("mark > position: ("
                                                       + mark + " > " + pos + ")");
                this.mark = mark;
            }
        }

        /// <summary>
        /// Returns this buffer's capacity.
        /// </summary>
        public int Capacity
        {
            get { return capacity; }
        }

        /// <summary>
        /// Returns this buffer's position.
        /// </summary>
        public int Position
        {
            get { return position; }
            set { SetPosition(value); }
        }

        /// <summary>
        /// Sets this buffer's position.  If the mark is defined and larger than the
        /// new position then it is discarded.
        /// </summary>
        /// <param name="newPosition">The new position value; must be non-negative and no larger than the current limit</param>
        /// <returns>This buffer</returns>
        /// <exception cref="ArgumentException">If the preconditions on <paramref name="newPosition"/> do not hold</exception>
        public Buffer SetPosition(int newPosition)
        {
            if ((newPosition > limit) || (newPosition < 0))
                throw new ArgumentException();
            position = newPosition;
            if (mark > position) mark = -1;
            return this;
        }


        /// <summary>
        /// Returns this buffer's limit.
        /// </summary>
        public int Limit
        {
            get { return limit; }
            set { SetLimit(value); }
        }

        /// <summary>
        /// Sets this buffer's limit.  If the position is larger than the new limit
        /// then it is set to the new limit.  If the mark is defined and larger than
        /// the new limit then it is discarded.
        /// </summary>
        /// <param name="newLimit">The new limit value; must be non-negative and no larger than this buffer's capacity</param>
        /// <returns>This buffer</returns>
        /// <exception cref="ArgumentException">If the preconditions on <paramref name="newLimit"/> do not hold</exception>
        public Buffer SetLimit(int newLimit)
        {
            if ((newLimit > capacity) || (newLimit < 0))
                throw new ArgumentException();
            limit = newLimit;
            if (position > limit) position = limit;
            if (mark > limit) mark = -1;
            return this;
        }

        /// <summary>
        /// Sets this buffer's mark at its position.
        /// </summary>
        /// <returns>This buffer</returns>
        public Buffer Mark()
        {
            mark = position;
            return this;
        }

        /// <summary>
        /// Resets this buffer's position to the previously-marked position.
        /// 
        /// <para>
        /// Invoking this method neither changes nor discards the mark's
        /// value.
        /// </para>
        /// </summary>
        /// <returns>This buffer</returns>
        /// <exception cref="InvalidMarkException">If the mark has not been set</exception>
        public Buffer Reset()
        {
            int m = mark;
            if (m < 0)
                throw new InvalidMarkException();
            position = m;
            return this;
        }

        /// <summary>
        /// Clears this buffer.  The position is set to zero, the limit is set to
        /// the capacity, and the mark is discarded.
        /// 
        /// <para>
        /// Invoke this method before using a sequence of channel-read or
        /// <c>Put</c> operations to fill this buffer.  For example:
        /// 
        /// <code>
        /// buf.Clear();     // Prepare buffer for reading
        /// in.Read(buf);    // Read data
        /// </code>
        /// </para>
        /// <para>
        /// This method does not actually erase the data in the buffer, but it
        /// is named as if it did because it will most often be used in situations
        /// in which that might as well be the case.
        /// </para>
        /// </summary>
        /// <returns>This buffer</returns>
        public Buffer Clear()
        {
            position = 0;
            limit = capacity;
            mark = -1;
            return this;
        }

        /// <summary>
        /// Flips this buffer.  The limit is set to the current position and then
        /// the position is set to zero.  If the mark is defined then it is
        /// discarded.
        /// 
        /// <para>
        /// After a sequence of channel-read or <c>Put</c> operations, invoke
        /// this method to prepare for a sequence of channel-write or relative
        /// <c>Get</c> operations.  For example:
        /// 
        /// <code>
        /// buf.Put(magic);    // Prepend header
        /// in.Read(buf);      // Read data into rest of buffer
        /// buf.Flip();        // Flip buffer
        /// out.Write(buf);    // Write header + data to channel
        /// </code>
        /// </para>
        /// <para>
        /// This method is often used in conjunction with the <see cref="ByteBuffer.Compact()"/>
        /// method when transferring data from one place to another.
        /// </para>
        /// </summary>
        /// <returns>This buffer</returns>
        public Buffer Flip()
        {
            limit = position;
            position = 0;
            mark = -1;
            return this;
        }

        /// <summary>
        /// Rewinds this buffer.  The position is set to zero and the mark is
        /// discarded.
        /// 
        /// <para>
        /// Invoke this method before a sequence of channel-write or <c>Get</c>
        /// operations, assuming that the limit has already been set
        /// appropriately.  For example:
        /// 
        /// <code>
        /// out.Write(buf);    // Write remaining data
        /// buf.Rewind();      // Rewind buffer
        /// buf.Get(array);    // Copy data into array
        /// </code>
        /// </para>
        /// </summary>
        /// <returns>This buffer</returns>
        public Buffer Rewind()
        {
            position = 0;
            mark = -1;
            return this;
        }

        /// <summary>
        /// Returns the number of elements between the current position and the
        /// limit.
        /// </summary>
        public int Remaining
        {
            get { return limit - position; }
        }

        /// <summary>
        /// Tells whether there are any elements between the current position and
        /// the limit.
        /// </summary>
        public bool HasRemaining
        {
            get { return position < limit; }
        }

        /// <summary>
        /// Tells whether or not this buffer is read-only.
        /// </summary>
        /// <returns>
        /// <c>true</c> if, and only if, this buffer is read-only
        /// </returns>
        public abstract bool IsReadOnly { get; }

        /// <summary>
        /// Tells whether or not this buffer is backed by an accessible
        /// array.
        /// 
        /// <para>
        /// If this method returns <c>true</c> then the <see cref="Array"/> 
        /// and <see cref="ArrayOffset"/> properties may be safely invoked.
        /// </para>
        /// </summary>
        /// <returns>
        /// <c>true</c> if, and only if, this buffer is backed by an array and is not read-only
        /// </returns>
        public abstract bool HasArray { get; }

        /// <summary>
        /// Returns the array that backs this
        /// buffer&nbsp;&nbsp;<i>(optional operation)</i>.
        /// 
        /// <para>
        /// This property is intended to allow array-backed buffers to be
        /// passed to native code more efficiently. Concrete subclasses
        /// provide more strongly-typed return values for this property.
        /// </para>
        /// <para>
        /// Modifications to this buffer's content will cause the returned
        /// array's content to be modified, and vice versa.
        /// </para>
        /// <para>
        /// Check the <see cref="HasArray"/> property before using this
        /// property in order to ensure that this buffer has an accessible backing
        /// array.
        /// </para>
        /// </summary>
        /// <returns>
        /// The array that backs this buffer
        /// </returns>
        /// <exception cref="ReadOnlyBufferException">If this buffer is backed by an array but is read-only</exception>
        /// <exception cref="InvalidOperationException">If this buffer is not backed by an accessible array</exception>
        public abstract object Array { get; }


        /// <summary>
        /// Returns the offset within this buffer's backing array of the first
        /// element of the buffer&nbsp;&nbsp;<i>(optional operation)</i>.
        /// 
        /// <para>
        /// If this buffer is backed by an array then buffer position <c>p</c>
        /// corresponds to array index <c>p</c>&nbsp;+&nbsp;<see cref="ArrayOffset"/><c>.
        /// </para>
        /// <para>
        /// Check the <see cref="HasArray"/> property before using this
        /// property in order to ensure that this buffer has an accessible backing
        /// array.
        /// </para>
        /// </summary>
        /// <returns>
        /// The offset within this buffer's array
        /// of the first element of the buffer
        /// </returns>
        /// <exception cref="ReadOnlyBufferException">If this buffer is backed by an array but is read-only</exception>
        /// <exception cref="InvalidOperationException">If this buffer is not backed by an accessible array</exception>
        public abstract int ArrayOffset { get; }

        /// <summary>
        /// Tells whether or not this buffer is <c>direct</c>
        /// </summary>
        /// <returns><c>true</c> if, and only if, this buffer is direct</returns>
        public abstract bool IsDirect { get; }

        // -- internal members for bounds checking, etc. --

        /// <summary>
        /// Checks the current position against the limit, throwing a
        /// <see cref="BufferUnderflowException"/> if it is not smaller than the limit, and then
        /// increments the position.
        /// </summary>
        /// <returns>The current position value, before it is incremented</returns>
        internal int NextGetIndex()
        {
            if (position >= limit)
                throw new BufferUnderflowException();
            return position++;
        }

        internal int NextGetIndex(int nb)
        {
            if (limit - position < nb)
                throw new BufferUnderflowException();
            int p = position;
            position += nb;
            return p;
        }

        /// <summary>
        /// Checks the current position against the limit, throwing a <see cref="BufferOverflowException"/>
        /// if it is not smaller than the limit, and then
        /// increments the position.
        /// </summary>
        /// <returns>The current position value, before it is incremented</returns>
        internal int NextPutIndex()
        {
            if (position >= limit)
                throw new BufferOverflowException();
            return position++;
        }

        internal int NextPutIndex(int nb)
        {
            if (limit - position < nb)
                throw new BufferOverflowException();
            int p = position;
            position += nb;
            return p;
        }

        /// <summary>
        /// Checks the given index against the limit, throwing an <see cref="IndexOutOfRangeException"/> 
        /// if it is not smaller than the limit or is smaller than zero.
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        internal int CheckIndex(int i)
        {
            if ((i < 0) || (i >= limit))
                throw new IndexOutOfRangeException();
            return i;
        }

        internal int CheckIndex(int i, int nb)
        {
            if ((i < 0) || (nb > limit - i))
                throw new IndexOutOfRangeException();
            return i;
        }

        internal int MarkValue
        {
            get { return mark; }
        }

        internal void Truncate()
        {
            mark = -1;
            position = 0;
            limit = 0;
            capacity = 0;
        }

        internal void DiscardMark()
        {
            mark = -1;
        }

        internal static void CheckBounds(int off, int len, int size)
        {
            if ((off | len | (off + len) | (size - (off + len))) < 0)
                throw new IndexOutOfRangeException();
        }
    }
}
