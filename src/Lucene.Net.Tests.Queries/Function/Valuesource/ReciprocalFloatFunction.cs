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
	/// <code>ReciprocalFloatFunction</code> implements a reciprocal function f(x) = a/(mx+b), based on
	/// the float value of a field or function as exported by
	/// <see cref="Org.Apache.Lucene.Queries.Function.ValueSource">Org.Apache.Lucene.Queries.Function.ValueSource
	/// 	</see>
	/// .
	/// <br />
	/// When a and b are equal, and x&gt;=0, this function has a maximum value of 1 that drops as x increases.
	/// Increasing the value of a and b together results in a movement of the entire function to a flatter part of the curve.
	/// <p>These properties make this an idea function for boosting more recent documents.
	/// <p>Example:<code>  recip(ms(NOW,mydatefield),3.16e-11,1,1)</code>
	/// <p>A multiplier of 3.16e-11 changes the units from milliseconds to years (since there are about 3.16e10 milliseconds
	/// per year).  Thus, a very recent date will yield a value close to 1/(0+1) or 1,
	/// a date a year in the past will get a multiplier of about 1/(1+1) or 1/2,
	/// and date two years old will yield 1/(2+1) or 1/3.
	/// </summary>
	/// <seealso cref="Org.Apache.Lucene.Queries.Function.FunctionQuery">Org.Apache.Lucene.Queries.Function.FunctionQuery
	/// 	</seealso>
	public class ReciprocalFloatFunction : ValueSource
	{
		protected internal readonly ValueSource source;

		protected internal readonly float m;

		protected internal readonly float a;

		protected internal readonly float b;

		/// <summary>f(source) = a/(m*float(source)+b)</summary>
		public ReciprocalFloatFunction(ValueSource source, float m, float a, float b)
		{
			this.source = source;
			this.m = m;
			this.a = a;
			this.b = b;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override FunctionValues GetValues(IDictionary context, AtomicReaderContext
			 readerContext)
		{
			FunctionValues vals = source.GetValues(context, readerContext);
			return new _FloatDocValues_66(this, vals, this);
		}

		private sealed class _FloatDocValues_66 : FloatDocValues
		{
			public _FloatDocValues_66(ReciprocalFloatFunction _enclosing, FunctionValues vals
				, ValueSource baseArg1) : base(baseArg1)
			{
				this._enclosing = _enclosing;
				this.vals = vals;
			}

			public override float FloatVal(int doc)
			{
				return this._enclosing.a / (this._enclosing.m * vals.FloatVal(doc) + this._enclosing
					.b);
			}

			public override string ToString(int doc)
			{
				return float.ToString(this._enclosing.a) + "/(" + this._enclosing.m + "*float(" +
					 vals.ToString(doc) + ')' + '+' + this._enclosing.b + ')';
			}

			private readonly ReciprocalFloatFunction _enclosing;

			private readonly FunctionValues vals;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void CreateWeight(IDictionary context, IndexSearcher searcher)
		{
			source.CreateWeight(context, searcher);
		}

		public override string Description()
		{
			return float.ToString(a) + "/(" + m + "*float(" + source.Description() + ")" + "+"
				 + b + ')';
		}

		public override int GetHashCode()
		{
			int h = Sharpen.Runtime.FloatToIntBits(a) + Sharpen.Runtime.FloatToIntBits(m);
			h ^= (h << 13) | ((int)(((uint)h) >> 20));
			return h + (Sharpen.Runtime.FloatToIntBits(b)) + source.GetHashCode();
		}

		public override bool Equals(object o)
		{
			if (typeof(Org.Apache.Lucene.Queries.Function.Valuesource.ReciprocalFloatFunction
				) != o.GetType())
			{
				return false;
			}
			Org.Apache.Lucene.Queries.Function.Valuesource.ReciprocalFloatFunction other = (Org.Apache.Lucene.Queries.Function.Valuesource.ReciprocalFloatFunction
				)o;
			return this.m == other.m && this.a == other.a && this.b == other.b && this.source
				.Equals(other.source);
		}
	}
}
