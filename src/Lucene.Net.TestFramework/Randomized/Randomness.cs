/*
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using Lucene.Net.Support.Threading;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Lucene.Net.Randomized
{
    /// <summary>
    ///  Per-thread, per-lifecycle state randomness defined as an initial seed and
    ///  the current Random instance for that context.
    /// </summary>
    /// <remarks>
    ///    <para>
    ///     An instance of this class will be typically available from <see cref="RandomizedContext"/>.
    ///     No need to instantiate manually.
    ///     </para>
    /// </remarks>
    /// <see cref="Lucene.Net.Randomized.RandomizedContext"/>
    /// <see cref="Lucene.Net.Randomized.SingleThreadedRandom"/>
    public class Randomness : IDisposable
    {
        private List<ISeedDecorator> decorators;

        public Random Random
        {
            get { return this.SingleThreadedRandom; }
        }

        protected SingleThreadedRandom SingleThreadedRandom { get; set; }

        public int Seed { get; protected set; }

        public Randomness(ThreadClass owner, int seed, params ISeedDecorator[] decorators)
            : this(owner, seed, decorators.ToList())
        {
        }

        public Randomness(int seed, params ISeedDecorator[] decorators)
            : this(ThreadClass.Current(), seed, decorators)
        {
        }

        protected Randomness(ThreadClass owner, int seed, List<ISeedDecorator> decorators)
        {
            this.Seed = seed;
            this.decorators = decorators.ToList();

            var decoratedSeed = Decorate(seed, this.decorators);

            this.SingleThreadedRandom = new SingleThreadedRandom(owner,
                                 new Random(decoratedSeed)
                            );
        }

        public Randomness Clone(ThreadClass newOwner)
        {
            return new Randomness(newOwner, this.Seed, this.decorators);
        }

        public override string ToString()
        {
            return "[Randomess, seed=" + this.Seed.ToString() + "]";
        }

        private static int Decorate(int seed, List<ISeedDecorator> decorators)
        {
            var result = seed;
            decorators.ForEach(o => result = o.Decorate(result));

            return result;
        }

        public void Dispose()
        {
            if (this.SingleThreadedRandom != null)
                this.SingleThreadedRandom.Dispose();
        }
    }
}