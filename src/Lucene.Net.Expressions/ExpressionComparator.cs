using Lucene.Net.Diagnostics;
using Lucene.Net.Index;
using Lucene.Net.Queries.Function;
using Lucene.Net.Search;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

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
    internal class ExpressionComparer : FieldComparer<double>
    {
        private readonly double[] values;
        private double bottom;
        private double topValue;

        private ValueSource source;
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

            if (Debugging.ShouldAssert(readerContext != null)) Debugging.ThrowAssert();
            try
            {
                var context = new Dictionary<string, object>();
                if (Debugging.ShouldAssert(scorer != null)) Debugging.ThrowAssert();
                context["scorer"] = scorer;
                scores = source.GetValues(context, readerContext);
            }
            catch (IOException e)
            {
                throw new Exception(e.ToString(), e);
            }
        }

        public override int Compare(int slot1, int slot2)
        {
            return values[slot1].CompareTo(values[slot2]);
        }

        public override void SetBottom(int slot)
        {
            bottom = values[slot];
        }

        public override void SetTopValue(object value)
        {
            topValue = (double)value;
        }

        public override int CompareBottom(int doc)
        {
            return bottom.CompareTo(scores.DoubleVal(doc));
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
        public override IComparable this[int slot] => values[slot];

        public override int CompareTop(int doc)
        {
            return topValue.CompareTo(scores.DoubleVal(doc));
        }
    }
}
