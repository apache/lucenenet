using Lucene.Net.Analysis.TokenAttributes;
using NUnit.Framework;
using System;
using Console = Lucene.Net.Support.SystemConsole;

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

    using TestUtil = Lucene.Net.Util.TestUtil;

    // TODO: sometimes remove tokens too...?

    /// <summary>
    /// Randomly inserts overlapped (posInc=0) tokens with
    ///  posLength sometimes > 1.  The chain must have
    ///  an OffsetAttribute.
    /// </summary>

    public sealed class MockGraphTokenFilter : LookaheadTokenFilter<LookaheadTokenFilter.Position>
    {
        new private static bool DEBUG = false;

        private readonly ICharTermAttribute termAtt;

        private readonly long seed;
        private Random random;

        public MockGraphTokenFilter(Random random, TokenStream input)
            : base(input)
        {
            seed = random.Next();
            termAtt = AddAttribute<ICharTermAttribute>();
        }

        protected internal override LookaheadTokenFilter.Position NewPosition()
        {
            return new LookaheadTokenFilter.Position();
        }

        protected internal override void AfterPosition()
        {
            if (DEBUG)
            {
                Console.WriteLine("MockGraphTF.afterPos");
            }
            if (random.Next(7) == 5)
            {
                int posLength = TestUtil.NextInt(random, 1, 5);

                if (DEBUG)
                {
                    Console.WriteLine("  do insert! posLen=" + posLength);
                }

                LookaheadTokenFilter.Position posEndData = positions.Get(OutputPos + posLength);

                // Look ahead as needed until we figure out the right
                // endOffset:
                while (!end && posEndData.EndOffset == -1 && InputPos <= (OutputPos + posLength))
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
                    PosLenAtt.PositionLength = posLength;
                    termAtt.Append(TestUtil.RandomUnicodeString(random));
                    PosIncAtt.PositionIncrement = 0;
                    OffsetAtt.SetOffset(positions.Get(OutputPos).StartOffset, posEndData.EndOffset);
                    if (DEBUG)
                    {
                        Console.WriteLine("  inject: outputPos=" + OutputPos + " startOffset=" + OffsetAtt.StartOffset + " endOffset=" + OffsetAtt.EndOffset + " posLength=" + PosLenAtt.PositionLength);
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
            this.random = new Random((int)seed);
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
                Console.WriteLine("MockGraphTF.incr inputPos=" + InputPos + " outputPos=" + OutputPos);
            }
            if (random == null)
            {
                throw new AssertionException("incrementToken called in wrong state!");
            }
            return NextToken();
        }
    }
}