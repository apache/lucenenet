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

    using DataOutput = Lucene.Net.Store.DataOutput;

    /// <summary>
    /// A data compressor.
    /// </summary>
    public abstract class Compressor
    {
        /// <summary>
        /// Sole constructor, typically called from sub-classes. </summary>
        protected Compressor()
        {
        }

        /// <summary>
        /// Compress bytes into <paramref name="out"/>. It it the responsibility of the
        /// compressor to add all necessary information so that a <see cref="Decompressor"/>
        /// will know when to stop decompressing bytes from the stream.
        /// </summary>
        public abstract void Compress(byte[] bytes, int off, int len, DataOutput @out);
    }
}