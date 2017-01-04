using Lucene.Net.Analysis.TokenAttributes;
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

    using TestUtil = Lucene.Net.Util.TestUtil;

    // TODO: maybe, instead to be more "natural", we should make
    // a MockRemovesTokensTF, ideally subclassing FilteringTF
    // (in modules/analysis)

    /// <summary>
    /// Randomly injects holes (similar to what a stopfilter would do)
    /// </summary>
    public sealed class MockHoleInjectingTokenFilter : TokenFilter
    {
        private readonly int RandomSeed;
        private Random Random;
        private readonly IPositionIncrementAttribute PosIncAtt;
        private readonly IPositionLengthAttribute PosLenAtt;
        private int MaxPos;
        private int Pos;

        public MockHoleInjectingTokenFilter(Random random, TokenStream @in)
            : base(@in)
        {
            RandomSeed = random.Next();
            PosIncAtt = AddAttribute<IPositionIncrementAttribute>();
            PosLenAtt = AddAttribute<IPositionLengthAttribute>();
        }

        public override void Reset()
        {
            base.Reset();
            Random = new Random(RandomSeed);
            MaxPos = -1;
            Pos = -1;
        }

        public override bool IncrementToken()
        {
            if (m_input.IncrementToken())
            {
                int posInc = PosIncAtt.PositionIncrement;

                int nextPos = Pos + posInc;

                // Carefully inject a hole only where it won't mess up
                // the graph:
                if (posInc > 0 && MaxPos <= nextPos && Random.Next(5) == 3)
                {
                    int holeSize = TestUtil.NextInt(Random, 1, 5);
                    PosIncAtt.PositionIncrement = posInc + holeSize;
                    nextPos += holeSize;
                }

                Pos = nextPos;
                MaxPos = Math.Max(MaxPos, Pos + PosLenAtt.PositionLength);

                return true;
            }

            return false;
        }

        // TODO: end?
    }
}