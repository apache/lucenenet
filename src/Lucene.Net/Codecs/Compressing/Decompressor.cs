using System;
using BytesRef = Lucene.Net.Util.BytesRef;

namespace Lucene.Net.Codecs.Compressing
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

    using DataInput = Lucene.Net.Store.DataInput;

    /// <summary>
    /// A decompressor.
    /// </summary>
    public abstract class Decompressor // LUCENENET specific: Not implementing ICloneable per Microsoft's recommendation
    {
        /// <summary>
        /// Sole constructor, typically called from sub-classes. </summary>
        protected Decompressor()
        {
        }

        /// <summary>
        /// Decompress bytes that were stored between offsets <paramref name="offset"/> and
        /// <c>offset+length</c> in the original stream from the compressed
        /// stream <paramref name="in"/> to <paramref name="bytes"/>. After returning, the length
        /// of <paramref name="bytes"/> (<c>bytes.Length</c>) must be equal to
        /// <paramref name="length"/>. Implementations of this method are free to resize
        /// <paramref name="bytes"/> depending on their needs.
        /// </summary>
        /// <param name="in"> The input that stores the compressed stream. </param>
        /// <param name="originalLength"> The length of the original data (before compression). </param>
        /// <param name="offset"> Bytes before this offset do not need to be decompressed. </param>
        /// <param name="length"> Bytes after <c>offset+length</c> do not need to be decompressed. </param>
        /// <param name="bytes"> a <see cref="BytesRef"/> where to store the decompressed data. </param>
        public abstract void Decompress(DataInput @in, int originalLength, int offset, int length, BytesRef bytes);

        public abstract object Clone();
    }
}