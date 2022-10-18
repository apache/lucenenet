using Lucene.Net.Index;
using Lucene.Net.Util;
using RandomizedTesting.Generators;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lucene.Net.Search
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
    /// Helper class that adds some extra checks to ensure correct
    /// usage of <see cref="IndexSearcher"/> and <see cref="Weight"/>.
    /// </summary>
    public class AssertingIndexSearcher : IndexSearcher
    {
        internal readonly Random random;

        public AssertingIndexSearcher(Random random, IndexReader r)
            : base(r)
        {
            this.random = new J2N.Randomizer(random.NextInt64());
        }

        public AssertingIndexSearcher(Random random, IndexReaderContext context)
            : base(context)
        {
            this.random = new J2N.Randomizer(random.NextInt64());
        }

        public AssertingIndexSearcher(Random random, IndexReader r, TaskScheduler ex)
            : base(r, ex)
        {
            this.random = new J2N.Randomizer(random.NextInt64());
        }

        public AssertingIndexSearcher(Random random, IndexReaderContext context, TaskScheduler ex)
            : base(context, ex)
        {
            this.random = new J2N.Randomizer(random.NextInt64());
        }

        /// <summary>
        /// Ensures, that the returned <see cref="Weight"/> is not normalized again, which may produce wrong scores. </summary>
        public override Weight CreateNormalizedWeight(Query query)
        {
            Weight w = base.CreateNormalizedWeight(query);
            return new AssertingWeightAnonymousClass(random, w);
        }

        private sealed class AssertingWeightAnonymousClass : AssertingWeight
        {
            public AssertingWeightAnonymousClass(Random random, Weight w)
                : base(random, w)
            {
            }

            public override void Normalize(float norm, float topLevelBoost)
            {
                throw IllegalStateException.Create("Weight already normalized.");
            }

            public override float GetValueForNormalization()
            {
                throw IllegalStateException.Create("Weight already normalized.");
            }
        }

        public override Query Rewrite(Query original)
        {
            // TODO: use the more sophisticated QueryUtils.check sometimes!
            QueryUtils.Check(original);
            Query rewritten = base.Rewrite(original);
            QueryUtils.Check(rewritten);
            return rewritten;
        }

        protected override Query WrapFilter(Query query, Filter filter)
        {
            if (random.NextBoolean())
            {
                return base.WrapFilter(query, filter);
            }
            return (filter is null) ? query : new FilteredQuery(query, filter, TestUtil.RandomFilterStrategy(random));
        }

        protected override void Search(IList<AtomicReaderContext> leaves, Weight weight, ICollector collector)
        {
            // TODO: shouldn't we AssertingCollector.wrap(collector) here?
            base.Search(leaves, AssertingWeight.Wrap(random, weight), collector);
        }

        public override string ToString()
        {
            return "AssertingIndexSearcher(" + base.ToString() + ")";
        }
    }
}