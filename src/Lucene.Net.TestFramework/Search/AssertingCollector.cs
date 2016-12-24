using System;
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

    using AtomicReaderContext = Lucene.Net.Index.AtomicReaderContext;

    /// <summary>
    /// Wraps another Collector and checks that
    ///  acceptsDocsOutOfOrder is respected.
    /// </summary>

    public class AssertingCollector : Collector
    {
        public static Collector Wrap(Random random, Collector other, bool inOrder)
        {
            return other is AssertingCollector ? other : new AssertingCollector(random, other, inOrder);
        }

        internal readonly Random Random;
        internal readonly Collector @in;
        internal readonly bool InOrder;
        internal int LastCollected;

        internal AssertingCollector(Random random, Collector @in, bool inOrder)
        {
            this.Random = random;
            this.@in = @in;
            this.InOrder = inOrder;
            LastCollected = -1;
        }

        public override void SetScorer(Scorer scorer)
        {
            @in.SetScorer(AssertingScorer.GetAssertingScorer(Random, scorer));
        }

        public override void Collect(int doc)
        {
            if (InOrder || !AcceptsDocsOutOfOrder())
            {
                Debug.Assert(doc > LastCollected, "Out of order : " + LastCollected + " " + doc);
            }
            @in.Collect(doc);
            LastCollected = doc;
        }

        public override AtomicReaderContext NextReader
        {
            set
            {
                LastCollected = -1;
            }
        }

        public override bool AcceptsDocsOutOfOrder()
        {
            return @in.AcceptsDocsOutOfOrder();
        }
    }
}