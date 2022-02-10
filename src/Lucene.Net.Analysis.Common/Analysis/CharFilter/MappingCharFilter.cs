// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.Util;
using Lucene.Net.Diagnostics;
using Lucene.Net.Util;
using Lucene.Net.Util.Fst;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace Lucene.Net.Analysis.CharFilters
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
    /// Simplistic <see cref="CharFilter"/> that applies the mappings
    /// contained in a <see cref="NormalizeCharMap"/> to the character
    /// stream, and correcting the resulting changes to the
    /// offsets.  Matching is greedy (longest pattern matching at
    /// a given point wins).  Replacement is allowed to be the
    /// empty string.
    /// </summary>
    public class MappingCharFilter : BaseCharFilter
    {
        private readonly Outputs<CharsRef> outputs = CharSequenceOutputs.Singleton;
        private readonly FST<CharsRef> map;
        private readonly FST.BytesReader fstReader;
        private readonly RollingCharBuffer buffer = new RollingCharBuffer();
        private readonly FST.Arc<CharsRef> scratchArc = new FST.Arc<CharsRef>();
        private readonly IDictionary<char?, FST.Arc<CharsRef>> cachedRootArcs;

        private CharsRef replacement;
        private int replacementPointer;
        private int inputOff;

        /// <summary>
        /// LUCENENET specific support to buffer the reader.
        /// </summary>
        private readonly BufferedCharFilter _input;

        /// <summary>
        /// Default constructor that takes a <see cref="TextReader"/>. </summary>
        public MappingCharFilter(NormalizeCharMap normMap, TextReader @in) 
            : base(@in)
        {
            //LUCENENET support to reset the reader.
            _input = GetBufferedReader(@in);
            _input.Mark(BufferedCharFilter.DEFAULT_CHAR_BUFFER_SIZE);
            buffer.Reset(_input);
            //buffer.Reset(@in);

            map = normMap.map;
            cachedRootArcs = normMap.cachedRootArcs;

            if (map != null)
            {
                fstReader = map.GetBytesReader();
            }
            else
            {
                fstReader = null;
            }
        }

        /// <summary>
        /// LUCENENET: Copied this method from the <see cref="WordlistLoader"/> class - this class requires readers
        /// with a Reset() method (which .NET readers don't support). So, we use the <see cref="BufferedCharFilter"/> 
        /// (which is similar to Java BufferedReader) as a wrapper for whatever reader the user passes 
        /// (unless it is already a <see cref="BufferedCharFilter"/>).
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        private static BufferedCharFilter GetBufferedReader(TextReader reader)
        {
            return (reader is BufferedCharFilter) ? (BufferedCharFilter)reader : new BufferedCharFilter(reader);
        }

        public override void Reset()
        {
            // LUCENENET: reset the BufferedCharFilter. 
            _input.Reset();
            buffer.Reset(_input);
            replacement = null;
            inputOff = 0;
        }

        public override int Read()
        {

            //System.out.println("\nread");
            while (true)
            {

                if (replacement != null && replacementPointer < replacement.Length)
                {
                    //System.out.println("  return repl[" + replacementPointer + "]=" + replacement.chars[replacement.offset + replacementPointer]);
                    return replacement.Chars[replacement.Offset + replacementPointer++];
                }

                // TODO: a more efficient approach would be Aho/Corasick's
                // algorithm
                // (http://en.wikipedia.org/wiki/Aho%E2%80%93Corasick_string_matching_algorithm)
                // or this generalizatio: www.cis.uni-muenchen.de/people/Schulz/Pub/dictle5.ps
                //
                // I think this would be (almost?) equivalent to 1) adding
                // epsilon arcs from all final nodes back to the init
                // node in the FST, 2) adding a .* (skip any char)
                // loop on the initial node, and 3) determinizing
                // that.  Then we would not have to Restart matching
                // at each position.

                int lastMatchLen = -1;
                CharsRef lastMatch = null;

                int firstCH = buffer.Get(inputOff);
                if (firstCH != -1)
                {
                    // LUCENENET fix: Check the dictionary to ensure it contains a key before reading it.
                    char key = Convert.ToChar((char)firstCH);
                    if (cachedRootArcs.TryGetValue(key, out FST.Arc<CharsRef> arc) && arc != null)
                    {
                        if (!FST.TargetHasArcs(arc))
                        {
                            // Fast pass for single character match:
                            if (Debugging.AssertsEnabled) Debugging.Assert(arc.IsFinal);
                            lastMatchLen = 1;
                            lastMatch = arc.Output;
                        }
                        else
                        {
                            int lookahead = 0;
                            CharsRef output = arc.Output;
                            while (true)
                            {
                                lookahead++;

                                if (arc.IsFinal)
                                {
                                    // Match! (to node is final)
                                    lastMatchLen = lookahead;
                                    lastMatch = outputs.Add(output, arc.NextFinalOutput);
                                    // Greedy: keep searching to see if there's a
                                    // longer match...
                                }

                                if (!FST.TargetHasArcs(arc))
                                {
                                    break;
                                }

                                int ch = buffer.Get(inputOff + lookahead);
                                if (ch == -1)
                                {
                                    break;
                                }
                                if ((arc = map.FindTargetArc(ch, arc, scratchArc, fstReader)) is null)
                                {
                                    // Dead end
                                    break;
                                }
                                output = outputs.Add(output, arc.Output);
                            }
                        }
                    }
                }

                if (lastMatch != null)
                {
                    inputOff += lastMatchLen;
                    //System.out.println("  match!  len=" + lastMatchLen + " repl=" + lastMatch);
                    int diff = lastMatchLen - lastMatch.Length;

                    if (diff != 0)
                    {
                        int prevCumulativeDiff = LastCumulativeDiff;
                        if (diff > 0)
                        {
                            // Replacement is shorter than matched input:
                            AddOffCorrectMap(inputOff - diff - prevCumulativeDiff, prevCumulativeDiff + diff);
                        }
                        else
                        {
                            // Replacement is longer than matched input: remap
                            // the "extra" chars all back to the same input
                            // offset:
                            int outputStart = inputOff - prevCumulativeDiff;
                            for (int extraIDX = 0; extraIDX < -diff; extraIDX++)
                            {
                                AddOffCorrectMap(outputStart + extraIDX, prevCumulativeDiff - extraIDX - 1);
                            }
                        }
                    }

                    replacement = lastMatch;
                    replacementPointer = 0;

                }
                else
                {
                    int ret = buffer.Get(inputOff);
                    if (ret != -1)
                    {
                        inputOff++;
                        buffer.FreeBefore(inputOff);
                    }
                    return ret;
                }
            }
        }

        public override int Read(char[] cbuf, int off, int len)
        {
            int numRead = 0;
            for (int i = off; i < off + len; i++)
            {
                int c = Read();
                if (c == -1)
                {
                    break;
                }
                cbuf[i] = (char)c;
                numRead++;
            }

            return numRead == 0 ? -1 : numRead;
        }
    }
}