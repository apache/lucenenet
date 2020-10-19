using Lucene.Net.Diagnostics;
using System;

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

    /// <summary>
    /// LUCENENET specific class to allow referencing static members of
    /// <see cref="RollingBuffer{T}"/> without referencing its generic closing type.
    /// </summary>
    public static class RollingBuffer
    {
        /// <summary>
        /// Implement to reset an instance
        /// </summary>
        public interface IResettable
        {
            void Reset();
        }
    }

    /// <summary>
    /// Acts like forever growing <see cref="T:T[]"/>, but internally uses a
    /// circular buffer to reuse instances of <typeparam name="T"/>.
    /// <para/>
    /// @lucene.internal
    /// </summary>
    public abstract class RollingBuffer<T>
        where T : RollingBuffer.IResettable
    {
        private T[] buffer = new T[8];

        // Next array index to write to:
        private int nextWrite;

        // Next position to write:
        private int nextPos;

        // How many valid Position are held in the
        // array:
        private int count;

        protected RollingBuffer()
        {
            for (var idx = 0; idx < buffer.Length; idx++)
            {
                buffer[idx] = NewInstance(); // TODO GIVE ROLLING BUFFER A DELEGATE FOR NEW INSTANCE
            }
        }

        protected RollingBuffer(Func<T> factory)
        {
            for (int idx = 0; idx < buffer.Length; idx++)
            {
                buffer[idx] = factory();
            }
        }

        protected abstract T NewInstance();

        public virtual void Reset()
        {
            nextWrite--;
            while (count > 0)
            {
                if (nextWrite == -1)
                {
                    nextWrite = buffer.Length - 1;
                }
                buffer[nextWrite--].Reset();
                count--;
            }
            nextWrite = 0;
            nextPos = 0;
            count = 0;
        }

        // For assert:
        private bool InBounds(int pos)
        {
            return pos < nextPos && pos >= nextPos - count;
        }

        private int GetIndex(int pos)
        {
            int index = nextWrite - (nextPos - pos);
            if (index < 0)
            {
                index += buffer.Length;
            }
            return index;
        }

        /// <summary>
        /// Get <typeparamref name="T"/> instance for this absolute position;
        /// This is allowed to be arbitrarily far "in the
        /// future" but cannot be before the last <see cref="FreeBefore(int)"/>.
        /// </summary>
        public virtual T Get(int pos)
        {
            //System.out.println("RA.get pos=" + pos + " nextPos=" + nextPos + " nextWrite=" + nextWrite + " count=" + count);
            while (pos >= nextPos)
            {
                if (count == buffer.Length)
                {
                    var newBuffer = new T[ArrayUtil.Oversize(1 + count, RamUsageEstimator.NUM_BYTES_OBJECT_REF)];
                    Array.Copy(buffer, nextWrite, newBuffer, 0, buffer.Length - nextWrite);
                    Array.Copy(buffer, 0, newBuffer, buffer.Length - nextWrite, nextWrite);
                    for (int i = buffer.Length; i < newBuffer.Length; i++)
                    {
                        newBuffer[i] = NewInstance();
                    }
                    nextWrite = buffer.Length;
                    buffer = newBuffer;
                }
                if (nextWrite == buffer.Length)
                {
                    nextWrite = 0;
                }
                // Should have already been reset:
                nextWrite++;
                nextPos++;
                count++;
            }
            if (Debugging.AssertsEnabled) Debugging.Assert(InBounds(pos));
            int index = GetIndex(pos);
            //System.out.println("  pos=" + pos + " nextPos=" + nextPos + " -> index=" + index);
            //assert buffer[index].pos == pos;
            return buffer[index];
        }

        /// <summary>
        /// Returns the maximum position looked up, or -1 if no
        /// position has been looked up since <see cref="Reset()"/>/init.
        /// </summary>
        public virtual int MaxPos => nextPos - 1;

        public virtual void FreeBefore(int pos)
        {
            int toFree = count - (nextPos - pos);
            if (Debugging.AssertsEnabled)
            {
                Debugging.Assert(toFree >= 0);
                if (Debugging.ShouldAssert(toFree <= count)) Debugging.ThrowAssert("toFree={0} count={1}", toFree, count);
            }
            int index = nextWrite - count;
            if (index < 0)
            {
                index += buffer.Length;
            }
            for (int i = 0; i < toFree; i++)
            {
                if (index == buffer.Length)
                {
                    index = 0;
                }
                //System.out.println("  fb idx=" + index);
                buffer[index].Reset();
                index++;
            }
            count -= toFree;
        }
    }
}