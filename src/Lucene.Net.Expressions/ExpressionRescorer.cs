using Lucene.Net.Index;
using Lucene.Net.Search;
using System;
using System.Collections.Generic;

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

    /// <summary>
    /// A <see cref="Lucene.Net.Search.Rescorer"/> that uses an expression to re-score
    /// first pass hits.  Functionally this is the same as
    /// <see cref="Lucene.Net.Search.SortRescorer"/> (if you build the
    /// <see cref="Lucene.Net.Search.Sort"/> using
    /// <see cref="Expression.GetSortField(Bindings, bool)"/>), except for the <see cref="Explain"/> method
    /// which gives more detail by showing the value of each
    /// variable.
    /// @lucene.experimental
    /// </summary>
    internal class ExpressionRescorer : SortRescorer
    {
        private readonly Expression expression;
        private readonly Bindings bindings;

        /// <summary>
        /// Uses the provided <see cref="Lucene.Net.Queries.Function.ValueSource"/> to assign second
        /// pass scores.
        /// </summary>
        public ExpressionRescorer(Expression expression, Bindings bindings)
            : base(new Sort(expression.GetSortField(bindings, true)))
        {
            this.expression = expression;
            this.bindings = bindings;
        }

        private class FakeScorer : Scorer
        {
            internal float score;
            internal int doc = -1;
            internal int freq = 1;

            public FakeScorer()
                : base(null)
            {
            }

            public override int Advance(int target)
            {
                throw UnsupportedOperationException.Create("FakeScorer doesn't support Advance(int)");
            }

            public override int DocID => doc;

            public override int Freq => freq;

            public override int NextDoc()
            {
                throw UnsupportedOperationException.Create("FakeScorer doesn't support NextDoc()");
            }

            public override float GetScore()
            {
                return score;
            }

            public override long GetCost()
            {
                return 1;
            }

            public override Weight Weight => throw UnsupportedOperationException.Create();

            public override ICollection<Scorer.ChildScorer> GetChildren()
            {
                throw UnsupportedOperationException.Create();
            }
        }


        public override Explanation Explain(IndexSearcher searcher, Explanation firstPassExplanation, int docID)
        {
            Explanation result = base.Explain(searcher, firstPassExplanation, docID);
            IList<AtomicReaderContext> leaves = searcher.IndexReader.Leaves;
            int subReader = ReaderUtil.SubIndex(docID, leaves);
            AtomicReaderContext readerContext = leaves[subReader];
            int docIDInSegment = docID - readerContext.DocBase;
            var context = new Dictionary<string, object>();
            var fakeScorer = new FakeScorer { score = firstPassExplanation.Value, doc = docIDInSegment };
            context["scorer"] = fakeScorer;
            foreach (string variable in expression.Variables)
            {
                result.AddDetail(new Explanation((float)bindings.GetValueSource(variable).GetValues
                    (context, readerContext).DoubleVal(docIDInSegment), "variable \"" + variable + "\""
                    ));
            }
            return result;
        }
    }
}
