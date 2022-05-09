using Lucene.Net.Diagnostics;
using Lucene.Net.Util.Fst;
using System.Diagnostics;
using Int64 = J2N.Numerics.Int64;

namespace Lucene.Net.Analysis.Ja.Dict
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
    /// Thin wrapper around an FST with root-arc caching for Japanese.
    /// <para/>
    /// Depending upon fasterButMoreRam, either just kana (191 arcs),
    /// or kana and han (28,607 arcs) are cached. The latter offers
    /// additional performance at the cost of more RAM.
    /// </summary>
    public sealed class TokenInfoFST
    {
        private readonly FST<Int64> fst;

        // depending upon fasterButMoreRam, we cache root arcs for either 
        // kana (0x3040-0x30FF) or kana + han (0x3040-0x9FFF)
        // false: 191 arcs
        // true:  28,607 arcs (costs ~1.5MB)
        private readonly int cacheCeiling;
        private readonly FST.Arc<Int64>[] rootCache;

        private readonly Int64 NO_OUTPUT;

        // LUCENENET specific - made field private
        // and added public property for reading it.
        public Int64 NoOutput => NO_OUTPUT;

        public TokenInfoFST(FST<Int64> fst, bool fasterButMoreRam)
        {
            this.fst = fst;
            this.cacheCeiling = fasterButMoreRam ? 0x9FFF : 0x30FF;
            NO_OUTPUT = fst.Outputs.NoOutput;
            rootCache = CacheRootArcs();
        }

        private FST.Arc<Int64>[] CacheRootArcs()
        {
            FST.Arc<Int64>[] rootCache = new FST.Arc<Int64>[1 + (cacheCeiling - 0x3040)];
            FST.Arc<Int64> firstArc = new FST.Arc<Int64>();
            fst.GetFirstArc(firstArc);
            FST.Arc<Int64> arc = new FST.Arc<Int64>();
            FST.BytesReader fstReader = fst.GetBytesReader();
            // TODO: jump to 3040, readNextRealArc to ceiling? (just be careful we don't add bugs)
            for (int i = 0; i < rootCache.Length; i++)
            {
                if (fst.FindTargetArc(0x3040 + i, firstArc, arc, fstReader) != null)
                {
                    rootCache[i] = new FST.Arc<Int64>().CopyFrom(arc);
                }
            }
            return rootCache;
        }

        public FST.Arc<Int64> FindTargetArc(int ch, FST.Arc<Int64> follow, FST.Arc<Int64> arc, bool useCache, FST.BytesReader fstReader)
        {
            if (useCache && ch >= 0x3040 && ch <= cacheCeiling)
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(ch != FST.END_LABEL);
                FST.Arc<Int64> result = rootCache[ch - 0x3040];
                if (result is null)
                {
                    return null;
                }
                else
                {
                    arc.CopyFrom(result);
                    return arc;
                }
            }
            else
            {
                return fst.FindTargetArc(ch, follow, arc, fstReader);
            }
        }

        public FST.Arc<Int64> GetFirstArc(FST.Arc<Int64> arc)
        {
            return fst.GetFirstArc(arc);
        }

        public FST.BytesReader GetBytesReader()
        {
            return fst.GetBytesReader();
        }

        /// <summary>
        /// for testing only
        /// <para/>
        /// @lucene.internal 
        /// </summary>
        internal FST<Int64> InternalFST => fst;
    }
}
