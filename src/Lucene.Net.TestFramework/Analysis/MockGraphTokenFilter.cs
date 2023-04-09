using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Util;
using RandomizedTesting.Generators;
using System;
using Console = Lucene.Net.Util.SystemConsole;

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

    // TODO: sometimes remove tokens too...?

    /// <summary>
    /// Randomly inserts overlapped (posInc=0) tokens with
    /// posLength sometimes > 1.  The chain must have
    /// an <see cref="IOffsetAttribute"/>.
    /// </summary>

    public sealed class MockGraphTokenFilter : LookaheadTokenFilter<LookaheadTokenFilter.Position>
    {
#pragma warning disable CA1802 // Use literals where appropriate
        new private static readonly bool DEBUG = false;
#pragma warning restore CA1802 // Use literals where appropriate

        private readonly ICharTermAttribute termAtt;

        private readonly long seed;
        private Random random;

        // LUCENENET specific - removed NewPosition override and using factory instead
        public MockGraphTokenFilter(Random random, TokenStream input)
            : base(input, RollingBufferItemFactory<LookaheadTokenFilter.Position>.Default)
        {
            seed = random.NextInt64();
            termAtt = AddAttribute<ICharTermAttribute>();
        }

        protected override void AfterPosition()
        {
            if (DEBUG)
            {
                Console.WriteLine("MockGraphTF.afterPos");
            }
            if (random.Next(7) == 5)
            {
                int posLength = TestUtil.NextInt32(random, 1, 5);

                if (DEBUG)
                {
                    Console.WriteLine("  do insert! posLen=" + posLength);
                }

                Position posEndData = m_positions.Get(m_outputPos + posLength);

                // Look ahead as needed until we figure out the right
                // endOffset:
                while (!m_end && posEndData.EndOffset == -1 && m_inputPos <= (m_outputPos + posLength))
                {
                    if (!PeekToken())
                    {
                        break;
                    }
                }

                if (posEndData.EndOffset != -1)
                {
                    // Notify super class that we are injecting a token:
                    InsertToken();
                    ClearAttributes();
                    m_posLenAtt.PositionLength = posLength;
                    termAtt.Append(TestUtil.RandomUnicodeString(random));
                    m_posIncAtt.PositionIncrement = 0;
                    m_offsetAtt.SetOffset(m_positions.Get(m_outputPos).StartOffset, posEndData.EndOffset);
                    if (DEBUG)
                    {
                        Console.WriteLine("  inject: outputPos=" + m_outputPos + " startOffset=" + m_offsetAtt.StartOffset + " endOffset=" + m_offsetAtt.EndOffset + " posLength=" + m_posLenAtt.PositionLength);
                    }
                    // TODO: set TypeAtt too?
                }
                else
                {
                    // Either 1) the tokens ended before our posLength,
                    // or 2) our posLength ended inside a hole from the
                    // input.  In each case we just skip the inserted
                    // token.
                }
            }
        }

        public override void Reset()
        {
            base.Reset();
            // NOTE: must be "deterministically random" because
            // baseTokenStreamTestCase pulls tokens twice on the
            // same input and asserts they are the same:
            this.random = new J2N.Randomizer(seed);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                base.Dispose(disposing);
                this.random = null;
            }
        }

        public override bool IncrementToken()
        {
            if (DEBUG)
            {
                Console.WriteLine("MockGraphTF.incr inputPos=" + m_inputPos + " outputPos=" + m_outputPos);
            }
            if (random is null)
            {
                throw IllegalStateException.Create("IncrementToken() called in wrong state!");
            }
            return NextToken();
        }
    }
}