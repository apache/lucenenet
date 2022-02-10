using Lucene.Net.Index;
using Lucene.Net.Util;
using RandomizedTesting.Generators;
using System;

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

    internal class AssertingWeight : Weight
    {
        internal static Weight Wrap(Random random, Weight other)
        {
            return other is AssertingWeight ? other : new AssertingWeight(random, other);
        }

        internal readonly bool scoresDocsOutOfOrder;
        internal readonly Random random;
        internal readonly Weight @in;

        internal AssertingWeight(Random random, Weight @in)
        {
            this.random = random;
            this.@in = @in;
            scoresDocsOutOfOrder = @in.ScoresDocsOutOfOrder || random.NextBoolean();
        }

        public override Explanation Explain(AtomicReaderContext context, int doc)
        {
            return @in.Explain(context, doc);
        }

        public override Query Query => @in.Query;

        public override float GetValueForNormalization()
            => @in.GetValueForNormalization();

        public override void Normalize(float norm, float topLevelBoost)
            => @in.Normalize(norm, topLevelBoost);

        public override Scorer GetScorer(AtomicReaderContext context, IBits acceptDocs)
        {
            // if the caller asks for in-order scoring or if the weight does not support
            // out-of order scoring then collection will have to happen in-order.
            Scorer inScorer = @in.GetScorer(context, acceptDocs);
            return AssertingScorer.Wrap(new J2N.Randomizer(random.NextInt64()), inScorer);
        }

        public override BulkScorer GetBulkScorer(AtomicReaderContext context, bool scoreDocsInOrder, IBits acceptDocs)
        {
            // if the caller asks for in-order scoring or if the weight does not support
            // out-of order scoring then collection will have to happen in-order.
            BulkScorer inScorer = @in.GetBulkScorer(context, scoreDocsInOrder, acceptDocs);
            if (inScorer is null)
            {
                return null;
            }

            if (AssertingBulkScorer.ShouldWrap(inScorer))
            {
                // The incoming scorer already has a specialized
                // implementation for BulkScorer, so we should use it:
                inScorer = AssertingBulkScorer.Wrap(new J2N.Randomizer(random.NextInt64()), inScorer);
            }
            else if (random.NextBoolean())
            {
                // Let super wrap this.scorer instead, so we use
                // AssertingScorer:
                inScorer = base.GetBulkScorer(context, scoreDocsInOrder, acceptDocs);
            }

            if (scoreDocsInOrder == false && random.NextBoolean())
            {
                // The caller claims it can handle out-of-order
                // docs; let's confirm that by pulling docs and
                // randomly shuffling them before collection:
                inScorer = new AssertingBulkOutOfOrderScorer(new J2N.Randomizer(random.NextInt64()), inScorer);
            }
            return inScorer;
        }

        public override bool ScoresDocsOutOfOrder => scoresDocsOutOfOrder;
    }
}