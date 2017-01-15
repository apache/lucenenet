using System;
using System.Collections;
using Lucene.Net.Index;
using Lucene.Net.Queries.Function;
using Lucene.Net.Search;

namespace Lucene.Net.Expressions
{
	/// <summary>
	/// A
	/// <see cref="Lucene.Net.Queries.Function.ValueSource">Lucene.Net.Queries.Function.ValueSource
	/// 	</see>
	/// which uses the
	/// <see cref="Lucene.Net.Search.Scorer">Lucene.Net.Search.Scorer</see>
	/// passed through
	/// the context map by
	/// <see cref="ExpressionComparer">ExpressionComparer</see>
	/// .
	/// </summary>
	internal class ScoreValueSource : ValueSource
	{
	    private Scorer hashCodeObj;

	    /// <summary>
		/// <code>context</code> must contain a key "scorer" which is a
		/// <see cref="Lucene.Net.Search.Scorer">Lucene.Net.Search.Scorer</see>
		/// .
		/// </summary>
		/// <exception cref="System.IO.IOException"></exception>
		public override FunctionValues GetValues(IDictionary context, AtomicReaderContext
			 readerContext)
		{
			Scorer v = (Scorer)context["scorer"];
		    hashCodeObj = v;
			if (v == null)
			{
				throw new InvalidOperationException("Expressions referencing the score can only be used for sorting"
					);
			}
			return new ScoreFunctionValues(this, v);
		}

		public override bool Equals(object o)
		{
			return o == this;
		}

		public override int GetHashCode()
		{
            //TODO: revist this and return something meaningful
		    return 777;
		}

		public override string Description
		{
		    get { return "score()"; }
		}
	}
}
