using Lucene.Net.Diagnostics;
using Lucene.Net.Index;
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

    /// <summary>
    /// Wraps another <see cref="ICollector"/> and checks that
    /// <see cref="AcceptsDocsOutOfOrder"/> is respected.
    /// </summary>
    public class AssertingCollector : ICollector
    {
        public static ICollector Wrap(Random random, ICollector other, bool inOrder)
        {
            return other is AssertingCollector ? other : new AssertingCollector(random, other, inOrder);
        }

        internal readonly Random random;
        internal readonly ICollector @in;
        internal readonly bool inOrder;
        internal int lastCollected;

        internal AssertingCollector(Random random, ICollector @in, bool inOrder)
        {
            this.random = random;
            this.@in = @in;
            this.inOrder = inOrder;
            lastCollected = -1;
        }

        public virtual void SetScorer(Scorer scorer)
            => @in.SetScorer(AssertingScorer.GetAssertingScorer(random, scorer));

        public virtual void Collect(int doc)
        {
            if (inOrder || !AcceptsDocsOutOfOrder)
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(doc > lastCollected,"Out of order : {0} {1}", lastCollected, doc);
            }
            @in.Collect(doc);
            lastCollected = doc;
        }

        public virtual void SetNextReader(AtomicReaderContext context)
            => lastCollected = -1;

        public virtual bool AcceptsDocsOutOfOrder
            => @in.AcceptsDocsOutOfOrder;
    }
}