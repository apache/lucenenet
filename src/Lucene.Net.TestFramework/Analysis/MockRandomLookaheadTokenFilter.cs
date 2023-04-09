using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Util;
using RandomizedTesting.Generators;
using System;
using System.Threading;
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

    /// <summary>
    /// Uses <see cref="LookaheadTokenFilter"/> to randomly peek at future tokens.
    /// </summary>
    public sealed class MockRandomLookaheadTokenFilter : LookaheadTokenFilter<LookaheadTokenFilter.Position>
    {
        private readonly ICharTermAttribute termAtt;
        private readonly J2N.Randomizer random;
        private readonly long seed;

        // LUCENENET specific - removed NewPosition override and using factory instead
        public MockRandomLookaheadTokenFilter(Random random, TokenStream @in)
            : base(@in, RollingBufferItemFactory<LookaheadTokenFilter.Position>.Default)
        {
            this.termAtt = AddAttribute<ICharTermAttribute>();
            this.seed = random.NextInt64();
            this.random = new J2N.Randomizer(seed);
        }

        protected override void AfterPosition()
        {
            if (!m_end && random.Next(4) == 2)
            {
                PeekToken();
            }
        }

        public override bool IncrementToken()
        {
            if (DEBUG)
            {
                Console.WriteLine("\n" + Thread.CurrentThread.Name + ": MRLTF.incrToken");
            }

            if (!m_end)
            {
                while (true)
                {
                    if (random.Next(3) == 1)
                    {
                        if (!PeekToken())
                        {
                            if (DEBUG)
                            {
                                Console.WriteLine("  peek; inputPos=" + m_inputPos + " END");
                            }
                            break;
                        }
                        if (DEBUG)
                        {
                            Console.WriteLine("  peek; inputPos=" + m_inputPos + " token=" + termAtt);
                        }
                    }
                    else
                    {
                        if (DEBUG)
                        {
                            Console.WriteLine("  done peek");
                        }
                        break;
                    }
                }
            }

            bool result = NextToken();
            if (result)
            {
                if (DEBUG)
                {
                    Console.WriteLine("  return nextToken token=" + termAtt);
                }
            }
            else
            {
                if (DEBUG)
                {
                    Console.WriteLine("  return nextToken END");
                }
            }
            return result;
        }

        public override void Reset()
        {
            base.Reset();
            random.Seed = seed;
        }
    }
}