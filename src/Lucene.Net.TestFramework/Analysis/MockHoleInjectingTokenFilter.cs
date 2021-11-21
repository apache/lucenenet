using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Util;
using RandomizedTesting.Generators;
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

    // TODO: maybe, instead to be more "natural", we should make
    // a MockRemovesTokensTF, ideally subclassing FilteringTF
    // (in modules/analysis)

    /// <summary>
    /// Randomly injects holes (similar to what a stopfilter would do)
    /// </summary>
    public sealed class MockHoleInjectingTokenFilter : TokenFilter
    {
        private readonly long randomSeed;
        private Random random;
        private readonly IPositionIncrementAttribute posIncAtt;
        private readonly IPositionLengthAttribute posLenAtt;
        private int maxPos;
        private int pos;

        public MockHoleInjectingTokenFilter(Random random, TokenStream @in)
            : base(@in)
        {
            randomSeed = random.NextInt64();
            posIncAtt = AddAttribute<IPositionIncrementAttribute>();
            posLenAtt = AddAttribute<IPositionLengthAttribute>();
        }

        public override void Reset()
        {
            base.Reset();
            random = new J2N.Randomizer(randomSeed);
            maxPos = -1;
            pos = -1;
        }

        public override bool IncrementToken()
        {
            if (m_input.IncrementToken())
            {
                int posInc = posIncAtt.PositionIncrement;

                int nextPos = pos + posInc;

                // Carefully inject a hole only where it won't mess up
                // the graph:
                if (posInc > 0 && maxPos <= nextPos && random.Next(5) == 3)
                {
                    int holeSize = TestUtil.NextInt32(random, 1, 5);
                    posIncAtt.PositionIncrement = posInc + holeSize;
                    nextPos += holeSize;
                }

                pos = nextPos;
                maxPos = Math.Max(maxPos, pos + posLenAtt.PositionLength);

                return true;
            }

            return false;
        }

        // TODO: end?
    }
}