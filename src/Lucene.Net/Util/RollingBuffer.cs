using Lucene.Net.Diagnostics;
using Lucene.Net.Support;
using System.Runtime.CompilerServices;

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
    /// Implement to reset an instance
    /// </summary>
    public interface IResettable
    {
        void Reset();
    }

    /// <summary>
    /// LUCENENET specific interface to allow overriding rolling buffer item creation
    /// without having to call virtual methods from the constructor
    /// </summary>
    public interface IRollingBufferItemFactory<out T>
    {
        T Create(object rollingBuffer);
    }

    /// <summary>
    /// LUCENENET specific class that provides default implementation for
    /// <see cref="IRollingBufferItemFactory{T}"/>.
    /// </summary>
    public class RollingBufferItemFactory<T> : IRollingBufferItemFactory<T> where T : new()
    {
        public static RollingBufferItemFactory<T> Default { get; } = new RollingBufferItemFactory<T>();
        public virtual T Create(object rollingBuffer)
        {
            return new T();
        }
    }

    /// <summary>
    /// Acts like forever growing <see cref="T:T[]"/>, but internally uses a
    /// circular buffer to reuse instances of <typeparam name="T"/>.
    /// <para/>
    /// @lucene.internal
    /// </summary>
    // LUCENENET specific - removed NewInstance override and using NewPosition as factory
    public abstract class RollingBuffer<T>
        where T : IResettable
    {
        private T[] buffer = new T[8];

        // Next array index to write to:
        private int nextWrite;

        // Next position to write:
        private int nextPos;

        // How many valid Position are held in the
        // array:
        private int count;
        private IRollingBufferItemFactory<T> itemFactory;

        protected RollingBuffer(IRollingBufferItemFactory<T> itemFactory)
        {
            this.itemFactory = itemFactory; // LUCENENET specific - storing factory for usage in class
            for (int idx = 0; idx < buffer.Length; idx++)
            {
                buffer[idx] = itemFactory.Create(this);
            }
        }

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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
                    Arrays.Copy(buffer, nextWrite, newBuffer, 0, buffer.Length - nextWrite);
                    Arrays.Copy(buffer, 0, newBuffer, buffer.Length - nextWrite, nextWrite);
                    for (int i = buffer.Length; i < newBuffer.Length; i++)
                    {
                        newBuffer[i] = this.itemFactory.Create(this); // LUCENENET specific - using factory to create new instance
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
                Debugging.Assert(toFree <= count, "toFree={0} count={1}", toFree, count);
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