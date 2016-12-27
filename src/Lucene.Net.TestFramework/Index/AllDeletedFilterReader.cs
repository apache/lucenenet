using System.Diagnostics;

namespace Lucene.Net.Index
{
    using Lucene.Net.Util;

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
    /// Filters the incoming reader and makes all documents appear deleted.
    /// </summary>
    public class AllDeletedFilterReader : FilterAtomicReader
    {
        internal readonly IBits LiveDocs_Renamed;

        public AllDeletedFilterReader(AtomicReader @in)
            : base(@in)
        {
            LiveDocs_Renamed = new Bits.MatchNoBits(@in.MaxDoc);
            Debug.Assert(MaxDoc == 0 || HasDeletions);
        }

        public override IBits LiveDocs
        {
            get
            {
                return LiveDocs_Renamed;
            }
        }

        public override int NumDocs
        {
            get { return 0; }
        }
    }
}