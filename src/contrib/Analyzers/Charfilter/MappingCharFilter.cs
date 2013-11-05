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

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Lucene.Net.Analysis.Support;
using Lucene.Net.Analysis.Util;
using Lucene.Net.Util;
using Lucene.Net.Util.Fst;

namespace Lucene.Net.Analysis.Charfilter
{
    public class MappingCharFilter : BaseCharFilter
    {
        private readonly Outputs<CharsRef> _outputs = CharSequenceOutputs.GetSingleton();
        private readonly FST<CharsRef> _map;
        private readonly FST.BytesReader _fstReader;
        private readonly RollingCharBuffer _buffer = new RollingCharBuffer();
        private readonly FST.Arc<CharsRef> _scratchArc = new FST.Arc<CharsRef>();
        private readonly IDictionary<char, FST.Arc<CharsRef>> _cachedRootArcs;

        private CharsRef _replacement;
        private int _replacementPointer;
        private int _inputOff;

        public MappingCharFilter(NormalizeCharMap normMap, StreamReader input)
            : base(input)
        {
            _buffer.Reset(input);

            _map = normMap.Map;
            _cachedRootArcs = normMap.CachedRootArcs;

            if (_map != null)
            {
                _fstReader = _map.GetBytesReader();
            }
            else
            {
                _fstReader = null;
            }
        }

        public void Reset()
        {
            input.Reset();
            _buffer.Reset(input);
            _replacement = null;
            _inputOff = 0;
        }

        public override int Read()
        {
            while (true)
            {
                if (_replacement != null && _replacementPointer < _replacement.Length)
                {
                    return _replacement.chars[_replacement.offset + _replacementPointer++];
                }

                var lastMatchLen = -1;
                CharsRef lastMatch = null;

                var firstCH = _buffer.Get(_inputOff);
                if (firstCH != -1)
                {
                    var arc = _cachedRootArcs[(char) firstCH];
                    if (arc != null)
                    {
                        if (!FST<CharsRef>.TargetHasArcs(arc))
                        {
                            Debug.Assert(arc.IsFinal());
                            lastMatchLen = 1;
                            lastMatch = arc.Output;
                        }
                        else
                        {
                            var lookahead = 0;
                            var output = arc.Output;
                            while (true)
                            {
                                lookahead++;

                                if (arc.IsFinal())
                                {
                                    lastMatchLen = lookahead;
                                    lastMatch = _outputs.Add(output, arc.NextFinalOutput);
                                }

                                if (!FST<CharsRef>.TargetHasArcs(arc))
                                {
                                    break;
                                }

                                var ch = _buffer.Get(_inputOff + lookahead);
                                if (ch == -1)
                                {
                                    break;
                                }
                                output = _outputs.Add(output, arc.Output);
                            }
                        }
                    }
                }

                if (lastMatch != null)
                {
                    _inputOff += lastMatchLen;

                    var diff = lastMatchLen - lastMatch.Length;

                    if (diff != 0)
                    {
                        var prevCumulativeDiff = LastCumulativeDiff;
                        if (diff > 0)
                        {
                            AddOffCorrectMap(_inputOff - diff - prevCumulativeDiff, prevCumulativeDiff + diff);
                        }
                        else
                        {
                            var outputStart = _inputOff - prevCumulativeDiff;
                            for (var extraIDX = 0; extraIDX < -diff; extraIDX++)
                            {
                                AddOffCorrectMap(outputStart + extraIDX, prevCumulativeDiff - extraIDX - 1);
                            }
                        }
                    }

                    _replacement = lastMatch;
                    _replacementPointer = 0;
                }
                else
                {
                    var ret = _buffer.Get(_inputOff);
                    if (ret != -1)
                    {
                        _inputOff++;
                        _buffer.FreeBefore(_inputOff);
                    }
                    return ret;
                }
            }
        }

        public override int Read(char[] buffer, int offset, int count)
        {
            var numRead = 0;
            for (var i = offset; i < offset + count; i++)
            {
                var c = Read();
                if (c == -1) break;
                buffer[i] = (char) c;
                numRead++;
            }

            return numRead == 0 ? -1 : numRead;
        }
    }
}
