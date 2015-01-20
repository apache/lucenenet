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
	/// Abstract
	/// <see cref="Org.Apache.Lucene.Queries.Function.ValueSource">Org.Apache.Lucene.Queries.Function.ValueSource
	/// 	</see>
	/// implementation which wraps two ValueSources
	/// and applies an extendible float function to their values.
	/// </summary>
	public abstract class DualFloatFunction : ValueSource
	{
		protected internal readonly ValueSource a;

		protected internal readonly ValueSource b;

		/// <param name="a">the base.</param>
		/// <param name="b">the exponent.</param>
		public DualFloatFunction(ValueSource a, ValueSource b)
		{
			this.a = a;
			this.b = b;
		}

		protected internal abstract string Name();

		protected internal abstract float Func(int doc, FunctionValues aVals, FunctionValues
			 bVals);

		public override string Description()
		{
			return Name() + "(" + a.Description() + "," + b.Description() + ")";
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override FunctionValues GetValues(IDictionary context, AtomicReaderContext
			 readerContext)
		{
			FunctionValues aVals = a.GetValues(context, readerContext);
			FunctionValues bVals = b.GetValues(context, readerContext);
			return new _FloatDocValues_58(this, aVals, bVals, this);
		}

		private sealed class _FloatDocValues_58 : FloatDocValues
		{
			public _FloatDocValues_58(DualFloatFunction _enclosing, FunctionValues aVals, FunctionValues
				 bVals, ValueSource baseArg1) : base(baseArg1)
			{
				this._enclosing = _enclosing;
				this.aVals = aVals;
				this.bVals = bVals;
			}

			public override float FloatVal(int doc)
			{
				return this._enclosing.Func(doc, aVals, bVals);
			}

			public override string ToString(int doc)
			{
				return this._enclosing.Name() + '(' + aVals.ToString(doc) + ',' + bVals.ToString(
					doc) + ')';
			}

			private readonly DualFloatFunction _enclosing;

			private readonly FunctionValues aVals;

			private readonly FunctionValues bVals;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void CreateWeight(IDictionary context, IndexSearcher searcher)
		{
			a.CreateWeight(context, searcher);
			b.CreateWeight(context, searcher);
		}

		public override int GetHashCode()
		{
			int h = a.GetHashCode();
			h ^= (h << 13) | ((int)(((uint)h) >> 20));
			h += b.GetHashCode();
			h ^= (h << 23) | ((int)(((uint)h) >> 10));
			h += Name().GetHashCode();
			return h;
		}

		public override bool Equals(object o)
		{
			if (this.GetType() != o.GetType())
			{
				return false;
			}
			Org.Apache.Lucene.Queries.Function.Valuesource.DualFloatFunction other = (Org.Apache.Lucene.Queries.Function.Valuesource.DualFloatFunction
				)o;
			return this.a.Equals(other.a) && this.b.Equals(other.b);
		}
	}
}
