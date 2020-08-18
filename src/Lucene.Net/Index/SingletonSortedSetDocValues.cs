using Lucene.Net.Diagnostics;

namespace Lucene.Net.Index
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

    using BytesRef = Lucene.Net.Util.BytesRef;

    /// <summary>
    /// Exposes multi-valued view over a single-valued instance.
    /// <para/>
    /// This can be used if you want to have one multi-valued implementation
    /// against e.g. <see cref="Search.IFieldCache.GetDocTermOrds(AtomicReader, string)"/> that also works for single-valued
    /// fields.
    /// </summary>
    internal sealed class SingletonSortedSetDocValues : SortedSetDocValues
    {
        private readonly SortedDocValues @in;
        private int docID;
        private bool set;

        /// <summary>
        /// Creates a multi-valued view over the provided <see cref="Index.SortedDocValues"/> </summary>
        public SingletonSortedSetDocValues(SortedDocValues @in)
        {
            this.@in = @in;
            if (Debugging.AssertsEnabled) Debugging.Assert(NO_MORE_ORDS == -1); // this allows our nextOrd() to work for missing values without a check
        }

        /// <summary>
        /// Return the wrapped <see cref="Index.SortedDocValues"/> </summary>
        public SortedDocValues SortedDocValues => @in;

        public override long NextOrd()
        {
            if (set)
            {
                return NO_MORE_ORDS;
            }
            else
            {
                set = true;
                return @in.GetOrd(docID);
            }
        }

        public override void SetDocument(int docID)
        {
            this.docID = docID;
            set = false;
        }

        public override void LookupOrd(long ord, BytesRef result)
        {
            // cast is ok: single-valued cannot exceed Integer.MAX_VALUE
            @in.LookupOrd((int)ord, result);
        }

        public override long ValueCount => @in.ValueCount;

        public override long LookupTerm(BytesRef key)
        {
            return @in.LookupTerm(key);
        }
    }
}