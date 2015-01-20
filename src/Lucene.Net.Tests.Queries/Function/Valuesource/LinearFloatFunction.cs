/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Collections;
using Org.Apache.Lucene.Index;
using Org.Apache.Lucene.Queries.Function;
using Org.Apache.Lucene.Queries.Function.Docvalues;
using Org.Apache.Lucene.Search;
using Sharpen;

namespace Org.Apache.Lucene.Queries.Function.Valuesource
{
	/// <summary>
	/// <code>LinearFloatFunction</code> implements a linear function over
	/// another
	/// <see cref="Org.Apache.Lucene.Queries.Function.ValueSource">Org.Apache.Lucene.Queries.Function.ValueSource
	/// 	</see>
	/// .
	/// <br />
	/// Normally Used as an argument to a
	/// <see cref="Org.Apache.Lucene.Queries.Function.FunctionQuery">Org.Apache.Lucene.Queries.Function.FunctionQuery
	/// 	</see>
	/// </summary>
	public class LinearFloatFunction : ValueSource
	{
		protected internal readonly ValueSource source;

		protected internal readonly float slope;

		protected internal readonly float intercept;

		public LinearFloatFunction(ValueSource source, float slope, float intercept)
		{
			this.source = source;
			this.slope = slope;
			this.intercept = intercept;
		}

		public override string Description()
		{
			return slope + "*float(" + source.Description() + ")+" + intercept;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override FunctionValues GetValues(IDictionary context, AtomicReaderContext
			 readerContext)
		{
			FunctionValues vals = source.GetValues(context, readerContext);
			return new _FloatDocValues_56(this, vals, this);
		}

		private sealed class _FloatDocValues_56 : FloatDocValues
		{
			public _FloatDocValues_56(LinearFloatFunction _enclosing, FunctionValues vals, ValueSource
				 baseArg1) : base(baseArg1)
			{
				this._enclosing = _enclosing;
				this.vals = vals;
			}

			public override float FloatVal(int doc)
			{
				return vals.FloatVal(doc) * this._enclosing.slope + this._enclosing.intercept;
			}

			public override string ToString(int doc)
			{
				return this._enclosing.slope + "*float(" + vals.ToString(doc) + ")+" + this._enclosing
					.intercept;
			}

			private readonly LinearFloatFunction _enclosing;

			private readonly FunctionValues vals;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void CreateWeight(IDictionary context, IndexSearcher searcher)
		{
			source.CreateWeight(context, searcher);
		}

		public override int GetHashCode()
		{
			int h = Sharpen.Runtime.FloatToIntBits(slope);
			h = ((int)(((uint)h) >> 2)) | (h << 30);
			h += Sharpen.Runtime.FloatToIntBits(intercept);
			h ^= (h << 14) | ((int)(((uint)h) >> 19));
			return h + source.GetHashCode();
		}

		public override bool Equals(object o)
		{
			if (typeof(Org.Apache.Lucene.Queries.Function.Valuesource.LinearFloatFunction) !=
				 o.GetType())
			{
				return false;
			}
			Org.Apache.Lucene.Queries.Function.Valuesource.LinearFloatFunction other = (Org.Apache.Lucene.Queries.Function.Valuesource.LinearFloatFunction
				)o;
			return this.slope == other.slope && this.intercept == other.intercept && this.source
				.Equals(other.source);
		}
	}
}
