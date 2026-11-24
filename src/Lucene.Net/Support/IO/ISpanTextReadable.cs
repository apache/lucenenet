
using System;
using System.IO;

namespace Lucene.Net.IO
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
    /// Provides efficient synchronous character-reading operations using spans.
    /// </summary>
    /// <remarks>
    /// This interface acts as a bridge to patch older target frameworks that
    /// do not support <see cref="Span{T}"/> overloads on <see cref="TextReader"/>.
    /// </remarks>
    internal interface ISpanTextReadable
    {
        /// <summary>
        /// Reads a sequence of characters from the input and writes them into
        /// the specified character span.
        /// </summary>
        /// <param name="buffer">
        /// The span into which the characters are written.
        /// </param>
        /// <returns>
        /// The number of characters read, or <c>0</c> if the end of the input
        /// has been reached.
        /// </returns>
        int Read(Span<char> buffer);

        /// <summary>
        /// Reads characters from the input until the specified span is filled
        /// or the end of the input is reached.
        /// </summary>
        /// <param name="buffer">
        /// The span into which the characters are written.
        /// </param>
        /// <returns>
        /// The number of characters read. This may be less than the length of
        /// <paramref name="buffer"/> if the end of the input is reached.
        /// </returns>
        int ReadBlock(Span<char> buffer);
    }
}
