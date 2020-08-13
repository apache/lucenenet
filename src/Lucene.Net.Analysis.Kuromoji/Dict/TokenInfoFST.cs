using Lucene.Net.Diagnostics;
using Lucene.Net.Util.Fst;
using System.Diagnostics;

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
        private readonly FST<long?> fst;

        // depending upon fasterButMoreRam, we cache root arcs for either 
        // kana (0x3040-0x30FF) or kana + han (0x3040-0x9FFF)
        // false: 191 arcs
        // true:  28,607 arcs (costs ~1.5MB)
        private readonly int cacheCeiling;
        private readonly FST.Arc<long?>[] rootCache;

        private readonly long? NO_OUTPUT;

        // LUCENENET specific - made field private
        // and added public property for reading it.
        public long? NoOutput => NO_OUTPUT;

        public TokenInfoFST(FST<long?> fst, bool fasterButMoreRam)
        {
            this.fst = fst;
            this.cacheCeiling = fasterButMoreRam ? 0x9FFF : 0x30FF;
            NO_OUTPUT = fst.Outputs.NoOutput;
            rootCache = CacheRootArcs();
        }

        private FST.Arc<long?>[] CacheRootArcs()
        {
            FST.Arc<long?>[] rootCache = new FST.Arc<long?>[1 + (cacheCeiling - 0x3040)];
            FST.Arc<long?> firstArc = new FST.Arc<long?>();
            fst.GetFirstArc(firstArc);
            FST.Arc<long?> arc = new FST.Arc<long?>();
            FST.BytesReader fstReader = fst.GetBytesReader();
            // TODO: jump to 3040, readNextRealArc to ceiling? (just be careful we don't add bugs)
            for (int i = 0; i < rootCache.Length; i++)
            {
                if (fst.FindTargetArc(0x3040 + i, firstArc, arc, fstReader) != null)
                {
                    rootCache[i] = new FST.Arc<long?>().CopyFrom(arc);
                }
            }
            return rootCache;
        }

        public FST.Arc<long?> FindTargetArc(int ch, FST.Arc<long?> follow, FST.Arc<long?> arc, bool useCache, FST.BytesReader fstReader)
        {
            if (useCache && ch >= 0x3040 && ch <= cacheCeiling)
            {
                Debugging.Assert(() => ch != FST.END_LABEL);
                FST.Arc<long?> result = rootCache[ch - 0x3040];
                if (result == null)
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

        public FST.Arc<long?> GetFirstArc(FST.Arc<long?> arc)
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
        internal FST<long?> InternalFST => fst;
    }
}
