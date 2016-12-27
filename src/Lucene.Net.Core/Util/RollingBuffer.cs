using System;
using System.Diagnostics;

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

    public static class RollingBuffer
    {
        public interface Resettable // LUCENENET TODO: rename with "I"
        {
            void Reset();
        }
    }

    /// <summary>
    /// Acts like forever growing T[], but internally uses a
    ///  circular buffer to reuse instances of T.
    ///
    ///  @lucene.internal
    /// </summary>
    public abstract class RollingBuffer<T>
        where T : RollingBuffer.Resettable
    {
        private T[] Buffer = new T[8];

        // Next array index to write to:
        private int NextWrite;

        // Next position to write:
        private int NextPos;

        // How many valid Position are held in the
        // array:
        private int Count;

        protected RollingBuffer() // LUCENENET TODO: Remove ? not used
        {
            for (var idx = 0; idx < Buffer.Length; idx++)
            {
                Buffer[idx] = NewInstance(); // TODO GIVE ROLLING BUFFER A DELEGATE FOR NEW INSTANCE
            }
        }

        protected RollingBuffer(Func<T> factory)
        {
            for (int idx = 0; idx < Buffer.Length; idx++)
            {
                Buffer[idx] = factory();
            }
        }

        protected abstract T NewInstance();

        public virtual void Reset()
        {
            NextWrite--;
            while (Count > 0)
            {
                if (NextWrite == -1)
                {
                    NextWrite = Buffer.Length - 1;
                }
                Buffer[NextWrite--].Reset();
                Count--;
            }
            NextWrite = 0;
            NextPos = 0;
            Count = 0;
        }

        // For assert:
        private bool InBounds(int pos)
        {
            return pos < NextPos && pos >= NextPos - Count;
        }

        private int GetIndex(int pos)
        {
            int index = NextWrite - (NextPos - pos);
            if (index < 0)
            {
                index += Buffer.Length;
            }
            return index;
        }

        /// <summary>
        /// Get T instance for this absolute position;
        ///  this is allowed to be arbitrarily far "in the
        ///  future" but cannot be before the last freeBefore.
        /// </summary>
        public virtual T Get(int pos)
        {
            //System.out.println("RA.get pos=" + pos + " nextPos=" + nextPos + " nextWrite=" + nextWrite + " count=" + count);
            while (pos >= NextPos)
            {
                if (Count == Buffer.Length)
                {
                    var newBuffer = new T[ArrayUtil.Oversize(1 + Count, RamUsageEstimator.NUM_BYTES_OBJECT_REF)];
                    Array.Copy(Buffer, NextWrite, newBuffer, 0, Buffer.Length - NextWrite);
                    Array.Copy(Buffer, 0, newBuffer, Buffer.Length - NextWrite, NextWrite);
                    for (int i = Buffer.Length; i < newBuffer.Length; i++)
                    {
                        newBuffer[i] = NewInstance();
                    }
                    NextWrite = Buffer.Length;
                    Buffer = newBuffer;
                }
                if (NextWrite == Buffer.Length)
                {
                    NextWrite = 0;
                }
                // Should have already been reset:
                NextWrite++;
                NextPos++;
                Count++;
            }
            Debug.Assert(InBounds(pos));
            int index = GetIndex(pos);
            return Buffer[index];
        }

        /// <summary>
        /// Returns the maximum position looked up, or -1 if no
        ///  position has been looked up sinc reset/init.
        /// </summary>
        public virtual int MaxPos
        {
            get
            {
                return NextPos - 1;
            }
        }

        public virtual void FreeBefore(int pos)
        {
            int toFree = Count - (NextPos - pos);
            Debug.Assert(toFree >= 0);
            Debug.Assert(toFree <= Count, "toFree=" + toFree + " count=" + Count);
            int index = NextWrite - Count;
            if (index < 0)
            {
                index += Buffer.Length;
            }
            for (int i = 0; i < toFree; i++)
            {
                if (index == Buffer.Length)
                {
                    index = 0;
                }
                //System.out.println("  fb idx=" + index);
                Buffer[index].Reset();
                index++;
            }
            Count -= toFree;
        }
    }
}