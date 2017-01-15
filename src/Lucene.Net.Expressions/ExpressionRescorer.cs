using System;
using System.Collections.Generic;
using Lucene.Net.Index;
using Lucene.Net.Search;

namespace Lucene.Net.Expressions
{
    /// <summary>
    /// A
    /// <see cref="Lucene.Net.Search.Rescorer">Lucene.Net.Search.Rescorer</see>
    /// that uses an expression to re-score
    /// first pass hits.  Functionally this is the same as
    /// <see cref="Lucene.Net.Search.SortRescorer">Lucene.Net.Search.SortRescorer
    /// 	</see>
    /// (if you build the
    /// <see cref="Lucene.Net.Search.Sort">Lucene.Net.Search.Sort</see>
    /// using
    /// <see cref="Expression.GetSortField(Bindings, bool)">Expression.GetSortField(Bindings, bool)
    /// 	</see>
    /// ), except for the explain method
    /// which gives more detail by showing the value of each
    /// variable.
    /// </summary>
    /// <lucene.experimental></lucene.experimental>
    internal class ExpressionRescorer : SortRescorer
    {
        private readonly Expression expression;

        private readonly Bindings bindings;

        /// <summary>
        /// Uses the provided
        /// <see cref="Lucene.Net.Queries.Function.ValueSource">Lucene.Net.Queries.Function.ValueSource
        /// 	</see>
        /// to assign second
        /// pass scores.
        /// </summary>
        public ExpressionRescorer(Expression expression, Bindings bindings)
            : base(new Sort
                (expression.GetSortField(bindings, true)))
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
                throw new NotSupportedException("FakeScorer doesn't support advance(int)");
            }

            public override int DocID
            {
                get { return doc; }
            }

            public override int Freq
            {
                get { return freq; }
            }

            public override int NextDoc()
            {
                throw new NotSupportedException("FakeScorer doesn't support nextDoc()");
            }

            public override float GetScore()
            {
                return score;
            }

            public override long Cost()
            {
                return 1;
            }

            public override Weight Weight
            {
                get { throw new NotSupportedException(); }
            }

            public override ICollection<Scorer.ChildScorer> GetChildren()
            {
                throw new NotSupportedException();
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
            foreach (string variable in expression.variables)
            {
                result.AddDetail(new Explanation((float)bindings.GetValueSource(variable).GetValues
                    (context, readerContext).DoubleVal(docIDInSegment), "variable \"" + variable + "\""
                    ));
            }
            return result;
        }
    }
}
