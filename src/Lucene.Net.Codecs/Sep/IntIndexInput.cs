using Lucene.Net.Store;
using System;

namespace Lucene.Net.Codecs.Sep
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
    /// Defines basic API for writing ints to an <see cref="IndexOutput"/>.
    /// IntBlockCodec interacts with this API. See IntBlockReader.
    /// <para/>
    /// NOTE: This was IntIndexInput in Lucene
    /// <para/>
    /// @lucene.experimental 
    /// </summary>
    public abstract class Int32IndexInput : IDisposable
    {
        public abstract Reader GetReader();

        // LUCENENET specific - implementing proper dispose pattern
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        protected abstract void Dispose(bool disposing);

        public abstract Index GetIndex();

        /// <summary>
        /// Records a single skip-point in the <see cref="Int32IndexInput.GetReader()"/>. </summary>
        public abstract class Index
        {
            public abstract void Read(DataInput indexIn, bool absolute);

            /// <summary>Seeks primary stream to the last read offset. </summary>
            public abstract void Seek(Reader stream);

            public abstract void CopyFrom(Index other);

            public abstract object Clone();
        }

        /// <summary>Reads <see cref="int"/> values.</summary>
        public abstract class Reader
        {
            /// <summary>Reads next single <see cref="int"/>.</summary>
            public abstract int Next();
        }
    }
}
