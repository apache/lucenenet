using Lucene.Net.Diagnostics;
using System;
using System.Diagnostics;

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

    using IBits = Lucene.Net.Util.IBits;

    /// <summary>
    /// Exposes a slice of an existing <see cref="IBits"/> as a new <see cref="IBits"/>.
    /// <para/>
    /// @lucene.internal
    /// </summary>
    internal sealed class BitsSlice : IBits
    {
        private readonly IBits parent;
        private readonly int start;
        private readonly int length;

        // start is inclusive; end is exclusive (length = end-start)
        public BitsSlice(IBits parent, ReaderSlice slice)
        {
            this.parent = parent;
            this.start = slice.Start;
            this.length = slice.Length;
            if (Debugging.AssertsEnabled) Debugging.Assert(length >= 0,"length={0}", length);
        }

        public bool Get(int doc)
        {
            if (doc >= length)
            {
                throw RuntimeException.Create("doc " + doc + " is out of bounds 0 .. " + (length - 1));
            }
            if (Debugging.AssertsEnabled) Debugging.Assert(doc < length,"doc={0} length={1}", doc, length);
            return parent.Get(doc + start);
        }

        public int Length => length;
    }
}