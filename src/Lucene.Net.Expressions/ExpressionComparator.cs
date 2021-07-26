using Lucene.Net.Diagnostics;
using Lucene.Net.Index;
using Lucene.Net.Queries.Function;
using Lucene.Net.Search;
using System;
using System.Collections.Generic;
using JCG = J2N.Collections.Generic;

namespace Lucene.Net.Expressions
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

    /// <summary>A custom comparer for sorting documents by an expression</summary>
    internal class ExpressionComparer : FieldComparer<J2N.Numerics.Double>
    {
        private readonly double[] values;
        private double bottom;
        private double topValue;

        private readonly ValueSource source; // LUCENENET: marked readonly
        private FunctionValues scores;
        private AtomicReaderContext readerContext;

        public ExpressionComparer(ValueSource source, int numHits)
        {
            values = new double[numHits];
            this.source = source;
        }

        // TODO: change FieldComparer.setScorer to throw IOException and remove this try-catch
        public override void SetScorer(Scorer scorer)
        {
            base.SetScorer(scorer);
            // TODO: might be cleaner to lazy-init 'source' and set scorer after?

            if (Debugging.AssertsEnabled) Debugging.Assert(readerContext != null);
            try
            {
                var context = new Dictionary<string, object>();
                if (Debugging.AssertsEnabled) Debugging.Assert(scorer != null);
                context["scorer"] = scorer;
                scores = source.GetValues(context, readerContext);
            }
            catch (Exception e) when (e.IsIOException())
            {
                throw RuntimeException.Create(e);
            }
        }

        public override int Compare(int slot1, int slot2)
        {
            // LUCENENET specific - use JCG comparer to get the same logic as Java
            return JCG.Comparer<double>.Default.Compare(values[slot1], values[slot2]);
        }

        public override void SetBottom(int slot)
        {
            bottom = values[slot];
        }

        public override void SetTopValue(J2N.Numerics.Double value)
        {
            topValue = value ?? throw new ArgumentNullException(nameof(value)); // LUCENENET specific - throw ArgumentNullException rather than getting a cast exception
        }

        public override int CompareBottom(int doc)
        {
            // LUCENENET specific - use JCG comparer to get the same logic as Java
            return JCG.Comparer<double>.Default.Compare(bottom, scores.DoubleVal(doc));
        }

        public override void Copy(int slot, int doc)
        {
            values[slot] = scores.DoubleVal(doc);
        }

        public override FieldComparer SetNextReader(AtomicReaderContext context)
        {
            this.readerContext = context;
            return this;
        }

        // LUCENENET NOTE: This was value(int) in Lucene.
        public override J2N.Numerics.Double this[int slot] => values[slot]; // LUCENENET NOTE: Implicit cast will instantiate new instance

        public override int CompareTop(int doc)
        {
            // LUCENENET specific - use JCG comparer to get the same logic as Java
            return JCG.Comparer<double>.Default.Compare(topValue, scores.DoubleVal(doc));
        }
    }
}
