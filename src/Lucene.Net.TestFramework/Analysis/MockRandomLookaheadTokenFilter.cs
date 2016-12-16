using System;
using System.Threading;

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

    using CharTermAttribute = Lucene.Net.Analysis.TokenAttributes.CharTermAttribute;

    /// <summary>
    /// Uses <seealso cref="LookaheadTokenFilter"/> to randomly peek at future tokens.
    /// </summary>

    public sealed class MockRandomLookaheadTokenFilter
        : LookaheadTokenFilter<LookaheadTokenFilter.Position>
    {
        private readonly CharTermAttribute TermAtt;// = addAttribute(typeof(CharTermAttribute));
        private Random random; //not readonly to reset seed later
        private readonly long Seed;

        public MockRandomLookaheadTokenFilter(Random random, TokenStream @in)
            : base(@in)
        {
            this.Seed = random.Next();
            this.random = new Random((int)Seed);
        }

        protected internal override LookaheadTokenFilter.Position NewPosition()
        {
            return new LookaheadTokenFilter.Position();
        }

        protected internal override void AfterPosition()
        {
            if (!End && random.Next(4) == 2)
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

            if (!End)
            {
                while (true)
                {
                    if (random.Next(3) == 1)
                    {
                        if (!PeekToken())
                        {
                            if (DEBUG)
                            {
                                Console.WriteLine("  peek; inputPos=" + InputPos + " END");
                            }
                            break;
                        }
                        if (DEBUG)
                        {
                            Console.WriteLine("  peek; inputPos=" + InputPos + " token=" + TermAtt);
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
                    Console.WriteLine("  return nextToken token=" + TermAtt);
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
            random = new Random((int)Seed);
        }
    }
}