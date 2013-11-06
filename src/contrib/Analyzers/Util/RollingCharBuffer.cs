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

using System;
using System.IO;
using Lucene.Net.Util;

namespace Lucene.Net.Analysis.Util
{
    /// <summary>
    /// Acts like a forever growing char[] as you read
    /// characters into it from the provided reader, but
    /// internally it uses a circular buffer to only hold the
    /// characters that haven't been freed yet.  This is like a
    /// PushbackReader, except you don't have to specify
    /// up-front the max size of the buffer, but you do have to
    /// periodically call {@link #freeBefore}. 
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
        /// Clear array and switch to new reader.
        /// </summary>
        /// <param name="reader"></param>
        public void Reset(TextReader reader)
        {
            this.reader = reader;
            nextPos = 0;
            nextWrite = 0;
            count = 0;
            end = false;
        }

        /// <summary>
        /// Absolute position read. NOTE: pos must not jump
        /// ahead by more than 1! I.e., it's OK to read arbitrarily
        /// far back (just not prior to the last {@link
        /// #freeBefore}), but NOT ok to read arbitrarily far
        /// ahead. Returns -1 if you hit EOF.
        /// </summary>
        /// <param name="pos"></param>
        /// <returns></returns>
        public int Get(int pos)
        {
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
                    Array.Copy(buffer, nextWrite, newBuffer, 0, buffer.Length - nextWrite);
                    Array.Copy(buffer, 0, newBuffer, buffer.Length - nextWrite, nextWrite);
                    nextWrite = buffer.Length;
                    buffer = newBuffer;
                }
                if (nextWrite == buffer.Length)
                {
                    nextWrite = 0;
                }

                var toRead = buffer.Length - Math.Max(count, nextWrite);
                var readCount = reader.Read(buffer, nextWrite, toRead);
                if (readCount == -1)
                {
                    end = true;
                    return -1;
                }
                var ch = buffer[nextWrite];
                nextWrite += readCount;
                count += readCount;
                nextPos += readCount;
                return ch;
            }
            else
            {
                // Cannot read from future (except by 1):
                // assert pos < nextPos;
                if (pos >= nextPos)
                    throw new InvalidOperationException("Cannot read from future (except by 1).");

                // Cannot read from already freed past:
                // assert nextPos - pos <= count: "nextPos=" + nextPos + " pos=" + pos + " count=" + count;
                if (nextPos - pos > count)
                    throw new InvalidOperationException("nextPos=" + nextPos + " pos=" + pos + " count=" + count);

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
            var index = nextWrite - (nextPos - pos);
            if (index < 0)
            {
                // Wrap:
                index += buffer.Length;
                //assert index >= 0;
                if (index < 0)
                    throw new InvalidOperationException();
            }
            return index;
        }


        public char[] Get(int posStart, int length)
        {
            if (length <= 0)
                throw new ArgumentException("Must be greater than zero.", "length");

            if (!InBounds(posStart))
                throw new ArgumentException("posStart=" + posStart + " length=" + length, "posStart");

            var startIndex = GetIndex(posStart);
            var endIndex = GetIndex(posStart + length);

            var result = new char[length];
            if (endIndex >= startIndex && length < buffer.Length)
            {
                Array.Copy(buffer, startIndex, result, 0, endIndex - startIndex);
            }
            else
            {
                // wrapped:
                var part1 = buffer.Length - startIndex;
                Array.Copy(buffer, startIndex, result, 0, part1);
                Array.Copy(buffer, 0, result, buffer.Length-startIndex, length-part1);
            }
            return result;
        }

        /// <summary>
        /// Call this to notify us that no chars before this
        /// absolute position are needed anymore.
        /// </summary>
        /// <param name="pos"></param>
        public void FreeBefore(int pos)
        {
            if (pos < 0)
                throw new ArgumentException("Must be greater than or equal to zero.", "pos");

            if (pos > nextPos)
                throw new ArgumentException("Must be less than or equal to nextPos", "pos");

            var newCount = nextPos - pos;

            if (newCount > count)
                throw new InvalidOperationException("newCount=" + newCount + " count=" + count);

            if (newCount > buffer.Length)
                throw new InvalidOperationException("newCount=" + newCount + " buf.length=" + buffer.Length);

            count = newCount;
        }
    }
}
