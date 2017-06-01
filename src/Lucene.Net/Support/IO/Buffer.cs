// This class was sourced from the Apache Harmony project
// https://svn.apache.org/repos/asf/harmony/enhanced/java/trunk/

using System;

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
    /// A buffer is a list of elements of a specific primitive type.
    /// <para/>
    /// A buffer can be described by the following properties:
    /// <list type="bullet">
    ///     <item><description>
    ///         Capacity:
    ///         The number of elements a buffer can hold. Capacity may not be
    ///         negative and never changes.
    ///     </description></item>
    ///     <item><description>
    ///         Position:
    ///         A cursor of this buffer. Elements are read or written at the
    ///         position if you do not specify an index explicitly. Position may not be
    ///         negative and not greater than the limit.
    ///     </description></item>
    ///     <item><description>
    ///         Limit:
    ///         Controls the scope of accessible elements. You can only read or
    ///         write elements from index zero to <c>limit - 1</c>. Accessing
    ///         elements out of the scope will cause an exception. Limit may not be negative
    ///         and not greater than capacity.
    ///     </description></item>
    ///     <item><description>
    ///         Mark: 
    ///         Used to remember the current position, so that you can reset the
    ///         position later. Mark may not be negative and no greater than position.
    ///     </description></item>
    ///     <item><description>
    ///         A buffer can be read-only or read-write. Trying to modify the elements
    ///         of a read-only buffer will cause a <see cref="ReadOnlyBufferException"/>,
    ///         while changing the position, limit and mark of a read-only buffer is OK.
    ///     </description></item>
    ///     <item><description>
    ///         A buffer can be direct or indirect. A direct buffer will try its best to
    ///         take advantage of native memory APIs and it may not stay in the heap,
    ///         thus it is not affected by garbage collection.
    ///     </description></item>
    /// </list>
    /// <para/>
    /// Buffers are not thread-safe. If concurrent access to a buffer instance is
    /// required, then the callers are responsible to take care of the
    /// synchronization issues.
    /// </summary>
#if FEATURE_SERIALIZABLE
    [Serializable]
#endif
    public abstract class Buffer
    {
        /// <summary>
        /// <c>UNSET_MARK</c> means the mark has not been set.
        /// </summary>
        internal readonly static int UNSET_MARK = -1;

        /// <summary>
        /// The capacity of this buffer, which never change.
        /// </summary>
        internal readonly int capacity;

        /// <summary>
        /// <c>limit - 1</c> is the last element that can be read or written.
        /// Limit must be no less than zero and no greater than <see cref="capacity"/>.
        /// </summary>
        internal int limit;

        /// <summary>
        /// Mark is where position will be set when <see cref="Reset()"/> is called.
        /// Mark is not set by default. Mark is always no less than zero and no
        /// greater than <see cref="position"/>.
        /// </summary>
        internal int mark = UNSET_MARK;

        /// <summary>
        /// The current position of this buffer. Position is always no less than zero
        /// and no greater than <see cref="limit"/>.
        /// </summary>
        internal int position = 0;

        /// <summary>
        /// Construct a buffer with the specified capacity.
        /// </summary>
        /// <param name="capacity">The capacity of this buffer</param>
        internal Buffer(int capacity)
        {
            if (capacity < 0)
            {
                throw new ArgumentException("Negative capacity: " + capacity);
            }
            this.capacity = this.limit = capacity;
        }

        /// <summary>
        /// Returns the capacity of this buffer.
        /// </summary>
        public int Capacity
        {
            get { return capacity; }
        }

        /// <summary>
        /// Clears this buffer.
        /// <para>
        /// While the content of this buffer is not changed, the following internal
        /// changes take place: the current position is reset back to the start of
        /// the buffer, the value of the buffer limit is made equal to the capacity
        /// and mark is cleared.
        /// </para>
        /// </summary>
        /// <returns>This buffer</returns>
        public Buffer Clear()
        {
            position = 0;
            mark = UNSET_MARK;
            limit = capacity;
            return this;
        }

        /// <summary>
        /// Flips this buffer.
        /// <para/>
        /// The limit is set to the current position, then the position is set to
        /// zero, and the mark is cleared.
        /// <para/>
        /// The content of this buffer is not changed.
        /// </summary>
        /// <returns>This buffer</returns>
        public Buffer Flip()
        {
            limit = position;
            position = 0;
            mark = UNSET_MARK;
            return this;
        }

        /// <summary>
        /// Indicates if there are elements remaining in this buffer, that is if
        /// <c>position &lt; limit</c>.
        /// </summary>
        public bool HasRemaining
        {
            get { return position < limit; }
        }

        /// <summary>
        /// Indicates whether this buffer is read-only.
        /// </summary>
        /// <returns>
        /// <c>true</c> if, this buffer is read-only; otherwise <c>false</c>.
        /// </returns>
        public abstract bool IsReadOnly { get; }

        /// <summary>
        /// Gets or Sets the limit of this buffer.
        /// </summary>
        public int Limit
        {
            get { return limit; }
            set { SetLimit(value); }
        }

        /// <summary>
        /// Sets the limit of this buffer.
        /// <para/>
        /// If the current position in the buffer is in excess of
        /// <c>newLimit</c> then, on returning from this call, it will have
        /// been adjusted to be equivalent to <c>newLimit</c>. If the mark
        /// is set and is greater than the new limit, then it is cleared.
        /// </summary>
        /// <param name="newLimit">The new limit value; must be non-negative and no larger than this buffer's capacity</param>
        /// <returns>This buffer</returns>
        /// <exception cref="ArgumentException">If <paramref name="newLimit"/> is invalid.</exception>
        public Buffer SetLimit(int newLimit)
        {
            if (newLimit < 0 || newLimit > capacity)
            {
                throw new ArgumentException();
            }

            limit = newLimit;
            if (position > newLimit)
            {
                position = newLimit;
            }
            if ((mark != UNSET_MARK) && (mark > newLimit))
            {
                mark = UNSET_MARK;
            }
            return this;
        }

        /// <summary>
        /// Marks the current position, so that the position may return to this point
        /// later by calling <see cref="Reset()"/>.
        /// </summary>
        /// <returns>This buffer</returns>
        public Buffer Mark()
        {
            mark = position;
            return this;
        }

        /// <summary>
        /// Returns the position of this buffer.
        /// </summary>
        public int Position
        {
            get { return position; }
            set { SetPosition(value); }
        }

        /// <summary>
        /// Sets the position of this buffer.
        /// <para/>
        /// If the mark is set and it is greater than the new position, then it is
        /// cleared.
        /// </summary>
        /// <param name="newPosition">The new position, must be not negative and not greater than limit.</param>
        /// <returns>This buffer</returns>
        /// <exception cref="ArgumentException">If <paramref name="newPosition"/> is invalid.</exception>
        public Buffer SetPosition(int newPosition)
        {
            if (newPosition < 0 || newPosition > limit)
            {
                throw new ArgumentException();
            }

            position = newPosition;
            if ((mark != UNSET_MARK) && (mark > position))
            {
                mark = UNSET_MARK;
            }
            return this;
        }

        /// <summary>
        /// Returns the number of remaining elements in this buffer, that is
        /// <c>limit - position</c>.
        /// </summary>
        public int Remaining
        {
            get { return limit - position; }
        }

        /// <summary>
        /// Resets the position of this buffer to the <see cref="mark"/>.
        /// </summary>
        /// <returns>This buffer</returns>
        /// <exception cref="InvalidMarkException">If the mark has not been set</exception>
        public Buffer Reset()
        {
            if (mark == UNSET_MARK)
            {
                throw new InvalidMarkException();
            }
            position = mark;
            return this;
        }





        /// <summary>
        /// Rewinds this buffer.
        /// <para/>
        /// The position is set to zero, and the mark is cleared. The content of this]
        /// buffer is not changed.
        /// </summary>
        /// <returns>This buffer</returns>
        public Buffer Rewind()
        {
            position = 0;
            mark = UNSET_MARK;
            return this;
        }
    }
}