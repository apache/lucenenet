using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lucene.Net.Search
{
    using Lucene.Net.Randomized.Generators;

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

    using AtomicReaderContext = Lucene.Net.Index.AtomicReaderContext;
    using IndexReader = Lucene.Net.Index.IndexReader;
    using IndexReaderContext = Lucene.Net.Index.IndexReaderContext;
    using TestUtil = Lucene.Net.Util.TestUtil;

    /// <summary>
    /// Helper class that adds some extra checks to ensure correct
    /// usage of {@code IndexSearcher} and {@code Weight}.
    /// </summary>
    public class AssertingIndexSearcher : IndexSearcher
    {
        internal readonly Random Random;

        public AssertingIndexSearcher(Random random, IndexReader r)
            : base(r)
        {
            this.Random = new Random(random.Next());
        }

        public AssertingIndexSearcher(Random random, IndexReaderContext context)
            : base(context)
        {
            this.Random = new Random(random.Next());
        }

        public AssertingIndexSearcher(Random random, IndexReader r, TaskScheduler ex)
            : base(r, ex)
        {
            this.Random = new Random(random.Next());
        }

        public AssertingIndexSearcher(Random random, IndexReaderContext context, TaskScheduler ex)
            : base(context, ex)
        {
            this.Random = new Random(random.Next());
        }

        /// <summary>
        /// Ensures, that the returned {@code Weight} is not normalized again, which may produce wrong scores. </summary>
        public override Weight CreateNormalizedWeight(Query query)
        {
            Weight w = base.CreateNormalizedWeight(query);
            return new AssertingWeightAnonymousInnerClassHelper(this, Random, w);
        }

        private class AssertingWeightAnonymousInnerClassHelper : AssertingWeight
        {
            private readonly AssertingIndexSearcher OuterInstance;

            public AssertingWeightAnonymousInnerClassHelper(AssertingIndexSearcher outerInstance, Random random, Weight w)
                : base(random, w)
            {
                this.OuterInstance = outerInstance;
            }

            public override void Normalize(float norm, float topLevelBoost)
            {
                throw new InvalidOperationException("Weight already normalized.");
            }

            public override float ValueForNormalization
            {
                get
                {
                    throw new InvalidOperationException("Weight already normalized.");
                }
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

        protected internal override Query WrapFilter(Query query, Filter filter)
        {
            if (Random.NextBoolean())
            {
                return base.WrapFilter(query, filter);
            }
            return (filter == null) ? query : new FilteredQuery(query, filter, TestUtil.RandomFilterStrategy(Random));
        }

        protected internal override void Search(IList<AtomicReaderContext> leaves, Weight weight, Collector collector)
        {
            // TODO: shouldn't we AssertingCollector.wrap(collector) here?
            base.Search(leaves, AssertingWeight.Wrap(Random, weight), collector);
        }

        public override string ToString()
        {
            return "AssertingIndexSearcher(" + base.ToString() + ")";
        }
    }
}