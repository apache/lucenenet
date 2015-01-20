/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Collections;
using Org.Apache.Lucene.Index;
using Org.Apache.Lucene.Queries.Function;
using Org.Apache.Lucene.Queries.Function.Docvalues;
using Org.Apache.Lucene.Queries.Function.Valuesource;
using Sharpen;

namespace Org.Apache.Lucene.Queries.Function.Valuesource
{
	/// <summary>A simple float function with a single argument</summary>
	public abstract class SimpleFloatFunction : SingleFunction
	{
		public SimpleFloatFunction(ValueSource source) : base(source)
		{
		}

		protected internal abstract float Func(int doc, FunctionValues vals);

		/// <exception cref="System.IO.IOException"></exception>
		public override FunctionValues GetValues(IDictionary context, AtomicReaderContext
			 readerContext)
		{
			FunctionValues vals = source.GetValues(context, readerContext);
			return new _FloatDocValues_40(this, vals, this);
		}

		private sealed class _FloatDocValues_40 : FloatDocValues
		{
			public _FloatDocValues_40(SimpleFloatFunction _enclosing, FunctionValues vals, ValueSource
				 baseArg1) : base(baseArg1)
			{
				this._enclosing = _enclosing;
				this.vals = vals;
			}

			public override float FloatVal(int doc)
			{
				return this._enclosing.Func(doc, vals);
			}

			public override string ToString(int doc)
			{
				return this._enclosing.Name() + '(' + vals.ToString(doc) + ')';
			}

			private readonly SimpleFloatFunction _enclosing;

			private readonly FunctionValues vals;
		}
	}
}
