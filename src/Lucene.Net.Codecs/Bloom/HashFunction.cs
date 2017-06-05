using Lucene.Net.Util;

namespace Lucene.Net.Codecs.Bloom
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
    /// Base class for hashing functions that can be referred to by name.
    /// Subclasses are expected to provide threadsafe implementations of the hash function
    /// on the range of bytes referenced in the provided <see cref="BytesRef"/>.
    /// <para/>
    /// @lucene.experimental
    /// </summary>
    public abstract class HashFunction
    {

        /// <summary>
        /// Hashes the contents of the referenced bytes.
        /// </summary>
        /// <param name="bytes">The data to be hashed.</param>
        /// <returns>The hash of the bytes referenced by bytes.offset and length bytes.Length.</returns>
        public abstract int Hash(BytesRef bytes);
    }
}