using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Diagnostics;

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

    using AssertingAtomicReader = Lucene.Net.Index.AssertingAtomicReader;

    /// <summary>
    /// Wraps a Scorer with additional checks </summary>
    public class AssertingScorer : Scorer
    {
        // we need to track scorers using a weak hash map because otherwise we
        // could loose references because of eg.
        // AssertingScorer.Score(Collector) which needs to delegate to work correctly
        private static IDictionary<Scorer, WeakReference> ASSERTING_INSTANCES = new ConcurrentHashMapWrapper<Scorer, WeakReference>(new HashMap<Scorer, WeakReference>());

        public static Scorer Wrap(Random random, Scorer other)
        {
            if (other == null || other is AssertingScorer)
            {
                return other;
            }
            AssertingScorer assertScorer = new AssertingScorer(random, other);
            ASSERTING_INSTANCES[other] = new WeakReference(assertScorer);
            return assertScorer;
        }

        internal static Scorer GetAssertingScorer(Random random, Scorer other)
        {
            if (other == null || other is AssertingScorer)
            {
                return other;
            }
            WeakReference assertingScorerRef = ASSERTING_INSTANCES[other];
            AssertingScorer assertingScorer = assertingScorerRef == null ? null : (AssertingScorer)assertingScorerRef.Target;
            if (assertingScorer == null)
            {
                // can happen in case of memory pressure or if
                // scorer1.Score(collector) calls
                // collector.setScorer(scorer2) with scorer1 != scorer2, such as
                // BooleanScorer. In that case we can't enable all assertions
                return new AssertingScorer(random, other);
            }
            else
            {
                return assertingScorer;
            }
        }

        internal readonly Random Random;
        internal readonly Scorer @in;
        internal readonly AssertingAtomicReader.AssertingDocsEnum DocsEnumIn;

        private AssertingScorer(Random random, Scorer @in)
            : base(@in.Weight)
        {
            this.Random = random;
            this.@in = @in;
            this.DocsEnumIn = new AssertingAtomicReader.AssertingDocsEnum(@in);
        }

        public virtual Scorer In
        {
            get
            {
                return @in;
            }
        }

        internal virtual bool Iterating()
        {
            switch (DocID)
            {
                case -1:
                case NO_MORE_DOCS:
                    return false;

                default:
                    return true;
            }
        }

        public override float Score()
        {
            Debug.Assert(Iterating());
            float score = @in.Score();
            Debug.Assert(!float.IsNaN(score));
            Debug.Assert(!float.IsNaN(score));
            return score;
        }

        public override ICollection<ChildScorer> GetChildren()
        {
            // We cannot hide that we hold a single child, else
            // collectors (e.g. ToParentBlockJoinCollector) that
            // need to walk the scorer tree will miss/skip the
            // Scorer we wrap:
            return new List<ChildScorer>() { new ChildScorer(@in, "SHOULD") };
        }

        public override int Freq
        {
            get
            {
                Debug.Assert(Iterating());
                return @in.Freq;
            }
        }

        public override int DocID
        {
            get { return @in.DocID; }
        }

        public override int NextDoc()
        {
            return DocsEnumIn.NextDoc();
        }

        public override int Advance(int target)
        {
            return DocsEnumIn.Advance(target);
        }

        public override long Cost()
        {
            return @in.Cost();
        }

        public override string ToString()
        {
            return "AssertingScorer(" + @in + ")";
        }
    }
}