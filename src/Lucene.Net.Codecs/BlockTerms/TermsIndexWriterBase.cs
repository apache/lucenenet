using Lucene.Net.Index;
using Lucene.Net.Support;
using Lucene.Net.Util;
using System;

namespace Lucene.Net.Codecs.BlockTerms
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
    /// Base class for terms index implementations to plug
    /// into <see cref="BlockTermsWriter"/>.
    /// <para/>
    /// @lucene.experimental 
    /// </summary>
    /// <seealso cref="TermsIndexReaderBase"/>
    public abstract class TermsIndexWriterBase : IDisposable
    {
        // LUCENENET specific - optimized empty array creation
        internal static readonly short[] EMPTY_INT16S = Arrays.Empty<short>();

        // LUCENENET specific - optimized empty array creation
        internal static readonly int[] EMPTY_INT32S = Arrays.Empty<int>();

        /// <summary>Terms index API for a single field.</summary>
        public abstract class FieldWriter
        {
            public abstract bool CheckIndexTerm(BytesRef text, TermStats stats);
            public abstract void Add(BytesRef text, TermStats stats, long termsFilePointer);
            public abstract void Finish(long termsFilePointer);
        }

        public abstract FieldWriter AddField(FieldInfo fieldInfo, long termsFilePointer);

        // LUCENENET specific - implementing proper dispose pattern
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected abstract void Dispose(bool disposing);
    }
}