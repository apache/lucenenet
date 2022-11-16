// Lucene version compatibility level 4.8.1
using Lucene.Net.Diagnostics;
using Lucene.Net.Support;
using Lucene.Net.Util;
using System;
using System.Diagnostics;
using System.IO;

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
    /// Acts like a forever growing <see cref="T:char[]"/> as you read
    /// characters into it from the provided reader, but
    /// internally it uses a circular buffer to only hold the
    /// characters that haven't been freed yet.  This is like a
    /// PushbackReader, except you don't have to specify
    /// up-front the max size of the buffer, but you do have to
    /// periodically call <see cref="FreeBefore"/>. 
    /// </summary>
    public sealed class RollingCharBuffer
    {
        private TextReader reader;

        private char[] buffer = new char[512];

        // Next array index to write to in buffer:
        private int nextWrite;

        // Next absolute position to read from reader:
        private int nextPos;

        // How many valid chars (wrapped) are in the buffer:
        private int count;

        // True if we hit EOF
        private bool end;

        /// <summary>
        /// Clear array and switch to new reader. </summary>
        public void Reset(TextReader reader)
        {
            this.reader = reader;
            nextPos = 0;
            nextWrite = 0;
            count = 0;
            end = false;
        }

        /// <summary>
        /// Absolute position read.  NOTE: pos must not jump
        /// ahead by more than 1!  Ie, it's OK to read arbitarily
        /// far back (just not prior to the last <see cref="FreeBefore(int)"/>, 
        /// but NOT ok to read arbitrarily far
        /// ahead.  Returns -1 if you hit EOF.
        /// </summary>
        public int Get(int pos)
        {
            //System.out.println("    Get pos=" + pos + " nextPos=" + nextPos + " count=" + count);
            if (pos == nextPos)
            {
                if (end)
                {
                    return -1;
                }
                if (count == buffer.Length)
                {
                    // Grow
                    var newBuffer = new char[ArrayUtil.Oversize(1 + count, RamUsageEstimator.NUM_BYTES_CHAR)];
                    //System.out.println(Thread.currentThread().getName() + ": cb grow " + newBuffer.length);
                    Arrays.Copy(buffer, nextWrite, newBuffer, 0, buffer.Length - nextWrite);
                    Arrays.Copy(buffer, 0, newBuffer, buffer.Length - nextWrite, nextWrite);
                    nextWrite = buffer.Length;
                    buffer = newBuffer;
                }
                if (nextWrite == buffer.Length)
                {
                    nextWrite = 0;
                }

                int toRead = buffer.Length - Math.Max(count, nextWrite);
                int readCount = reader.Read(buffer, nextWrite, toRead);
                if (readCount <= 0)
                {
                    end = true;
                    return -1;
                }
                int ch = buffer[nextWrite];
                nextWrite += readCount;
                count += readCount;
                nextPos += readCount;
                return ch;
            }
            else
            {
                if (Debugging.AssertsEnabled)
                {
                    // Cannot read from future (except by 1):
                    Debugging.Assert(pos < nextPos);

                    // Cannot read from already freed past:
                    Debugging.Assert(nextPos - pos <= count, "nextPos={0} pos={1} count={2}", nextPos, pos, count);
                }

                return buffer[GetIndex(pos)];
            }
        }

        // For assert:
        private bool InBounds(int pos)
        {
            return pos >= 0 && pos < nextPos && pos >= nextPos - count;
        }

        private int GetIndex(int pos)
        {
            int index = nextWrite - (nextPos - pos);
            if (index < 0)
            {
                // Wrap:
                index += buffer.Length;
                if (Debugging.AssertsEnabled) Debugging.Assert(index >= 0);
            }
            return index;
        }

        public char[] Get(int posStart, int length)
        {
            if (Debugging.AssertsEnabled)
            {
                Debugging.Assert(length > 0);
                Debugging.Assert(InBounds(posStart), "posStart={0} length={1}", posStart, length);
            }
            //System.out.println("    buffer.Get posStart=" + posStart + " len=" + length);

            int startIndex = GetIndex(posStart);
            int endIndex = GetIndex(posStart + length);
            //System.out.println("      startIndex=" + startIndex + " endIndex=" + endIndex);

            var result = new char[length];
            if (endIndex >= startIndex && length < buffer.Length)
            {
                Arrays.Copy(buffer, startIndex, result, 0, endIndex - startIndex);
            }
            else
            {
                // Wrapped:
                int part1 = buffer.Length - startIndex;
                Arrays.Copy(buffer, startIndex, result, 0, part1);
                Arrays.Copy(buffer, 0, result, buffer.Length - startIndex, length - part1);
            }
            return result;
        }

        /// <summary>
        /// Call this to notify us that no chars before this
        /// absolute position are needed anymore. 
        /// </summary>
        public void FreeBefore(int pos)
        {
            if (Debugging.AssertsEnabled)
            {
                Debugging.Assert(pos >= 0);
                Debugging.Assert(pos <= nextPos);
            }
            int newCount = nextPos - pos;
            if (Debugging.AssertsEnabled)
            {
                Debugging.Assert(newCount <= count, "newCount={0} count={1}", newCount, count);
                Debugging.Assert(newCount <= buffer.Length, "newCount={0} buf.length={1}", newCount, buffer.Length);
            }
            count = newCount;
        }
    }
}