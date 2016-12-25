using System;
using System.Diagnostics;
using System.IO;
using Lucene.Net.Queries.Function;
using Lucene.Net.Queries.Function.DocValues;
using Lucene.Net.Search;

namespace Lucene.Net.Expressions
{
    /// <summary>
    /// A utility class to allow expressions to access the score as a
    /// <see cref="Lucene.Net.Queries.Function.FunctionValues">Lucene.Net.Queries.Function.FunctionValues
    /// 	</see>
    /// .
    /// </summary>
    internal class ScoreFunctionValues : DoubleDocValues
    {
        internal readonly Scorer scorer;

        internal ScoreFunctionValues(ValueSource parent, Scorer scorer)
            : base(parent)
        {
            this.scorer = scorer;
        }

        public override double DoubleVal(int document)
        {
            Debug.Assert(document == scorer.DocID);
            var score = scorer.Score();
            Console.WriteLine("Score = {0}",score);
            return score;
        }
    }
}
