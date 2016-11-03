using System;

namespace Lucene.Net.Codecs.Compressing
{
    using BytesRef = Lucene.Net.Util.BytesRef;

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

    using DataInput = Lucene.Net.Store.DataInput;

    /// <summary>
    /// A decompressor.
    /// </summary>
    public abstract class Decompressor
    {
        /// <summary>
        /// Sole constructor, typically called from sub-classes. </summary>
        protected internal Decompressor()
        {
        }

        /// <summary>
        /// Decompress bytes that were stored between offsets <code>offset</code> and
        /// <code>offset+length</code> in the original stream from the compressed
        /// stream <code>in</code> to <code>bytes</code>. After returning, the length
        /// of <code>bytes</code> (<code>bytes.length</code>) must be equal to
        /// <code>length</code>. Implementations of this method are free to resize
        /// <code>bytes</code> depending on their needs.
        /// </summary>
        /// <param name="in"> the input that stores the compressed stream </param>
        /// <param name="originalLength"> the length of the original data (before compression) </param>
        /// <param name="offset"> bytes before this offset do not need to be decompressed </param>
        /// <param name="length"> bytes after <code>offset+length</code> do not need to be decompressed </param>
        /// <param name="bytes"> a <seealso cref="BytesRef"/> where to store the decompressed data </param>
        public abstract void Decompress(DataInput @in, int originalLength, int offset, int length, BytesRef bytes);

        public abstract object Clone();
    }
}