using Lucene.Net.Diagnostics;
using Lucene.Net.Index;
using Lucene.Net.Runtime.CompilerServices;
using Lucene.Net.Support;
using Lucene.Net.Support.Threading;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using JCG = J2N.Collections.Generic;

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
    /// Wraps a <see cref="Scorer"/> with additional checks.</summary>
    public class AssertingScorer : Scorer
    {
        // we need to track scorers using a weak hash map because otherwise we
        // could loose references because of eg.
        // AssertingScorer.Score(Collector) which needs to delegate to work correctly
#if !FEATURE_CONDITIONALWEAKTABLE_ADDORUPDATE
        private static readonly object assertingInstancesLock = new object();
#endif
        private static readonly ConditionalWeakTable<Scorer, WeakReference<AssertingScorer>> assertingInstances =
            new ConditionalWeakTable<Scorer, WeakReference<AssertingScorer>>();

        public static Scorer Wrap(Random random, Scorer other)
        {
            if (other is null || other is AssertingScorer)
            {
                return other;
            }
            AssertingScorer assertScorer = new AssertingScorer(random, other);
#if !FEATURE_CONDITIONALWEAKTABLE_ADDORUPDATE
            UninterruptableMonitor.Enter(assertingInstancesLock);
            try
            {
#endif
                assertingInstances.AddOrUpdate(other, new WeakReference<AssertingScorer>(assertScorer));
#if !FEATURE_CONDITIONALWEAKTABLE_ADDORUPDATE
            }
            finally
            {
                UninterruptableMonitor.Exit(assertingInstancesLock);
            }
#endif

            return assertScorer;
        }

        internal static Scorer GetAssertingScorer(Random random, Scorer other)
        {
            if (other is null || other is AssertingScorer)
            {
                return other;
            }
#if !FEATURE_CONDITIONALWEAKTABLE_ADDORUPDATE
            UninterruptableMonitor.Enter(assertingInstancesLock);
            try
            {
#endif
                if (!assertingInstances.TryGetValue(other, out WeakReference<AssertingScorer> assertingScorerRef) || assertingScorerRef is null ||
                    !assertingScorerRef.TryGetTarget(out AssertingScorer assertingScorer) || assertingScorer is null)
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
#if !FEATURE_CONDITIONALWEAKTABLE_ADDORUPDATE
            }
            finally
            {
                UninterruptableMonitor.Exit(assertingInstancesLock);
            }
#endif
        }

        internal readonly Random random;
        internal readonly Scorer @in;
        internal readonly AssertingDocsEnum docsEnumIn;

        private AssertingScorer(Random random, Scorer @in)
            : base(@in.Weight)
        {
            this.random = random;
            this.@in = @in;
            this.docsEnumIn = new AssertingDocsEnum(@in);
        }

        public virtual Scorer In => @in;

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

        public override float GetScore()
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(Iterating());
            float score = @in.GetScore();
            if (Debugging.AssertsEnabled) Debugging.Assert(!float.IsNaN(score));
            if (Debugging.AssertsEnabled) Debugging.Assert(!float.IsNaN(score));
            return score;
        }

        public override ICollection<ChildScorer> GetChildren()
        {
            // We cannot hide that we hold a single child, else
            // collectors (e.g. ToParentBlockJoinCollector) that
            // need to walk the scorer tree will miss/skip the
            // Scorer we wrap:
            return new JCG.List<ChildScorer>() { new ChildScorer(@in, "SHOULD") };
        }

        public override int Freq
        {
            get
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(Iterating());
                return @in.Freq;
            }
        }

        public override int DocID => @in.DocID;

        public override int NextDoc()
        {
            return docsEnumIn.NextDoc();
        }

        public override int Advance(int target)
        {
            return docsEnumIn.Advance(target);
        }

        public override long GetCost()
        {
            return @in.GetCost();
        }

        public override string ToString()
        {
            return "AssertingScorer(" + @in + ")";
        }
    }
}