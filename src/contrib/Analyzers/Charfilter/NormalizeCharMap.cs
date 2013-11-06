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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Lucene.Net.Support;
using Lucene.Net.Util;
using Lucene.Net.Util.Fst;

namespace Lucene.Net.Analysis.Charfilter
{
    public class NormalizeCharMap
    {
        protected internal readonly FST<CharsRef> map;
        protected internal readonly IDictionary<char, FST.Arc<CharsRef>> cachedRootArcs = new HashMap<char, FST.Arc<CharsRef>>();

        public FST<CharsRef> Map { get { return map; } }
        public IDictionary<char, FST.Arc<CharsRef>> CachedRootArcs { get { return cachedRootArcs; } }

        private NormalizeCharMap(FST<CharsRef> map)
        {
            this.map = map;
            if (map != null)
            {
                try
                {
                    // Pre-cache root arcs
                    var scratchArc = new FST.Arc<CharsRef>();
                    var fstReader = map.GetBytesReader();
                    map.GetFirstArc(scratchArc);
                    if (FST<CharsRef>.TargetHasArcs(scratchArc))
                    {
                        map.ReadFirstRealTargetArc(scratchArc.Target, scratchArc, fstReader);
                        while (true)
                        {
                            Debug.Assert(scratchArc.Label != FST<CharsRef>.END_LABEL);
                            cachedRootArcs.Add((char)scratchArc.Label,
                                               new FST.Arc<CharsRef>().CopyFrom(scratchArc));
                            if (scratchArc.IsLast())
                            {
                                break;
                            }
                            map.ReadNextRealArc(scratchArc, fstReader);
                        }
                    }
                }
                catch (IOException ioe)
                {// Bogus FST IOExceptions!!  (will never happen)
                    throw new Exception("FST threw exception", ioe);
                }
            }
        }

        public class Builder
        {
            private readonly IDictionary<string, string> _pendingPairs = new TreeMap<string, string>();

            public void Add(string match, string replacement)
            {
                if (match.Length == 0)
                {
                    throw new ArgumentException("Cannot match the empty string.");
                }
                if (_pendingPairs.ContainsKey(match))
                {
                    throw new ArgumentException(string.Format("Match \"{0}\" was already added.", match));
                }
                _pendingPairs.Add(match, replacement);
            }

            public NormalizeCharMap Build()
            {
                FST<CharsRef> map;
                try
                {
                    var outputs = CharSequenceOutputs.GetSingleton();
                    var builder = new Builder<CharsRef>(FST.INPUT_TYPE.BYTE2, outputs);
                    var scratch = new IntsRef();
                    foreach (var entry in _pendingPairs)
                    {
                        builder.Add(Lucene.Net.Util.Fst.Util.ToUTF16(entry.Key, scratch), new CharsRef(entry.Value));
                    }
                    map = builder.Finish();
                    _pendingPairs.Clear();
                }
                catch (IOException ioe)
                {
                    // Bogus FST IOExceptions!! (will never happen)
                    throw new Exception("FST threw exception", ioe);
                }
                return new NormalizeCharMap(map);
            }
        }
    }
}
