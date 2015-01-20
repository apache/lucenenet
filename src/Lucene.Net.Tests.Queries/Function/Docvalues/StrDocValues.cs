/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Org.Apache.Lucene.Queries.Function;
using Org.Apache.Lucene.Util.Mutable;
using Sharpen;

namespace Org.Apache.Lucene.Queries.Function.Docvalues
{
	/// <summary>
	/// Abstract
	/// <see cref="Org.Apache.Lucene.Queries.Function.FunctionValues">Org.Apache.Lucene.Queries.Function.FunctionValues
	/// 	</see>
	/// implementation which supports retrieving String values.
	/// Implementations can control how the String values are loaded through
	/// <see cref="StrVal(int)">StrVal(int)</see>
	/// }
	/// </summary>
	public abstract class StrDocValues : FunctionValues
	{
		protected internal readonly ValueSource vs;

		public StrDocValues(ValueSource vs)
		{
			this.vs = vs;
		}

		public abstract override string StrVal(int doc);

		public override object ObjectVal(int doc)
		{
			return Exists(doc) ? StrVal(doc) : null;
		}

		public override bool BoolVal(int doc)
		{
			return Exists(doc);
		}

		public override string ToString(int doc)
		{
			return vs.Description() + "='" + StrVal(doc) + "'";
		}

		public override FunctionValues.ValueFiller GetValueFiller()
		{
			return new _ValueFiller_56(this);
		}

		private sealed class _ValueFiller_56 : FunctionValues.ValueFiller
		{
			public _ValueFiller_56(StrDocValues _enclosing)
			{
				this._enclosing = _enclosing;
				this.mval = new MutableValueStr();
			}

			private readonly MutableValueStr mval;

			public override MutableValue GetValue()
			{
				return this.mval;
			}

			public override void FillValue(int doc)
			{
				this.mval.exists = this._enclosing.BytesVal(doc, this.mval.value);
			}

			private readonly StrDocValues _enclosing;
		}
	}
}
