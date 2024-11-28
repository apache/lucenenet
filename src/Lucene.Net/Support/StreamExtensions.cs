#if !FEATURE_STREAM_READEXACTLY
using System.IO;

namespace Lucene.Net.Support
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

    public static class StreamExtensions
    {
        /// <summary>
        /// Reads exactly <paramref name="count"/> bytes from the stream into the buffer at the specified offset.
        /// </summary>
        /// <param name="stream">The stream to read from.</param>
        /// <param name="buffer">The buffer to read into.</param>
        /// <param name="offset">The offset in the buffer to start reading into.</param>
        /// <param name="count">The number of bytes to read.</param>
        /// <exception cref="EndOfStreamException">If the end of the stream is reached before reading all the bytes.</exception>
        /// <remarks>
        /// This method is a polyfill for platforms (prior to .NET 9) that do not have the ReadExactly method.
        /// </remarks>
        public static void ReadExactly(this Stream stream, byte[] buffer, int offset, int count)
        {
            int numRead = stream.Read(buffer, offset, count);

            if (numRead < count)
            {
                throw new EndOfStreamException();
            }
        }
    }
}
#endif
