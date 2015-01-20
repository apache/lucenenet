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
using Org.Apache.Lucene.Search;
using Sharpen;

namespace Org.Apache.Lucene.Queries.Function.Valuesource
{
	/// <summary>
	/// <see cref="BoolFunction">BoolFunction</see>
	/// implementation which applies an extendible boolean
	/// function to the values of a single wrapped
	/// <see cref="Org.Apache.Lucene.Queries.Function.ValueSource">Org.Apache.Lucene.Queries.Function.ValueSource
	/// 	</see>
	/// .
	/// Functions this can be used for include whether a field has a value or not,
	/// or inverting the boolean value of the wrapped ValueSource.
	/// </summary>
	public abstract class SimpleBoolFunction : BoolFunction
	{
		protected internal readonly ValueSource source;

		public SimpleBoolFunction(ValueSource source)
		{
			this.source = source;
		}

		protected internal abstract string Name();

		protected internal abstract bool Func(int doc, FunctionValues vals);

		/// <exception cref="System.IO.IOException"></exception>
		public override FunctionValues GetValues(IDictionary context, AtomicReaderContext
			 readerContext)
		{
			FunctionValues vals = source.GetValues(context, readerContext);
			return new _BoolDocValues_50(this, vals, this);
		}

		private sealed class _BoolDocValues_50 : BoolDocValues
		{
			public _BoolDocValues_50(SimpleBoolFunction _enclosing, FunctionValues vals, ValueSource
				 baseArg1) : base(baseArg1)
			{
				this._enclosing = _enclosing;
				this.vals = vals;
			}

			public override bool BoolVal(int doc)
			{
				return this._enclosing.Func(doc, vals);
			}

			public override string ToString(int doc)
			{
				return this._enclosing.Name() + '(' + vals.ToString(doc) + ')';
			}

			private readonly SimpleBoolFunction _enclosing;

			private readonly FunctionValues vals;
		}

		public override string Description()
		{
			return Name() + '(' + source.Description() + ')';
		}

		public override int GetHashCode()
		{
			return source.GetHashCode() + Name().GetHashCode();
		}

		public override bool Equals(object o)
		{
			if (this.GetType() != o.GetType())
			{
				return false;
			}
			Org.Apache.Lucene.Queries.Function.Valuesource.SimpleBoolFunction other = (Org.Apache.Lucene.Queries.Function.Valuesource.SimpleBoolFunction
				)o;
			return this.source.Equals(other.source);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void CreateWeight(IDictionary context, IndexSearcher searcher)
		{
			source.CreateWeight(context, searcher);
		}
	}
}
