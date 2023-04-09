using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Diagnostics;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using Console = Lucene.Net.Util.SystemConsole;
using JCG = J2N.Collections.Generic;

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

    using AttributeSource = Lucene.Net.Util.AttributeSource;

    //using RollingBuffer = Lucene.Net.Util.RollingBuffer;

    // TODO: cut SynFilter over to this
    // TODO: somehow add "nuke this input token" capability...

    /// <summary>
    /// LUCENENET specific abstraction so we can reference <see cref="LookaheadTokenFilter.Position"/> without
    /// specifying a generic closing type.
    /// </summary>
    public abstract class LookaheadTokenFilter : TokenFilter
    {
        internal LookaheadTokenFilter(TokenStream input) // Not for end users to use directly
            : base(input)
        { }


        public abstract override bool IncrementToken();

        /// <summary>
        /// Holds all state for a single position; subclass this
        /// to record other state at each position.
        /// </summary>
        // LUCENENET NOTE: This class was originally marked protected, but was made public because of
        // inconsistent accessibility issues with using it as a generic constraint.
        public class Position : IResettable
        {
            // Buffered input tokens at this position:
            public IList<AttributeSource.State> InputTokens { get; private set; } = new JCG.List<AttributeSource.State>();

            // Next buffered token to be returned to consumer:
            public int NextRead { get; set; }

            // Any token leaving from this position should have this startOffset:
            public int StartOffset { get; set; } = -1;

            // Any token arriving to this position should have this endOffset:
            public int EndOffset { get; set; } = -1;

            public void Reset()
            {
                InputTokens.Clear();
                NextRead = 0;
                StartOffset = -1;
                EndOffset = -1;
            }

            public virtual void Add(AttributeSource.State state)
            {
                InputTokens.Add(state);
            }

            public virtual AttributeSource.State NextState()
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(NextRead < InputTokens.Count);
                return InputTokens[NextRead++];
            }
        }
    }

    /// <summary>
    /// An abstract <see cref="TokenFilter"/> to make it easier to build graph
    /// token filters requiring some lookahead.  This class handles
    /// the details of buffering up tokens, recording them by
    /// position, restoring them, providing access to them, etc.
    /// </summary>
    public abstract class LookaheadTokenFilter<T> : LookaheadTokenFilter
        where T : LookaheadTokenFilter.Position
    {
        protected readonly static bool DEBUG = 
#if VERBOSE_TEST_LOGGING
            true
#else
            false
#endif
            ;

        protected readonly IPositionIncrementAttribute m_posIncAtt;
        protected readonly IPositionLengthAttribute m_posLenAtt;
        protected readonly IOffsetAttribute m_offsetAtt;

        // Position of last read input token:
        protected int m_inputPos;

        // Position of next possible output token to return:
        protected int m_outputPos;

        // True if we hit end from our input:
        protected bool m_end;

        private bool tokenPending;
        private bool insertPending;

        // LUCENENET specific - moved Position class to a non-generic class named LookaheadTokenFilter so we can refer to
        // it without referring to the generic closing type.
        // removed virtual NewPosition() method and added factory in the constructor

        protected internal LookaheadTokenFilter(TokenStream input, IRollingBufferItemFactory<T> itemFactory)
            : base(input)
        {
            m_positions = new RollingBufferAnonymousClass(itemFactory);
            m_posIncAtt = AddAttribute<IPositionIncrementAttribute>();
            m_posLenAtt = AddAttribute<IPositionLengthAttribute>();
            m_offsetAtt = AddAttribute<IOffsetAttribute>();
        }

        /// <summary>
        /// Call this only from within <see cref="AfterPosition()"/>, to insert a new
        /// token. After calling this you should set any
        /// necessary token you need.
        /// </summary>
        protected virtual void InsertToken()
        {
            if (tokenPending)
            {
                m_positions.Get(m_inputPos).Add(CaptureState());
                tokenPending = false;
            }
            if (Debugging.AssertsEnabled) Debugging.Assert(!insertPending);
            insertPending = true;
        }

        /// <summary>
        /// This is called when all input tokens leaving a given
        /// position have been returned.  Override this and
        /// call insertToken and then set whichever token's
        /// attributes you want, if you want to inject
        /// a token starting from this position.
        /// </summary>
        protected virtual void AfterPosition()
        {
        }

        protected readonly RollingBuffer<T> m_positions;

        private sealed class RollingBufferAnonymousClass : RollingBuffer<T>
        {
            // LUCENENET specific - adjusted to accept factory as a parameter
            // instead of using NewInstance virtual
            public RollingBufferAnonymousClass(IRollingBufferItemFactory<T> itemFactory)
                : base(itemFactory)
            {
            }
        }

        /// <summary>
        /// Returns true if there is a new token. </summary>
        protected virtual bool PeekToken()
        {
            if (DEBUG)
            {
                Console.WriteLine("LTF.peekToken inputPos=" + m_inputPos + " outputPos=" + m_outputPos + " tokenPending=" + tokenPending);
            }
            if (Debugging.AssertsEnabled) Debugging.Assert(!m_end);
            if (Debugging.AssertsEnabled) Debugging.Assert(m_inputPos == -1 || m_outputPos <= m_inputPos);
            if (tokenPending)
            {
                m_positions.Get(m_inputPos).Add(CaptureState());
                tokenPending = false;
            }
            bool gotToken = m_input.IncrementToken();
            if (DEBUG)
            {
                Console.WriteLine("  input.incrToken() returned " + gotToken);
            }
            if (gotToken)
            {
                m_inputPos += m_posIncAtt.PositionIncrement;
                if (Debugging.AssertsEnabled) Debugging.Assert(m_inputPos >= 0);
                if (DEBUG)
                {
                    Console.WriteLine("  now inputPos=" + m_inputPos);
                }

                Position startPosData = m_positions.Get(m_inputPos);
                Position endPosData = m_positions.Get(m_inputPos + m_posLenAtt.PositionLength);

                int startOffset = m_offsetAtt.StartOffset;
                if (startPosData.StartOffset == -1)
                {
                    startPosData.StartOffset = startOffset;
                }
                else
                {
                    // Make sure our input isn't messing up offsets:
                    if (Debugging.AssertsEnabled) Debugging.Assert(startPosData.StartOffset == startOffset, "prev startOffset={0} vs new startOffset={1} inputPos={2}", startPosData.StartOffset, startOffset, m_inputPos);
                }

                int endOffset = m_offsetAtt.EndOffset;
                if (endPosData.EndOffset == -1)
                {
                    endPosData.EndOffset = endOffset;
                }
                else
                {
                    // Make sure our input isn't messing up offsets:
                    if (Debugging.AssertsEnabled) Debugging.Assert(endPosData.EndOffset == endOffset, "prev endOffset={0} vs new endOffset={1} inputPos={2}", endPosData.EndOffset, endOffset, m_inputPos);
                }

                tokenPending = true;
            }
            else
            {
                m_end = true;
            }

            return gotToken;
        }

        /// <summary>
        /// Call this when you are done looking ahead; it will set
        /// the next token to return.  Return the boolean back to
        /// the caller.
        /// </summary>
        protected virtual bool NextToken()
        {
            //System.out.println("  nextToken: tokenPending=" + tokenPending);
            if (DEBUG)
            {
                Console.WriteLine("LTF.nextToken inputPos=" + m_inputPos + " outputPos=" + m_outputPos + " tokenPending=" + tokenPending);
            }

            Position posData = m_positions.Get(m_outputPos);

            // While loop here in case we have to
            // skip over a hole from the input:
            while (true)
            {
                //System.out.println("    check buffer @ outputPos=" +
                //outputPos + " inputPos=" + inputPos + " nextRead=" +
                //posData.nextRead + " vs size=" +
                //posData.inputTokens.size());

                // See if we have a previously buffered token to
                // return at the current position:
                if (posData.NextRead < posData.InputTokens.Count)
                {
                    if (DEBUG)
                    {
                        Console.WriteLine("  return previously buffered token");
                    }
                    // this position has buffered tokens to serve up:
                    if (tokenPending)
                    {
                        m_positions.Get(m_inputPos).Add(CaptureState());
                        tokenPending = false;
                    }
                    RestoreState(m_positions.Get(m_outputPos).NextState());
                    //System.out.println("      return!");
                    return true;
                }

                if (m_inputPos == -1 || m_outputPos == m_inputPos)
                {
                    // No more buffered tokens:
                    // We may still get input tokens at this position
                    //System.out.println("    break buffer");
                    if (tokenPending)
                    {
                        // Fast path: just return token we had just incr'd,
                        // without having captured/restored its state:
                        if (DEBUG)
                        {
                            Console.WriteLine("  pass-through: return pending token");
                        }
                        tokenPending = false;
                        return true;
                    }
                    else if (m_end || !PeekToken())
                    {
                        if (DEBUG)
                        {
                            Console.WriteLine("  END");
                        }
                        AfterPosition();
                        if (insertPending)
                        {
                            // Subclass inserted a token at this same
                            // position:
                            if (DEBUG)
                            {
                                Console.WriteLine("  return inserted token");
                            }
                            if (Debugging.AssertsEnabled) Debugging.Assert(InsertedTokenConsistent());
                            insertPending = false;
                            return true;
                        }

                        return false;
                    }
                }
                else
                {
                    if (posData.StartOffset != -1)
                    {
                        // this position had at least one token leaving
                        if (DEBUG)
                        {
                            Console.WriteLine("  call afterPosition");
                        }
                        AfterPosition();
                        if (insertPending)
                        {
                            // Subclass inserted a token at this same
                            // position:
                            if (DEBUG)
                            {
                                Console.WriteLine("  return inserted token");
                            }
                            if (Debugging.AssertsEnabled) Debugging.Assert(InsertedTokenConsistent());
                            insertPending = false;
                            return true;
                        }
                    }

                    // Done with this position; move on:
                    m_outputPos++;
                    if (DEBUG)
                    {
                        Console.WriteLine("  next position: outputPos=" + m_outputPos);
                    }
                    m_positions.FreeBefore(m_outputPos);
                    posData = m_positions.Get(m_outputPos);
                }
            }
        }

        // If subclass inserted a token, make sure it had in fact
        // looked ahead enough:
        private bool InsertedTokenConsistent()
        {
            int posLen = m_posLenAtt.PositionLength;
            Position endPosData = m_positions.Get(m_outputPos + posLen);
            if (Debugging.AssertsEnabled) Debugging.Assert(endPosData.EndOffset != -1);
            if (Debugging.AssertsEnabled) Debugging.Assert(m_offsetAtt.EndOffset == endPosData.EndOffset,"offsetAtt.endOffset={0} vs expected={1}", m_offsetAtt.EndOffset, endPosData.EndOffset);
            return true;
        }

        // TODO: end()?
        // TODO: close()?

        public override void Reset()
        {
            base.Reset();
            m_positions.Reset();
            m_inputPos = -1;
            m_outputPos = 0;
            tokenPending = false;
            m_end = false;
        }
    }
}