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
    /// Exposes a slice of an existing Bits as a new Bits.
    ///
    /// @lucene.internal
    /// </summary>
    internal sealed class BitsSlice : IBits
    {
        private readonly IBits Parent;
        private readonly int Start;
        private readonly int Length_Renamed;

        // start is inclusive; end is exclusive (length = end-start)
        public BitsSlice(IBits parent, ReaderSlice slice)
        {
            this.Parent = parent;
            this.Start = slice.Start;
            this.Length_Renamed = slice.Length;
            Debug.Assert(Length_Renamed >= 0, "length=" + Length_Renamed);
        }

        public bool Get(int doc)
        {
            if (doc >= Length_Renamed)
            {
                throw new Exception("doc " + doc + " is out of bounds 0 .. " + (Length_Renamed - 1));
            }
            Debug.Assert(doc < Length_Renamed, "doc=" + doc + " length=" + Length_Renamed);
            return Parent.Get(doc + Start);
        }

        public int Length()
        {
            return Length_Renamed;
        }
    }
}