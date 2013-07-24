using Lucene.Net.Analysis.Tokenattributes;
using Lucene.Net.Util;
using Lucene.Net.Util.Automaton;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Analysis
{
    public class TokenStreamToAutomaton
    {
        public TokenStreamToAutomaton()
        {
        }

        private class Position : RollingBuffer.Resettable
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
            protected override Position NewInstance()
            {
                return new Position();
            }
        }

        protected virtual BytesRef ChangeToken(BytesRef input)
        {
            return input;
        }

        /** We create transition between two adjacent tokens. */
        public const int POS_SEP = 256;

        /** We add this arc to represent a hole. */
        public const int HOLE = 257;

        public virtual Automaton ToAutomaton(TokenStream input)
        {
            Automaton a = new Automaton();
            bool deterministic = true;
                        
            ITermToBytesRefAttribute termBytesAtt = input.AddAttribute<ITermToBytesRefAttribute>();
            IPositionIncrementAttribute posIncAtt = input.AddAttribute<IPositionIncrementAttribute>();
            IPositionLengthAttribute posLengthAtt = input.AddAttribute<IPositionLengthAttribute>();
            IOffsetAttribute offsetAtt = input.AddAttribute<IOffsetAttribute>();

            BytesRef term = termBytesAtt.BytesRef;

            input.Reset();

            // Only temporarily holds states ahead of our current
            // position:

            RollingBuffer<Position> positions = new Positions();

            int pos = -1;
            Position posData = null;
            int maxOffset = 0;
            while (input.IncrementToken())
            {
                int posInc = posIncAtt.PositionIncrement;
                //assert pos > -1 || posInc > 0;

                if (posInc > 0)
                {

                    // New node:
                    pos += posInc;

                    posData = positions.Get(pos);
                    //assert posData.leaving == null;

                    if (posData.arriving == null)
                    {
                        // No token ever arrived to this position
                        if (pos == 0)
                        {
                            // OK: this is the first token
                            posData.leaving = a.InitialState;
                        }
                        else
                        {
                            // This means there's a hole (eg, StopFilter
                            // does this):
                            posData.leaving = new State();
                            AddHoles(a.InitialState, positions, pos);
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
                            AddHoles(a.InitialState, positions, pos);
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
                BytesRef term2 = ChangeToken(term);
                Position endPosData = positions.Get(endPos);
                if (endPosData.arriving == null)
                {
                    endPosData.arriving = new State();
                }

                State state = posData.leaving;
                for (int byteIDX = 0; byteIDX < term2.length; byteIDX++)
                {
                    State nextState = byteIDX == term2.length - 1 ? endPosData.arriving : new State();
                    state.AddTransition(new Transition(term2.bytes[term2.offset + byteIDX] & 0xff, nextState));
                    state = nextState;
                }

                maxOffset = Math.Max(maxOffset, offsetAtt.EndOffset);
            }

            input.End();
            State endState = null;
            if (offsetAtt.EndOffset > maxOffset)
            {
                endState = new State();
                endState.Accept = true;
            }

            pos++;
            while (pos <= positions.GetMaxPos())
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
            a.Deterministic = deterministic;
            return a;
        }

        private static void AddHoles(State startState, RollingBuffer<Position> positions, int pos)
        {
            Position posData = positions.Get(pos);
            Position prevPosData = positions.Get(pos - 1);

            while (posData.arriving == null || prevPosData.leaving == null)
            {
                if (posData.arriving == null)
                {
                    posData.arriving = new State();
                    posData.arriving.AddTransition(new Transition(POS_SEP, posData.leaving));
                }
                if (prevPosData.leaving == null)
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
