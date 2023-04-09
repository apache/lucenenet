using J2N;
using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Diagnostics;
using Lucene.Net.Util;
using System;

namespace Lucene.Net.Analysis
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

    using Automaton = Lucene.Net.Util.Automaton.Automaton;
    using State = Lucene.Net.Util.Automaton.State;
    using Transition = Lucene.Net.Util.Automaton.Transition;

    // TODO: maybe also toFST?  then we can translate atts into FST outputs/weights

    /// <summary>
    /// Consumes a <see cref="TokenStream"/> and creates an <see cref="Automaton"/>
    /// where the transition labels are UTF8 bytes (or Unicode
    /// code points if unicodeArcs is true) from the <see cref="ITermToBytesRefAttribute"/>.
    /// Between tokens we insert
    /// <see cref="POS_SEP"/> and for holes we insert <see cref="HOLE"/>.
    ///
    /// @lucene.experimental
    /// </summary>
    public class TokenStreamToAutomaton
    {
        //backing variables
        private bool preservePositionIncrements;

        private bool unicodeArcs;

        /// <summary>
        /// Sole constructor. </summary>
        public TokenStreamToAutomaton()
        {
            this.preservePositionIncrements = true;
        }

        /// <summary>
        /// Whether to generate holes in the automaton for missing positions, <c>true</c> by default. </summary>
        public virtual bool PreservePositionIncrements
        {
            get => this.preservePositionIncrements; // LUCENENET specific - properties should always have a getter
            set => this.preservePositionIncrements = value;
        }

        /// <summary>
        /// Whether to make transition labels Unicode code points instead of UTF8 bytes,
        /// <c>false</c> by default
        /// </summary>
        public virtual bool UnicodeArcs
        {
            get => this.unicodeArcs; // LUCENENET specific - properties should always have a getter
            set => this.unicodeArcs = value;
        }

        private class Position : IResettable
        {
            // Any tokens that ended at our position arrive to this state:
            internal State arriving;

            // Any tokens that start at our position leave from this state:
            internal State leaving;

            public void Reset()
            {
                arriving = null;
                leaving = null;
            }
        }

        private class Positions : RollingBuffer<Position>
        {
            // LUCENENET specific - removed NewInstance override and using PositionsFactory to create instances
            public Positions()
                : base(RollingBufferItemFactory<Position>.Default) { }
        }

        /// <summary>
        /// Subclass &amp; implement this if you need to change the
        /// token (such as escaping certain bytes) before it's
        /// turned into a graph.
        /// </summary>
        protected internal virtual BytesRef ChangeToken(BytesRef @in)
        {
            return @in;
        }

        /// <summary>
        /// We create transition between two adjacent tokens. </summary>
        public const int POS_SEP = 0x001f;

        /// <summary>
        /// We add this arc to represent a hole. </summary>
        public const int HOLE = 0x001e;

        /// <summary>
        /// Pulls the graph (including <see cref="IPositionLengthAttribute"/>
        /// from the provided <see cref="TokenStream"/>, and creates the corresponding
        /// automaton where arcs are bytes (or Unicode code points
        /// if unicodeArcs = true) from each term.
        /// </summary>
        public virtual Automaton ToAutomaton(TokenStream @in)
        {
            var a = new Automaton();
            bool deterministic = true;

            var posIncAtt = @in.AddAttribute<IPositionIncrementAttribute>();
            var posLengthAtt = @in.AddAttribute<IPositionLengthAttribute>();
            var offsetAtt = @in.AddAttribute<IOffsetAttribute>();
            var termBytesAtt = @in.AddAttribute<ITermToBytesRefAttribute>();

            BytesRef term = termBytesAtt.BytesRef;

            @in.Reset();

            // Only temporarily holds states ahead of our current
            // position:

            RollingBuffer<Position> positions = new Positions();

            int pos = -1;
            Position posData = null;
            int maxOffset = 0;
            while (@in.IncrementToken())
            {
                int posInc = posIncAtt.PositionIncrement;
                if (!preservePositionIncrements && posInc > 1)
                {
                    posInc = 1;
                }
                if (Debugging.AssertsEnabled) Debugging.Assert(pos > -1 || posInc > 0);

                if (posInc > 0)
                {
                    // New node:
                    pos += posInc;

                    posData = positions.Get(pos);
                    if (Debugging.AssertsEnabled) Debugging.Assert(posData.leaving is null);

                    if (posData.arriving is null)
                    {
                        // No token ever arrived to this position
                        if (pos == 0)
                        {
                            // OK: this is the first token
                            posData.leaving = a.GetInitialState();
                        }
                        else
                        {
                            // this means there's a hole (eg, StopFilter
                            // does this):
                            posData.leaving = new State();
                            AddHoles(a.GetInitialState(), positions, pos);
                        }
                    }
                    else
                    {
                        posData.leaving = new State();
                        posData.arriving.AddTransition(new Transition(POS_SEP, posData.leaving));
                        if (posInc > 1)
                        {
                            // A token spanned over a hole; add holes
                            // "under" it:
                            AddHoles(a.GetInitialState(), positions, pos);
                        }
                    }
                    positions.FreeBefore(pos);
                }
                else
                {
                    // note: this isn't necessarily true. its just that we aren't surely det.
                    // we could optimize this further (e.g. buffer and sort synonyms at a position)
                    // but thats probably overkill. this is cheap and dirty
                    deterministic = false;
                }

                int endPos = pos + posLengthAtt.PositionLength;

                termBytesAtt.FillBytesRef();
                BytesRef termUTF8 = ChangeToken(term);
                int[] termUnicode = null;
                Position endPosData = positions.Get(endPos);
                if (endPosData.arriving is null)
                {
                    endPosData.arriving = new State();
                }

                State state = posData.leaving;
                int termLen = termUTF8.Length;
                if (unicodeArcs)
                {
                    string utf16 = termUTF8.Utf8ToString();
                    termUnicode = new int[utf16.CodePointCount(0, utf16.Length)];
                    termLen = termUnicode.Length;
                    for (int cp, i = 0, j = 0; i < utf16.Length; i += Character.CharCount(cp))
                    {
                        termUnicode[j++] = cp = Character.CodePointAt(utf16, i);
                    }
                }
                //else
                //{
                //    termLen = termUTF8.Length; // LUCENENET: IDE0059: Remove unnecessary value assignment
                //}

                for (int byteIDX = 0; byteIDX < termLen; byteIDX++)
                {
                    State nextState = byteIDX == termLen - 1 ? endPosData.arriving : new State();
                    int c;
                    if (unicodeArcs)
                    {
                        c = termUnicode[byteIDX];
                    }
                    else
                    {
                        c = termUTF8.Bytes[termUTF8.Offset + byteIDX] & 0xff;
                    }
                    state.AddTransition(new Transition(c, nextState));
                    state = nextState;
                }

                maxOffset = Math.Max(maxOffset, offsetAtt.EndOffset);
            }

            @in.End();
            State endState = null;
            if (offsetAtt.EndOffset > maxOffset)
            {
                endState = new State();
                endState.Accept = true;
            }

            pos++;
            while (pos <= positions.MaxPos)
            {
                posData = positions.Get(pos);
                if (posData.arriving != null)
                {
                    if (endState != null)
                    {
                        posData.arriving.AddTransition(new Transition(POS_SEP, endState));
                    }
                    else
                    {
                        posData.arriving.Accept = true;
                    }
                }
                pos++;
            }

            //toDot(a);
            a.IsDeterministic = deterministic;
            return a;
        }

        // for debugging!
        /*
        private static void toDot(Automaton a) throws IOException {
          final String s = a.toDot();
          Writer w = new OutputStreamWriter(new FileOutputStream("/tmp/out.dot"));
          w.write(s);
          w.Dispose();
          System.out.println("TEST: saved to /tmp/out.dot");
        }
        */

        private static void AddHoles(State startState, RollingBuffer<Position> positions, int pos)
        {
            Position posData = positions.Get(pos);
            Position prevPosData = positions.Get(pos - 1);

            while (posData.arriving is null || prevPosData.leaving is null)
            {
                if (posData.arriving is null)
                {
                    posData.arriving = new State();
                    posData.arriving.AddTransition(new Transition(POS_SEP, posData.leaving));
                }
                if (prevPosData.leaving is null)
                {
                    if (pos == 1)
                    {
                        prevPosData.leaving = startState;
                    }
                    else
                    {
                        prevPosData.leaving = new State();
                    }
                    if (prevPosData.arriving != null)
                    {
                        prevPosData.arriving.AddTransition(new Transition(POS_SEP, prevPosData.leaving));
                    }
                }
                prevPosData.leaving.AddTransition(new Transition(HOLE, posData.arriving));
                pos--;
                if (pos <= 0)
                {
                    break;
                }
                posData = prevPosData;
                prevPosData = positions.Get(pos - 1);
            }
        }
    }
}